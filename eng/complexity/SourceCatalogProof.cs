using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NeNeCommander.Complexity;

internal static class SourceCatalogProof
{
    internal static void Run(MetricFixture fixture, string output)
    {
        string root = Path.Join(output, "source-fixture");
        Prepare(root);
        fixture.Check("working-tree ignored and untracked source included", () => Included(root));
        fixture.Check("missing project and empty project refused", () => Missing(root));
        fixture.Check("unknown project source refused", () => Unknown(root));
        fixture.Check("rooted and escaping project paths refused", () => Paths(root));
        fixture.Check("deterministic metric ordering", () => Determinism(fixture));
    }

    private static void Prepare(string root)
    {
        Directory.CreateDirectory(Path.Join(root, "src", "P"));
        Directory.CreateDirectory(Path.Join(root, "tests", "T"));
        Directory.CreateDirectory(Path.Join(root, "eng"));
        File.WriteAllText(Path.Join(root, "src", "P", "P.csproj"), "<Project />");
        File.WriteAllText(Path.Join(root, "tests", "T", "T.csproj"), "<Project />");
        File.WriteAllText(Path.Join(root, "src", "P", "Fresh.cs"), "class Product { }");
        File.WriteAllText(Path.Join(root, "tests", "T", "Test.cs"), "class Tests { }");
        File.WriteAllText(Path.Join(root, "eng", "Tool.cs"), "class Tool { }");
        File.WriteAllText(Path.Join(root, ".gitignore"), "*.cs");
        File.WriteAllText(Path.Join(root, "eng", "architecture.json"), """
            {"projects":[{"name":"P","path":"src/P/P.csproj","projectReferences":[]},{"name":"T","path":"tests/T/T.csproj","projectReferences":["P"]}]}
            """);
    }

    private static void Included(string root)
    {
        Directory.CreateDirectory(Path.Join(root, "src", "P", "obj"));
        File.WriteAllText(Path.Join(root, "src", "P", "obj", "Generated.cs"), "invalid generated code !");
        MetricFixture.Equal(3, SourceCatalog.Read(root).SelectMany(project => project.Files).Count(), "all handwritten files, no generated output");
    }

    private static void Missing(string root)
    {
        string project = Path.Join(root, "src", "P", "P.csproj");
        File.Move(project, project + ".saved", true);
        MetricFixture.Reject(() => SourceCatalog.Read(root), "Missing project");
        File.Move(project + ".saved", project, true);
        string source = Path.Join(root, "src", "P", "Fresh.cs");
        File.Move(source, source + ".saved", true);
        MetricFixture.Reject(() => SourceCatalog.Read(root), "Empty project");
        File.Move(source + ".saved", source, true);
    }

    private static void Unknown(string root)
    {
        string source = Path.Join(root, "src", "Unassigned.cs");
        File.WriteAllText(source, "class Unknown {}");
        MetricFixture.Reject(() => SourceCatalog.Read(root), "Unassigned source");
        File.Move(source, source + ".saved", true);
        string unknown = Path.Join(root, "src", "Unknown", "bin");
        Directory.CreateDirectory(unknown);
        string hidden = Path.Join(unknown, "Hidden.cs");
        File.WriteAllText(hidden, "class Hidden {}");
        MetricFixture.Reject(() => SourceCatalog.Read(root), "Unassigned source");
        File.Move(hidden, hidden + ".saved", true);
    }

    private static void Determinism(MetricFixture fixture)
    {
        const string Source = "partial class F { void B() {} void A() {} }";
        MetricAnalysis first = fixture.Measure(Source);
        MetricAnalysis second = fixture.Measure(Source);
        MetricFixture.Require(JsonSerializer.Serialize(first.Members) == JsonSerializer.Serialize(second.Members), "member determinism");
        MetricFixture.Require(JsonSerializer.Serialize(first.Types) == JsonSerializer.Serialize(second.Types), "type determinism");
    }

    private static void Paths(string root)
    {
        string manifest = Path.Join(root, "eng", "architecture.json");
        string original = File.ReadAllText(manifest);
        try
        {
            RejectPath(root, Path.GetFullPath(Path.Join(root, "src", "P", "P.csproj")), "must be relative");
            RejectPath(root, "Z:src/P/P.csproj", "must be relative");
            RejectPath(root, "src/../../source-fixture-other/P.csproj", "escapes the repository");
            RejectPath(root, "eng/Tool.cs", "outside src/tests");
        }
        finally
        {
            File.WriteAllText(manifest, original);
        }
    }

    private static void RejectPath(string root, string relative, string message)
    {
        File.WriteAllText(Path.Join(root, "eng", "architecture.json"), JsonSerializer.Serialize(new
        {
            projects = new[] { new { name = "P", path = relative, projectReferences = Array.Empty<string>() } },
        }));
        MetricFixture.Reject(() => SourceCatalog.Read(root), message);
    }
}
