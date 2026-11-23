using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Settings;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Proves version-six placement is part of the strict complete settings document.</summary>
[TestClass]
public sealed class WindowPlacementDocumentTests
{
    /// <summary>Proves absent, normal and maximized placements are admitted in the new schema.</summary>
    [TestMethod]
    [DataRow("null")]
    [DataRow("{\"left\":-1500,\"top\":20,\"width\":1000,\"height\":700,\"state\":\"restored\"}")]
    [DataRow("{\"left\":200,\"top\":30,\"width\":900,\"height\":650,\"state\":\"maximized\"}")]
    public void ValidateWhenVersionSixPlacementIsValidAcceptsCompleteDocument(string placement)
    {
        _ = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(Document(placement)));
    }

    /// <summary>Proves valid physical coordinates and both startup states survive exact serialization.</summary>
    [TestMethod]
    [DataRow("restored")]
    [DataRow("maximized")]
    public void ValidateWhenPlacementExistsKeepsEveryValueAndOtherPreferences(string state)
    {
        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(
            Document("{\"left\":-1500,\"top\":20,\"width\":1000,\"height\":700,\"state\":\"" + state + "\"}"))).Settings;
        SavedWindowPlacement placement = Assert.IsInstanceOfType<SavedWindowPlacement>(settings.WindowPlacement);

        Assert.AreEqual(WindowBounds.Create(-1500, 20, 1000, 700), placement.Bounds);
        Assert.AreSame(state == "restored" ? WindowPresenterState.Restored : WindowPresenterState.Maximized, placement.State);
        Assert.AreSame(InterfaceLanguage.Japanese, settings.Display.InterfaceLanguage);
        Assert.AreSame(ColorScheme.Ubuntu, settings.Display.ColorScheme);
        Assert.AreSame(HiddenItemVisibility.Shown, settings.Display.HiddenItemVisibility);
        Assert.AreSame(KeyHintVisibility.Hidden, settings.Display.KeyHintVisibility);
        byte[] serialized = SettingsDocumentSerializer.Serialize(settings);
        UserSettings roundTrip = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(Encoding.UTF8.GetString(serialized))).Settings;
        Assert.AreEqual(settings.Display, roundTrip.Display);
        Assert.AreEqual(settings.PaneSortOrders, roundTrip.PaneSortOrders);
        Assert.AreEqual(settings.WindowPlacement, roundTrip.WindowPlacement);
        CollectionAssert.AreEqual(serialized, SettingsDocumentSerializer.Serialize(roundTrip));
    }

    /// <summary>Proves hostile numeric and state values reject the complete document.</summary>
    [TestMethod]
    [TestProperty("ThreatId", "ADV-012")]
    [TestCategory("Adversarial")]
    [DataRow("left", "2147483648")]
    [DataRow("top", "-2147483649")]
    [DataRow("left", "1.5")]
    [DataRow("width", "0")]
    [DataRow("height", "-1")]
    [DataRow("width", "1e300")]
    [DataRow("height", "\"700\"")]
    [DataRow("left", "null")]
    [DataRow("top", "true")]
    [DataRow("width", "{}")]
    [DataRow("height", "[]")]
    [DataRow("state", "0")]
    [DataRow("state", "null")]
    [DataRow("state", "\"minimized\"")]
    [DataRow("state", "\"RESTORED\"")]
    [DataRow("state", "\" restored\"")]
    public void ValidateWhenPlacementValueIsInvalidRejectsWholeDocument(string property, string value)
    {
        string left = property == "left" ? value : "-1500";
        string top = property == "top" ? value : "20";
        string width = property == "width" ? value : "1000";
        string height = property == "height" ? value : "700";
        string state = property == "state" ? value : "\"restored\"";
        string placement = "{\"left\":" + left + ",\"top\":" + top + ",\"width\":" + width +
            ",\"height\":" + height + ",\"state\":" + state + "}";

        SettingsRejected rejection = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(Document(placement)));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejection.Kind);
    }

    /// <summary>Proves placement has only an object or explicit absent representation.</summary>
    [TestMethod]
    [DataRow("0")]
    [DataRow("true")]
    [DataRow("[]")]
    [DataRow("\"restored\"")]
    public void ValidateWhenPlacementHasWrongContainerRejectsMalformed(string placement)
    {
        SettingsRejected rejection = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(Document(placement)));

        Assert.AreSame(SettingsReadFailureKind.Malformed, rejection.Kind);
    }

    /// <summary>Proves missing, repeated and unexpected placement properties are rejected before value interpretation.</summary>
    [TestMethod]
    public void ValidateWhenPlacementShapeIsInvalidUsesExactPropertyBoundary()
    {
        const string valid = "{\"left\":0,\"top\":0,\"width\":1000,\"height\":700,\"state\":\"restored\"}";
        SettingsRejected missing = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(
            Document(valid.Replace("\"top\":0,", string.Empty, StringComparison.Ordinal))));
        SettingsRejected duplicate = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(
            Document(valid.Replace("\"top\":0,", "\"top\":0,\"top\":0,", StringComparison.Ordinal))));
        SettingsRejected unknown = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(
            Document(valid[..^1] + ",\"extra\":0}")));
        SettingsRejected missingRoot = Assert.IsInstanceOfType<SettingsRejected>(SettingsDocumentValidator.Validate(
            Document("null").Replace(",\"windowPlacement\":null", string.Empty, StringComparison.Ordinal)));

        Assert.AreSame(SettingsReadFailureKind.Incomplete, missing.Kind);
        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, duplicate.Kind);
        Assert.AreSame(SettingsReadFailureKind.UnexpectedProperty, unknown.Kind);
        Assert.AreSame(SettingsReadFailureKind.Incomplete, missingRoot.Kind);
    }

    /// <summary>Proves extreme but valid Int32 inputs remain available to the overflow-safe restore planner.</summary>
    [TestMethod]
    public void ValidateWhenCoordinatesAreExtremeKeepsTheirExactIntegerValues()
    {
        UserSettings settings = Assert.IsInstanceOfType<SettingsRead>(SettingsDocumentValidator.Validate(Document(
            "{\"left\":2147483647,\"top\":-2147483648,\"width\":2147483647,\"height\":1,\"state\":\"restored\"}"))).Settings;
        SavedWindowPlacement placement = Assert.IsInstanceOfType<SavedWindowPlacement>(settings.WindowPlacement);

        Assert.AreEqual(int.MaxValue, placement.Bounds.Left);
        Assert.AreEqual(int.MinValue, placement.Bounds.Top);
        Assert.AreEqual(int.MaxValue, placement.Bounds.Width);
        Assert.AreEqual(1, placement.Bounds.Height);
    }

    private static string Document(string placement)
    {
        const string order = "{\"key\":\"name\",\"direction\":\"ascending\"}";
        return "{\"schemaVersion\":6,\"showHiddenItems\":true,\"colorScheme\":\"ubuntu\"," +
            "\"bookmarkCategories\":[],\"bookmarks\":[],\"paneSortOrders\":{\"left\":" + order + ",\"right\":" + order + "}," +
            "\"showKeyHints\":false,\"uiLanguage\":\"ja-JP\",\"windowPlacement\":" + placement + "}";
    }
}
