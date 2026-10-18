using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NeNeCommander.Complexity;

internal static class CallableNodes
{
    internal static bool Contains(SyntaxNode node)
    {
        return node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or
            AccessorDeclarationSyntax or AnonymousFunctionExpressionSyntax or DelegateDeclarationSyntax ||
            node is PropertyDeclarationSyntax property && property.ExpressionBody is not null ||
            node is IndexerDeclarationSyntax;
    }

    internal static IMethodSymbol? Symbol(SemanticModel model, SyntaxNode node)
    {
        if (node is AnonymousFunctionExpressionSyntax)
        {
            return (model.GetOperation(node) as IAnonymousFunctionOperation)?.Symbol;
        }
        ISymbol? symbol = model.GetDeclaredSymbol(node);
        return symbol switch
        {
            IMethodSymbol method => method,
            IPropertySymbol property => property.GetMethod,
            INamedTypeSymbol type => type.DelegateInvokeMethod,
            _ => null,
        };
    }

    internal static int Parameters(SyntaxNode node)
    {
        return node switch
        {
            BaseMethodDeclarationSyntax method => method.ParameterList.Parameters.Count,
            LocalFunctionStatementSyntax local => local.ParameterList.Parameters.Count,
            ParenthesizedLambdaExpressionSyntax lambda => lambda.ParameterList.Parameters.Count,
            SimpleLambdaExpressionSyntax => 1,
            AnonymousMethodExpressionSyntax anonymous => anonymous.ParameterList?.Parameters.Count ?? 0,
            IndexerDeclarationSyntax indexer => indexer.ParameterList.Parameters.Count,
            DelegateDeclarationSyntax declaration => declaration.ParameterList.Parameters.Count,
            _ => 0,
        };
    }

    internal static string Name(SyntaxNode node, IMethodSymbol? symbol)
    {
        if (node is AnonymousFunctionExpressionSyntax) { return "anonymous@" + node.SpanStart; }
        return symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ??
            node.GetType().Name + "@" + node.SpanStart;
    }
}
