using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Immutable transfer annotations attached to the same frozen provider entry facts.</summary>
internal sealed record FileEntryTransfer
{
    private FileEntryTransfer(FileSystemPath? target, TransferConflictChoice? choice)
    {
        Target = target;
        Choice = choice;
    }

    internal FileSystemPath? Target { get; }
    internal TransferConflictChoice? Choice { get; }

    internal static FileEntryTransfer Create(FileSystemPath? target, TransferConflictChoice? choice)
    {
        return new FileEntryTransfer(target, choice);
    }
}
