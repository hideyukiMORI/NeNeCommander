using System.Collections.Generic;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal sealed record UnsupportedFileOperationRequest : FileOperationRequest
{
    internal UnsupportedFileOperationRequest(IReadOnlyList<FileSystemPath> sources)
        : base(sources)
    {
    }
}
