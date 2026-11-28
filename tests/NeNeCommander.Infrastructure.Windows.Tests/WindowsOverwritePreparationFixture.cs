using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Identifiers;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class WindowsOverwritePreparationFixture : IDisposable
{
    private WindowsOverwritePreparationFixture(TestOwnedTemporaryRoot root, WindowsOverwritePairLease pair)
    {
        Root = root;
        Pair = pair;
    }

    internal TestOwnedTemporaryRoot Root { get; }
    internal WindowsOverwritePairLease Pair { get; }

    internal static WindowsOverwritePreparationFixture Create(string source, string target, string targetName = "target", WindowsDriveBoundary? driveBoundary = null)
    {
        TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsOverwritePairLease? pair = null;
        try
        {
            WindowsLocalPath sourcePath = Parse(root.WriteFile("source", source));
            WindowsLocalPath targetPath = Parse(root.WriteFile(targetName, target));
            FileEntrySnapshot snapshot = FileEntrySnapshot.Create(sourcePath, WindowsLocalEntryIdentity.Describe(new FileInfo(sourcePath.CanonicalText)), DeletionCapability.NotRequested);
            pair = WindowsOverwritePairLease.Open(snapshot, targetPath, driveBoundary ?? new WindowsDriveBoundary());
            return new WindowsOverwritePreparationFixture(root, pair);
        }
        catch
        {
            pair?.Dispose();
            root.Dispose();
            throw;
        }
    }

    internal WindowsOverwriteAttempt Attempt(IIdentifierSource identifiers, WindowsTransferContentIo io, WindowsTransferArtifactQueries queries, WindowsTransferArtifactRemoval? removal = null)
    {
        return WindowsOverwriteAttempt.Create(Pair, Pair.Evidence, new WindowsOverwriteDependencies(identifiers, io, queries, removal ?? WindowsTransferArtifactRemoval.Native));
    }

    internal static IdentifiedTransferArtifact Artifact(WindowsOverwriteAttempt attempt, TransferArtifactRole role)
    {
        return Assert.IsInstanceOfType<IdentifiedTransferArtifact>(attempt.Receipt.Artifacts.Single(artifact => artifact.Role == role));
    }

    internal static bool IsArtifact(SafeFileHandle handle, WindowsOverwriteAttempt? attempt, TransferArtifactRole role)
    {
        FileObjectIdentity identity = WindowsTransferArtifactQueries.Native.ReadIdentity(handle);
        return attempt?.Receipt.Artifacts.Any(artifact => artifact is IdentifiedTransferArtifact known && known.Role == role && known.Artifact.ObjectIdentity == identity) == true;
    }

    internal string Read(FileSystemPath path)
    {
        Assert.AreEqual(Root.Path, path.Parent);
        using FileStream stream = new(path.CanonicalText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    internal void RequireUntouched(WindowsOverwriteAttempt attempt, string source, string target)
    {
        ProviderStepReceipt receipt = attempt.Receipt;
        Assert.HasCount(0, receipt.Effects);
        UntouchedTransferTarget untouched = Assert.IsInstanceOfType<UntouchedTransferTarget>(receipt.Target);
        Assert.AreEqual(Pair.Evidence.Target.Observation.Entry, untouched.Target.Entry);
        Assert.AreEqual(TransferPathObservation.LastKnownPath, untouched.Target.PathObservation);
        Assert.AreEqual(source, Read(Pair.Source.Path));
        Assert.AreEqual(target, Read(Pair.Target.Path));
        Pair.Revalidate(Pair.Evidence);
    }

    private static WindowsLocalPath Parse(string path)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(path)).Path);
    }

    public void Dispose()
    {
        Pair.Dispose();
        Root.Dispose();
    }
}
