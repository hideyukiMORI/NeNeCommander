using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Carries the fixed location facts of one live run: the configured root, its dedicated run child,
/// and the outer ancestors captured at fixture start (ADR-0043, ADR-0049).
/// </summary>
internal sealed record LiveWslRootAnchor
{
    private LiveWslRootAnchor(WslPath configuredRoot, WslPath runRoot, IReadOnlyList<LiveWslRootEntry> outerAncestors)
    {
        ConfiguredRoot = configuredRoot;
        RunRoot = runRoot;
        OuterAncestors = outerAncestors;
    }

    /// <summary>Gets the operator-configured root that must be empty before and after the run.</summary>
    internal WslPath ConfiguredRoot { get; }

    /// <summary>Gets the run child that the owner creates inside the configured root.</summary>
    internal WslPath RunRoot { get; }

    /// <summary>Gets the distribution root and temporary root captured at fixture start.</summary>
    internal IReadOnlyList<LiveWslRootEntry> OuterAncestors { get; }

    /// <summary>Creates the location facts of one live run.</summary>
    /// <param name="configuredRoot">Operator-configured root.</param>
    /// <param name="runRoot">Run child inside the configured root.</param>
    /// <param name="outerAncestors">Outer ancestors captured at fixture start.</param>
    /// <returns>The location facts.</returns>
    internal static LiveWslRootAnchor Create(
        WslPath configuredRoot,
        WslPath runRoot,
        IReadOnlyList<LiveWslRootEntry> outerAncestors)
    {
        return new LiveWslRootAnchor(configuredRoot, runRoot, outerAncestors);
    }
}
