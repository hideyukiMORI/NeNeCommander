using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.App.Views;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Architecture.Tests;

/// <summary>Protects the window's sole session and async-work ownership when visual parts are composed.</summary>
[TestClass]
public sealed class ViewCompositionOwnershipTests
{
    /// <summary>Proves extracted visual parts cannot acquire session or background-work ownership.</summary>
    [TestMethod]
    [DataRow("CommandPaletteView")]
    [DataRow("OperationBarView")]
    [DataRow("CommanderPaneView")]
    [DataRow("BookmarkBrowseView")]
    [DataRow("BookmarkDetailsView")]
    [DataRow("BookmarkManagerView")]
    public void VisualPartWhenComposedRetainsNoSessionOrAsyncWork(string name)
    {
        Type type = typeof(CommanderWindow).Assembly.GetType("NeNeCommander.App.Views." + name) ??
            throw new AssertFailedException("The composed view type was not found.");
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.IsFalse(fields.Any(field => field.FieldType == typeof(CommanderSession)), name);
        Assert.IsFalse(fields.Any(field => field.FieldType == typeof(AsyncWorkOwner)), name);
        Assert.IsFalse(fields.Any(field => typeof(Task).IsAssignableFrom(field.FieldType)), name);
        Assert.IsFalse(type.IsPublic, name);
    }

    /// <summary>Proves visual composition does not move excessive parameter lists into extracted constructors.</summary>
    [TestMethod]
    [DataRow("CommandPaletteView")]
    [DataRow("OperationBarView")]
    [DataRow("CommanderPaneView")]
    [DataRow("BookmarkBrowseView")]
    [DataRow("BookmarkDetailsView")]
    [DataRow("BookmarkManagerView")]
    public void VisualPartWhenCreatedStaysWithinParameterLimit(string name)
    {
        Type type = typeof(CommanderWindow).Assembly.GetType("NeNeCommander.App.Views." + name) ??
            throw new AssertFailedException("The composed view type was not found.");
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.IsTrue(constructors.All(constructor => constructor.GetParameters().Length <= 4), name);
    }
}
