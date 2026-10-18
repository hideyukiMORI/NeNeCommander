using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NeNeCommander.RecordClosure;

/// <summary>Expresses ordinary C# derivation attempts without confusing public abstract requirements with closure.</summary>
internal static class RecordClosureProbe
{
    internal static void Verify(INamedTypeSymbol root, RecordClosureCompiler compiler)
    {
        if (root.Arity != 0 || root.ContainingType is not null)
        {
            throw new InvalidOperationException("Review probe construction for generic or nested root: " + Name(root));
        }
        string label = root.ToDisplayString();
        _ = compiler.Accept(label + ".abstract-copy", "public abstract record Probe : " + Name(root)
            + " { protected Probe(" + Name(root) + " original) : base(original) { } }");
        _ = compiler.Accept(label + ".known-copy", CopyUsage(root));
        compiler.Reject(label + ".external-copy", Concrete(root, "base(original)"), ["CS0534"]);
        compiler.Reject(label + ".external-ordinary", Concrete(root, "base()"), ["CS0534", "CS0122", "CS7036", "CS1729"]);
        compiler.Reject(label + ".external-override", Concrete(root, "base(original)") + HiddenOverrideMarker(root), ["CS0534", "CS0115"]);
    }

    internal static string CopyUsage(INamedTypeSymbol type)
    {
        return "public static class KnownCopy { public static " + Name(type) + " Copy(" + Name(type)
            + " original) { return original with { }; } }";
    }

    internal static bool Visible(INamedTypeSymbol type)
    {
        return type.DeclaredAccessibility == Accessibility.Public && (type.ContainingType is null || Visible(type.ContainingType));
    }

    internal static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol root)
    {
        for (INamedTypeSymbol? parent = type.BaseType; parent is not null; parent = parent.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(parent, root))
            {
                return true;
            }
        }
        return false;
    }

    private static string Concrete(INamedTypeSymbol root, string initializer)
    {
        StringBuilder source = new("public sealed partial record Probe : " + Name(root) + " { public Probe(" + Name(root)
            + " original) : " + initializer + " { } ");
        foreach (ISymbol member in AbstractMembers(root).Where(ExternallyImplementable))
        {
            source.Append(Override(member));
        }
        return source.Append('}').ToString();
    }

    private static string HiddenOverrideMarker(INamedTypeSymbol root)
    {
        StringBuilder source = new(" public sealed partial record Probe { ");
        ISymbol[] hidden = [.. AbstractMembers(root).Where(member => !ExternallyImplementable(member))];
        if (hidden.Length == 0)
        {
            throw new InvalidOperationException("[CS-002] No inaccessible abstract member on " + Name(root));
        }
        foreach (ISymbol member in hidden)
        {
            source.Append(Override(member));
        }
        return source.Append('}').ToString();
    }

    private static IEnumerable<ISymbol> AbstractMembers(INamedTypeSymbol root)
    {
        return root.GetMembers().Where(member => member.IsAbstract && !member.Name.StartsWith('<')
            && (member is IPropertySymbol || member is IMethodSymbol { MethodKind: MethodKind.Ordinary }));
    }

    private static bool ExternallyImplementable(ISymbol member)
    {
        return member.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
    }

    private static string Override(ISymbol member)
    {
        string access = member.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Protected or Accessibility.ProtectedOrInternal => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => throw new InvalidOperationException("Unsupported abstract access: " + member),
        };
        if (member is IPropertySymbol { IsIndexer: false, SetMethod: null } property)
        {
            return access + " override " + Name(property.Type) + " " + property.Name + " { get { throw new global::System.InvalidOperationException(); } } ";
        }
        if (member is IMethodSymbol { Arity: 0 } method && method.Parameters.All(parameter => parameter.RefKind == RefKind.None))
        {
            string parameters = string.Join(", ", method.Parameters.Select(parameter => Name(parameter.Type) + " " + parameter.Name));
            return access + " override " + Name(method.ReturnType) + " " + method.Name + "(" + parameters + ") { throw new global::System.InvalidOperationException(); } ";
        }
        throw new InvalidOperationException("Unsupported abstract member; extend the probe explicitly: " + member);
    }

    private static string Name(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
    }
}
