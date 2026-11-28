using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Checks the missing component at a transfer boundary without binding tests to exception wording.</summary>
internal static class TransferArgumentAssertions
{
    internal static void Rejects(Action action, string parameter)
    {
        ArgumentNullException failure = Assert.ThrowsExactly<ArgumentNullException>(action);
        Assert.AreEqual(parameter, failure.ParamName);
    }

    internal static async Task RejectsAsync(Func<Task> action, string parameter)
    {
        ArgumentNullException failure = await Assert.ThrowsExactlyAsync<ArgumentNullException>(action);
        Assert.AreEqual(parameter, failure.ParamName);
    }
}
