using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace NeNeCommander.RecordClosure;

/// <summary>Runs the single compiler-driven closure check independently of application execution.</summary>
internal static class RecordClosureProgram
{
    private static int Main(string[] arguments)
    {
        if (arguments.Length < 3)
        {
            throw new ArgumentException("Expected mode, SDK reference directory, output directory and production assemblies.");
        }
        MetadataReference[] framework = [.. Directory.GetFiles(arguments[1], "*.dll").Order(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))];
        string output = Path.Combine(arguments[2], arguments[0]);
        RecordClosureCompiler compiler = new(framework, output);
        if (arguments[0] == "SelfTest")
        {
            RecordClosureSelfTest.Run(compiler, output);
            return 0;
        }
        if (arguments[0] != "Product" || arguments.Length != 7)
        {
            throw new ArgumentException("Product mode requires the four non-host production assemblies.");
        }
        MetadataReference[] products = [.. arguments.Skip(3).Select(path => MetadataReference.CreateFromFile(path))];
        RecordClosureCompiler productCompiler = new(framework.Concat(products), output);
        return VerifyProducts(productCompiler, products, output);
    }

    private static int VerifyProducts(RecordClosureCompiler compiler, MetadataReference[] products, string output)
    {
        ImmutableArray<INamedTypeSymbol> types = [.. products.SelectMany(product => compiler.Types(product))];
        INamedTypeSymbol[] roots = [.. types.Where(type => type.IsRecord && type.IsAbstract).OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal)];
        List<string> accepted = [];
        List<string> failures = [];
        foreach (INamedTypeSymbol root in roots.Where(RecordClosureProbe.Visible))
        {
            try
            {
                RecordClosureProbe.Verify(root, compiler);
                VerifyCases(root, types, compiler);
                accepted.Add(root.ToDisplayString());
            }
            catch (InvalidOperationException exception)
            {
                failures.Add(root.ToDisplayString() + ": " + exception.Message);
            }
        }
        if (accepted.Count + failures.Count == 0)
        {
            failures.Add("No externally visible abstract records were discovered.");
        }
        File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new
        {
            abstractRecords = roots.Length,
            publicRoots = accepted.Count + failures.Count,
            accepted,
            failures,
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"[CS-002] Abstract records: {roots.Length}; closed visible roots: {accepted.Count}; failures: {failures.Count}.");
        foreach (string failure in failures)
        {
            Console.Error.WriteLine(failure);
        }
        return failures.Count == 0 ? 0 : 1;
    }

    private static void VerifyCases(INamedTypeSymbol root, ImmutableArray<INamedTypeSymbol> types, RecordClosureCompiler compiler)
    {
        INamedTypeSymbol[] cases = [.. types.Where(type => !type.IsAbstract && RecordClosureProbe.DerivesFrom(type, root))];
        if (cases.Length == 0 || cases.Any(type => !type.IsSealed))
        {
            throw new InvalidOperationException("[CS-002] Every root needs known sealed concrete cases: " + root);
        }
        foreach (INamedTypeSymbol knownCase in cases.Where(RecordClosureProbe.Visible))
        {
            _ = compiler.Accept(knownCase.ToDisplayString() + ".known-copy", RecordClosureProbe.CopyUsage(knownCase));
        }
    }
}
