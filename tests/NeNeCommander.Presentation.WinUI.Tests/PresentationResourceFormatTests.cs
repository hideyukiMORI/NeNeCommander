using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Checks the shipped templates against independent argument contracts in every supported locale.</summary>
[TestClass]
public sealed class PresentationResourceFormatTests
{
    /// <summary>Each required argument is consumed and no template asks for an absent argument.</summary>
    [TestMethod]
    [DynamicData(nameof(Formats))]
    public void FormatWhenUsingShippedCatalogConsumesExactlyTheDeclaredArguments(string locale, string key, int count)
    {
        PresentationResourceCatalog catalog = new(locale);
        CompositeFormat format = CompositeFormat.Parse(catalog.Get(key));
        string[] arguments = [.. Enumerable.Range(0, count).Select(index => "argument-" + index.ToString(CultureInfo.InvariantCulture))];

        string rendered = string.Format(CultureInfo.InvariantCulture, format, arguments.Cast<object>().ToArray());

        Assert.AreEqual(count, format.MinimumArgumentCount);
        foreach (string argument in arguments)
        {
            Assert.Contains(argument, rendered);
        }
    }

    /// <summary>Declares each resource's call-site argument contract independently of its template text.</summary>
    public static IEnumerable<object[]> Formats()
    {
        (string Key, int Count)[] formats =
        [
            ("ConflictSourcePath", 1), ("ConflictTargetPath", 1), ("ConflictCandidatePath", 1),
            ("ConflictOverwriteSourceDetails", 3), ("ConflictOverwriteTargetDetails", 3),
            ("ConflictOverwriteActionName", 1), ("ConflictOverwriteScope", 1),
            ("TransferObservedCurrentPath", 1), ("TransferObservedLastKnownPath", 1),
            ("TransferTargetUntouched", 1), ("TransferTargetOriginalVerified", 1),
            ("TransferTargetMayHaveChanged", 1), ("TransferTargetDesiredVerified", 1),
            ("TransferOverwriteSourceRetained", 1), ("TransferOverwriteSourceRemoved", 1),
            ("TransferOverwriteSourcePartlyRemoved", 1), ("TransferOverwriteSourceUnconfirmed", 1),
            ("TransferArtifactObservedFormat", 4), ("TransferArtifactAttemptedFormat", 2),
            ("PaneFilterEditingHint", 2), ("CommandEntryToolTipFormat", 2),
            ("OpenFolderInRightPaneFormat", 1), ("OpenFolderInLeftPaneFormat", 1),
        ];
        foreach (string locale in new[] { "en-US", "ja-JP", "zh-Hans" })
        {
            foreach ((string key, int count) in formats)
            {
                yield return [locale, key, count];
            }
        }
    }
}
