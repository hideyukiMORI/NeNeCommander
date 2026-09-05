using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.App.Views;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Architecture.Tests;

/// <summary>Protects the single framework-task ownership mechanism adopted by ADR-0030.</summary>
[TestClass]
public sealed class AsyncLifecycleOwnershipTests
{
    /// <summary>Proves App framework types retain owners instead of unobserved raw tasks.</summary>
    [TestMethod]
    public void FrameworkBoundariesOwnAsyncWorkWithoutRawTaskFields()
    {
        FieldInfo[] applicationFields = FieldsOf(typeof(CommanderApplication));
        FieldInfo[] windowFields = FieldsOf(typeof(CommanderWindow));

        Assert.IsFalse(applicationFields.Any(field => typeof(Task).IsAssignableFrom(field.FieldType)));
        Assert.IsFalse(windowFields.Any(field => typeof(Task).IsAssignableFrom(field.FieldType)));
        Assert.AreEqual(2, applicationFields.Count(field => field.FieldType == typeof(AsyncWorkOwner)));
        Assert.AreEqual(1, windowFields.Count(field => field.FieldType == typeof(AsyncWorkOwner)));
    }

    private static FieldInfo[] FieldsOf(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
