using Microsoft.CodeAnalysis;

namespace NeNeCommander.Complexity;

internal sealed class TypePart
{
    internal TypePart(SourceFile file, SyntaxNode node, INamedTypeSymbol symbol)
    {
        Project = file.Project;
        Name = Identity(symbol);
        Location = new MetricLocation(file.Path, node);
        Lines = LogicalLines.Count(node);
    }

    internal string Project { get; }
    internal string Name { get; }
    internal MetricLocation Location { get; }
    internal int Lines { get; }

    private static string Identity(INamedTypeSymbol symbol)
    {
        string owner = symbol.ContainingType is null
            ? symbol.ContainingNamespace.ToDisplayString()
            : Identity(symbol.ContainingType);
        return owner + "." + symbol.MetadataName;
    }
}
