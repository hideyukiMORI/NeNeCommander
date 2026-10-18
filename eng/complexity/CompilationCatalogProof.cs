using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NeNeCommander.Complexity;

internal static class CompilationCatalogProof
{
    internal static void Run(MetricFixture fixture, IReadOnlyList<MetadataReference> references)
    {
        fixture.Check("transitive project symbols", () => Transitive(references));
        fixture.Check("missing cyclic and duplicate project identity refusal", () => Invalid(references));
    }

    private static void Transitive(IReadOnlyList<MetadataReference> references)
    {
        SourceProject[] projects = [
            Project("A", [], "public class A {}"),
            Project("B", ["A"], "public class B : A {}"),
            Project("C", ["B"], "public class C { public A M(B value) { return value; } }"),
        ];
        IReadOnlyDictionary<string, CSharpCompilation> compilations = CompilationCatalog.Create(projects, references);
        MetricFixture.Require(!compilations["C"].GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            "transitive dependency binds without product build");
    }

    private static void Invalid(IReadOnlyList<MetadataReference> references)
    {
        SourceProject project = Project("A", [], "class A {}");
        MetricFixture.Reject(() => CompilationCatalog.Create([project, project], references), "Duplicate project");
        MetricFixture.Reject(() => CompilationCatalog.Create([Project("A", ["Missing"], "class A {}")], references), "Missing or cyclic");
        MetricFixture.Reject(() => CompilationCatalog.Create([Project("A", ["A"], "class A {}")], references), "Missing or cyclic");
    }

    private static SourceProject Project(string name, IReadOnlyList<string> dependencies, string source)
    {
        return new SourceProject(name, dependencies, [new SourceFile(name, name + ".cs", source)]);
    }
}
