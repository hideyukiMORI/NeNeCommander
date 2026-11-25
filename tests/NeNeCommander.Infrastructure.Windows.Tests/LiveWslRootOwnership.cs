using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Holds the ownership proof steps of the one live WSL root owner (ADR-0043, ADR-0049): the
/// create-new ownership marker, the checks before the run child exists, the effect-boundary
/// verification over the owner's captured entries, and the gateway evidence that lets the owner
/// adopt a produced tree. The owner keeps every captured entry; these steps only read it.
/// </summary>
internal static class LiveWslRootOwnership
{
    private const string OwnershipMarkerName = ".nene-commander-owner";
    private static readonly byte[] OwnershipMarkerBytes = [0x4E, 0x65, 0x4E, 0x65];

    internal static WslPath WriteOwnershipMarker(WslPath runRoot, Func<WslPath, string> resolvePath)
    {
        WslPath marker = LiveWslRootPath.Child(runRoot, OwnershipMarkerName);
        using (FileStream stream = new(resolvePath(marker), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(OwnershipMarkerBytes);
            stream.Flush(true);
        }
        return marker;
    }

    internal static LiveWslRootOpenRejected? RefuseUnsafeStart(
        LiveWslRootAnchor anchor,
        LiveWslRootEntry configuredRootEntry,
        LiveWslRootMechanism mechanism)
    {
        if (anchor.OuterAncestors.Any(entry => entry.Kind == LiveWslRootEntryKind.Link) ||
            configuredRootEntry.Kind == LiveWslRootEntryKind.Link)
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.LinkDetected);
        }
        if (Directory.EnumerateFileSystemEntries(mechanism.ResolvePath(anchor.ConfiguredRoot)).Any())
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootNotEmpty);
        }
        string runResolved = mechanism.ResolvePath(anchor.RunRoot);
        if (Directory.Exists(runResolved) || File.Exists(runResolved))
        {
            return new LiveWslRootOpenRejected(LiveWslRootFailureKind.RootNotEmpty);
        }

        LiveWslRootCheckOutcome admitted = LiveWslRootFileSystem.VerifyEntries(
            [.. anchor.OuterAncestors, configuredRootEntry],
            mechanism.FileSystem);
        return admitted is LiveWslRootCheckRejected rejected
            ? new LiveWslRootOpenRejected(rejected.Failure)
            : null;
    }

    internal static bool RunChildIsNotSoleEntry(
        LiveWslRootAnchor anchor,
        LiveWslRootEntry configuredRootEntry,
        LiveWslRootMechanism mechanism)
    {
        string runResolved = mechanism.ResolvePath(anchor.RunRoot);
        string[] rootEntries = [.. Directory.EnumerateFileSystemEntries(mechanism.ResolvePath(anchor.ConfiguredRoot))];
        return LiveWslRootFileSystem.VerifyShape(anchor.OuterAncestors, mechanism.FileSystem) is LiveWslRootCheckRejected ||
            configuredRootEntry.Kind != LiveWslRootEntryKind.Directory ||
            rootEntries.Length != 1 ||
            !rootEntries[0].Equals(runResolved, StringComparison.Ordinal);
    }

    internal static LiveWslRootCheckOutcome VerifyOwnedRun(
        LiveWslRootAnchor anchor,
        LiveWslRootEntry configuredRootEntry,
        IReadOnlyDictionary<string, LiveWslRootEntry> ownedEntries,
        LiveWslRootMechanism mechanism)
    {
        // ADR-0049 makes a directory's token change whenever its direct children change, so the
        // order below reports the most specific closed reason: the create-new ownership marker and
        // the stable file and link identities first, then foreign residue, then the volatile
        // directory identities that residue would otherwise mask.
        string markerResolved = mechanism.ResolvePath(LiveWslRootPath.Child(anchor.RunRoot, OwnershipMarkerName));
        if (!File.Exists(markerResolved) ||
            !File.ReadAllBytes(markerResolved).SequenceEqual(OwnershipMarkerBytes))
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        LiveWslRootCheckOutcome stable = LiveWslRootFileSystem.VerifyEntries(
            ownedEntries.Values.Where(entry => entry.Kind != LiveWslRootEntryKind.Directory),
            mechanism.FileSystem);
        if (stable is LiveWslRootCheckRejected)
        {
            return stable;
        }
        if (!RootContainsOnlyRunChild(anchor, mechanism.ResolvePath))
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.ForeignResidue);
        }
        IReadOnlyList<LiveWslRootEntry> observed =
            LiveWslRootFileSystem.EnumerateTree(anchor.RunRoot, mechanism.FileSystem, mechanism.ResolvePath);
        if (observed.Count != ownedEntries.Count ||
            !observed.All(entry => ownedEntries.ContainsKey(entry.ResolvedPath)))
        {
            return new LiveWslRootCheckRejected(LiveWslRootFailureKind.ForeignResidue);
        }
        LiveWslRootCheckOutcome directories = LiveWslRootFileSystem.VerifyEntries(
            [
                .. ownedEntries.Values.Where(entry => entry.Kind == LiveWslRootEntryKind.Directory),
                configuredRootEntry,
            ],
            mechanism.FileSystem);
        return directories is LiveWslRootCheckRejected
            ? directories
            : LiveWslRootFileSystem.VerifyShape(anchor.OuterAncestors, mechanism.FileSystem);
    }

    internal static LiveWslRootEntry RequireEmptyCreatedFile(
        FileOperationOutcome outcome, WslPath target, LiveWslRootMechanism mechanism)
    {
        RequireEffects(outcome, target, [FileOperationEffectKind.FileCreated]);
        LiveWslRootEntry before = LiveWslRootFileSystem.CaptureEntry(target, mechanism.FileSystem, mechanism.ResolvePath);
        return before.Kind != LiveWslRootEntryKind.File || File.ReadAllBytes(mechanism.ResolvePath(target)).Length != 0 ||
            LiveWslRootFileSystem.VerifyEntries([before], mechanism.FileSystem) is LiveWslRootCheckRejected
            ? throw new InvalidOperationException("The produced live entry is not the unchanged empty regular file.")
            : before;
    }

    internal static LiveWslRootCleanupRejected? Unlink(
        LiveWslRootEntry link, LiveWslLinkTarget target,
        IReadOnlyDictionary<string, LiveWslRootEntry> ownedEntries, LiveWslRootMechanism mechanism)
    {
        if (LiveWslRootFileSystem.VerifyEntries([link], mechanism.FileSystem) is LiveWslRootCheckRejected ||
            !target.Matches(ownedEntries, mechanism))
        {
            return new LiveWslRootCleanupRejected(LiveWslRootFailureKind.IdentityChanged);
        }
        mechanism.UnlinkEntry(link.Path);
        return mechanism.FileSystem.Find(link.Path) is not null
            ? new LiveWslRootCleanupRejected(LiveWslRootFailureKind.CleanupFailed)
            : target.Matches(ownedEntries, mechanism)
                ? null
                : new LiveWslRootCleanupRejected(LiveWslRootFailureKind.IdentityChanged);
    }

    internal static void RequireEffects(
        FileOperationOutcome outcome,
        FileSystemPath source,
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

    internal static IReadOnlyList<LiveWslDeclaredEntry> DeclareTree(WslPath path, IReadOnlyList<LiveWslRootEntry> entries)
    {
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

    internal static string[] RelativeNames(WslPath path, IReadOnlyList<LiveWslRootEntry> produced)
    {
        string prefix = path.LinuxPath + "/";
        return
        [
            .. produced.Select(entry => entry.Path.LinuxPath.Equals(path.LinuxPath, StringComparison.Ordinal)
                ? string.Empty
                : entry.Path.LinuxPath[prefix.Length..])
            .Order(StringComparer.Ordinal)
        ];
    }

    private static bool RootContainsOnlyRunChild(LiveWslRootAnchor anchor, Func<WslPath, string> resolvePath)
    {
        string[] entries = [.. Directory.EnumerateFileSystemEntries(resolvePath(anchor.ConfiguredRoot))];
        return entries.Length == 1 && entries[0].Equals(resolvePath(anchor.RunRoot), StringComparison.Ordinal);
    }
}
