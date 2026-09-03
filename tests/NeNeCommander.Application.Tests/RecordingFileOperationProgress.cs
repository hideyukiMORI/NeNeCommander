using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Application.Tests;

internal sealed class RecordingFileOperationProgress : IFileOperationProgressObserver
{
    private readonly List<FileOperationProgress> _reports;

    private RecordingFileOperationProgress()
    {
        _reports = [];
    }

    internal IReadOnlyList<FileOperationProgress> Reports => new ReadOnlyCollection<FileOperationProgress>(_reports);

    internal static RecordingFileOperationProgress Create()
    {
        return new RecordingFileOperationProgress();
    }

    public void Report(FileOperationProgress progress)
    {
        _reports.Add(progress);
    }
}
