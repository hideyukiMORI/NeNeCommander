using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NeNeCommander.Complexity;

internal sealed class MetricFixture
{
    private readonly IReadOnlyList<MetadataReference> _references;
    private readonly List<string> _passed = [];

    internal MetricFixture(IReadOnlyList<MetadataReference> references)
    {
        _references = references;
    }

    internal IReadOnlyList<string> Passed => _passed;

    internal MetricAnalysis Measure(params string[] sources)
    {
        SourceFile[] files = [.. sources.Select((source, index) => new SourceFile("Fixture", "fixture" + index + ".cs", source))];
        return new MetricAnalysis([new SourceProject("Fixture", [], files)], _references);
    }

    internal MemberMetric Method(string body)
    {
        return Measure("class Fixture { int Run(int x, bool a, bool b, bool c) { " + body + " } }")
            .Members.Single(member => member.Name.StartsWith("Fixture.Run(", StringComparison.Ordinal));
    }

    internal void Check(string name, Action action)
    {
        action();
        _passed.Add(name);
    }

    internal static void Equal(int expected, int actual, string label)
    {
        if (expected != actual) { throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual); }
    }

    internal static void Require(bool condition, string label)
    {
        if (!condition) { throw new InvalidOperationException(label); }
    }

    internal static void Reject(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException exception) when (exception.Message.Contains(message, StringComparison.Ordinal)) { return; }
        throw new InvalidOperationException("Expected input rejection: " + message);
    }
}
