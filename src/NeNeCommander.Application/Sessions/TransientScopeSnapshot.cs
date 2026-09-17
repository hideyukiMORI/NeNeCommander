using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the immutable state of every transient scope at one snapshot. It is the one place a
/// later transient scope is added, so the session snapshot constructor keeps its arity.
/// </summary>
public sealed record TransientScopeSnapshot
{
    internal TransientScopeSnapshot(AddressEditorState addressEditor, CommandPaletteState commandPalette)
    {
        ArgumentNullException.ThrowIfNull(addressEditor);
        ArgumentNullException.ThrowIfNull(commandPalette);
        AddressEditor = addressEditor;
        CommandPalette = commandPalette;
    }

    /// <summary>Gets the address editor scope state.</summary>
    public AddressEditorState AddressEditor { get; }

    /// <summary>Gets the command palette scope state.</summary>
    public CommandPaletteState CommandPalette { get; }
}
