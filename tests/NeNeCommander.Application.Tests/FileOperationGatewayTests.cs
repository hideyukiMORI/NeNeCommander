using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves serialized, preflighted, and fail-closed filesystem mutation behavior.</summary>
[TestClass]
public sealed class FileOperationGatewayTests
{
    /// <summary>Proves an unregistered request variant fails closed without touching a provider.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRequestVariantIsUnsupportedThrowsWithoutProviderAccess()
    {
        FileOperationRequest request = new UnsupportedFileOperationRequest([ParsePath("C:\\source")]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None));
        Assert.IsEmpty(port.Calls);
    }

    /// <summary>Proves complete preflight precedes ordered composite effects.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveSucceedsReportsOrderedEffectsAfterCompletePreflight()
    {
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        MoveRequest request = CreateMove([first, second]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            port.EnqueueVerification(ProviderStepOutcome.Succeeded());
            port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        }
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(6, outcome.Effects);
        Assert.HasCount(9, port.Calls);
        Assert.AreEqual("Inspect:C:\\first", port.Calls[0]);
        Assert.AreEqual("Inspect:C:\\second", port.Calls[1]);
        Assert.AreEqual("Preflight:D:\\destination", port.Calls[2]);
        Assert.AreEqual("Copy:C:\\first", port.Calls[3]);
        Assert.AreEqual("Verify:C:\\first", port.Calls[4]);
        Assert.AreEqual("Delete:C:\\first", port.Calls[5]);
        Assert.AreEqual("Copy:C:\\second", port.Calls[6]);
        Assert.AreEqual("Verify:C:\\second", port.Calls[7]);
        Assert.AreEqual("Delete:C:\\second", port.Calls[8]);
    }

    /// <summary>Proves a copy runs copy and verify per source after complete preflight and never deletes a source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopySucceedsReportsCopyAndVerifyWithoutDeletingSources()
    {
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        CopyRequest request = CreateCopy([first, second]);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(first, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(Inspection(second, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        for (int index = 0; index < 2; index++)
        {
            port.EnqueueCopy(ProviderStepOutcome.Succeeded());
            port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        }
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(4, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.AreSame(second, outcome.Effects[3].Source);
        Assert.HasCount(7, port.Calls);
        Assert.AreEqual("Inspect:C:\\first", port.Calls[0]);
        Assert.AreEqual("Inspect:C:\\second", port.Calls[1]);
        Assert.AreEqual("Preflight:D:\\destination", port.Calls[2]);
        Assert.AreEqual("Copy:C:\\first", port.Calls[3]);
        Assert.AreEqual("Verify:C:\\first", port.Calls[4]);
        Assert.AreEqual("Copy:C:\\second", port.Calls[5]);
        Assert.AreEqual("Verify:C:\\second", port.Calls[6]);
    }

    /// <summary>Proves progress is reported once per completed source for transfers and deletions, and never for a source that failed.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenSourcesCompleteReportsProgressOncePerSource()
    {
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Failed(FileOperationFailureKind.Copy));
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(Inspection(second, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress copyProgress = RecordingFileOperationProgress.Create();
        RecordingFileOperationProgress deleteProgress = RecordingFileOperationProgress.Create();

        FileOperationOutcome copy = await gateway.ExecuteAsync(CreateCopy([first, second]), copyProgress, CancellationToken.None);
        FileOperationOutcome delete = await gateway.ExecuteAsync(CreateDelete([first, second], null), deleteProgress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, copy.Completion);
        Assert.HasCount(1, copyProgress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 2), copyProgress.Reports[0]);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, delete.Completion);
        Assert.HasCount(2, deleteProgress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 2), deleteProgress.Reports[0]);
        Assert.AreEqual(FileOperationProgress.Create(2, 2), deleteProgress.Reports[1]);
    }

    /// <summary>Proves a directory creation inspects the location, creates once, and reports one effect and full progress.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDirectoryIsCreatedReportsEffectAndProgress()
    {
        FileSystemPath location = ParsePath("C:\\location");
        CreateDirectoryRequest request = CreateCreateDirectory(location, "child");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(location, DeletionCapability.PermanentOnly));
        port.EnqueueDirectoryCreation(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.DirectoryCreated, outcome.Effects[0].Kind);
        Assert.AreSame(request.Target, outcome.Effects[0].Source);
        Assert.HasCount(2, port.Calls);
        Assert.AreEqual("Inspect:C:\\location", port.Calls[0]);
        Assert.AreEqual("CreateDirectory:C:\\location\\child", port.Calls[1]);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 1), progress.Reports[0]);
    }

    /// <summary>Proves inspection failure, cancellation after inspection, and provider rejection each create nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenDirectoryCreationCannotProceedCreatesNothing()
    {
        FileSystemPath location = ParsePath("C:\\location");
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort missing = ScriptedFileOperationPort.Create(null, null);
        missing.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        ScriptedFileOperationPort cancelled = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterInspection, cancellation.Cancel);
        cancelled.EnqueueInspection(Inspection(location, DeletionCapability.PermanentOnly));
        ScriptedFileOperationPort refused = ScriptedFileOperationPort.Create(null, null);
        refused.EnqueueInspection(Inspection(location, DeletionCapability.PermanentOnly));
        refused.EnqueueDirectoryCreation(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway missingGateway = new(missing);
        using FileOperationGateway cancelledGateway = new(cancelled);
        using FileOperationGateway refusedGateway = new(refused);

        FileOperationOutcome missingOutcome = await missingGateway.ExecuteAsync(CreateCreateDirectory(location, "child"), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome cancelledOutcome = await cancelledGateway.ExecuteAsync(CreateCreateDirectory(location, "child"), RecordingFileOperationProgress.Create(), cancellation.Token);
        FileOperationOutcome refusedOutcome = await refusedGateway.ExecuteAsync(CreateCreateDirectory(location, "child"), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missingOutcome.Failure);
        Assert.HasCount(1, missing.Calls);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelledOutcome.Completion);
        Assert.HasCount(1, cancelled.Calls);
        Assert.AreSame(FileOperationFailureKind.Conflict, refusedOutcome.Failure);
        Assert.IsEmpty(refusedOutcome.Effects);
        Assert.HasCount(2, refused.Calls);
    }

    /// <summary>Proves a rename inspects the source, renames once, and reports one effect and full progress.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenEntryIsRenamedReportsEffectAndProgress()
    {
        FileSystemPath source = ParsePath("C:\\location\\before.txt");
        RenameRequest request = CreateRename(source, "after.txt");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source, DeletionCapability.PermanentOnly));
        port.EnqueueRename(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);
        RecordingFileOperationProgress progress = RecordingFileOperationProgress.Create();

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, progress, CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Renamed, outcome.Effects[0].Kind);
        Assert.AreSame(request.Source, outcome.Effects[0].Source);
        Assert.HasCount(2, port.Calls);
        Assert.AreEqual("Inspect:C:\\location\\before.txt", port.Calls[0]);
        Assert.AreEqual("Rename:C:\\location\\before.txt>C:\\location\\after.txt", port.Calls[1]);
        Assert.HasCount(1, progress.Reports);
        Assert.AreEqual(FileOperationProgress.Create(1, 1), progress.Reports[0]);
    }

    /// <summary>Proves inspection failure, cancellation after inspection, and provider rejection each rename nothing.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenRenameCannotProceedRenamesNothing()
    {
        FileSystemPath source = ParsePath("C:\\location\\before.txt");
        using CancellationTokenSource cancellation = new();
        ScriptedFileOperationPort missing = ScriptedFileOperationPort.Create(null, null);
        missing.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        ScriptedFileOperationPort cancelled = ScriptedFileOperationPort.Create(ScriptedCallbackPoint.AfterInspection, cancellation.Cancel);
        cancelled.EnqueueInspection(Inspection(source, DeletionCapability.PermanentOnly));
        ScriptedFileOperationPort refused = ScriptedFileOperationPort.Create(null, null);
        refused.EnqueueInspection(Inspection(source, DeletionCapability.PermanentOnly));
        refused.EnqueueRename(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway missingGateway = new(missing);
        using FileOperationGateway cancelledGateway = new(cancelled);
        using FileOperationGateway refusedGateway = new(refused);

        FileOperationOutcome missingOutcome = await missingGateway.ExecuteAsync(CreateRename(source, "after.txt"), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome cancelledOutcome = await cancelledGateway.ExecuteAsync(CreateRename(source, "after.txt"), RecordingFileOperationProgress.Create(), cancellation.Token);
        FileOperationOutcome refusedOutcome = await refusedGateway.ExecuteAsync(CreateRename(source, "after.txt"), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, missingOutcome.Failure);
        Assert.HasCount(1, missing.Calls);
        Assert.AreSame(FileOperationCompletionKind.Cancelled, cancelledOutcome.Completion);
        Assert.HasCount(1, cancelled.Calls);
        Assert.AreSame(FileOperationFailureKind.Conflict, refusedOutcome.Failure);
        Assert.IsEmpty(refusedOutcome.Effects);
        Assert.HasCount(2, refused.Calls);
    }

    /// <summary>Proves a copy whose verification fails stops the batch with the copied effect and starts no further source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCopyVerificationFailsStopsBeforeNextSource()
    {
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(first, DeletionCapability.PermanentOnly));
        port.EnqueueInspection(Inspection(second, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Failed(FileOperationFailureKind.IdentityChanged));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateCopy([first, second]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(5, port.Calls);
        Assert.AreEqual("Verify:C:\\first", port.Calls[4]);
    }

    /// <summary>Proves a copy rejected by preflight starts no mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenCopyPreflightFindsConflictNoMutationStarts()
    {
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.PermanentOnly));
        port.EnqueuePreflight(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateCopy([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves a later inspection failure prevents every mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-018")]
    public async Task ExecuteAsyncWhenLaterSourceFailsInspectionNoMutationStarts()
    {
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.ProviderUnavailable));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([first, second]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves a preflight collision prevents every mutation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-006")]
    public async Task ExecuteAsyncWhenPreflightFindsConflictNoMutationStarts()
    {
        FileSystemPath source = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Failed(FileOperationFailureKind.Conflict));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([source]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.Conflict, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves verification failure never deletes the source.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-004")]
    [TestProperty("ThreatId", "ADV-007")]
    public async Task ExecuteAsyncWhenCopiedIdentityCannotBeVerifiedSourceIsNotDeleted()
    {
        FileSystemPath source = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(source, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Failed(FileOperationFailureKind.IdentityChanged));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([source]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.IdentityChanged, outcome.Failure);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(4, port.Calls);
        Assert.AreEqual("Verify:C:\\source", port.Calls[3]);
    }

    /// <summary>Proves cancellation reports completed effects and starts no new step.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-005")]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterCopyReportsCopyAndStartsNoNewStep()
    {
        using CancellationTokenSource source = new();
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterCopy,
            source.Cancel);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), source.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Copied, outcome.Effects[0].Kind);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Proves permanent deletion requires exact confirmation.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-008")]
    public async Task ExecuteAsyncWhenPermanentDeleteIsUnconfirmedRejectsWithoutMutation()
    {
        FileSystemPath path = ParsePath("\\\\server\\share\\source");
        DeleteRequest request = CreateDelete([path], null);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.PermanentOnly));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Rejected, outcome.Completion);
        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, outcome.Failure);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves confirmed permanent deletion uses the permanent provider mode.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPermanentDeleteIsConfirmedUsesPermanentMode()
    {
        FileSystemPath path = ParsePath("\\\\server\\share\\source");
        DeleteRequest unconfirmed = CreateDelete([path], null);
        PermanentDeletionConfirmation confirmation = PermanentDeletionConfirmation.CreateFor(unconfirmed);
        DeleteRequest confirmed = CreateDelete([path], confirmation);
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.PermanentOnly));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(confirmed, RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.AreSame(FileOperationEffectKind.PermanentlyDeleted, outcome.Effects[0].Kind);
        Assert.AreEqual("Delete:" + path.CanonicalText, port.Calls[1]);
    }

    /// <summary>Proves recycle capability avoids permanent deletion.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenRecycleIsSupportedUsesRecycleModeWithoutConfirmation()
    {
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateDelete([path], null), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        Assert.AreSame(FileOperationEffectKind.Recycled, outcome.Effects[0].Kind);
        Assert.AreEqual("Recycle:" + path.CanonicalText, port.Calls[1]);
    }

    /// <summary>Proves concurrent dispatch is rejected deterministically.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-014")]
    public async Task ExecuteAsyncWhenAnotherRequestOwnsGatewayReentrantRequestIsRejected()
    {
        FileSystemPath path = ParsePath("C:\\source");
        BlockingInspectionPort port = BlockingInspectionPort.Create(
            Inspection(path, DeletionCapability.Recycle));
        using FileOperationGateway gateway = new(port);

        Task<FileOperationOutcome> firstExecution = gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);
        FileOperationOutcome second = await gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);
        port.Release();
        FileOperationOutcome first = await firstExecution;

        Assert.AreSame(FileOperationCompletionKind.Rejected, second.Completion);
        Assert.AreSame(FileOperationFailureKind.Reentrant, second.Failure);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, first.Completion);
    }

    /// <summary>Proves pre-cancelled work performs no provider call.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenAlreadyCancelledReturnsCancelledWithoutCallingPort()
    {
        using CancellationTokenSource source = new();
        source.Cancel();
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateMove([ParsePath("C:\\source")]),
            RecordingFileOperationProgress.Create(),
            source.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.IsEmpty(port.Calls);
    }

    /// <summary>Proves move inspection cancellation stops before the next source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveInspectionIsCancelledStopsBeforeNextSource()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterInspection,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves delete inspection cancellation stops before the next source.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteInspectionIsCancelledStopsBeforeNextSource()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterInspection,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateDelete([first, second], null),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves cancellation after move preflight starts no copy.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterPreflightStartsNoCopy()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterPreflight,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(2, port.Calls);
    }

    /// <summary>Proves delete inspection failure starts no mutation.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteInspectionFailsStartsNoMutation()
    {
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(FileInspectionOutcome.Failed(FileOperationFailureKind.NotFound));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateDelete([path], null),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.NotFound, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(1, port.Calls);
    }

    /// <summary>Proves copy failure reports no completed effect.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCopyFailsReportsNoCompletedEffect()
    {
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Failed(FileOperationFailureKind.Copy));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Copy, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Proves cancellation after verification preserves two exact effects.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenCancellationArrivesAfterVerificationStartsNoDelete()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterVerification,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(FileOperationEffectKind.Verified, outcome.Effects[1].Kind);
        Assert.HasCount(4, port.Calls);
    }

    /// <summary>Proves move source-deletion failure preserves copy and verification effects.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveSourceDeletionFailsReportsPartialCompletion()
    {
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Failed(FileOperationFailureKind.Delete));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(CreateMove([path]), RecordingFileOperationProgress.Create(), CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.Delete, outcome.Failure);
        Assert.HasCount(2, outcome.Effects);
        Assert.AreSame(FileOperationCompletionKind.PartiallyCompleted, outcome.Completion);
    }

    /// <summary>Proves move cancellation between sources starts no second copy.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenMoveCancellationArrivesBetweenSourcesStopsBatch()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterDeletion,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(Inspection(second, DeletionCapability.Recycle));
        port.EnqueuePreflight(ProviderStepOutcome.Succeeded());
        port.EnqueueCopy(ProviderStepOutcome.Succeeded());
        port.EnqueueVerification(ProviderStepOutcome.Succeeded());
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateMove([first, second]),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(3, outcome.Effects);
        Assert.HasCount(6, port.Calls);
    }

    /// <summary>Proves delete cancellation between sources starts no second deletion.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteCancellationArrivesBetweenSourcesStopsBatch()
    {
        using CancellationTokenSource cancellation = new();
        FileSystemPath first = ParsePath("C:\\first");
        FileSystemPath second = ParsePath("C:\\second");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(
            ScriptedCallbackPoint.AfterDeletion,
            cancellation.Cancel);
        port.EnqueueInspection(Inspection(first, DeletionCapability.Recycle));
        port.EnqueueInspection(Inspection(second, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Succeeded());
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateDelete([first, second], null),
            RecordingFileOperationProgress.Create(),
            cancellation.Token);

        Assert.AreSame(FileOperationCompletionKind.Cancelled, outcome.Completion);
        Assert.HasCount(1, outcome.Effects);
        Assert.HasCount(3, port.Calls);
    }

    /// <summary>Proves provider deletion failure returns an exact rejection.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenDeleteFailsReturnsProviderFailure()
    {
        FileSystemPath path = ParsePath("C:\\source");
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        port.EnqueueInspection(Inspection(path, DeletionCapability.Recycle));
        port.EnqueueDeletion(ProviderStepOutcome.Failed(FileOperationFailureKind.AccessDenied));
        using FileOperationGateway gateway = new(port);

        FileOperationOutcome outcome = await gateway.ExecuteAsync(
            CreateDelete([path], null),
            RecordingFileOperationProgress.Create(),
            CancellationToken.None);

        Assert.AreSame(FileOperationFailureKind.AccessDenied, outcome.Failure);
        Assert.IsEmpty(outcome.Effects);
    }

    /// <summary>Proves confirmation cannot authorize a different source count or ordering.</summary>
    [TestMethod]
    public async Task ExecuteAsyncWhenPermanentConfirmationDoesNotMatchExactSourcesRejectsMutation()
    {
        FileSystemPath first = ParsePath("\\\\server\\share\\first");
        FileSystemPath second = ParsePath("\\\\server\\share\\second");
        DeleteRequest oneSource = CreateDelete([first], null);
        DeleteRequest ordered = CreateDelete([first, second], null);

        FileOperationOutcome countMismatch = await ExecutePermanentDeleteAsync(
            [first, second],
            PermanentDeletionConfirmation.CreateFor(oneSource));
        FileOperationOutcome orderMismatch = await ExecutePermanentDeleteAsync(
            [second, first],
            PermanentDeletionConfirmation.CreateFor(ordered));

        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, countMismatch.Failure);
        Assert.AreSame(FileOperationFailureKind.ConfirmationRequired, orderMismatch.Failure);
    }

    private static async Task<FileOperationOutcome> ExecutePermanentDeleteAsync(
        FileSystemPath[] sources,
        PermanentDeletionConfirmation confirmation)
    {
        ScriptedFileOperationPort port = ScriptedFileOperationPort.Create(null, null);
        foreach (FileSystemPath source in sources)
        {
            port.EnqueueInspection(Inspection(source, DeletionCapability.PermanentOnly));
        }
        using FileOperationGateway gateway = new(port);
        return await gateway.ExecuteAsync(CreateDelete(sources, confirmation), RecordingFileOperationProgress.Create(), CancellationToken.None);
    }

    private static FileInspectionOutcome Inspection(
        FileSystemPath path,
        DeletionCapability capability)
    {
        FileIdentityAccepted identity = Assert.IsInstanceOfType<FileIdentityAccepted>(
            FileIdentity.Parse("identity:" + path.CanonicalText));
        return FileInspectionOutcome.Succeeded(FileEntrySnapshot.Create(path, identity.Identity, capability));
    }

    private static MoveRequest CreateMove(FileSystemPath[] sources)
    {
        FileOperationRequestCreation outcome = MoveRequest.Create(sources, ParsePath("D:\\destination"));
        return Assert.IsInstanceOfType<MoveRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    private static CopyRequest CreateCopy(FileSystemPath[] sources)
    {
        FileOperationRequestCreation outcome = CopyRequest.Create(sources, ParsePath("D:\\destination"));
        return Assert.IsInstanceOfType<CopyRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    private static CreateDirectoryRequest CreateCreateDirectory(FileSystemPath location, string name)
    {
        FileOperationRequestCreation outcome = CreateDirectoryRequest.Create(location, name);
        return Assert.IsInstanceOfType<CreateDirectoryRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    private static RenameRequest CreateRename(FileSystemPath source, string name)
    {
        FileOperationRequestCreation outcome = RenameRequest.Create(source, name);
        return Assert.IsInstanceOfType<RenameRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    private static DeleteRequest CreateDelete(
        FileSystemPath[] sources,
        PermanentDeletionConfirmation? confirmation)
    {
        FileOperationRequestCreation outcome = DeleteRequest.Create(sources, confirmation);
        return Assert.IsInstanceOfType<DeleteRequest>(
            Assert.IsInstanceOfType<FileOperationRequestAccepted>(outcome).Request);
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
