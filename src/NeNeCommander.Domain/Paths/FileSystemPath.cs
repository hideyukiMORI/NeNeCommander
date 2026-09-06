using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NeNeCommander.Domain.Paths;

/// <summary>
/// Provides the sole parser and canonical representation for supported filesystem paths.
/// </summary>
public abstract record FileSystemPath
{
    private const int MaximumPathLength = 32767;
    private static readonly SearchValues<char> WindowsInvalidCharacters =
        SearchValues.Create(['<', '>', ':', '"', '/', '\\', '|', '?', '*']);

    /// <summary>
    /// Initializes a validated path variant with canonical display and persistence text.
    /// </summary>
    /// <param name="canonicalText">Canonical provider-qualified text.</param>
    private protected FileSystemPath(string canonicalText)
    {
        CanonicalText = canonicalText;
    }

    /// <summary>Gets the canonical text used for display and persistence.</summary>
    public string CanonicalText { get; }

    /// <summary>
    /// Gets the containing location derived without re-parsing, or absence at the provider root.
    /// </summary>
    public abstract FileSystemPath? Parent { get; }

    /// <summary>
    /// Derives the direct child named by untrusted text under the same segment rules as parsing,
    /// without touching the filesystem. Separators and dot segments are rejected so the result can
    /// never be the location itself or anything outside it.
    /// </summary>
    /// <param name="name">Untrusted single-segment name.</param>
    /// <returns>The child path or a closed rejection reason.</returns>
    public PathParseOutcome Child(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return new PathParseFailure(PathParseFailureKind.Empty);
        }
        if (name.Contains('\\') ||
            name.Contains('/') ||
            name.Equals(".", StringComparison.Ordinal) ||
            name.Equals("..", StringComparison.Ordinal))
        {
            return new PathParseFailure(PathParseFailureKind.InvalidSegment);
        }
        string separator = CanonicalText.EndsWith('\\') ? string.Empty : "\\";
        return Parse(CanonicalText + separator + name);
    }

    internal abstract bool HasSameIdentity(FileSystemPath other);

    internal abstract int GetIdentityHashCode();

    /// <summary>
    /// Parses untrusted absolute Windows, UNC, or WSL namespace text exactly once.
    /// </summary>
    /// <param name="input">Untrusted path text.</param>
    /// <returns>A validated path or a closed rejection reason.</returns>
    public static PathParseOutcome Parse(string? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input))
        {
            return new PathParseFailure(PathParseFailureKind.Empty);
        }

        if (input.Length > MaximumPathLength)
        {
            return new PathParseFailure(PathParseFailureKind.TooLong);
        }

        if (ContainsControlCharacter(input))
        {
            return new PathParseFailure(PathParseFailureKind.InvalidSegment);
        }

        string normalizedSeparators = input.Replace('/', '\\');
        PathParseOutcome outcome = IsDeviceNamespace(normalizedSeparators)
            ? new PathParseFailure(PathParseFailureKind.DeviceNamespace)
            : normalizedSeparators.StartsWith("\\\\", StringComparison.Ordinal)
                ? ParseUnc(normalizedSeparators)
                : ParseLocal(normalizedSeparators);
        return outcome is PathParseSuccess success && success.Path.CanonicalText.Length > MaximumPathLength
            ? new PathParseFailure(PathParseFailureKind.TooLong)
            : outcome;
    }

    private static PathParseOutcome ParseLocal(string input)
    {
        if (input.Length < 3 || !char.IsAsciiLetter(input[0]) || input[1] != ':' || input[2] != '\\')
        {
            return new PathParseFailure(PathParseFailureKind.Relative);
        }

        string drive = char.ToUpperInvariant(input[0]).ToString(CultureInfo.InvariantCulture) + ":";
        string remainder = input[3..];
        SegmentNormalization normalization = NormalizeSegments(remainder, SegmentRules.Windows);
        if (normalization.Failure is not null)
        {
            return new PathParseFailure(normalization.Failure);
        }

        string canonicalText = BuildBackslashPath(drive + "\\", normalization.Segments);
        return new PathParseSuccess(new WindowsLocalPath(canonicalText, drive));
    }

    private static PathParseOutcome ParseUnc(string input)
    {
        string remainder = input[2..];
        string[] components = remainder.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (components.Length < 2)
        {
            return new PathParseFailure(PathParseFailureKind.InvalidRoot);
        }

        string server = components[0];
        string rootName = components[1];
        return !IsValidWindowsRootComponent(server) || !IsValidWindowsRootComponent(rootName)
            ? new PathParseFailure(PathParseFailureKind.InvalidRoot)
            : server.Equals("wsl.localhost", StringComparison.OrdinalIgnoreCase) ||
                server.Equals("wsl$", StringComparison.OrdinalIgnoreCase)
                ? ParseWsl(rootName, components)
                : ParseWindowsUnc(server, rootName, components);
    }

    private static PathParseOutcome ParseWsl(string distributionName, IReadOnlyList<string> components)
    {
        if (!IsValidDistributionName(distributionName))
        {
            return new PathParseFailure(PathParseFailureKind.InvalidDistribution);
        }

        string remainder = JoinComponents(components, 2);
        SegmentNormalization normalization = NormalizeSegments(remainder, SegmentRules.Wsl);
        if (normalization.Failure is not null)
        {
            return new PathParseFailure(normalization.Failure);
        }

        string root = "\\\\wsl.localhost\\" + distributionName + "\\";
        string canonicalText = BuildBackslashPath(root, normalization.Segments);
        string linuxPath = "/" + string.Join('/', normalization.Segments);
        return new PathParseSuccess(new WslPath(canonicalText, distributionName, linuxPath));
    }

    private static PathParseOutcome ParseWindowsUnc(
        string server,
        string share,
        IReadOnlyList<string> components)
    {
        string remainder = JoinComponents(components, 2);
        SegmentNormalization normalization = NormalizeSegments(remainder, SegmentRules.Windows);
        if (normalization.Failure is not null)
        {
            return new PathParseFailure(normalization.Failure);
        }

        string root = "\\\\" + server + "\\" + share + "\\";
        string canonicalText = BuildBackslashPath(root, normalization.Segments);
        return new PathParseSuccess(new WindowsUncPath(canonicalText, server, share));
    }

    private static SegmentNormalization NormalizeSegments(string input, SegmentRules rules)
    {
        List<string> normalized = [];
        string[] segments = input.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        foreach (string segment in segments)
        {
            if (segment.Equals(".", StringComparison.Ordinal))
            {
                continue;
            }

            if (segment.Equals("..", StringComparison.Ordinal))
            {
                if (normalized.Count == 0)
                {
                    return SegmentNormalization.Rejected(PathParseFailureKind.ParentTraversal);
                }
                normalized.RemoveAt(normalized.Count - 1);
                continue;
            }

            if (!IsValidSegment(segment, rules))
            {
                return SegmentNormalization.Rejected(PathParseFailureKind.InvalidSegment);
            }
            normalized.Add(segment);
        }

        return SegmentNormalization.Accepted(normalized);
    }

    private static bool IsValidSegment(string segment, SegmentRules rules)
    {
        if (rules == SegmentRules.Wsl)
        {
            return true;
        }

        if (segment.EndsWith(' ') || segment.EndsWith('.') || segment.AsSpan().ContainsAny(WindowsInvalidCharacters))
        {
            return false;
        }

        string stem = segment.Split('.', 2)[0];
        return !IsReservedWindowsName(stem);
    }

    private static bool IsValidWindowsRootComponent(string component)
    {
        return !component.Equals(".", StringComparison.Ordinal) &&
            !component.Equals("..", StringComparison.Ordinal) &&
            !component.AsSpan().ContainsAny(WindowsInvalidCharacters) &&
            !component.EndsWith(' ') &&
            !component.EndsWith('.');
    }

    private static bool IsValidDistributionName(string distributionName)
    {
        return distributionName.Length > 0 &&
            char.IsAsciiLetterOrDigit(distributionName[0]) &&
            distributionName.All(IsDistributionNameCharacter);
    }

    private static bool IsDistributionNameCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-';
    }

    private static bool IsReservedWindowsName(string stem)
    {
        string candidate = stem.ToUpperInvariant();
        return candidate is "CON" or "PRN" or "AUX" or "NUL" ||
            (candidate.Length == 4 &&
                (candidate.StartsWith("COM", StringComparison.Ordinal) ||
                    candidate.StartsWith("LPT", StringComparison.Ordinal)) &&
                candidate[3] is >= '1' and <= '9');
    }

    private static bool ContainsControlCharacter(string input)
    {
        return input.Any(char.IsControl);
    }

    private static bool IsDeviceNamespace(string input)
    {
        return input.StartsWith("\\\\?\\", StringComparison.Ordinal) ||
            input.StartsWith("\\\\.\\", StringComparison.Ordinal);
    }

    /// <summary>
    /// Removes the last segment of canonical text whose root occupies <paramref name="rootLength"/>
    /// characters including its trailing separator. Callers decide root membership first.
    /// </summary>
    private protected static string RemoveLastSegment(string canonicalText, int rootLength)
    {
        int lastSeparator = canonicalText.LastIndexOf('\\');
        return lastSeparator < rootLength ? canonicalText[..rootLength] : canonicalText[..lastSeparator];
    }

    private static string JoinComponents(IReadOnlyList<string> components, int startIndex)
    {
        return string.Join('\\', components.Skip(startIndex));
    }

    private static string BuildBackslashPath(string root, IReadOnlyList<string> segments)
    {
        return root + string.Join('\\', segments);
    }

    private sealed class SegmentNormalization
    {
        private SegmentNormalization(IReadOnlyList<string> segments, PathParseFailureKind? failure)
        {
            Segments = segments;
            Failure = failure;
        }

        internal IReadOnlyList<string> Segments { get; }

        internal PathParseFailureKind? Failure { get; }

        internal static SegmentNormalization Accepted(IReadOnlyList<string> segments)
        {
            return new SegmentNormalization(Array.AsReadOnly([.. segments]), null);
        }

        internal static SegmentNormalization Rejected(PathParseFailureKind failure)
        {
            return new SegmentNormalization(Array.Empty<string>(), failure);
        }
    }

    private sealed class SegmentRules
    {
        internal static SegmentRules Windows { get; } = new SegmentRules();

        internal static SegmentRules Wsl { get; } = new SegmentRules();

        private SegmentRules()
        {
        }
    }
}
