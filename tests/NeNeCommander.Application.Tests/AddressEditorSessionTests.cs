using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Sessions;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the address editor scope owner answers every intent from the state it owns.</summary>
[TestClass]
public sealed class AddressEditorSessionTests
{
    /// <summary>
    /// Proves validation is total over the closed scope. The session validates precedence under its
    /// own read, so a closed scope can still receive an escape, a departure, or a submission
    /// qualified by the shared closed state; each keeps the closed state and navigates nothing.
    /// </summary>
    [TestMethod]
    public void ValidateWhenScopeIsClosedKeepsClosedStateAndNavigatesNothing()
    {
        AddressEditorSession owner = new();

        AddressEditorValidation escaped = owner.Validate(
            UserIntent.Escape,
            InteractionOwnership.ScopeOwnsInput);
        AddressEditorValidation departed = owner.Validate(
            UserIntent.LeaveAddress(AddressEditorState.Closed),
            InteractionOwnership.ScopeOwnsInput);
        AddressEditorValidation submitted = owner.Validate(
            UserIntent.SubmitAddress(AddressEditorState.Closed, "C:\\target"),
            InteractionOwnership.ScopeOwnsInput);
        AddressEditorValidation unrelated = owner.Validate(
            UserIntent.MoveNext,
            InteractionOwnership.AnotherScopeOwnsInput);

        Assert.AreSame(AddressEditorValidation.NothingToNavigate, escaped);
        Assert.AreSame(AddressEditorValidation.NothingToNavigate, departed);
        Assert.AreSame(AddressEditorValidation.NothingToNavigate, submitted);
        Assert.AreSame(AddressEditorValidation.NothingToNavigate, unrelated);
        Assert.AreSame(AddressEditorState.Closed, owner.Current);
    }
}
