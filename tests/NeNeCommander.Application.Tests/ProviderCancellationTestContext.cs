using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Supplies explicit cancelled provider returns for the gateway's existing mutation routes.</summary>
internal static class ProviderCancellationTestContext
{
    internal static FileOperationRequest Prepare(string operation, FileSystemPath source, ScriptedFileOperationPort port)
    {
        port.EnqueueInspection(GatewayTestContext.Inspection(source, DeletionCapability.Recycle));
        ProviderStepOutcome cancelled = ProviderStepOutcome.Create(ProviderStepCompletion.Cancelled, ProviderStepReceipt.Empty);
        switch (operation)
        {
            case "copy":
            case "move":
            case "atomic":
                return PrepareTransfer(operation, source, port, cancelled);
            case "delete":
                port.EnqueueDeletion(cancelled);
                return GatewayTestContext.CreateDelete([source], null);
            case "directory":
                port.EnqueueDirectoryCreation(cancelled);
                return GatewayTestContext.CreateCreateDirectory(source, "new");
            case "file":
                port.EnqueueFileCreation(cancelled);
                return Assert.IsInstanceOfType<FileOperationRequestAccepted>(CreateTextFileRequest.Create(source, "new.txt")).Request;
            case "rename":
                port.EnqueueRename(cancelled);
                return GatewayTestContext.CreateRename(source, "renamed.txt");
            default:
                throw new ArgumentException("Unknown test operation.", nameof(operation));
        }
    }

    private static FileOperationRequest PrepareTransfer(
        string operation,
        FileSystemPath source,
        ScriptedFileOperationPort port,
        ProviderStepOutcome cancelled)
    {
        port.EnqueuePreflight(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        if (operation == "atomic")
        {
            port.EnqueueAtomicMoveCapability(AtomicMoveCapabilityOutcome.Supported);
            port.EnqueueAtomicMove(cancelled);
        }
        else
        {
            port.EnqueueCopy(cancelled);
            port.EnqueueVerification(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
            port.EnqueueDeletion(ProviderStepOutcome.Create(ProviderStepCompletion.Succeeded, ProviderStepReceipt.Empty));
        }
        return operation == "copy" ? GatewayTestContext.CreateCopy([source]) : GatewayTestContext.CreateMove([source]);
    }
}
