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

    private readonly IReadOnlyList<LiveWslRootEntry> _outerAncestors;
    private readonly Dictionary<string, LiveWslRootEntry> _ownedEntries;
    private readonly WindowsWslFileSystem _fileSystem;
    private readonly Func<WslPath, string> _resolvePath;
    private readonly Action<WslPath, WslPath> _createLink;
    private readonly WslPath _configuredRoot;
    private LiveWslRootEntry _configuredRootEntry;

    private LiveWslTestRoot(
        WslPath configuredRoot,
        LiveWslRootEntry configuredRootEntry,
        WslPath runRoot,
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath,
        Action<WslPath, WslPath> createLink,
        IReadOnlyList<LiveWslRootEntry> outerAncestors,
        Dictionary<string, LiveWslRootEntry> ownedEntries)
    {
        _configuredRoot = configuredRoot;
        _configuredRootEntry = configuredRootEntry;
        RunRoot = runRoot;
        _fileSystem = fileSystem;
        _resolvePath = resolvePath;
        _createLink = createLink;
        _outerAncestors = outerAncestors;
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

        // The live run uses the production WSL file system exactly as the composition root does:
        // the canonical namespace mapping and the ADR-0049 9P-guarded handle-facts reader.
        return discovery is WslDistributionCatalogSucceeded succeeded
            ? Open(
                admission,
                succeeded.Roots,
                new WindowsWslFileSystem(),
                static path => path.CanonicalText,
                LiveWslLinkFixture.Create)
            : new LiveWslRootOpenRejected(LiveWslRootFailureKind.DistributionUnavailable);
    }

    /// <summary>
    /// Opens the configured root through the production WSL file system. The caller supplies the
    /// same namespace mapping that <paramref name="fileSystem"/> was composed with, so identity and
    /// setup always describe the same entry.
    /// </summary>
    /// <param name="admission">Launcher-owned admission facts.</param>
    /// <param name="registeredRoots">Distribution roots reported by the canonical catalog.</param>
    /// <param name="fileSystem">Production WSL file system that owns every identity.</param>
    /// <param name="resolvePath">Namespace mapping used for setup, evidence, and cleanup.</param>
    /// <param name="createLink">Creates one link fixture from its target and link path.</param>
    /// <returns>The opened owner or a closed rejection.</returns>
    internal static LiveWslRootOpenOutcome Open(
        LiveWslRootAdmission admission,
        IReadOnlyList<WslPath> registeredRoots,
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath,
        Action<WslPath, WslPath> createLink)
    {
        ArgumentNullException.ThrowIfNull(admission);
        ArgumentNullException.ThrowIfNull(registeredRoots);
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(resolvePath);
        ArgumentNullException.ThrowIfNull(createLink);
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
            return OpenVerified(root, fileSystem, resolvePath, createLink);
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
                Register(current);
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
        Register(path);
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
        if (ProviderPathContainment.Evaluate(RunRoot, path) is not ContainedPath ||
            ProviderPathContainment.Evaluate(RunRoot, target) is not ContainedPath)
        {
            throw new InvalidOperationException("The live link fixture escaped its run root.");
        }
        RequireVerifiedSetup();

        // ADR-0043/ADR-0049: an unprivileged Windows process cannot create a link on the WSL
        // share, so the live owner makes it inside the distribution. Cleanup still unlinks the
        // link entry from the Windows side without following it.
        _createLink(target, path);
        Register(path);
        return path;
    }

    internal byte[] ReadFile(string relativePath)
    {
        WslPath path = ResolveExisting(relativePath);
        RequireOwnedRead(path);
        return File.ReadAllBytes(_resolvePath(path));
    }

    /// <summary>
    /// Observes one owned entry's identity again through the production WSL file system, so a live
    /// cell can compare two observations of the same entry across an intervening read.
    /// </summary>
    /// <param name="relativePath">Path of the owned entry relative to the run child.</param>
    /// <returns>The ADR-0049 identity token observed now.</returns>
    internal string ReadIdentity(string relativePath)
    {
        WslPath path = OwnedPath(relativePath);
        return LiveWslRootFileSystem.CaptureEntry(path, _fileSystem, _resolvePath).Identity;
    }

    /// <summary>Resolves one owned entry's validated WSL path after proving the run is intact.</summary>
    /// <param name="relativePath">Path of the owned entry relative to the run child.</param>
    /// <returns>The validated WSL path of the owned entry.</returns>
    internal WslPath OwnedPath(string relativePath)
    {
        WslPath path = ResolveExisting(relativePath);
        RequireVerifiedSetup();
        return _ownedEntries.ContainsKey(_resolvePath(path))
            ? path
            : throw new InvalidOperationException("The live identity path is not owned by this run.");
    }

    internal IReadOnlyList<LiveWslDeclaredEntry> ReadDeclaredTree(string relativePath)
    {
        WslPath path = ResolveExisting(relativePath);
        RequireVerifiedSetup();
        IReadOnlyList<LiveWslRootEntry> entries =
            LiveWslRootFileSystem.EnumerateTree(path, _fileSystem, _resolvePath);
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
                entry.Kind == LiveWslRootEntryKind.Directory
                    ? DirectoryEntryKind.Directory
                    : DirectoryEntryKind.File,
                entry.Kind == LiveWslRootEntryKind.Directory ? 0 : new FileInfo(entry.ResolvedPath).Length))
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

        // ADR-0049: the deleted source's parent changed its direct children, so its own token
        // changed with them and is re-captured immediately after that mutation.
        RecaptureOwnedDirectory(Parent(path));
        AdoptExpectedTree(targetRelativePath, expectedEntries);
    }

    internal LiveWslRootCheckOutcome VerifyForEffect()
    {
        // ADR-0049 makes a directory's token change whenever its direct children change, so the
        // order below reports the most specific closed reason: the create-new ownership marker and
        // the stable file and link identities first, then foreign residue, then the volatile
        // directory identities that residue would otherwise mask.
        string markerResolved = _resolvePath(LiveWslRootPath.Child(RunRoot, OwnershipMarkerName));
        if (!File.Exists(markerResolved) ||
            !File.ReadAllBytes(markerResolved).SequenceEqual(OwnershipMarkerBytes))
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        LiveWslRootCheckOutcome stable = LiveWslRootFileSystem.VerifyEntries(
            _ownedEntries.Values.Where(entry => entry.Kind != LiveWslRootEntryKind.Directory),
            _fileSystem);
        if (stable is LiveWslRootCheckRejected)
        {
            return stable;
        }
        if (!RootContainsOnlyRunChild())
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.ForeignResidue);
        }
        IReadOnlyList<LiveWslRootEntry> observed =
            LiveWslRootFileSystem.EnumerateTree(RunRoot, _fileSystem, _resolvePath);
        if (observed.Count != _ownedEntries.Count ||
            !observed.All(entry => _ownedEntries.ContainsKey(entry.ResolvedPath)))
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.ForeignResidue);
        }
        LiveWslRootCheckOutcome directories = LiveWslRootFileSystem.VerifyEntries(
            [
                .. _ownedEntries.Values.Where(entry => entry.Kind == LiveWslRootEntryKind.Directory),
                _configuredRootEntry,
            ],
            _fileSystem);
        return directories is LiveWslRootCheckRejected
            ? directories
            : LiveWslRootFileSystem.VerifyShape(_outerAncestors, _fileSystem);
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
                RecaptureOwnedDirectory(Parent(link.Path));
            }
            LiveWslRootCheckOutcome afterUnlink = VerifyForEffect();
            if (afterUnlink is LiveWslRootCheckRejected rejectedAfterUnlink)
            {
                return new LiveWslRootCleanupRejected(rejectedAfterUnlink.Failure);
            }
            Directory.Delete(_resolvePath(RunRoot), recursive: true);

            // The configured root lost its only direct child, so its own token changed with it.
            _configuredRootEntry =
                LiveWslRootFileSystem.CaptureDirectory(_configuredRoot, _fileSystem, _resolvePath);
            return _configuredRootEntry.Kind == LiveWslRootEntryKind.Directory &&
                !Directory.EnumerateFileSystemEntries(_resolvePath(_configuredRoot)).Any() &&
                LiveWslRootFileSystem.VerifyShape(_outerAncestors, _fileSystem) is LiveWslRootCheckAccepted
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
        WindowsWslFileSystem fileSystem,
        Func<WslPath, string> resolvePath,
        Action<WslPath, WslPath> createLink)
    {
        WslPath temporaryRoot = (WslPath)(root.Parent ?? throw new InvalidOperationException("Missing parent."));
        WslPath distributionRoot = (WslPath)(temporaryRoot.Parent ?? throw new InvalidOperationException("Missing root."));

        // ADR-0049 integration: the C# owner alone captures the temporary root and the configured
        // root at fixture start and compares them immediately before its first mutation.
        LiveWslRootEntry[] outerAncestors =
        [
            LiveWslRootFileSystem.CaptureDirectory(distributionRoot, fileSystem, resolvePath),
            LiveWslRootFileSystem.CaptureDirectory(temporaryRoot, fileSystem, resolvePath),
        ];
        LiveWslRootEntry configuredRootEntry =
            LiveWslRootFileSystem.CaptureDirectory(root, fileSystem, resolvePath);
        if (outerAncestors.Any(entry => entry.Kind == LiveWslRootEntryKind.Link) ||
            configuredRootEntry.Kind == LiveWslRootEntryKind.Link)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.LinkDetected);
        }
        if (Directory.EnumerateFileSystemEntries(resolvePath(root)).Any())
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootNotEmpty);
        }
        WslPath runRoot = LiveWslRootPath.Child(root, RunChildName);
        string runResolved = resolvePath(runRoot);
        if (Directory.Exists(runResolved) || File.Exists(runResolved))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootNotEmpty);
        }

        LiveWslRootCheckOutcome admitted = LiveWslRootFileSystem.VerifyEntries(
            [.. outerAncestors, configuredRootEntry],
            fileSystem);
        if (admitted is LiveWslRootCheckRejected rejected)
        {
            return new LiveWslRootOpenRejected(rejected.Failure);
        }

        _ = Directory.CreateDirectory(runResolved);
        configuredRootEntry = LiveWslRootFileSystem.CaptureDirectory(root, fileSystem, resolvePath);
        string[] rootEntries = [.. Directory.EnumerateFileSystemEntries(resolvePath(root))];
        if (LiveWslRootFileSystem.VerifyShape(outerAncestors, fileSystem) is LiveWslRootCheckRejected ||
            configuredRootEntry.Kind != LiveWslRootEntryKind.Directory ||
            rootEntries.Length != 1 ||
            !rootEntries[0].Equals(runResolved, StringComparison.Ordinal))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        WslPath marker = LiveWslRootPath.Child(runRoot, OwnershipMarkerName);
        using (FileStream stream = new(resolvePath(marker), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(OwnershipMarkerBytes);
            stream.Flush(true);
        }

        // ADR-0049: the marker identity comes from the production file system on the marker's own
        // path, because the 9P namespace answers no identity query for the open write handle. The
        // run child is captured after that final owner mutation of its direct children.
        LiveWslRootEntry markerEntry = LiveWslRootFileSystem.CaptureEntry(marker, fileSystem, resolvePath);
        LiveWslRootEntry runEntry = LiveWslRootFileSystem.CaptureDirectory(runRoot, fileSystem, resolvePath);
        Dictionary<string, LiveWslRootEntry> owned = new(StringComparer.Ordinal)
        {
            [runResolved] = runEntry,
            [resolvePath(marker)] = markerEntry,
        };
        return markerEntry.Kind == LiveWslRootEntryKind.File
            ? new LiveWslRootOpened(new LiveWslTestRoot(
                root,
                configuredRootEntry,
                runRoot,
                fileSystem,
                resolvePath,
                createLink,
                outerAncestors,
                owned))
            : new LiveWslRootOpenRejected(LiveWslRootFailureKind.IdentityChanged);
    }

    private static WslPath Parent(WslPath path)
    {
        return (WslPath)(path.Parent ?? throw new InvalidOperationException("The live entry has no parent."));
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

    private void Register(WslPath path)
    {
        if (ProviderPathContainment.Evaluate(RunRoot, path) is not ContainedPath)
        {
            throw new InvalidOperationException("The live fixture escaped its run root.");
        }
        LiveWslRootEntry entry = LiveWslRootFileSystem.CaptureEntry(path, _fileSystem, _resolvePath);
        _ownedEntries.Add(entry.ResolvedPath, entry);

        // ADR-0049: the parent directory's token changed with its direct children, so the owner
        // re-captures it immediately after the mutation it just performed.
        RecaptureOwnedDirectory(Parent(path));
    }

    private void RecaptureOwnedDirectory(WslPath directory)
    {
        string resolved = _resolvePath(directory);
        if (!_ownedEntries.ContainsKey(resolved))
        {
            throw new InvalidOperationException("The mutated live parent is not an owned directory.");
        }
        LiveWslRootEntry recaptured =
            LiveWslRootFileSystem.CaptureDirectory(directory, _fileSystem, _resolvePath);
        if (recaptured.Kind != LiveWslRootEntryKind.Directory)
        {
            throw new InvalidOperationException("The mutated live parent is no longer a directory.");
        }
        _ownedEntries[resolved] = recaptured;
    }

    private void AdoptExpectedTree(string relativePath, IReadOnlyList<string> expectedEntries)
    {
        ArgumentNullException.ThrowIfNull(expectedEntries);
        WslPath path = ResolveExisting(relativePath);

        // ADR-0049: the product created the produced tree inside this owned parent, so the parent's
        // token changed with its direct children and is re-captured immediately after the mutation.
        RecaptureOwnedDirectory(Parent(path));
        IReadOnlyList<LiveWslRootEntry> produced =
            LiveWslRootFileSystem.EnumerateTree(path, _fileSystem, _resolvePath);
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
        if (VerifyForEffect() is LiveWslRootCheckRejected)
        {
            throw new InvalidOperationException("The live root changed around target adoption.");
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
            entry.Kind != LiveWslRootEntryKind.File ||
            LiveWslRootFileSystem.VerifyEntries([entry], _fileSystem) is LiveWslRootCheckRejected)
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
                Register(current);
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
