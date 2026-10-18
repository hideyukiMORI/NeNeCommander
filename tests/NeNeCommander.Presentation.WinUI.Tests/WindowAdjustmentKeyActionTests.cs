using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Windowing;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves private presentation cases preserve their action mapping and record value semantics.</summary>
[TestClass]
public sealed class WindowAdjustmentKeyActionTests
{
    /// <summary>Proves an action copy retains its type, mapped Application action, equality and hash.</summary>
    [TestMethod]
    public void WithWhenActionMapsToWindowPreservesMapping()
    {
        WindowAdjustmentKeyAction original = WindowAdjustmentKeyAction.MoveLeft;

        WindowAdjustmentKeyAction copied = original with { };

        Assert.AreNotSame(original, copied);
        Assert.AreEqual(original.GetType(), copied.GetType());
        Assert.AreEqual(original, copied);
        Assert.AreEqual(original.GetHashCode(), copied.GetHashCode());
        Assert.AreSame(WindowAdjustmentAction.MoveLeft, copied.WindowAction);
        Assert.AreNotEqual(WindowAdjustmentKeyAction.MoveRight, copied);
    }

    /// <summary>Proves a leave copy retains the absence of a window action and its distinct case.</summary>
    [TestMethod]
    public void WithWhenActionLeavesModePreservesAbsence()
    {
        WindowAdjustmentKeyAction original = WindowAdjustmentKeyAction.Leave;

        WindowAdjustmentKeyAction copied = original with { };

        Assert.AreNotSame(original, copied);
        Assert.AreEqual(original, copied);
        Assert.AreEqual(original.GetHashCode(), copied.GetHashCode());
        Assert.IsNull(copied.WindowAction);
        Assert.AreNotEqual(WindowAdjustmentKeyAction.Restore, copied);
    }
}
