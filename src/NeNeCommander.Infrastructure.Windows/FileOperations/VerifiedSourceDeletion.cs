using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Consumes recorded source entries bottom-up; path reopening retains ADR-0069's residual race.</summary>
internal sealed class VerifiedSourceDeletion
{
    private readonly VerifiedTreeAccess _access;
    private readonly List<VerifiedCopyEntry> _entries = [];
    private readonly HashSet<FileSystemPath> _deleted = new(FileSystemPathIdentityComparer.Instance);
    private readonly Dictionary<FileSystemPath, VerifiedEntrySnapshot> _expectedDirectories = new(FileSystemPathIdentityComparer.Instance);
    private readonly IReadOnlyList<VerifiedEntrySnapshot> _sourceAncestors;
    private readonly IReadOnlyList<VerifiedEntrySnapshot> _targetAncestors;

    private VerifiedSourceDeletion(VerifiedCopyReceipt receipt, VerifiedTreeAccess access)
    {
        _access = access;
        Append(receipt.Root);
        _sourceAncestors = receipt.Boundary.SourceAncestors;
        _targetAncestors = receipt.Boundary.TargetAncestors;
    }

    internal static ProviderStepOutcome Execute(FileEntrySnapshot source, VerifiedCopyReceipt receipt, VerifiedTreeAccess access)
    {
        if (!BindingMatches(source, receipt))
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.Verification);
        }
        try
        {
            return !ValidStructure(receipt.Root, access)
                ? ProviderStepOutcome.Failed(FileOperationFailureKind.Verification)
                : new VerifiedSourceDeletion(receipt, access).Run(receipt.Root);
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProviderStepOutcome.Failed(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Delete));
        }
        catch (WindowsDriveBoundaryRejectedException)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
        }
        catch (IOException exception)
        {
            return ProviderStepOutcome.Failed(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Delete));
        }
    }

    private static bool BindingMatches(FileEntrySnapshot source, VerifiedCopyReceipt receipt)
    {
        return source.Identity == receipt.Source.Identity &&
            FileSystemPathIdentityComparer.Instance.Equals(source.Path, receipt.Source.Path) &&
            source.TransferTarget is FileSystemPath target &&
            FileSystemPathIdentityComparer.Instance.Equals(target, receipt.Root.Target.Path) &&
            TransferRoute.Derive(source.Path, target) is not TransferRoute.Unavailable;
    }

    private ProviderStepOutcome Run(VerifiedCopyEntry root)
    {
        try
        {
            if (!ValidAncestors(root.Source.Path, _sourceAncestors) ||
                !ValidAncestors(root.Target.Path, _targetAncestors) ||
                !VerifiedTreeEvidence.Matches(root, _access))
            {
                return Failure(FileOperationFailureKind.Verification);
            }
            foreach (VerifiedCopyEntry entry in _entries)
            {
                if (!Admit(entry))
                {
                    return Failure(FileOperationFailureKind.IdentityChanged);
                }
                _access.Delete(entry.Source.Path);
                _ = _deleted.Add(entry.Source.Path);
                if (!ReanchorParent(entry))
                {
                    return Failure(FileOperationFailureKind.IdentityChanged);
                }
            }
            return ProviderStepOutcome.Succeeded();
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Delete));
        }
        catch (WindowsDriveBoundaryRejectedException)
        {
            return Failure(FileOperationFailureKind.ProviderUnavailable);
        }
        catch (IOException exception)
        {
            return Failure(WindowsLocalFileOperationAdapter.Normalize(exception.HResult, FileOperationFailureKind.Delete));
        }
    }

    private bool Admit(VerifiedCopyEntry entry)
    {
        return _access.MatchesAncestors(_sourceAncestors) && _access.MatchesAncestors(_targetAncestors) &&
            _entries.Where(candidate => !_deleted.Contains(candidate.Source.Path) &&
                candidate is VerifiedCopyEntry.VerifiedDirectory).All(AdmitDirectory) &&
            (entry is VerifiedCopyEntry.VerifiedFile file
                ? _access.Capture(file.Source.Path) == file.Source &&
                    _access.Capture(file.Target.Path) == file.Target &&
                    VerifiedTreeEvidence.MatchesFile(file, _access) &&
                    _access.Capture(file.Source.Path) == file.Source &&
                    _access.Capture(file.Target.Path) == file.Target
                : AdmitDirectory(entry));
    }

    private bool AdmitDirectory(VerifiedCopyEntry entry)
    {
        return entry is VerifiedCopyEntry.VerifiedDirectory directory &&
            _access.Capture(directory.Source.Path) == _expectedDirectories[directory.Source.Path] &&
            VerifiedTreeEvidence.Membership(directory.Source.Path,
                directory.Children.Where(child => !_deleted.Contains(child.Source.Path)).Select(child => child.Source.Path), _access) &&
            VerifiedTreeEvidence.TargetMatches(directory, _access);
    }

    private bool ReanchorParent(VerifiedCopyEntry deleted)
    {
        VerifiedCopyEntry.VerifiedDirectory? parent = _entries.OfType<VerifiedCopyEntry.VerifiedDirectory>()
            .FirstOrDefault(directory => FileSystemPathIdentityComparer.Instance.Equals(directory.Source.Path, deleted.Source.Path.Parent));
        if (parent is null)
        {
            return true;
        }
        VerifiedEntrySnapshot before = _access.Capture(parent.Source.Path);
        if (before.ObjectIdentity != parent.Source.ObjectIdentity ||
            !VerifiedTreeEvidence.Membership(parent.Source.Path,
                parent.Children.Where(child => !_deleted.Contains(child.Source.Path)).Select(child => child.Source.Path), _access) ||
            _access.Capture(parent.Source.Path) != before)
        {
            return false;
        }
        _expectedDirectories[parent.Source.Path] = before;
        return true;
    }

    private bool ValidAncestors(FileSystemPath path, IReadOnlyList<VerifiedEntrySnapshot> ancestors)
    {
        IReadOnlyList<VerifiedEntrySnapshot> current = _access.Ancestors(path);
        return current.Count == ancestors.Count && current.Select(entry => entry.Path)
            .SequenceEqual(ancestors.Select(entry => entry.Path), FileSystemPathIdentityComparer.Instance) &&
            _access.MatchesAncestors(ancestors);
    }

    private static bool ValidStructure(VerifiedCopyEntry entry, VerifiedTreeAccess access)
    {
        if (entry is not VerifiedCopyEntry.VerifiedDirectory directory)
        {
            return entry is VerifiedCopyEntry.VerifiedFile;
        }
        HashSet<FileSystemPath> sources = new(FileSystemPathIdentityComparer.Instance);
        HashSet<FileSystemPath> targets = new(FileSystemPathIdentityComparer.Instance);
        return directory.Children.All(child => sources.Add(child.Source.Path) && targets.Add(child.Target.Path) &&
            FileSystemPathIdentityComparer.Instance.Equals(child.Source.Path.Parent, directory.Source.Path) &&
            FileSystemPathIdentityComparer.Instance.Equals(child.Target.Path.Parent, directory.Target.Path) &&
            FileSystemPathIdentityComparer.Instance.Equals(child.Target.Path,
                WindowsLocalTreeCopy.DeriveChild(directory.Target.Path, access.Find(child.Source.Path).Name)) &&
            ValidStructure(child, access));
    }

    private void Append(VerifiedCopyEntry entry)
    {
        if (entry is VerifiedCopyEntry.VerifiedDirectory directory)
        {
            _expectedDirectories.Add(directory.Source.Path, directory.Source);
            foreach (VerifiedCopyEntry child in directory.Children)
            {
                Append(child);
            }
        }
        _entries.Add(entry);
    }

    private ProviderStepOutcome Failure(FileOperationFailureKind failure)
    {
        return _deleted.Count == 0 ? ProviderStepOutcome.Failed(failure)
            : ProviderStepOutcome.FailedAfterEffect(failure, ProviderStepEffectKind.SourcePartiallyDeleted);
    }
}
