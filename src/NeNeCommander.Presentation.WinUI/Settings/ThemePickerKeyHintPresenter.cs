using System;
using System.Linq;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Settings;

/// <summary>Projects the theme palette's concise instructions from the sole palette keyboard map.</summary>
public static class ThemePickerKeyHintPresenter
{
    /// <summary>Formats localized key labels and theme actions without declaring any additional binding.</summary>
    public static string Present(Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(localize);
        string previous = KeyLabel(CommandPaletteKeyAction.MovePrevious, localize);
        string next = KeyLabel(CommandPaletteKeyAction.MoveNext, localize);
        string apply = KeyLabel(CommandPaletteKeyAction.Execute, localize);
        string cancel = KeyLabel(CommandPaletteKeyAction.Cancel, localize);
        return string.Join(" · ",
            previous + "/" + next + " " + localize("ThemePickerHintPreview"),
            apply + " " + localize("ThemePickerHintApply"),
            cancel + " " + localize("ThemePickerHintCancel"));
    }

    private static string KeyLabel(CommandPaletteKeyAction action, Func<string, string> localize)
    {
        return localize(KeyboardIntentMapper.CommandPaletteBindings.Single(binding => binding.Action == action).Key.LabelResourceKey);
    }
}
