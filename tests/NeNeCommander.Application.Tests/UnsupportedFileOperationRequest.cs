using System.Collections.Generic;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

internal sealed record UnsupportedFileOperationRequest : FileOperationRequest
{
    private protected override void RestrictDerivation()
    {
    }

    internal UnsupportedFileOperationRequest(IReadOnlyList<FileSystemPath> sources)
        : base(sources)
    {
    }
}
