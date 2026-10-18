using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NeNeCommander.Complexity;

internal static class LogicalLines
{
    internal static int Count(SyntaxNode node)
    {
        HashSet<int> lines = [];
        SourceText source = node.SyntaxTree.GetText();
        foreach (SyntaxToken token in node.DescendantTokens())
        {
            if (token.IsMissing || token.Span.IsEmpty || token.IsKind(SyntaxKind.OpenBraceToken) || token.IsKind(SyntaxKind.CloseBraceToken)) { continue; }
            int start = source.Lines.GetLineFromPosition(token.SpanStart).LineNumber;
            int end = source.Lines.GetLineFromPosition(token.Span.End - 1).LineNumber;
            for (int line = start; line <= end; line++)
            {
                if (!string.IsNullOrWhiteSpace(source.Lines[line].ToString())) { _ = lines.Add(line); }
            }
        }
        return lines.Count;
    }
}
