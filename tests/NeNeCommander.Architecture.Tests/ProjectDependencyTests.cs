using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Time;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Architecture.Tests;

/// <summary>Proves the compiled production assembly graph points only inward.</summary>
[TestClass]
public sealed class ProjectDependencyTests
{
    /// <summary>Proves every compiled project reference matches the architecture manifest.</summary>
    [TestMethod]
    public void GetReferencedAssembliesWhenProductionAssembliesAreBuiltMatchesDeclaredGraph()
    {
        Assert.AreEqual(string.Empty, GetProjectReferences(typeof(FileSystemPath).Assembly));
        Assert.AreEqual(
            "NeNeCommander.Domain",
            GetProjectReferences(typeof(FileOperationGateway).Assembly));
        Assert.AreEqual(
            "NeNeCommander.Application,NeNeCommander.Domain",
            GetProjectReferences(typeof(StopwatchClock).Assembly));
        Assert.AreEqual(
            "NeNeCommander.Application",
            GetProjectReferences(typeof(KeyboardIntentMapper).Assembly));
        Assert.AreEqual(
            "NeNeCommander.Application,NeNeCommander.Infrastructure.Windows,NeNeCommander.Presentation.WinUI",
            GetProjectReferences(typeof(CommanderApplication).Assembly));
    }

    private static string GetProjectReferences(Assembly assembly)
    {
        return string.Join(
            ',',
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .OfType<string>()
                .Where(name => name.StartsWith("NeNeCommander.", System.StringComparison.Ordinal))
                .OrderBy(name => name, System.StringComparer.Ordinal));
    }
}
