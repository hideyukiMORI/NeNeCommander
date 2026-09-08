using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves address state transitions produce one-time text and focus effects.</summary>
[TestClass]
public sealed class AddressEditorPresenterTests
{
    /// <summary>Proves a new edit supplies canonical text, focus, and one Select All request.</summary>
    [TestMethod]
    public void PresentWhenEditingStartsProjectsCapturedCanonicalAddress()
    {
        AddressEditing state = CreateState<AddressEditing>(PaneSide.Right, ParsePath("c:/work"));

        AddressEditorPresentation presentation = AddressEditorPresenter.Present(state);

        Assert.AreSame(state, presentation.SourceState);
        Assert.AreSame(PaneSide.Right, presentation.EditingSide);
        Assert.AreEqual("C:\\work", presentation.ReplacementText);
        Assert.IsNull(presentation.Status);
        Assert.IsNull(presentation.FileListFocusSide);
        Assert.IsTrue(presentation.SelectAll);
    }

    /// <summary>Proves a rejected snapshot restores raw text once and caches later renders.</summary>
    [TestMethod]
    public void PresentWhenInputIsRejectedKeepsExactRawTextWithoutRepeatingEffect()
    {
        const string RawText = "  relative/path  ";
        AddressInputRejected state = CreateState<AddressInputRejected>(
            PaneSide.Left,
            ParsePath("C:\\work"),
            RawText,
            PathParseFailureKind.Relative);

        AddressEditorPresentation first = AddressEditorPresenter.Present(state);
        AddressEditorPresentation repeated = AddressEditorPresenter.Present(state, first);

        Assert.AreEqual(RawText, first.ReplacementText);
        Assert.AreSame(PaneSide.Left, first.EditingSide);
        Assert.AreSame(PaneStatus.InvalidAddress, first.Status);
        PaneStatus status = Assert.IsInstanceOfType<PaneStatus>(first.Status);
        Assert.AreEqual("PaneStatusInvalidAddress", status.ResourceKey);
        Assert.IsFalse(first.SelectAll);
        Assert.AreSame(first, repeated);
    }

    /// <summary>Proves equal data in a new rejection remains a distinct render transition.</summary>
    [TestMethod]
    public void PresentWhenNewRejectionHasEqualValuesDoesNotReuseOldStateEffect()
    {
        AddressInputRejected firstState = CreateState<AddressInputRejected>(
            PaneSide.Left,
            ParsePath("C:\\work"),
            "relative",
            PathParseFailureKind.Relative);
        AddressInputRejected secondState = CreateState<AddressInputRejected>(
            PaneSide.Left,
            ParsePath("C:\\work"),
            "relative",
            PathParseFailureKind.Relative);
        AddressEditorPresentation first = AddressEditorPresenter.Present(firstState);

        AddressEditorPresentation second = AddressEditorPresenter.Present(secondState, first);

        Assert.AreNotSame(firstState, secondState);
        Assert.AreNotSame(first, second);
        Assert.AreSame(secondState, second.SourceState);
    }

    /// <summary>Proves explicit close focus and ordinary focus departure remain distinct.</summary>
    [TestMethod]
    public void PresentWhenEditorClosesProjectsOnlyCapturedFocusDisposition()
    {
        AddressEditorClosed focusing = CreateState<AddressEditorClosed>(PaneSide.Right);

        AddressEditorPresentation focused = AddressEditorPresenter.Present(focusing);
        AddressEditorPresentation departed = AddressEditorPresenter.Present(AddressEditorState.Closed);

        Assert.IsNull(focused.EditingSide);
        Assert.IsNull(focused.ReplacementText);
        Assert.AreSame(PaneSide.Right, focused.FileListFocusSide);
        Assert.IsNull(departed.FileListFocusSide);
    }

    /// <summary>Proves the public presenter rejects absent application state.</summary>
    [TestMethod]
    public void PresentWhenStateIsAbsentThrowsArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => AddressEditorPresenter.Present(null!));
    }

    private static T CreateState<T>(params object?[] arguments)
    {
        ConstructorInfo constructor = typeof(T)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate =>
                candidate.GetParameters().Length == arguments.Length &&
                (arguments.Length != 1 || candidate.GetParameters()[0].ParameterType != typeof(T)));
        return (T)constructor.Invoke(arguments);
    }

    private static FileSystemPath ParsePath(string text)
    {
        return Assert.IsInstanceOfType<PathParseSuccess>(FileSystemPath.Parse(text)).Path;
    }
}
