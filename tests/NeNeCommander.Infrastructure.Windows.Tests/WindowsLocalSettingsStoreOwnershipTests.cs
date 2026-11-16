using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Identifiers;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Execution;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves the Windows settings store keeps its owned baseline and refuses foreign or rejected documents without repair.</summary>
[TestClass]
public sealed class WindowsLocalSettingsStoreOwnershipTests
{
    private const string DocumentName = "settings.json";
    private const string TemporarySuffix = ".00000000000000000000000000000001.tmp";
    private const string SecondTemporarySuffix = ".00000000000000000000000000000002.tmp";

    /// <summary>Proves a detected existing-ancestor replacement cannot become the created baseline.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenAncestorIsReplacedAfterDirectoryCreationRejectsNewChainAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string StableParent = "stable-parent";
        const string NestedDocument = "stable-parent\\created-parent\\settings.json";
        _ = root.CreateDirectory(StableParent);
        WindowsLocalSettingsStore store = CreateStoreAt(
            root,
            NestedDocument,
            new ScriptedSettingsWriteTestHook
            {
                OnDirectoryCreated = _ =>
                {
                    Directory.Move(root.Resolve(StableParent), root.Resolve("original-parent"));
                    _ = root.CreateDirectory("stable-parent\\created-parent");
                },
            });

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.UnsafeLocation, outcome.Failure);
        Assert.AreSame(SettingsDirectoryEffect.CreationUnconfirmed, outcome.DirectoryEffect);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
        Assert.IsTrue(Directory.Exists(root.Resolve("original-parent\\created-parent")));
    }

    /// <summary>Proves the post-publish baseline detects a foreign edit before the next write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncAfterOwnedPublishRejectsForeignDocumentEditAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        const string Foreign =
            "{\"schemaVersion\":1,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"}";
        File.WriteAllText(root.Resolve(DocumentName), Foreign);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Hidden, BookmarkCatalog.Empty, PaneSortPreferences.Default),
                CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Foreign, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves identical foreign bytes cannot become the owned post-publish baseline.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenPublishedDocumentIsReplacedWithIdenticalBytesKeepsBaselineUncertainAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        bool replaceAfterPublish = true;
        WindowsLocalSettingsStore store = CreateStore(
            root,
            new ScriptedSettingsWriteTestHook
            {
                OnPublished = documentPath =>
                {
                    if (!replaceAfterPublish)
                    {
                        return;
                    }
                    replaceAfterPublish = false;
                    byte[] publishedBytes = File.ReadAllBytes(documentPath);
                    File.Move(documentPath, documentPath + ".owned");
                    File.WriteAllBytes(documentPath, publishedBytes);
                },
            });

        SettingsWriteOutcome first = await store.WriteAsync(UserSettings.Default, CancellationToken.None);
        SettingsWriteRejected second = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default),
                CancellationToken.None));

        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(first);
        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, second.Failure);
        Assert.AreSame(SettingsWriteEffect.None, second.TemporaryEffect);
        CollectionAssert.AreEqual(
            File.ReadAllBytes(root.Resolve(DocumentName + ".owned")),
            File.ReadAllBytes(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves the post-publish location baseline detects a replaced created parent.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncAfterOwnedDirectoryCreationRejectsReplacedParentAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string NestedDocument = "created-parent\\settings.json";
        WindowsLocalSettingsStore store = CreateStoreAt(root, NestedDocument);
        _ = Assert.IsInstanceOfType<SettingsWriteSucceeded>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));
        Directory.Move(root.Resolve("created-parent"), root.Resolve("owned-parent"));
        _ = root.CreateDirectory("created-parent");

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(
                UserSettings.Create(ColorScheme.Dracula, HiddenItemVisibility.Shown, BookmarkCatalog.Empty, PaneSortPreferences.Default),
                CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.DestinationChanged, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument)));
        Assert.IsFalse(File.Exists(root.Resolve(NestedDocument + TemporarySuffix)));
        Assert.IsTrue(File.Exists(root.Resolve("owned-parent\\settings.json")));
    }

    /// <summary>Proves a malformed existing document blocks mutation and remains byte-for-byte unchanged.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenExistingDocumentIsRejectedLeavesItUntouchedAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Malformed = "{\"schemaVersion\":1";
        _ = root.WriteFile(DocumentName, Malformed);
        WindowsLocalSettingsStore store = CreateStore(root);
        SettingsReadOutcome read = await store.ReadAsync(CancellationToken.None);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(
            SettingsReadFailureKind.Malformed,
            Assert.IsInstanceOfType<SettingsRejected>(read).Kind);
        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(Malformed, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a rejected startup document cannot become an implicit repair write.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenRejectedDocumentChangesAfterReadStillRefusesRepairAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string documentPath = root.WriteFile(DocumentName, "{\"schemaVersion\":1");
        WindowsLocalSettingsStore store = CreateStore(root);
        _ = Assert.IsInstanceOfType<SettingsRejected>(
            await store.ReadAsync(CancellationToken.None));
        const string ExternalRepair =
            "{\"schemaVersion\":1,\"showHiddenItems\":false,\"colorScheme\":\"nene-light\"}";
        File.WriteAllText(documentPath, ExternalRepair);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreEqual(ExternalRepair, File.ReadAllText(documentPath));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a preexisting temporary artifact is neither overwritten nor deleted.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenTemporaryPathIsOccupiedRejectsWithoutMutationAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Occupant = "owned elsewhere";
        _ = root.WriteFile(DocumentName + TemporarySuffix, Occupant);
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, identifiers);

        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.TemporaryArtifactCollision, outcome.Failure);
        Assert.AreSame(SettingsWriteEffect.None, outcome.TemporaryEffect);
        Assert.AreEqual(1, identifiers.IssuedCount);
        Assert.AreEqual(Occupant, File.ReadAllText(root.Resolve(DocumentName + TemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + SecondTemporarySuffix)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
    }

    /// <summary>Proves an invalid startup document never consumes a write candidate.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-012")]
    public async Task WriteAsyncWhenStartupDocumentIsRejectedRequestsNoIdentifierAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        const string Malformed = "{\"schemaVersion\":1";
        _ = root.WriteFile(DocumentName, Malformed);
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, identifiers);

        _ = Assert.IsInstanceOfType<SettingsRejected>(await store.ReadAsync(CancellationToken.None));
        Assert.AreEqual(0, identifiers.IssuedCount);
        SettingsWriteRejected outcome = Assert.IsInstanceOfType<SettingsWriteRejected>(
            await store.WriteAsync(UserSettings.Default, CancellationToken.None));

        Assert.AreSame(SettingsWriteFailureKind.ExistingDocumentRejected, outcome.Failure);
        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.AreEqual(Malformed, File.ReadAllText(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves a cancelled write does not request a candidate or mutate the document.</summary>
    [TestMethod]
    public async Task WriteAsyncWhenCancelledBeforePreflightRequestsNoIdentifierAsync()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        DeterministicIdentifierSource identifiers = new();
        WindowsLocalSettingsStore store = CreateStore(root, identifiers);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.WriteAsync(UserSettings.Default, cancellation.Token));

        Assert.AreEqual(0, identifiers.IssuedCount);
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName)));
        Assert.IsFalse(File.Exists(root.Resolve(DocumentName + TemporarySuffix)));
    }

    /// <summary>Proves the maximum candidate path is validated without requesting an identifier.</summary>
    [TestMethod]
    public void ConstructWhenCandidatePathWouldExceedLimitRejectsBeforeIdentifierAccess()
    {
        string documentText = "C:\\" + new string('a', 32767 - TemporarySuffix.Length - 3 + 1);
        WindowsLocalPath documentPath = Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(documentText)).Path);
        DeterministicIdentifierSource identifiers = new();

        _ = Assert.ThrowsExactly<ArgumentException>(
            () => new WindowsLocalSettingsStore(
                documentPath,
                new WindowsLocalIoExecutionBoundary(),
                identifiers));

        Assert.AreEqual(0, identifiers.IssuedCount);
    }

    private static WindowsLocalSettingsStore CreateStore(TestOwnedTemporaryRoot root)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
    }

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        ISettingsWriteTestHook writeTestHook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource(),
            writeTestHook);
    }

    private static WindowsLocalSettingsStore CreateStore(
        TestOwnedTemporaryRoot root,
        IIdentifierSource identifierSource)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, DocumentName),
            new WindowsLocalIoExecutionBoundary(),
            identifierSource);
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource());
    }

    private static WindowsLocalSettingsStore CreateStoreAt(
        TestOwnedTemporaryRoot root,
        string childName,
        ISettingsWriteTestHook writeTestHook)
    {
        return new WindowsLocalSettingsStore(
            ParseChild(root, childName),
            new WindowsLocalIoExecutionBoundary(),
            new DeterministicIdentifierSource(),
            writeTestHook);
    }

    private static WindowsLocalPath ParseChild(TestOwnedTemporaryRoot root, string childName)
    {
        return Assert.IsInstanceOfType<WindowsLocalPath>(
            Assert.IsInstanceOfType<PathParseSuccess>(
                FileSystemPath.Parse(root.Resolve(childName))).Path);
    }
}
