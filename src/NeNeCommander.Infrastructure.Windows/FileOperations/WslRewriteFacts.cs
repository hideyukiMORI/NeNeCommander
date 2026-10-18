namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Freezes the rewrite-sensitive length, link count and timestamps from one opened WSL handle.</summary>
internal sealed record WslRewriteFacts
{
    internal WslRewriteFacts(long endOfFile, uint numberOfLinks, long lastWriteFileTimeUtc, long changeFileTimeUtc)
    {
        EndOfFile = endOfFile;
        NumberOfLinks = numberOfLinks;
        LastWriteFileTimeUtc = lastWriteFileTimeUtc;
        ChangeFileTimeUtc = changeFileTimeUtc;
    }

    internal long EndOfFile { get; }
    internal uint NumberOfLinks { get; }
    internal long LastWriteFileTimeUtc { get; }
    internal long ChangeFileTimeUtc { get; }
}
