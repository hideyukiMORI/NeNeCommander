using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;
using NeNeCommander.Infrastructure.Windows.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class LiveWslTestRoot
{
    private const string RunChildName = "NeNeCommander-Live-Run";

    private readonly LiveWslRootAnchor _anchor;
    private readonly LiveWslRootMechanism _mechanism;
    private readonly Dictionary<string, LiveWslRootEntry> _ownedEntries;
    private readonly WindowsWslFileSystem _fileSystem;
    private readonly Func<WslPath, string> _resolvePath;
    private readonly Dictionary<string, LiveWslLinkTarget> _linkTargets;
    private LiveWslRootEntry _configuredRootEntry;

    private LiveWslTestRoot(
        LiveWslRootAnchor anchor,
        LiveWslRootEntry configuredRootEntry,
        LiveWslRootMechanism mechanism,
        Dictionary<string, LiveWslRootEntry> ownedEntries)
    {
        _anchor = anchor;
        _configuredRootEntry = configuredRootEntry;
        _mechanism = mechanism;
        _fileSystem = mechanism.FileSystem;
        _resolvePath = mechanism.ResolvePath;
        _linkTargets = new Dictionary<string, LiveWslLinkTarget>(StringComparer.Ordinal);
        _ownedEntries = ownedEntries;
    }

    internal WslPath RunRoot => _anchor.RunRoot;

    /// <summary>
    /// Opens the configured root through the production WSL file system. The caller supplies the
    /// same namespace mapping that the mechanism's file system was composed with, so identity and
    /// setup always describe the same entry.
    /// </summary>
    /// <param name="admission">Launcher-owned admission facts.</param>
    /// <param name="registeredRoots">Distribution roots reported by the canonical catalog.</param>
    /// <param name="mechanism">Production WSL file system, its namespace mapping, and the link seams.</param>
    /// <returns>The opened owner or a closed rejection.</returns>
    internal static LiveWslRootOpenOutcome Open(
        LiveWslRootAdmission admission,
        IReadOnlyList<WslPath> registeredRoots,
        LiveWslRootMechanism mechanism)
    {
        ArgumentNullException.ThrowIfNull(admission);
        ArgumentNullException.ThrowIfNull(registeredRoots);
        ArgumentNullException.ThrowIfNull(mechanism);
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
            return OpenVerified(root, mechanism);
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
        return CreateLink(path, LiveWslLinkTarget.Existing(LiveWslRootPath.Resolve(RunRoot, targetRelativePath)));
    }

    internal WslPath CreateDanglingFileSymbolicLink(string relativePath, string targetRelativePath)
    {
        WslPath path = ResolveForCreation(relativePath);
        return CreateLink(path, LiveWslLinkTarget.Absent(ResolveForCreation(targetRelativePath)));
    }

    private WslPath CreateLink(WslPath path, LiveWslLinkTarget target)
    {
        if (ProviderPathContainment.Evaluate(RunRoot, path) is not ContainedPath ||
            ProviderPathContainment.Evaluate(RunRoot, target.Path) is not ContainedPath ||
            !target.Matches(_ownedEntries, _mechanism))
        {
            throw new InvalidOperationException("The live link fixture or its declared target is not owned.");
        }
        RequireVerifiedSetup();
        // ADR-0043/0049: create inside the distribution; cleanup unlinks only the owned entry.
        _mechanism.CreateLink(target.Path, path);
        Register(path);
        _linkTargets.Add(_resolvePath(path), target);
        return target.Matches(_ownedEntries, _mechanism)
            ? path
            : throw new InvalidOperationException("The live link target changed during setup.");
    }

    internal byte[] ReadFile(string relativePath)
    {
        WslPath path = LiveWslRootPath.Resolve(RunRoot, relativePath);
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
        WslPath path = LiveWslRootPath.Resolve(RunRoot, relativePath);
        RequireVerifiedSetup();
        return _ownedEntries.ContainsKey(_resolvePath(path))
            ? path
            : throw new InvalidOperationException("The live identity path is not owned by this run.");
    }

    internal IReadOnlyList<LiveWslDeclaredEntry> ReadDeclaredTree(string relativePath)
    {
        WslPath path = LiveWslRootPath.Resolve(RunRoot, relativePath);
        RequireVerifiedSetup();
        IReadOnlyList<LiveWslRootEntry> entries =
            LiveWslRootFileSystem.EnumerateTree(path, _fileSystem, _resolvePath);
        return entries.Any(entry => !_ownedEntries.ContainsKey(entry.ResolvedPath))
            ? throw new InvalidOperationException("The live declared tree contains an unowned entry.")
            : LiveWslRootOwnership.DeclareTree(path, entries);
    }

    internal bool Exists(string relativePath)
    {
        WslPath path = LiveWslRootPath.Resolve(RunRoot, relativePath);
        string resolved = _resolvePath(path);
        RequireVerifiedSetup();
        bool exists = File.Exists(resolved) || Directory.Exists(resolved);
        return exists && !_ownedEntries.ContainsKey(resolved)
            ? throw new InvalidOperationException("The live evidence path is not owned by this run.")
            : exists;
    }

    internal void AdoptCreatedFile(string relativePath, FileOperationOutcome outcome)
    {
        WslPath target = LiveWslRootPath.Resolve(RunRoot, relativePath);
        LiveWslRootEntry entry = LiveWslRootOwnership.RequireEmptyCreatedFile(outcome, target, _mechanism);
        _ownedEntries.Add(entry.ResolvedPath, entry);
        RecaptureOwnedDirectory(LiveWslRootPath.Parent(target));
        RequireVerifiedSetup();
    }

    internal void AdoptCopiedTree(
        string relativePath,
        IReadOnlyList<string> expectedEntries,
        FileOperationOutcome outcome,
        FileSystemPath source)
    {
        LiveWslRootOwnership.RequireEffects(
            outcome,
            source,
            [FileOperationEffectKind.Copied, FileOperationEffectKind.Verified]);
        AdoptExpectedTree(relativePath, expectedEntries);
    }

    internal void AdoptCrossMovedTree(
        string relativePath,
        IReadOnlyList<string> expectedEntries,
        FileOperationOutcome outcome,
        WindowsLocalPath source)
    {
        LiveWslRootOwnership.RequireEffects(
            outcome,
            source,
            [FileOperationEffectKind.Copied, FileOperationEffectKind.Verified, FileOperationEffectKind.SourceDeleted]);
        AdoptExpectedTree(relativePath, expectedEntries);
    }

    internal void AdoptMovedTree(
        LiveWslTreeMove move,
        IReadOnlyList<string> expectedEntries,
        FileOperationOutcome outcome,
        WslPath source)
    {
        ArgumentNullException.ThrowIfNull(move);
        LiveWslRootOwnership.RequireEffects(
            outcome,
            source,
            [FileOperationEffectKind.Copied, FileOperationEffectKind.Verified, FileOperationEffectKind.SourceDeleted]);
        RetireMovedSource(move.SourceRelativePath);
        AdoptExpectedTree(move.TargetRelativePath, expectedEntries);
    }

    internal void AdoptMovedOutTree(string sourceRelativePath, FileOperationOutcome outcome, WslPath source)
    {
        LiveWslRootOwnership.RequireEffects(outcome, source,
            [FileOperationEffectKind.Copied, FileOperationEffectKind.Verified, FileOperationEffectKind.SourceDeleted]);
        RetireMovedSource(sourceRelativePath);
    }

    private void RetireMovedSource(string sourceRelativePath)
    {
        WslPath path = LiveWslRootPath.Resolve(RunRoot, sourceRelativePath);
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
        RecaptureOwnedDirectory(LiveWslRootPath.Parent(path));
    }

    internal LiveWslRootCheckOutcome VerifyForEffect()
    {
        return LiveWslRootOwnership.VerifyOwnedRun(_anchor, _configuredRootEntry, _ownedEntries, _mechanism);
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

            // ADR-0043/ADR-0049 cleanup order. First every owned link entry is removed without
            // following it, and the owner proves the entry is gone and its target unchanged.
            foreach (LiveWslRootEntry link in _ownedEntries.Values
                .Where(entry => entry.Kind == LiveWslRootEntryKind.Link)
                .ToArray())
            {
                if (UnlinkOwnedEntry(link) is LiveWslRootCleanupRejected rejectedUnlink)
                {
                    return rejectedUnlink;
                }
            }

            // Second, the run child is re-enumerated without following links; any remaining
            // reparse entry, owned or foreign, refuses the recursive removal.
            if (LiveWslRootFileSystem.EnumerateTree(RunRoot, _fileSystem, _resolvePath)
                .Any(entry => entry.Kind == LiveWslRootEntryKind.Link))
            {
                return new LiveWslRootCleanupRejected(LiveWslRootFailureKind.LinkDetected);
            }
            LiveWslRootCheckOutcome afterUnlink = VerifyForEffect();
            if (afterUnlink is LiveWslRootCheckRejected rejectedAfterUnlink)
            {
                return new LiveWslRootCleanupRejected(rejectedAfterUnlink.Failure);
            }

            // Only now is the link-free run child removed from the Windows side.
            Directory.Delete(_resolvePath(RunRoot), recursive: true);

            // The configured root lost its only direct child, so its own token changed with it.
            _configuredRootEntry =
                LiveWslRootFileSystem.CaptureDirectory(_anchor.ConfiguredRoot, _fileSystem, _resolvePath);
            return _configuredRootEntry.Kind == LiveWslRootEntryKind.Directory &&
                !Directory.EnumerateFileSystemEntries(_resolvePath(_anchor.ConfiguredRoot)).Any() &&
                LiveWslRootFileSystem.VerifyShape(_anchor.OuterAncestors, _fileSystem) is LiveWslRootCheckAccepted
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

    private static LiveWslRootOpenOutcome OpenVerified(WslPath root, LiveWslRootMechanism mechanism)
    {
        WslPath temporaryRoot = (WslPath)(root.Parent ?? throw new InvalidOperationException("Missing parent."));
        WslPath distributionRoot = (WslPath)(temporaryRoot.Parent ?? throw new InvalidOperationException("Missing root."));

        // ADR-0049 integration: the C# owner alone captures the temporary root and the configured
        // root at fixture start and compares them immediately before its first mutation.
        LiveWslRootAnchor anchor = LiveWslRootAnchor.Create(
            root,
            LiveWslRootPath.Child(root, RunChildName),
            [
                LiveWslRootFileSystem.CaptureDirectory(distributionRoot, mechanism.FileSystem, mechanism.ResolvePath),
                LiveWslRootFileSystem.CaptureDirectory(temporaryRoot, mechanism.FileSystem, mechanism.ResolvePath),
            ]);
        LiveWslRootEntry configuredRootEntry =
            LiveWslRootFileSystem.CaptureDirectory(root, mechanism.FileSystem, mechanism.ResolvePath);
        if (LiveWslRootOwnership.RefuseUnsafeStart(anchor, configuredRootEntry, mechanism) is LiveWslRootOpenRejected refused)
        {
            return refused;
        }

        _ = Directory.CreateDirectory(mechanism.ResolvePath(anchor.RunRoot));
        configuredRootEntry = LiveWslRootFileSystem.CaptureDirectory(root, mechanism.FileSystem, mechanism.ResolvePath);
        return LiveWslRootOwnership.RunChildIsNotSoleEntry(anchor, configuredRootEntry, mechanism)
            ? new LiveWslRootOpenRejected(LiveWslRootFailureKind.IdentityChanged)
            : CreateOwner(anchor, configuredRootEntry, mechanism);
    }

    private static LiveWslRootOpenOutcome CreateOwner(
        LiveWslRootAnchor anchor,
        LiveWslRootEntry configuredRootEntry,
        LiveWslRootMechanism mechanism)
    {
        WslPath marker = LiveWslRootOwnership.WriteOwnershipMarker(anchor.RunRoot, mechanism.ResolvePath);

        // ADR-0049: the marker identity comes from the production file system on the marker's own
        // path, because the 9P namespace answers no identity query for the open write handle. The
        // run child is captured after that final owner mutation of its direct children.
        LiveWslRootEntry markerEntry = LiveWslRootFileSystem.CaptureEntry(marker, mechanism.FileSystem, mechanism.ResolvePath);
        LiveWslRootEntry runEntry = LiveWslRootFileSystem.CaptureDirectory(anchor.RunRoot, mechanism.FileSystem, mechanism.ResolvePath);
        Dictionary<string, LiveWslRootEntry> owned = new(StringComparer.Ordinal)
        {
            [mechanism.ResolvePath(anchor.RunRoot)] = runEntry,
            [mechanism.ResolvePath(marker)] = markerEntry,
        };
        return markerEntry.Kind == LiveWslRootEntryKind.File
            ? new LiveWslRootOpened(new LiveWslTestRoot(anchor, configuredRootEntry, mechanism, owned))
            : new LiveWslRootOpenRejected(LiveWslRootFailureKind.IdentityChanged);
    }

    private LiveWslRootCleanupRejected? UnlinkOwnedEntry(LiveWslRootEntry link)
    {
        if (!_linkTargets.TryGetValue(link.ResolvedPath, out LiveWslLinkTarget? target))
        {
            return new LiveWslRootCleanupRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        LiveWslRootCleanupRejected? rejected = LiveWslRootOwnership.Unlink(link, target, _ownedEntries, _mechanism);
        if (rejected is not null)
        {
            return rejected;
        }
        _ = _ownedEntries.Remove(link.ResolvedPath);
        _ = _linkTargets.Remove(link.ResolvedPath);
        RecaptureOwnedDirectory(LiveWslRootPath.Parent(link.Path));
        return null;
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
        RecaptureOwnedDirectory(LiveWslRootPath.Parent(path));
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
        WslPath path = LiveWslRootPath.Resolve(RunRoot, relativePath);

        // ADR-0049: the product created the produced tree inside this owned parent, so the parent's
        // token changed with its direct children and is re-captured immediately after the mutation.
        RecaptureOwnedDirectory(LiveWslRootPath.Parent(path));
        IReadOnlyList<LiveWslRootEntry> produced =
            LiveWslRootFileSystem.EnumerateTree(path, _fileSystem, _resolvePath);
        string[] actual = LiveWslRootOwnership.RelativeNames(path, produced);
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


}
