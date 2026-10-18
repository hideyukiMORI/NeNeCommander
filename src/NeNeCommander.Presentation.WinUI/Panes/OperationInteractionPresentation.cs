using System;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Captures operation-owned editor/modal projections with their imposed keyboard context.</summary>
internal sealed record OperationInteractionPresentation
{
    private OperationInteractionPresentation(
        NameEntryPresentation nameEntry,
        ConflictModalPresentation conflictModal,
        KeyboardContext inputContext)
    {
        NameEntry = nameEntry;
        ConflictModal = conflictModal;
        InputContext = inputContext;
    }

    internal NameEntryPresentation NameEntry { get; }
    internal ConflictModalPresentation ConflictModal { get; }
    internal KeyboardContext InputContext { get; }

    internal static OperationInteractionPresentation Create(
        NameEntryPresentation nameEntry,
        ConflictModalPresentation conflictModal,
        KeyboardContext inputContext)
    {
        ArgumentNullException.ThrowIfNull(nameEntry);
        ArgumentNullException.ThrowIfNull(conflictModal);
        ArgumentNullException.ThrowIfNull(inputContext);
        return new OperationInteractionPresentation(nameEntry, conflictModal, inputContext);
    }
}
