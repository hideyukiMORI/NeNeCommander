using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the Windows settings store reads bounded documents and never writes.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreTests
{
    private const string DocumentName = "settings.json";

    /// <summary>Proves a complete stored document becomes the typed settings it describes.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsCompleteReturnsItsSettingsAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.WriteFile(
            DocumentName,
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"monokai\"}");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(outcome).Settings;
        Assert.AreSame(ColorScheme.Monokai, settings.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.HiddenItemVisibility);
    }

    /// <summary>Proves an absent document is reported and is never created by the read.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsAbsentReportsAbsenceWithoutWritingAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsAbsent>(outcome);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsEmpty(Directory.GetFileSystemEntries(root.Path.CanonicalText));
    }

    /// <summary>Proves a missing settings directory is absence rather than an unreadable failure.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenSettingsDirectoryIsAbsentReportsAbsenceAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = new(ParseChild(root, "absent-directory\\" + DocumentName));

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        _ = Assert.IsInstanceOfType<SettingsAbsent>(outcome);
        Assert.IsFalse(Directory.Exists(root.Resolve("absent-directory")));
    }

    /// <summary>Proves an oversized document is rejected on length and left byte-for-byte unchanged.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentExceedsBoundaryRejectsItWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string oversized = new('x', SettingsDocumentValidator.MaximumDocumentLength + 1);
        string documentPath = root.WriteFile(DocumentName, oversized);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.TooLarge,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
        Assert.AreEqual(oversized, File.ReadAllText(documentPath));
    }

    /// <summary>Proves a malformed document is rejected without repairing the stored text.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsMalformedRejectsItWithoutRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, "{\"schemaVersion\":1,\"colorScheme\":\"nene-");
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.Malformed,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
        Assert.AreEqual("{\"schemaVersion\":1,\"colorScheme\":\"nene-", File.ReadAllText(documentPath));
    }

    /// <summary>Proves an expected input failure normalizes to the unreadable rejection.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentCannotBeOpenedReturnsUnreadableAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        _ = root.CreateDirectory(DocumentName);
        WindowsLocalSettingsStore store = CreateStore(root);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
    }

    /// <summary>Proves a document held for exclusive writing is unreadable rather than empty settings.</summary>
    [TestMethod]
    public async Task ReadAsyncWhenDocumentIsLockedReturnsUnreadableAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, "{}");
        WindowsLocalSettingsStore store = CreateStore(root);
        using FileStream exclusive = new(documentPath, FileMode.Open, FileAccess.Write, FileShare.None);

        SettingsReadOutcome outcome = await store.ReadAsync(CancellationToken.None);

        Assert.AreSame(
            SettingsReadFailureKind.Unreadable,
            Assert.IsInstanceOfType<SettingsRejected>(outcome).Kind);
    }

    /// <summary>Proves the composed production location names the product settings document.</summary>
    [TestMethod]
    public void ResolveWhenCalledNamesTheProductSettingsDocument()
    {
        WindowsLocalPath location = WindowsLocalSettingsLocation.Resolve();

        Assert.EndsWith("\\NeNeCommander\\settings.json", location.CanonicalText);
    }

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(ParseChild(root, DocumentName));
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
