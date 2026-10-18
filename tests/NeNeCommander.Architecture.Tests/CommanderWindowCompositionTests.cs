using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.App.Views;

namespace NeNeCommander.Architecture.Tests;

/// <summary>Protects the focused CS-013 parameter boundary of window composition.</summary>
[TestClass]
public sealed class CommanderWindowCompositionTests
{
    /// <summary>Proves the window's sole construction path stays within the four-parameter ceiling.</summary>
    [TestMethod]
    public void ConstructorWhenWindowIsComposedStaysWithinParameterLimit()
    {
        ConstructorInfo constructor = typeof(CommanderWindow).GetConstructors().Single();

        Assert.IsTrue(constructor.GetParameters().Length <= 4, "Window construction exceeds the CS-013 four-parameter ceiling.");
    }

    /// <summary>Proves dependency aggregation does not move an excessive parameter list into its construction path.</summary>
    [TestMethod]
    public void DependenciesWhenCreatedStayWithinParameterLimit()
    {
        MethodInfo factory = typeof(CommanderWindowDependencies).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(CommanderWindowDependencies.Create));
        ConstructorInfo[] constructors = typeof(CommanderWindowDependencies).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsTrue(factory.GetParameters().Length <= 4, "Dependency creation exceeds the CS-013 four-parameter ceiling.");
        Assert.IsTrue(constructors.All(constructor => constructor.GetParameters().Length <= 4),
            "A dependency constructor exceeds the CS-013 four-parameter ceiling.");
        Assert.AreEqual(0, typeof(CommanderWindowDependencies).GetConstructors().Length);
    }
}
