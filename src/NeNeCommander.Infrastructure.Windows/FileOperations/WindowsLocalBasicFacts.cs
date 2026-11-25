namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Retains native basic-information fields from one held local-provider handle without asserting a metadata freeze.</summary>
internal sealed record WindowsLocalBasicFacts
{
    internal WindowsLocalBasicFacts(long creationFileTimeUtc, long lastWriteFileTimeUtc, long changeFileTimeUtc, uint attributes)
    {
        CreationFileTimeUtc = creationFileTimeUtc;
        LastWriteFileTimeUtc = lastWriteFileTimeUtc;
        ChangeFileTimeUtc = changeFileTimeUtc;
        Attributes = attributes;
    }

    internal long CreationFileTimeUtc { get; }
    internal long LastWriteFileTimeUtc { get; }
    internal long ChangeFileTimeUtc { get; }
    internal uint Attributes { get; }
}
