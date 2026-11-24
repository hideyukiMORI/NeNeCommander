using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves the theme picker displays localized canonical keys with its preview/apply/cancel wording.</summary>
[TestClass]
public sealed class ThemePickerKeyHintPresenterTests
{
    /// <summary>Proves visible key names come from the existing palette bindings rather than a second key map.</summary>
    [TestMethod]
    public void PresentUsesCanonicalPaletteKeysAndLocalizedThemeActions()
    {
        Assert.AreEqual(
            "<KeyLabelUp>/<KeyLabelDown> <ThemePickerHintPreview> · <KeyLabelEnter> <ThemePickerHintApply> · <KeyLabelEscape> <ThemePickerHintCancel>",
            ThemePickerKeyHintPresenter.Present(static key => "<" + key + ">"));
    }
}
