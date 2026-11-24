using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>Proves the owned display facts cannot be changed through caller storage or collection views.</summary>
[TestClass]
public sealed class WindowDisplayModelTests
{
    /// <summary>Opaque IDs retain their entire unsigned range without invented index semantics.</summary>
    [TestMethod]
    [DataRow(0UL)]
    [DataRow(ulong.MaxValue)]
    public void CreateIdentityWhenAnyOpaqueValueIsSuppliedRetainsIt(ulong value)
    {
        Assert.AreEqual(value, WindowDisplayIdentity.Create(value).Value);
    }

    /// <summary>Invalid scale observations never become usable transfer facts.</summary>
    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(0d)]
    [DataRow(-1d)]
    public void CreateDisplayWhenScaleIsInvalidRejectsIt(double scale)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WindowDisplay.Create(
            WindowTransferTestContext.Source.Identity, WindowTransferTestContext.Source.WorkArea, scale));
    }

    /// <summary>Copying, sorting and both enumeration surfaces keep the original complete observations.</summary>
    [TestMethod]
    public void CreateSnapshotWhenCallerMutatesListKeepsOwnedFactsWithoutMutableInterfaces()
    {
        List<WindowDisplay> caller = [WindowTransferTestContext.Target, WindowTransferTestContext.Source];
        WindowDisplaySnapshot snapshot = WindowDisplaySnapshot.Create(WindowTransferTestContext.Source, caller);

        caller.Clear();

        Assert.AreSame(WindowTransferTestContext.Source, snapshot.Current);
        Assert.AreSame(WindowTransferTestContext.Source, snapshot[0]);
        Assert.AreSame(WindowTransferTestContext.Target, snapshot[1]);
        Assert.HasCount(2, snapshot);
        IEnumerator enumerator = ((IEnumerable)snapshot).GetEnumerator();
        List<object> observed = [];
        while (enumerator.MoveNext())
        {
            observed.Add(enumerator.Current);
        }
        CollectionAssert.AreEqual(snapshot.ToArray(), observed.ToArray());
        Assert.IsFalse((object)snapshot is ICollection);
        Assert.IsFalse((object)snapshot is IList<WindowDisplay>);
        Assert.IsFalse((object)snapshot is ICollection<WindowDisplay>);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => snapshot[-1]);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => snapshot[2]);
    }

    /// <summary>The same ID with different facts is not a matching current observation.</summary>
    [TestMethod]
    public void CreateSnapshotWhenCurrentOrIdentitySetIsInconsistentRejectsIt()
    {
        WindowDisplay source = WindowTransferTestContext.Source;
        WindowDisplay changed = WindowDisplay.Create(source.Identity, source.WorkArea, 2d);

        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowDisplaySnapshot.Create(source, []));
        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowDisplaySnapshot.Create(source, [WindowTransferTestContext.Target]));
        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowDisplaySnapshot.Create(source, [changed]));
        _ = Assert.ThrowsExactly<ArgumentException>(() => WindowDisplaySnapshot.Create(source, [source, changed]));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => WindowDisplaySnapshot.Create(source, new WindowDisplay[1]));
    }

    /// <summary>Every public observation factory checks absent references before publishing facts.</summary>
    [TestMethod]
    public void CreateFactsWhenRequiredReferenceIsAbsentRejectsIt()
    {
        WindowDisplay source = WindowTransferTestContext.Source;
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowDisplay), "Create", [null, source.WorkArea, 1d]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowDisplay), "Create", [source.Identity, null, 1d]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowDisplaySnapshot), "Create", [null, new[] { source }]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowDisplaySnapshot), "Create", [source, null]);
        NullGuardAssertions.AssertStaticNullGuard(typeof(WindowTransferInput), "Create", [null]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(WindowTransferAvailable), [null]);
        _ = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(WindowTransferGeometry), "Create", null, [null, source.WorkArea]);
        _ = NullGuardAssertions.AssertInternalMethodNullGuard(typeof(WindowTransferGeometry), "Create", null, [source.WorkArea, null]);
    }

    /// <summary>A transfer plan and its receipt cannot carry missing or unrelated parts.</summary>
    [TestMethod]
    public void CreatePlanOrReceiptWhenPartIsAbsentOrActionIsOrdinaryRejectsIt()
    {
        WindowTransferPlan plan = WindowTransferTestContext.Plan;
        object?[] parts = [plan.Source, plan.Displays, plan.Target, plan.Geometry];
        for (int index = 0; index < parts.Length; index++)
        {
            object?[] missing = [.. parts];
            missing[index] = null;
            NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(WindowTransferPlan), missing);
        }
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(WindowTransferReported), [null, WindowTransferEffect.Observed]);
        NullGuardAssertions.AssertInternalConstructorNullGuard(typeof(WindowTransferReported), [WindowAdjustmentAction.NextDisplay, null]);
        _ = Assert.ThrowsExactly<ArgumentException>(() => new WindowTransferReported(WindowAdjustmentAction.MoveRight, WindowTransferEffect.Observed));
        WindowTransferAvailable available = Assert.IsInstanceOfType<WindowTransferAvailable>(WindowTransferInput.Create(plan.Displays));
        Assert.AreSame(plan.Displays, available.Displays);
    }
}
