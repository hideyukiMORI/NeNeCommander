using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Projects an existing intent's localized label and canonical shortcut into a button.</summary>
public sealed record CommandEntryButton
{
    internal CommandEntryButton(UserIntent intent, KeyBinding binding)
    {
        Intent = intent;
        LabelResourceKey = CommandLabelCatalog.LabelFor(intent).ResourceKey;
        ShortcutResourceKey = binding.KeyLabelResourceKey;
    }

    /// <summary>Gets the existing intent forwarded by this button.</summary>
    public UserIntent Intent { get; }

    /// <summary>Gets the existing localized command label.</summary>
    public string LabelResourceKey { get; }

    /// <summary>Gets the shortcut from the canonical file-list map.</summary>
    public string ShortcutResourceKey { get; }
}
