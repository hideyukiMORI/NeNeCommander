using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Lifecycle;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves public Presentation owners reject absent required collaborators.</summary>
[TestClass]
public sealed class NullGuardTests
{
    /// <summary>Proves each normal-close callback is required at construction.</summary>
    [TestMethod]
    public void ConstructWhenWindowShutdownCallbackIsNullThrowsArgumentNullException()
    {
        ConstructorInfo constructor = typeof(WindowShutdownOwner).GetConstructor(
            [typeof(Func<Task>), typeof(Action), typeof(Action<Exception>)]) ??
            throw new AssertFailedException("The public window shutdown constructor was not found.");
        Func<Task> drain = static () => Task.CompletedTask;
        Action close = static () => { };
        Action<Exception> observer = static _ => { };
        object?[] valid = [drain, close, observer];

        for (int index = 0; index < valid.Length; index++)
        {
            object?[] supplied = (object?[])valid.Clone();
            supplied[index] = null;
            TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
                () => constructor.Invoke(supplied));
            ArgumentNullException actual = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
            Assert.AreEqual(constructor.GetParameters()[index].Name, actual.ParamName);
        }
    }
}
