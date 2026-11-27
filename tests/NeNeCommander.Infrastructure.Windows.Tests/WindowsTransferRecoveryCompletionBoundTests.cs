using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the bounded completion schema fits one existing content-I/O write even at its maximum escaped identity size.</summary>
[TestClass]
public sealed class WindowsTransferRecoveryCompletionBoundTests
{
    /// <summary>Every target identity unit can expand to a six-byte JSON escape without exceeding the content transport buffer.</summary>
    [TestMethod]
    [DataRow('日')]
    [DataRow((char)1)]
    public void EncodeCompletionWhenTargetIdentityHasMaximumEscapedLengthFitsOneBoundedWrite(char value)
    {
        WindowsTransferRecoveryMapping basis = WindowsTransferRecoveryTestFixture.Mapping();
        VerifiedEntrySnapshot target = VerifiedEntrySnapshot.Create(basis.Files.Target.Path, basis.Files.Target.Identity,
            FileObjectIdentity.Create(new string(value, 512)));
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryFiles.Create(basis.Files.Source, target, basis.Files.Backup, basis.Files.Prepared);
        WindowsTransferRecoveryMapping mapping = WindowsTransferRecoveryMapping.Create(basis.Names.Token, files, basis.Original, basis.Desired);
        byte[] initial = WindowsTransferRecoveryCodec.EncodeInitial(mapping);

        byte[] completion = WindowsTransferRecoveryCodec.EncodeCompletion(initial);

        Assert.IsGreaterThan(512 * 6, completion.Length);
        Assert.IsLessThanOrEqualTo(WindowsTransferContentIo.BufferSize, completion.Length);
        WindowsTransferRecoveryCompleted decoded = Assert.IsInstanceOfType<WindowsTransferRecoveryCompleted>(
            WindowsTransferRecoveryCodec.Inspect(WindowsTransferRecoveryTestFixture.Join(initial, completion)));
        Assert.AreEqual(mapping, decoded.Mapping);
        Assert.AreEqual(WindowsTransferRecoveryCodec.Inspect(initial).Initial, decoded.Initial);
    }
}
