using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NeNeCommander.Complexity;

internal sealed class MemberMetric
{
    private readonly FlowMetric _flow;

    internal MemberMetric(SourceFile file, SyntaxNode node, SemanticModel model)
    {
        File = file;
        Node = node;
        Model = model;
        Symbol = CallableNodes.Symbol(model, node);
        _flow = new FlowMetric(node);
        Project = file.Project;
        Name = CallableNodes.Name(node, Symbol);
        Location = new MetricLocation(file.Path, node);
        Lines = LogicalLines.Count(node);
        Parameters = CallableNodes.Parameters(node);
        Native = NativeSignature.Classify(Symbol);
    }

    internal SourceFile File { get; }
    internal SyntaxNode Node { get; }
    internal SemanticModel Model { get; }
    internal IMethodSymbol? Symbol { get; }
    internal bool Recursive { get; set; }
    public string Project { get; }
    public string Name { get; }
    public MetricLocation Location { get; }
    public int Lines { get; }
    public int Parameters { get; }
    public int Cognitive => _flow.Cognitive + (Recursive ? 1 : 0);
    public int Nesting => _flow.Nesting;
    public string? Native { get; }

    internal IEnumerable<MetricFinding> Findings()
    {
        if (Lines > 40) { yield return new MetricFinding(Location, "method-lines", Lines, 40); }
        if (Cognitive > 10) { yield return new MetricFinding(Location, "cognitive", Cognitive, 10); }
        if (Nesting > 3) { yield return new MetricFinding(Location, "nesting", Nesting, 3); }
        if (Parameters > 4 && Native is null) { yield return new MetricFinding(Location, "parameters", Parameters, 4); }
    }
}
