using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Paths;

/// <summary>
/// Produces the one Win32 path string through which Infrastructure addresses a WSL entry
/// (ADR-0060). The ordinary <c>\\wsl.localhost\...</c> text passes through Win32 normalization,
/// which silently drops a trailing <c>.</c> or space from a segment; the extended-length form
/// reaches the 9P redirector unchanged. The canonical text stays the user-facing form, and the
/// string produced here never leaves Infrastructure.
/// </summary>
internal static class WslWin32PathText
{
    private const string OrdinaryUncPrefix = "\\\\";
    private const string ExtendedUncPrefix = "\\\\?\\UNC\\";

    /// <summary>Maps one validated WSL path to its extended-length UNC text without changing a segment.</summary>
    /// <param name="path">Validated WSL path.</param>
    /// <returns>The <c>\\?\UNC\wsl.localhost\&lt;distribution&gt;\&lt;segments&gt;</c> text.</returns>
    internal static string Resolve(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return string.Concat(ExtendedUncPrefix, path.CanonicalText.AsSpan(OrdinaryUncPrefix.Length));
    }
}
