namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Retains the native length, link count and BOOLEAN bytes from one held local-provider handle.</summary>
internal sealed record WindowsLocalStandardFacts
{
    internal WindowsLocalStandardFacts(long endOfFile, uint numberOfLinks, byte deletePending, byte directory)
    {
        EndOfFile = endOfFile;
        NumberOfLinks = numberOfLinks;
        DeletePending = deletePending;
        Directory = directory;
    }

    internal long EndOfFile { get; }
    internal uint NumberOfLinks { get; }
    internal byte DeletePending { get; }
    internal byte Directory { get; }
}
