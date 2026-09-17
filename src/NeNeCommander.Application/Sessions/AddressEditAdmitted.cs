using System;

namespace NeNeCommander.Application.Sessions;

/// <summary>Carries the editor captured from the pane snapshot read before any activation effect.</summary>
public sealed record AddressEditAdmitted : AddressEditAdmission
{
    internal AddressEditAdmitted(AddressEditing editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        Editor = editor;
    }

    /// <summary>Gets the captured editing state the owner opens once activation has completed.</summary>
    public AddressEditing Editor { get; }
}
