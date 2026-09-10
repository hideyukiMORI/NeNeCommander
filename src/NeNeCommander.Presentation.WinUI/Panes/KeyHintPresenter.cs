using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Commands;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>
/// Projects the shortcut hints one focus context shows. The order and the wording of the hints are
/// declared here; the complete key-cap resource of every hint is read from the canonical key map,
/// including its modifier, so a hint can only advertise a binding the mapper actually performs
/// (KBD-005). A context that declares no projection shows no hints.
/// </summary>
public static class KeyHintPresenter
{
    /// <summary>Projects the hints of one focus context in the order the design shows them.</summary>
    /// <param name="context">Focus context the operation state imposes.</param>
    /// <returns>The ordered hints; an empty list when the context shows none.</returns>
    public static IReadOnlyList<KeyHint> Present(KeyboardContext context)
    {
        IReadOnlyList<KeyBinding> bindings = KeyboardIntentMapper.BindingsFor(context);
        List<KeyHint> hints = [];
        foreach (UserIntent intent in ResolveIntents(context))
        {
            AddHint(hints, bindings, intent);
        }
        return hints.AsReadOnly();
    }

    private static void AddHint(List<KeyHint> hints, IReadOnlyList<KeyBinding> bindings, UserIntent intent)
    {
        KeyBinding? binding = bindings.FirstOrDefault(binding => binding.Intent == intent);
        if (binding is not null)
        {
            hints.Add(new KeyHint(
                binding.KeyLabelResourceKey,
                CommandLabelCatalog.LabelFor(intent).ResourceKey));
        }
    }

    private static IReadOnlyList<UserIntent> ResolveIntents(KeyboardContext context)
    {
        return context == KeyboardContext.FileList
            ? CreateFileListLabels()
            : context == KeyboardContext.Modal ? CreateModalLabels() : [];
    }

    private static IReadOnlyList<UserIntent> CreateFileListLabels()
    {
        return
        [
            UserIntent.Rename,
            UserIntent.Copy,
            UserIntent.Move,
            UserIntent.CreateDirectory,
            UserIntent.Delete,
            UserIntent.ActivateOtherPane,
            UserIntent.ToggleHiddenItems,
            UserIntent.OpenCommandPalette,
            UserIntent.OpenBookmarks,
            UserIntent.OpenSettings,
            UserIntent.Escape,
        ];
    }

    private static IReadOnlyList<UserIntent> CreateModalLabels()
    {
        return
        [
            UserIntent.Confirm,
            UserIntent.Escape,
        ];
    }
}
