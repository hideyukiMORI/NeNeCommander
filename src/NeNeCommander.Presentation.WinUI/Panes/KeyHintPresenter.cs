using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Input;
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
        foreach (IntentLabel label in ResolveLabels(context))
        {
            AddHint(hints, bindings, label);
        }
        return hints.AsReadOnly();
    }

    private static void AddHint(List<KeyHint> hints, IReadOnlyList<KeyBinding> bindings, IntentLabel label)
    {
        KeyBinding? binding = bindings.FirstOrDefault(binding => binding.Intent == label.Intent);
        if (binding is not null)
        {
            hints.Add(new KeyHint(binding.KeyLabelResourceKey, label.ResourceKey));
        }
    }

    private static IReadOnlyList<IntentLabel> ResolveLabels(KeyboardContext context)
    {
        return context == KeyboardContext.FileList
            ? CreateFileListLabels()
            : context == KeyboardContext.Modal ? CreateModalLabels() : [];
    }

    private static IReadOnlyList<IntentLabel> CreateFileListLabels()
    {
        return
        [
            new(UserIntent.Rename, "IntentLabelRename"),
            new(UserIntent.Copy, "IntentLabelCopy"),
            new(UserIntent.Move, "IntentLabelMove"),
            new(UserIntent.CreateDirectory, "IntentLabelCreateDirectory"),
            new(UserIntent.Delete, "IntentLabelDelete"),
            new(UserIntent.ActivateOtherPane, "IntentLabelActivateOtherPane"),
            new(UserIntent.ToggleHiddenItems, "IntentLabelToggleHiddenItems"),
            new(UserIntent.OpenSettings, "IntentLabelOpenSettings"),
            new(UserIntent.Escape, "IntentLabelEscape"),
        ];
    }

    private static IReadOnlyList<IntentLabel> CreateModalLabels()
    {
        return
        [
            new(UserIntent.Confirm, "IntentLabelConfirm"),
            new(UserIntent.Escape, "IntentLabelEscape"),
        ];
    }

    /// <summary>One declared hint: the intent to look up in the key map and how to name it.</summary>
    private sealed record IntentLabel
    {
        internal IntentLabel(UserIntent intent, string resourceKey)
        {
            Intent = intent;
            ResourceKey = resourceKey;
        }

        internal UserIntent Intent { get; }

        internal string ResourceKey { get; }
    }
}
