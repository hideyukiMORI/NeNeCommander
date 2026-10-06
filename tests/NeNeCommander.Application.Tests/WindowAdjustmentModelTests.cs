using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Application.Windowing;

namespace NeNeCommander.Application.Tests;

/// <summary>
/// Proves the window-adjustment outcome, refusal, action, plan, decision, and state models are
/// closed: no type outside Application can extend them and every published value is distinct.
/// </summary>
[TestClass]
public sealed class WindowAdjustmentModelTests
{
    /// <summary>Proves the refusal model holds exactly the eight declared reasons, each one distinct closed value.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public void RefusalWhenEnumeratedIsExactlyTheEightDeclaredReasons()
    {
        WindowAdjustmentRefusal[] reasons =
        [
            WindowAdjustmentRefusal.CaptionWouldLeaveDesktop,
            WindowAdjustmentRefusal.AtMaximumSize,
            WindowAdjustmentRefusal.AtMinimumSize,
            WindowAdjustmentRefusal.WindowIsMaximized,
            WindowAdjustmentRefusal.WindowIsMinimized,
            WindowAdjustmentRefusal.WindowIsRestored,
            WindowAdjustmentRefusal.PresenterIsNotOverlapped,
            WindowAdjustmentRefusal.PlacementUnavailable,
        ];

        AssertClosedValueSet(typeof(WindowAdjustmentRefusal), reasons);
    }

    /// <summary>Proves the action and presenter-state models hold exactly their declared values.</summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public void ActionAndPresenterStateWhenEnumeratedAreExactlyTheDeclaredValues()
    {
        AssertClosedValueSet(typeof(WindowAdjustmentAction), WindowAdjustmentPlannerTests.AllActions());
        AssertClosedValueSet(
            typeof(WindowPresenterState),
            [
                WindowPresenterState.Restored,
                WindowPresenterState.Maximized,
                WindowPresenterState.Minimized,
                WindowPresenterState.NotOverlapped,
                WindowPresenterState.Unavailable,
            ]);
    }

    /// <summary>
    /// Proves the outcome model is exactly none, planned, or refused, and that the plan, decision,
    /// and state models have exactly their declared cases, so a consumer's switch is exhaustive.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    public void OutcomePlanDecisionAndStateWhenEnumeratedHaveOnlyTheirDeclaredCases()
    {
        AssertCases(
            typeof(WindowAdjustmentOutcome),
            [WindowAdjustmentOutcome.None.GetType(), typeof(WindowActionPlanned), typeof(WindowActionRefused)]);
        AssertCases(
            typeof(WindowAdjustmentPlan),
            [
                WindowAdjustmentPlan.Maximize.GetType(),
                WindowAdjustmentPlan.Restore.GetType(),
                typeof(WindowMovePlan),
                typeof(WindowResizePlan),
                typeof(WindowRefusedPlan),
            ]);
        AssertCases(
            typeof(WindowAdjustmentDecision),
            [WindowAdjustmentDecision.NothingToApply.GetType(), typeof(WindowAdjustmentPlanned)]);
        AssertCases(
            typeof(WindowAdjustmentState),
            [typeof(WindowAdjustmentClosed), typeof(WindowAdjustmentOpen)]);
        Assert.AreNotSame(WindowAdjustmentPlan.Maximize, WindowAdjustmentPlan.Restore);
        Assert.AreNotEqual(WindowAdjustmentPlan.Maximize, WindowAdjustmentPlan.Restore);
    }

    private static void AssertClosedValueSet<T>(Type root, IReadOnlyList<T> values)
        where T : class
    {
        Type[] cases = [.. values.Select(value => value.GetType())];
        AssertCases(root, cases);
        Assert.HasCount(values.Count, values.Distinct());
        PropertyInfo[] published = root.GetProperties(BindingFlags.Public | BindingFlags.Static);
        Assert.HasCount(values.Count, published);
        foreach (PropertyInfo property in published)
        {
            Assert.Contains((T)property.GetValue(null)!, values);
        }
    }

    private static void AssertCases(Type root, Type[] expected)
    {
        Assert.IsTrue(root.IsAbstract);
        foreach (ConstructorInfo constructor in root.GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(
                constructor => !IsRecordCopyConstructor(root, constructor)))
        {
            Assert.IsFalse(constructor.IsPublic || constructor.IsFamily || constructor.IsFamilyOrAssembly);
        }
        Type[] derived =
        [
            .. root.Assembly.GetTypes().Where(type => type != root && root.IsAssignableFrom(type)),
        ];
        Assert.HasCount(expected.Length, derived);
        Assert.HasCount(expected.Length, expected.Distinct());
        foreach (Type type in derived)
        {
            Assert.IsTrue(type.IsSealed, type.Name + " must be sealed.");
            Assert.Contains(type, expected);
        }
    }

    /// <summary>
    /// The compiler gives every abstract record a protected copy constructor; only the declared
    /// constructors decide who may create a case, so the copy constructor is not one of them.
    /// </summary>
    private static bool IsRecordCopyConstructor(Type root, ConstructorInfo constructor)
    {
        ParameterInfo[] parameters = constructor.GetParameters();
        return parameters.Length == 1 && parameters[0].ParameterType == root;
    }
}
