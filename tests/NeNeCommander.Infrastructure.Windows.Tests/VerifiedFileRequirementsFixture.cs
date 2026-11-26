using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Owns a real copied file and explicitly qualified receipt for the internal source-deletion boundary.</summary>
internal sealed class VerifiedFileRequirementsFixture : IDisposable
{
    private VerifiedFileRequirementsFixture(TestOwnedTemporaryRoot root, VerifiedCopyReceipt receipt)
    {
        Root = root;
        Receipt = receipt;
    }

    internal TestOwnedTemporaryRoot Root { get; }
    internal VerifiedCopyReceipt Receipt { get; }
    internal string Source => Receipt.Root.Source.Path.CanonicalText;
    internal string Target => Receipt.Root.Target.Path.CanonicalText;
    internal VerifiedTreeAccess Access { get; } = new(new WindowsWslFileSystem(), new WindowsDriveBoundary());

    internal static async Task<VerifiedFileRequirementsFixture> CreateAsync(
        FileVerificationRequirements requirements, string content, string? existingStream)
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        try
        {
            _ = root.WriteFile("source.txt", content);
            _ = root.CreateDirectory("target");
            if (existingStream is not null)
            {
                _ = root.WriteFile("source.txt:extra", existingStream);
            }
            VerifiedCopyReceipt copied = await CopyAsync(root);
            VerifiedCopyEntry.VerifiedFile file = Assert.IsInstanceOfType<VerifiedCopyEntry.VerifiedFile>(copied.Root);
            Assert.AreSame(FileVerificationRequirements.DefaultStream, file.Requirements);
            VerifiedCopyEntry required = VerifiedCopyEntry.File(file.Source, file.Target, file.Content, requirements);
            return new VerifiedFileRequirementsFixture(root, VerifiedCopyReceipt.Create(copied.Source, required, copied.Boundary));
        }
        catch
        {
            root.Dispose();
            throw;
        }
    }

    internal FileSystemPath Path(string side)
    {
        return side switch
        {
            "source" => Receipt.Root.Source.Path,
            "target" => Receipt.Root.Target.Path,
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };
    }

    internal void Change(FileSystemPath affected, string defect)
    {
        string relative = affected == Receipt.Root.Target.Path ? "target\\source.txt" : affected == Receipt.Root.Source.Path ? "source.txt" : throw new ArgumentOutOfRangeException(nameof(affected));
        string path = Root.Resolve(relative);
        DateTime writeTime = File.GetLastWriteTimeUtc(path);
        switch (defect)
        {
            case "hidden":
                File.SetAttributes(path, FileAttributes.Hidden);
                break;
            case "emptyStream":
                _ = Root.WriteFile(relative + ":extra", string.Empty);
                break;
            case "namedStream":
                _ = Root.WriteFile(relative + ":extra", "new stream");
                break;
            case "hardlink":
                _ = Root.CreateHardLink(affected == Receipt.Root.Target.Path ? "target-alias.txt" : "source-alias.txt", relative);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }
        File.SetLastWriteTimeUtc(path, writeTime);
    }

    private static async Task<VerifiedCopyReceipt> CopyAsync(TestOwnedTemporaryRoot root)
    {
        WindowsLocalFileOperationAdapter adapter = new(new(),
            new WindowsRecycleBin(new ScriptedWindowsRecycleShell { BinQueryResult = -1 }));
        FileEntrySnapshot? frozen = null;
        StepHookPort port = new(adapter)
        {
            AfterCopy = source =>
            {
                frozen = source;
                return Task.CompletedTask;
            },
        };
        using FileOperationGateway gateway = new(port);
        FileSystemPath sourcePath = WindowsLocalOperationTestContext.ParsePath(root.Resolve("source.txt"));
        FileSystemPath destination = WindowsLocalOperationTestContext.ParsePath(root.Resolve("target"));
        CopyRequest request = Assert.IsInstanceOfType<CopyRequest>(Assert.IsInstanceOfType<FileOperationRequestAccepted>(
            CopyRequest.Create([sourcePath], destination)).Request);
        FileOperationOutcome outcome = await gateway.ExecuteAsync(request, IgnoredFileOperationProgress.Create(), CancellationToken.None);
        Assert.AreSame(FileOperationCompletionKind.Succeeded, outcome.Completion);
        FileEntrySnapshot source = frozen ?? throw new AssertFailedException("The real gateway did not freeze a source.");
        return Assert.IsInstanceOfType<CopyVerified>(await adapter.VerifyCopyAsync(source, destination, CancellationToken.None)).Receipt;
    }

    public void Dispose()
    {
        Root.Dispose();
    }
}
