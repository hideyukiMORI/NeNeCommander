using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>
/// Carries the owners of every transient scope the application session coordinates. It is the one
/// place a later transient scope is added, so the session constructor keeps its arity.
/// </summary>
public sealed record TransientScopeOwners
{
    /// <summary>Initializes the set of transient scope owners the composition root created.</summary>
    /// <param name="addressEditor">Sole address editor scope owner.</param>
    /// <param name="commandPalette">Sole command palette scope owner.</param>
    public TransientScopeOwners(AddressEditorSession addressEditor, CommandPaletteSession commandPalette)
    {
        ArgumentNullException.ThrowIfNull(addressEditor);
        ArgumentNullException.ThrowIfNull(commandPalette);
        AddressEditor = addressEditor;
        CommandPalette = commandPalette;
    }

    /// <summary>Gets the sole address editor scope owner.</summary>
    public AddressEditorSession AddressEditor { get; }

    /// <summary>Gets the sole command palette scope owner.</summary>
    public CommandPaletteSession CommandPalette { get; }
}
