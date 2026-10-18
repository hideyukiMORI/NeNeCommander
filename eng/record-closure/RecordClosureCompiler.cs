using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace NeNeCommander.RecordClosure;

/// <summary>Compiles non-friend probes with the pinned compiler and retains every diagnostic.</summary>
internal sealed class RecordClosureCompiler
{
    private readonly ImmutableArray<MetadataReference> _references;
    private readonly string _output;

    internal RecordClosureCompiler(IEnumerable<MetadataReference> references, string output)
    {
        _references = [.. references];
        _output = output;
        Directory.CreateDirectory(output);
    }

    internal CSharpCompilation Compilation(string name, string source)
    {
        return CSharpCompilation.Create(name,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14))],
            _references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release, generalDiagnosticOption: ReportDiagnostic.Error,
                nullableContextOptions: NullableContextOptions.Enable, metadataImportOptions: MetadataImportOptions.All));
    }

    internal MetadataReference Accept(string name, string source)
    {
        ImmutableArray<Diagnostic> errors = Emit(name, source);
        if (!errors.IsEmpty)
        {
            throw new InvalidOperationException(name + " unexpectedly rejected: " + Describe(errors));
        }
        return MetadataReference.CreateFromFile(Path.Combine(_output, name + ".dll"));
    }

    internal void Reject(string name, string source, string[] allowedErrors)
    {
        ImmutableArray<Diagnostic> errors = Emit(name, source);
        if (errors.IsEmpty)
        {
            throw new InvalidOperationException("[CS-002] " + name + " unexpectedly accepted concrete external derivation.");
        }
        if (!errors.Any(error => error.Id == "CS0534") || errors.Any(error => !allowedErrors.Contains(error.Id, StringComparer.Ordinal)))
        {
            throw new InvalidOperationException(name + " rejected for the wrong reason: " + Describe(errors));
        }
    }

    internal RecordClosureCompiler With(MetadataReference reference)
    {
        string label = Path.GetFileNameWithoutExtension(reference.Display ?? throw new InvalidOperationException("Fixture reference has no path."));
        return new RecordClosureCompiler(_references.Add(reference), Path.Combine(_output, label));
    }

    internal ImmutableArray<INamedTypeSymbol> Types(MetadataReference reference)
    {
        IAssemblySymbol assembly = (IAssemblySymbol)(Compilation("Inspection", string.Empty)
            .GetAssemblyOrModuleSymbol(reference) ?? throw new InvalidOperationException("Assembly metadata is absent."));
        return [.. NamespaceTypes(assembly.GlobalNamespace)];
    }

    private ImmutableArray<Diagnostic> Emit(string name, string source)
    {
        string prefix = Path.Combine(_output, name);
        File.Delete(prefix + ".dll");
        File.WriteAllText(prefix + ".cs", source);
        using MemoryStream binary = new();
        EmitResult result = Compilation("NonFriend." + name, source).Emit(binary);
        File.WriteAllLines(prefix + ".diagnostics.txt", result.Diagnostics.Select(diagnostic => diagnostic.ToString()));
        if (result.Success)
        {
            File.WriteAllBytes(prefix + ".dll", binary.ToArray());
        }
        return [.. result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }

    private static IEnumerable<INamedTypeSymbol> NamespaceTypes(INamespaceSymbol space)
    {
        foreach (INamespaceSymbol child in space.GetNamespaceMembers())
        {
            foreach (INamedTypeSymbol type in NamespaceTypes(child))
            {
                yield return type;
            }
        }
        foreach (INamedTypeSymbol type in space.GetTypeMembers())
        {
            foreach (INamedTypeSymbol nested in NestedTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> NestedTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (INamedTypeSymbol child in type.GetTypeMembers())
        {
            foreach (INamedTypeSymbol nested in NestedTypes(child))
            {
                yield return nested;
            }
        }
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture)));
    }
}
