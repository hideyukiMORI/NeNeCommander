using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeNeCommander.Complexity;

internal static class LogicalSequences
{
    internal static int Count(SyntaxNode node)
    {
        if (Operator(node) == 0) { return 0; }
        SyntaxNode current = node;
        while (current.Parent is ParenthesizedExpressionSyntax or ParenthesizedPatternSyntax) { current = current.Parent; }
        if (current.Parent is not null && Operator(current.Parent) != 0) { return 0; }
        List<int> sequence = [];
        Flatten(node, sequence);
        int changes = 0;
        int previous = 0;
        foreach (int operation in sequence)
        {
            if (operation != previous) { changes++; }
            previous = operation;
        }
        return changes;
    }

    private static void Flatten(SyntaxNode node, List<int> sequence)
    {
        if (node is ParenthesizedExpressionSyntax expression) { Flatten(expression.Expression, sequence); }
        else if (node is ParenthesizedPatternSyntax pattern) { Flatten(pattern.Pattern, sequence); }
        else if (node is BinaryExpressionSyntax binary && Operator(node) != 0)
        {
            Flatten(binary.Left, sequence);
            sequence.Add(Operator(node));
            Flatten(binary.Right, sequence);
        }
        else if (node is BinaryPatternSyntax binaryPattern && Operator(node) != 0)
        {
            Flatten(binaryPattern.Left, sequence);
            sequence.Add(Operator(node));
            Flatten(binaryPattern.Right, sequence);
        }
    }

    private static int Operator(SyntaxNode node)
    {
        return node.Kind() switch
        {
            SyntaxKind.LogicalAndExpression or SyntaxKind.AndPattern => 1,
            SyntaxKind.LogicalOrExpression or SyntaxKind.OrPattern => 2,
            _ => 0,
        };
    }
}
