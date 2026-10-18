using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Creates deterministic operation facts at the existing test ports.</summary>
internal static class TransferConflictTestContext
{
    internal static TransferConflict Conflict(
        FileSystemPath source,
        FileSystemPath destination,
        string candidateName)
    {
        string sourceName = source.CanonicalText[(source.CanonicalText.LastIndexOf('\\') + 1)..];
        return TransferConflict.Create(
            Inspection(source).Snapshot,
            ((PathParseSuccess)destination.Child(sourceName)).Path,
            ((PathParseSuccess)destination.Child(candidateName)).Path);
    }

    internal static FileInspectionSucceeded Inspection(FileSystemPath source)
    {
        FileIdentity identity = ((FileIdentityAccepted)FileIdentity.Parse("identity:" + source.CanonicalText)).Identity;
        return (FileInspectionSucceeded)FileInspectionOutcome.Succeeded(
            FileEntrySnapshot.Create(source, identity, DeletionCapability.PermanentOnly));
    }

    internal static FileEntrySnapshot ResolvedSource(
        TransferConflict conflict,
        TransferConflictDecision decision)
    {
        return conflict.Source.WithConflictChoice(TransferConflictChoice.Create(conflict, decision));
    }

    internal static CopyRequest CreateCopy(FileSystemPath[] sources, FileSystemPath destination)
    {
        return (CopyRequest)((FileOperationRequestAccepted)CopyRequest.Create(sources, destination)).Request;
    }

    internal static void AssertNullParameter(Action action, string expectedParameter)
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(action);
        Assert.AreEqual(expectedParameter, exception.ParamName);
    }

    internal static FileSystemPath ParsePath(string text)
    {
        return ((PathParseSuccess)FileSystemPath.Parse(text)).Path;
    }

    internal static Task<FileOperationOutcome> ResumeSkipAsync(FileOperationGateway gateway, TransferContinuation continuation)
    {
        return gateway.ResumeAsync(
            continuation,
            TransferConflictResponse.Create(TransferConflictDecision.Skip, TransferConflictScope.Current),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);
    }
}
