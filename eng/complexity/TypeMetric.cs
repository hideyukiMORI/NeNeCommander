using System.Collections.Generic;
using System.Linq;

namespace NeNeCommander.Complexity;

internal sealed class TypeMetric
{
    internal TypeMetric(string project, string name, IReadOnlyList<TypePart> parts)
    {
        Project = project;
        Name = name;
        Lines = parts.Sum(part => part.Lines);
        Locations = [.. parts.Select(part => part.Location)];
    }

    public string Project { get; }
    public string Name { get; }
    public int Lines { get; }
    public IReadOnlyList<MetricLocation> Locations { get; }

    internal IEnumerable<MetricFinding> Findings()
    {
        if (Lines > 300) { yield return new MetricFinding(Locations[0], "type-lines", Lines, 300); }
    }
}
