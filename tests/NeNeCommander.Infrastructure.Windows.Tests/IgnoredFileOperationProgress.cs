using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class IgnoredFileOperationProgress : IFileOperationProgressObserver
{
    private IgnoredFileOperationProgress()
    {
    }

    internal static IgnoredFileOperationProgress Create()
    {
        return new IgnoredFileOperationProgress();
    }

    public void Report(FileOperationProgress progress)
    {
    }
}
