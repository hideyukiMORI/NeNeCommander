using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeNeCommander.Complexity;

internal static class NativeSignature
{
    internal static string? Classify(IMethodSymbol? symbol)
    {
        if (symbol is null) { return null; }
        MethodDeclarationSyntax? node = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
        if (node is null) { return null; }
        string? import = Import(symbol, node);
        if (import is not null) { return import; }
        if (node.Body is null && node.ExpressionBody is null && IsNativeInterface(symbol.ContainingType)) { return "framework generated COM interface signature"; }
        return Implementation(symbol);
    }

    private static string? Import(IMethodSymbol symbol, MethodDeclarationSyntax node)
    {
        if (node.Body is not null || node.ExpressionBody is not null || !symbol.IsStatic) { return null; }
        if (symbol.IsExtern && Has(symbol, "System.Runtime.InteropServices.DllImportAttribute")) { return "framework DllImport signature"; }
        if (node.Modifiers.Any(SyntaxKind.PartialKeyword) && Has(symbol, "System.Runtime.InteropServices.LibraryImportAttribute")) { return "framework LibraryImport signature"; }
        return null;
    }

    private static string? Implementation(IMethodSymbol symbol)
    {
        if (!Has(symbol.ContainingType, "System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute")) { return null; }
        foreach (INamedTypeSymbol contract in symbol.ContainingType.AllInterfaces.Where(IsNativeInterface))
        {
            foreach (IMethodSymbol member in contract.GetMembers().OfType<IMethodSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(member), symbol))
                {
                    return "implementation of framework generated COM interface signature";
                }
            }
        }
        return null;
    }

    private static bool IsNativeInterface(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Interface &&
            Has(type, "System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute") &&
            Has(type, "System.Runtime.InteropServices.GuidAttribute");
    }

    private static bool Has(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().Any(attribute => attribute.AttributeClass is INamedTypeSymbol type &&
            type.ToDisplayString() == attributeName && type.Locations.Length != 0 && type.Locations.All(location => location.IsInMetadata));
    }
}
