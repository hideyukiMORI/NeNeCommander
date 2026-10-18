using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Creates deterministic operation facts at the existing test ports.</summary>
internal static class GatewayTestContext
{
    internal static FileInspectionOutcome Inspection(
        FileSystemPath path,
        DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    internal static MoveRequest CreateMove(FileSystemPath[] sources)
    {
        FileOperationRequestCreation outcome = MoveRequest.Create(sources, ParsePath("D:\\destination"));
        return Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    internal static CopyRequest CreateCopy(FileSystemPath[] sources)
    {
        FileOperationRequestCreation outcome = CopyRequest.Create(sources, ParsePath("D:\\destination"));
        return Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    internal static CreateDirectoryRequest CreateCreateDirectory(FileSystemPath location, string name)
    {
        FileOperationRequestCreation outcome = CreateDirectoryRequest.Create(location, name);
        return Assert.IsInstanceOfType<CreateDirectoryRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    internal static RenameRequest CreateRename(FileSystemPath source, string name)
    {
        FileOperationRequestCreation outcome = RenameRequest.Create(source, name);
        return Assert.IsInstanceOfType<RenameRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    internal static DeleteRequest CreateDelete(
        FileSystemPath[] sources,
        PermanentDeletionConfirmation? confirmation)
    {
        FileOperationRequestCreation outcome = DeleteRequest.Create(sources, confirmation);
        return Assert.IsInstanceOfType<DeleteRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    internal static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
