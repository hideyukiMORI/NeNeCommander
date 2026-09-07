using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Wsl;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Paths;
using NeNeCommander.Infrastructure.Windows.Wsl;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class LiveWslTestRoot
{
    private const string RunChildName = "NeNeCommander-Live-Run";
    private const string OwnershipMarkerName = ".nene-commander-owner";
    private static readonly byte[] OwnershipMarkerBytes = [0x4E, 0x65, 0x4E, 0x65];

    private readonly List<LiveWslRootEntry> _ancestorChain;
    private readonly Dictionary<string, LiveWslRootEntry> _ownedEntries;
    private readonly Func<WslPath, string> _resolvePath;
    private readonly WslPath _configuredRoot;

    private LiveWslTestRoot(
        WslPath configuredRoot,
        WslPath runRoot,
        Func<WslPath, string> resolvePath,
        List<LiveWslRootEntry> ancestorChain,
        Dictionary<string, LiveWslRootEntry> ownedEntries)
    {
        _configuredRoot = configuredRoot;
        RunRoot = runRoot;
        _resolvePath = resolvePath;
        _ancestorChain = ancestorChain;
        _ownedEntries = ownedEntries;
    }

    internal WslPath RunRoot { get; }

    internal static async Task<LiveWslRootOpenOutcome> OpenAsync(
        LiveWslRootAdmission admission,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(admission);
        if (admission.ConfiguredRoot is null)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.Unexecuted);
        }
        if (!admission.IsComplete)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.InvalidConfiguration);
        }

        WslDistributionCatalogOutcome discovery = await new WslDistributionCatalog().DiscoverAsync(cancellationToken);
        return discovery is WslDistributionCatalogSucceeded succeeded
            ? Open(admission, succeeded.Roots, static path => path.CanonicalText)
            : new LiveWslRootOpenRejected(LiveWslRootFailureKind.DistributionUnavailable);
    }

    internal static LiveWslRootOpenOutcome Open(
        LiveWslRootAdmission admission,
        IReadOnlyList<WslPath> registeredRoots,
        Func<WslPath, string> resolvePath)
    {
        ArgumentNullException.ThrowIfNull(admission);
        ArgumentNullException.ThrowIfNull(registeredRoots);
        ArgumentNullException.ThrowIfNull(resolvePath);
        if (admission.ConfiguredRoot is not string configuredRoot)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.Unexecuted);
        }
        if (!admission.IsComplete)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.InvalidConfiguration);
        }
        if (FileSystemPath.Parse(configuredRoot) is not PathParseSuccess { Path: WslPath root })
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.InvalidConfiguration);
        }
        if (!LiveWslRootPath.IsAllowedRootShape(root))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.UnsafeRoot);
        }
        if (!registeredRoots.Any(candidate => candidate.DistributionName.Equals(
                root.DistributionName,
                StringComparison.OrdinalIgnoreCase)))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.DistributionUnavailable);
        }

        try
        {
            return OpenVerified(root, resolvePath, admission);
        }
        catch (UnauthorizedAccessException)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootUnavailable);
        }
        catch (IOException)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootUnavailable);
        }
    }

    internal WslPath CreateDirectory(string relativePath)
    {
        WslPath current = RunRoot;
        foreach (string segment in LiveWslRootPath.Segments(relativePath))
        {
            current = LiveWslRootPath.Child(current, segment);
            string resolved = _resolvePath(current);
            if (Directory.Exists(resolved))
            {
                if (!_ownedEntries.ContainsKey(resolved) || VerifyForEffect() is LiveWslRootCheckRejected)
                {
                    throw new InvalidOperationException("The existing live fixture directory is not owned.");
                }
            }
            else
            {
                RequireVerifiedSetup();
                _ = Directory.CreateDirectory(resolved);
                Register(current, LiveWslRootEntryKind.Regular);
            }
        }
        return current;
    }

    internal WslPath WriteFile(string relativePath, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        WslPath path = ResolveForCreation(relativePath);
        string resolved = _resolvePath(path);
        RequireVerifiedSetup();
        using (FileStream stream = new(resolved, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(content);
            stream.Flush(true);
        }
        Register(path, LiveWslRootEntryKind.Regular);
        return path;
    }

    internal WslPath CreateFileSymbolicLink(string relativePath, string targetRelativePath)
    {
        WslPath path = ResolveForCreation(relativePath);
        WslPath target = ResolveExisting(targetRelativePath);
        if (!_ownedEntries.ContainsKey(_resolvePath(target)))
        {
            throw new InvalidOperationException("The live link target is not owned by this run.");
        }
        RequireVerifiedSetup();
        _ = File.CreateSymbolicLink(_resolvePath(path), _resolvePath(target));
        Register(path, LiveWslRootEntryKind.Link);
        return path;
    }

    internal byte[] ReadFile(string relativePath)
    {
        WslPath path = ResolveExisting(relativePath);
        RequireOwnedRead(path);
        return File.ReadAllBytes(_resolvePath(path));
    }

    internal IReadOnlyList<LiveWslDeclaredEntry> ReadDeclaredTree(string relativePath)
    {
        WslPath path = ResolveExisting(relativePath);
        RequireVerifiedSetup();
        IReadOnlyList<LiveWslRootEntry> entries = LiveWslRootFileSystem.EnumerateTree(path, _resolvePath);
        if (entries.Any(entry => !_ownedEntries.ContainsKey(entry.ResolvedPath)))
        {
            throw new InvalidOperationException("The live declared tree contains an unowned entry.");
        }
        string prefix = path.LinuxPath + "/";
        return
        [
            .. entries.Select(entry => LiveWslDeclaredEntry.Create(
                entry.Path.LinuxPath.Equals(path.LinuxPath, StringComparison.Ordinal)
                    ? string.Empty
                    : entry.Path.LinuxPath[prefix.Length..],
                Directory.Exists(entry.ResolvedPath) ? DirectoryEntryKind.Directory : DirectoryEntryKind.File,
                File.Exists(entry.ResolvedPath) ? new FileInfo(entry.ResolvedPath).Length : 0))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
        ];
    }

    internal bool Exists(string relativePath)
    {
        WslPath path = ResolveExisting(relativePath);
        string resolved = _resolvePath(path);
        RequireVerifiedSetup();
        bool exists = File.Exists(resolved) || Directory.Exists(resolved);
        return exists && !_ownedEntries.ContainsKey(resolved)
            ? throw new InvalidOperationException("The live evidence path is not owned by this run.")
            : exists;
    }

    internal void AdoptCopiedTree(
        string relativePath,
        IReadOnlyList<string> expectedEntries,
        FileOperationOutcome outcome,
        WslPath source)
    {
        RequireTransferEffects(
            outcome,
            source,
            [FileOperationEffectKind.Copied, FileOperationEffectKind.Verified]);
        AdoptExpectedTree(relativePath, expectedEntries);
    }

    internal void AdoptMovedTree(
        string sourceRelativePath,
        string targetRelativePath,
        IReadOnlyList<string> expectedEntries,
        FileOperationOutcome outcome,
        WslPath source)
    {
        RequireTransferEffects(
            outcome,
            source,
            [FileOperationEffectKind.Copied, FileOperationEffectKind.Verified, FileOperationEffectKind.SourceDeleted]);
        WslPath path = ResolveExisting(sourceRelativePath);
        string resolved = _resolvePath(path);
        if (File.Exists(resolved) || Directory.Exists(resolved))
        {
            throw new InvalidOperationException("The expected live source still exists.");
        }
        string boundary = resolved + "\\";
        string[] removed =
        [
            .. _ownedEntries.Keys
            .Where(candidate => candidate.Equals(resolved, StringComparison.Ordinal) ||
                candidate.StartsWith(boundary, StringComparison.Ordinal))
        ];
        foreach (string candidate in removed)
        {
            _ = _ownedEntries.Remove(candidate);
        }
        AdoptExpectedTree(targetRelativePath, expectedEntries);
    }

    internal LiveWslRootCheckOutcome VerifyForEffect()
    {
        LiveWslRootCheckOutcome chain = LiveWslRootFileSystem.VerifyEntries(_ancestorChain);
        if (chain is LiveWslRootCheckRejected)
        {
            return chain;
        }
        LiveWslRootCheckOutcome owned = LiveWslRootFileSystem.VerifyEntries(_ownedEntries.Values);
        if (owned is LiveWslRootCheckRejected)
        {
            return owned;
        }
        WslPath marker = LiveWslRootPath.Child(RunRoot, OwnershipMarkerName);
        if (!File.ReadAllBytes(_resolvePath(marker)).SequenceEqual(OwnershipMarkerBytes))
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        IReadOnlyList<LiveWslRootEntry> observed = LiveWslRootFileSystem.EnumerateTree(RunRoot, _resolvePath);
        return observed.Count == _ownedEntries.Count &&
            observed.All(entry => _ownedEntries.ContainsKey(entry.ResolvedPath)) &&
            RootContainsOnlyRunChild()
            ? LiveWslRootCheckOutcome.Accepted
            : new LiveWslRootCheckRejected(LiveWslRootFailureKind.ForeignResidue);
    }

    internal LiveWslRootCleanupOutcome Cleanup()
    {
        try
        {
            LiveWslRootCheckOutcome verified = VerifyForEffect();
            if (verified is LiveWslRootCheckRejected rejected)
            {
                return new LiveWslRootCleanupRejected(rejected.Failure);
            }
            foreach (LiveWslRootEntry link in _ownedEntries.Values
                .Where(entry => entry.Kind == LiveWslRootEntryKind.Link)
                .ToArray())
            {
                File.Delete(link.ResolvedPath);
                _ = _ownedEntries.Remove(link.ResolvedPath);
            }
            LiveWslRootCheckOutcome afterUnlink = VerifyForEffect();
            if (afterUnlink is LiveWslRootCheckRejected rejectedAfterUnlink)
            {
                return new LiveWslRootCleanupRejected(rejectedAfterUnlink.Failure);
            }
            Directory.Delete(_resolvePath(RunRoot), recursive: true);
            return !Directory.EnumerateFileSystemEntries(_resolvePath(_configuredRoot)).Any() &&
                LiveWslRootFileSystem.VerifyEntries(_ancestorChain) is LiveWslRootCheckAccepted
                ? LiveWslRootCleanupOutcome.Completed
                : new LiveWslRootCleanupRejected(LiveWslRootFailureKind.CleanupFailed);
        }
        catch (UnauthorizedAccessException)
        {
            return new LiveWslRootCleanupRejected(LiveWslRootFailureKind.CleanupFailed);
        }
        catch (IOException)
        {
            return new LiveWslRootCleanupRejected(LiveWslRootFailureKind.CleanupFailed);
        }
    }

    private static LiveWslRootOpenOutcome OpenVerified(
        WslPath root,
        Func<WslPath, string> resolvePath,
        LiveWslRootAdmission admission)
    {
        WslPath temporaryRoot = (WslPath)(root.Parent ?? throw new InvalidOperationException("Missing parent."));
        WslPath distributionRoot = (WslPath)(temporaryRoot.Parent ?? throw new InvalidOperationException("Missing root."));
        List<LiveWslRootEntry> chain =
        [
            LiveWslRootFileSystem.CaptureDirectory(distributionRoot, resolvePath),
            LiveWslRootFileSystem.CaptureDirectory(temporaryRoot, resolvePath),
            LiveWslRootFileSystem.CaptureDirectory(root, resolvePath),
        ];
        if (chain.Any(entry => entry.Kind == LiveWslRootEntryKind.Link))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.LinkDetected);
        }
        if (!chain[1].Identity.Equals(admission.TemporaryRootIdentity, StringComparison.Ordinal) ||
            !chain[2].Identity.Equals(admission.ConfiguredRootIdentity, StringComparison.Ordinal))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        if (Directory.EnumerateFileSystemEntries(resolvePath(root)).Any())
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootNotEmpty);
        }
        if (LiveWslRootFileSystem.VerifyEntries(chain) is LiveWslRootCheckRejected rejected)
        {
            return new LiveWslRootOpenRejected(rejected.Failure);
        }

        WslPath runRoot = LiveWslRootPath.Child(root, RunChildName);
        string runResolved = resolvePath(runRoot);
        if (Directory.Exists(runResolved) || File.Exists(runResolved))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootNotEmpty);
        }
        _ = Directory.CreateDirectory(runResolved);
        LiveWslRootEntry runEntry = LiveWslRootFileSystem.CaptureDirectory(runRoot, resolvePath);
        string[] rootEntries = [.. Directory.EnumerateFileSystemEntries(resolvePath(root))];
        if (LiveWslRootFileSystem.VerifyEntries(chain) is LiveWslRootCheckRejected ||
            rootEntries.Length != 1 ||
            !rootEntries[0].Equals(runResolved, StringComparison.Ordinal))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        WslPath marker = LiveWslRootPath.Child(runRoot, OwnershipMarkerName);
        LiveWslRootEntry markerEntry;
        using (FileStream stream = new(resolvePath(marker), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(OwnershipMarkerBytes);
            stream.Flush(true);
            markerEntry = LiveWslRootEntry.Create(
                marker,
                resolvePath(marker),
                WindowsFileIdentifier.DescribeHandle(stream.SafeFileHandle),
                LiveWslRootEntryKind.Regular);
        }
        Dictionary<string, LiveWslRootEntry> owned = new(StringComparer.Ordinal)
        {
            [runResolved] = runEntry,
            [resolvePath(marker)] = markerEntry,
        };
        return new LiveWslRootOpened(new LiveWslTestRoot(root, runRoot, resolvePath, chain, owned));
    }

    private void Register(WslPath path, LiveWslRootEntryKind kind)
    {
        if (ProviderPathContainment.Evaluate(RunRoot, path) is not ContainedPath)
        {
            throw new InvalidOperationException("The live fixture escaped its run root.");
        }
        LiveWslRootEntry entry = LiveWslRootFileSystem.CaptureEntry(path, _resolvePath, kind);
        _ownedEntries.Add(entry.ResolvedPath, entry);
    }

    private void AdoptExpectedTree(string relativePath, IReadOnlyList<string> expectedEntries)
    {
        ArgumentNullException.ThrowIfNull(expectedEntries);
        LiveWslRootCheckOutcome chain = LiveWslRootFileSystem.VerifyEntries(_ancestorChain);
        LiveWslRootCheckOutcome owned = LiveWslRootFileSystem.VerifyEntries(_ownedEntries.Values);
        if (chain is LiveWslRootCheckRejected || owned is LiveWslRootCheckRejected)
        {
            throw new InvalidOperationException("The live root changed before target adoption.");
        }
        WslPath path = ResolveExisting(relativePath);
        IReadOnlyList<LiveWslRootEntry> produced = LiveWslRootFileSystem.EnumerateTree(path, _resolvePath);
        string prefix = path.LinuxPath + "/";
        string[] actual =
        [
            .. produced.Select(entry => entry.Path.LinuxPath.Equals(path.LinuxPath, StringComparison.Ordinal)
                ? string.Empty
                : entry.Path.LinuxPath[prefix.Length..])
            .Order(StringComparer.Ordinal)
        ];
        string[] expected = [.. expectedEntries.Order(StringComparer.Ordinal)];
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal) ||
            produced.Any(entry => entry.Kind == LiveWslRootEntryKind.Link))
        {
            throw new InvalidOperationException("The produced live tree differs from the expected fixture tree.");
        }
        foreach (LiveWslRootEntry entry in produced)
        {
            _ownedEntries.Add(entry.ResolvedPath, entry);
        }
    }

    private static void RequireTransferEffects(
        FileOperationOutcome outcome,
        WslPath source,
        IReadOnlyList<FileOperationEffectKind> expected)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(source);
        bool matches = outcome.Completion == FileOperationCompletionKind.Succeeded &&
            outcome.Effects.Count == expected.Count;
        for (int index = 0; matches && index < expected.Count; index++)
        {
            matches = FileSystemPathIdentityComparer.Instance.Equals(outcome.Effects[index].Source, source) &&
                outcome.Effects[index].Kind == expected[index];
        }
        if (!matches)
        {
            throw new InvalidOperationException("The gateway outcome does not own the produced live tree.");
        }
    }

    private void RequireVerifiedSetup()
    {
        if (VerifyForEffect() is LiveWslRootCheckRejected)
        {
            throw new InvalidOperationException("The live root changed before fixture setup.");
        }
    }

    private void RequireOwnedRead(WslPath path)
    {
        RequireVerifiedSetup();
        string resolved = _resolvePath(path);
        if (!_ownedEntries.TryGetValue(resolved, out LiveWslRootEntry? entry) ||
            entry.Kind != LiveWslRootEntryKind.Regular ||
            LiveWslRootFileSystem.VerifyEntries([entry]) is LiveWslRootCheckRejected)
        {
            throw new InvalidOperationException("The live evidence path is not an owned regular entry.");
        }
    }

    private bool RootContainsOnlyRunChild()
    {
        string[] entries = [.. Directory.EnumerateFileSystemEntries(_resolvePath(_configuredRoot))];
        return entries.Length == 1 && entries[0].Equals(_resolvePath(RunRoot), StringComparison.Ordinal);
    }

    private WslPath ResolveForCreation(string relativePath)
    {
        string[] segments = LiveWslRootPath.Segments(relativePath);
        WslPath current = RunRoot;
        for (int index = 0; index < segments.Length - 1; index++)
        {
            current = LiveWslRootPath.Child(current, segments[index]);
            if (!Directory.Exists(_resolvePath(current)))
            {
                RequireVerifiedSetup();
                _ = Directory.CreateDirectory(_resolvePath(current));
                Register(current, LiveWslRootEntryKind.Regular);
            }
            else if (!_ownedEntries.ContainsKey(_resolvePath(current)))
            {
                throw new InvalidOperationException("The existing live fixture parent is not owned.");
            }
        }
        return LiveWslRootPath.Child(current, segments[^1]);
    }

    private WslPath ResolveExisting(string relativePath)
    {
        WslPath current = RunRoot;
        foreach (string segment in LiveWslRootPath.Segments(relativePath))
        {
            current = LiveWslRootPath.Child(current, segment);
        }
        return current;
    }
}
