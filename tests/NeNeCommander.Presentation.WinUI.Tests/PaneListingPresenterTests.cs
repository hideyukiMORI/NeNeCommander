using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Directories;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the deterministic projection of directory read outcomes onto one pane.</summary>
[TestClass]
public sealed class PaneListingPresenterTests
{
    /// <summary>Proves a complete listing shows its ordered rows and focuses the first one.</summary>
    [TestMethod]
    public void PresentWhenListingIsCompleteShowsRowsAndFocusesFirstEntry()
    {
        DirectoryListing listing = CreateListing(["b.txt", "a.txt"], DirectoryListingCompleteness.Complete, 0);

        PanePresentation presentation = PaneListingPresenter.Present(DirectoryReadOutcome.Succeeded(listing));

        PaneListingPresented presented = Assert.IsInstanceOfType<PaneListingPresented>(presentation);
        Assert.AreSame(listing, presented.Listing);
        Assert.AreSame(listing.Entries, presented.Entries);
        Assert.AreSame(listing.Entries[0], presented.FocusEntry);
        Assert.AreEqual("a.txt", presented.Entries[0].Name);
        Assert.AreSame(PaneStatus.Complete, presented.Status);
    }

    /// <summary>Proves an empty listing has no focus entry.</summary>
    [TestMethod]
    public void PresentWhenListingIsEmptyHasNoFocusEntry()
    {
        DirectoryListing listing = CreateListing([], DirectoryListingCompleteness.Complete, 0);

        PanePresentation presentation = PaneListingPresenter.Present(DirectoryReadOutcome.Succeeded(listing));

        Assert.IsNull(presentation.FocusEntry);
        Assert.IsEmpty(presentation.Entries);
        Assert.AreSame(PaneStatus.Complete, presentation.Status);
    }

    /// <summary>Proves a bounded listing reports the boundary before any omission.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-011")]
    public void PresentWhenListingIsBoundedReportsBoundedStatus()
    {
        DirectoryListing listing = CreateListing(["a.txt"], DirectoryListingCompleteness.Bounded, 2);

        PanePresentation presentation = PaneListingPresenter.Present(DirectoryReadOutcome.Succeeded(listing));

        Assert.AreSame(PaneStatus.Bounded, presentation.Status);
    }

    /// <summary>Proves omitted entries are never hidden from the status.</summary>
    [TestMethod]
    public void PresentWhenEntriesWereOmittedReportsEntriesOmittedStatus()
    {
        DirectoryListing listing = CreateListing(["a.txt"], DirectoryListingCompleteness.Complete, 1);

        PanePresentation presentation = PaneListingPresenter.Present(DirectoryReadOutcome.Succeeded(listing));

        Assert.AreSame(PaneStatus.EntriesOmitted, presentation.Status);
    }

    /// <summary>Proves cancellation clears rows and names the cancelled status.</summary>
    [TestMethod]
    public void PresentWhenReadIsCancelledHasNoRowsAndCancelledStatus()
    {
        PanePresentation presentation = PaneListingPresenter.Present(DirectoryReadOutcome.Cancelled());

        _ = Assert.IsInstanceOfType<PaneListingUnavailable>(presentation);
        Assert.IsEmpty(presentation.Entries);
        Assert.IsNull(presentation.FocusEntry);
        Assert.AreSame(PaneStatus.Cancelled, presentation.Status);
    }

    /// <summary>Proves each normalized failure maps to one closed status without a permissive default.</summary>
    [TestMethod]
    public void PresentWhenReadFailsTranslatesEachFailureKind()
    {
        AssertFailureStatus(FileOperationFailureKind.AccessDenied, PaneStatus.AccessDenied);
        AssertFailureStatus(FileOperationFailureKind.NotFound, PaneStatus.NotFound);
        AssertFailureStatus(FileOperationFailureKind.ProviderUnavailable, PaneStatus.ProviderUnavailable);
        AssertFailureStatus(FileOperationFailureKind.Copy, PaneStatus.ProviderUnavailable);
    }

    /// <summary>Proves the presenter rejects an absent outcome.</summary>
    [TestMethod]
    public void PresentWhenOutcomeIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(PaneListingPresenter).GetMethod(
            nameof(PaneListingPresenter.Present),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The present method was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    /// <summary>Proves every status names a distinct localization resource key.</summary>
    [TestMethod]
    public void ResourceKeyWhenStatusIsReadNamesExactResource()
    {
        Assert.AreEqual("PaneStatusComplete", PaneStatus.Complete.ResourceKey);
        Assert.AreEqual("PaneStatusBounded", PaneStatus.Bounded.ResourceKey);
        Assert.AreEqual("PaneStatusEntriesOmitted", PaneStatus.EntriesOmitted.ResourceKey);
        Assert.AreEqual("PaneStatusAccessDenied", PaneStatus.AccessDenied.ResourceKey);
        Assert.AreEqual("PaneStatusNotFound", PaneStatus.NotFound.ResourceKey);
        Assert.AreEqual("PaneStatusProviderUnavailable", PaneStatus.ProviderUnavailable.ResourceKey);
        Assert.AreEqual("PaneStatusCancelled", PaneStatus.Cancelled.ResourceKey);
    }

    private static void AssertFailureStatus(FileOperationFailureKind failure, PaneStatus expected)
    {
        PanePresentation presentation = PaneListingPresenter.Present(DirectoryReadOutcome.Failed(failure));

        PaneListingUnavailable unavailable = Assert.IsInstanceOfType<PaneListingUnavailable>(presentation);
        Assert.AreSame(expected, unavailable.Status);
        Assert.IsEmpty(unavailable.Entries);
    }

    private static DirectoryListing CreateListing(
        string[] names,
        DirectoryListingCompleteness completeness,
        int unrepresentableEntryCount)
    {
        FileSystemPath location = ParsePath("C:\\projects");
        DirectoryEntry[] entries = new DirectoryEntry[names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            entries[index] = DirectoryEntry.Create(
                ParsePath("C:\\projects\\" + names[index]),
                names[index],
                DirectoryEntryKind.File);
        }
        DirectoryListingCreation creation = DirectoryListing.Create(
            location,
            entries,
            completeness,
            unrepresentableEntryCount);
        return Assert.IsInstanceOfType<DirectoryListingAccepted>(creation).Listing;
    }

    private static FileSystemPath ParsePath(string input)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(input)).Path;
    }
}
