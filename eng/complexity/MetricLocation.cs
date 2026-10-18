using Microsoft.CodeAnalysis;

namespace NeNeCommander.Complexity;

internal sealed class MetricLocation
{
    internal MetricLocation(string path, SyntaxNode node)
    {
        FileLinePositionSpan span = node.GetLocation().GetLineSpan();
        Path = path;
        Line = span.StartLinePosition.Line + 1;
        Column = span.StartLinePosition.Character + 1;
    }

    public string Path { get; }
    public int Line { get; }
    public int Column { get; }
}
