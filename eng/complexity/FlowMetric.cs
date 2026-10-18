using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeNeCommander.Complexity;

internal sealed class FlowMetric : CSharpSyntaxWalker
{
    private readonly SyntaxNode _root;
    private int _depth;

    internal FlowMetric(SyntaxNode root)
    {
        _root = root;
        Visit(root);
    }

    internal int Cognitive { get; private set; }
    internal int Nesting { get; private set; }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        Cognitive += node.Parent is ElseClauseSyntax ? 1 : 1 + _depth;
        Nested(() => { Visit(node.Condition); Visit(node.Statement); });
        if (node.Else?.Statement is IfStatementSyntax next)
        {
            Visit(next);
        }
        else if (node.Else is not null)
        {
            Cognitive++;
            Nested(() => Visit(node.Else.Statement));
        }
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        Structural(() => base.VisitForStatement(node));
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        Structural(() => base.VisitForEachStatement(node));
    }

    public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        Structural(() => base.VisitForEachVariableStatement(node));
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        Structural(() => base.VisitWhileStatement(node));
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        Structural(() => base.VisitDoStatement(node));
    }

    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        Structural(() => base.VisitCatchClause(node));
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        Structural(() => base.VisitSwitchStatement(node));
    }

    public override void VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        Structural(() => base.VisitSwitchExpression(node));
    }

    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        Structural(() => base.VisitConditionalExpression(node));
    }

    public override void VisitGotoStatement(GotoStatementSyntax node)
    {
        Cognitive++;
        base.VisitGotoStatement(node);
    }

    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        Callable(node, () => base.VisitLocalFunctionStatement(node));
    }

    public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        Callable(node, () => base.VisitSimpleLambdaExpression(node));
    }

    public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        Callable(node, () => base.VisitParenthesizedLambdaExpression(node));
    }

    public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
    {
        Callable(node, () => base.VisitAnonymousMethodExpression(node));
    }

    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        Cognitive += LogicalSequences.Count(node);
        base.VisitBinaryExpression(node);
    }

    public override void VisitBinaryPattern(BinaryPatternSyntax node)
    {
        Cognitive += LogicalSequences.Count(node);
        base.VisitBinaryPattern(node);
    }

    private void Structural(Action visit)
    {
        Cognitive += 1 + _depth;
        Nested(visit);
    }

    private void Callable(SyntaxNode node, Action visit)
    {
        if (ReferenceEquals(node, _root)) { visit(); }
        else { Nested(visit); }
    }

    private void Nested(Action visit)
    {
        _depth++;
        Nesting = Math.Max(Nesting, _depth);
        visit();
        _depth--;
    }
}
