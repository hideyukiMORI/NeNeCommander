using System.Collections.Generic;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Windowing;

/// <summary>
/// Projects the helper's key hints from the window-adjustment binding table. It shows one hint per
/// declared hint group, carrying that group's displayed caps in declaration order; an entry that
/// declares no group is an alias and contributes no cap.
/// </summary>
public static class WindowAdjustmentKeyHintPresenter
{
    /// <summary>Gets the ordered hints declared by the mode's dedicated key map.</summary>
    /// <returns>One hint per declared hint group, in declaration order.</returns>
    public static IReadOnlyList<WindowAdjustmentKeyHint> Present()
    {
        List<WindowAdjustmentHintGroup> groups = [];
        Dictionary<WindowAdjustmentHintGroup, List<string>> caps = [];
        foreach (WindowAdjustmentKeyBinding binding in KeyboardIntentMapper.WindowAdjustmentBindings)
        {
            if (binding.HintGroup is not WindowAdjustmentHintGroup group)
            {
                continue;
            }
            if (!caps.TryGetValue(group, out List<string>? declared))
            {
                declared = [];
                caps.Add(group, declared);
                groups.Add(group);
            }
            declared.Add(binding.KeyLabelResourceKey);
        }
        List<WindowAdjustmentKeyHint> hints = [];
        foreach (WindowAdjustmentHintGroup group in groups)
        {
            hints.Add(new WindowAdjustmentKeyHint(caps[group].AsReadOnly(), group.LabelResourceKey));
        }
        return hints.AsReadOnly();
    }
}
