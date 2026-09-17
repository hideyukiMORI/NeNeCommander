using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Represents the immutable state of every transient scope at one snapshot. It is the one place a
/// later transient scope is added, so the session snapshot constructor keeps its arity.
/// </summary>
public sealed record TransientScopeSnapshot
{
    internal TransientScopeSnapshot(
        AddressEditorState addressEditor,
        CommandPaletteState commandPalette,
        WindowAdjustmentState windowAdjustment)
    {
        ArgumentNullException.ThrowIfNull(addressEditor);
        ArgumentNullException.ThrowIfNull(commandPalette);
        ArgumentNullException.ThrowIfNull(windowAdjustment);
        AddressEditor = addressEditor;
        CommandPalette = commandPalette;
        WindowAdjustment = windowAdjustment;
    }

    /// <summary>Gets the address editor scope state.</summary>
    public AddressEditorState AddressEditor { get; }

    /// <summary>Gets the command palette scope state.</summary>
    public CommandPaletteState CommandPalette { get; }

    /// <summary>Gets the window adjustment scope state.</summary>
    public WindowAdjustmentState WindowAdjustment { get; }
}
