using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal static class WindowsTransferRecoveryTestFixture
{
    internal static Guid Token { get; } = Guid.ParseExact("0123456789abcdef0123456789abcdef", "N");

    internal static WindowsTransferRecoveryMapping Mapping(string source = @"D:\recovery\source.txt", string target = @"D:\recovery\target.txt")
    {
        WindowsLocalPath destination = Path(target);
        WindowsTransferArtifactNames names = WindowsTransferArtifactNames.Create(destination, Token);
        WindowsTransferRecoveryFiles files = WindowsTransferRecoveryFiles.Create(Entry(Path(source), "source"), Entry(destination, "target"),
            Entry(names.OriginalBackup, "backup"), Entry(names.PreparedContent, "prepared"));
        return WindowsTransferRecoveryMapping.Create(Token, files, Content(3, 'A'), Content(5, 'B'));
    }

    internal static VerifiedEntrySnapshot Entry(FileSystemPath path, string identity)
    {
        FileIdentity full = Assert.IsInstanceOfType<FileIdentityAccepted>(FileIdentity.Parse("full-" + identity)).Identity;
        return VerifiedEntrySnapshot.Create(path, full, FileObjectIdentity.Create("object-" + identity));
    }

    internal static FileContentEvidence Content(long length, char digest)
    {
        return FileContentEvidence.Create((KnownEntrySize)EntrySize.Create(length), FileContentHash.Create(new string(digest, 64)));
    }

    internal static WindowsLocalPath Path(string text)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path);
    }

    internal static byte[] Join(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        byte[] joined = new byte[first.Length + second.Length];
        first.CopyTo(joined);
        second.CopyTo(joined.AsSpan(first.Length));
        return joined;
    }

    internal static byte[] JsonFrame(string json)
    {
        return WindowsTransferRecoveryFrame.Wrap(Encoding.UTF8.GetBytes(json));
    }
}
