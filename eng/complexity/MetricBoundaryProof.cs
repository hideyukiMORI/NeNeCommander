using System;
using System.Linq;

namespace NeNeCommander.Complexity;

internal static class MetricBoundaryProof
{
    internal static void Run(MetricFixture fixture)
    {
        fixture.Check("method lines 40/41", () => MethodLines(fixture));
        fixture.Check("type lines 300/301", () => TypeLines(fixture));
        fixture.Check("explicit parameters 4/5 including local/lambda/delegate/indexer", () => Parameters(fixture));
        fixture.Check("comments braces strings and declaration attributes", () => TokenLines(fixture));
        fixture.Check("partial nested generic and file-local identities", () => TypeIdentity(fixture));
        fixture.Check("parse/conditional/primary/top-level refusal", () => InvalidInput(fixture));
    }

    private static void MethodLines(MetricFixture fixture)
    {
        foreach (int limit in new[] { 40, 41 })
        {
            string statements = string.Join('\n', Enumerable.Repeat("x++;", limit - 1));
            MetricAnalysis result = fixture.Measure("class F\n{\nvoid M()\n{\n" + statements + "\n}\n}");
            MetricFixture.Equal(limit, result.Members.Single().Lines, "method lines");
            MetricFixture.Equal(limit == 41 ? 1 : 0, result.Findings().Count(finding => finding.Metric == "method-lines"), "method boundary");
        }
    }

    private static void TypeLines(MetricFixture fixture)
    {
        foreach (int limit in new[] { 300, 301 })
        {
            string fields = string.Join('\n', Enumerable.Range(0, limit - 1).Select(index => "int field" + index + ";"));
            MetricAnalysis result = fixture.Measure("class F\n{\n" + fields + "\n}");
            MetricFixture.Equal(limit, result.Types.Single().Lines, "type lines");
            MetricFixture.Equal(limit == 301 ? 1 : 0, result.Findings().Count(finding => finding.Metric == "type-lines"), "type boundary");
        }
    }

    private static void Parameters(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("""
            delegate int D(int a,int b,int c,int d,int e);
            class F {
                void Four(int a,int b,int c,int d) { }
                void Five(int a,int b,int c,int d,params int[] e) { }
                int this[int a,int b,int c,int d,int e] => 0;
                void Outer() {
                    void Local(int a,int b,int c,int d,int e) { }
                    D lambda = (a,b,c,d,e) => 0;
                }
            }
            class G { int this[int a,int b,int c,int d,int e] { get { return 0; } } }
            """);
        MetricFixture.Equal(6, result.Findings().Count(finding => finding.Metric == "parameters"), "six callable parameter findings");
    }

    private static void TokenLines(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("""
            // ignored before namespace
            namespace N;
            class F
            {
                [System.Obsolete]
                void M()
                {
                    // comment { }
                    string text = "// { }";
                }
            }
            """);
        MetricFixture.Equal(3, result.Members.Single().Lines, "attribute/signature/string");
        MetricFixture.Equal(4, result.Types.Single().Lines, "type lines exclude namespace/comments/braces");
        string literal = "class F\n{\nstring M()\n{\nreturn \"\"\"\nalpha\n\n{\nomega\n\"\"\";\n}\n}";
        MetricFixture.Equal(6, fixture.Measure(literal).Members.Single().Lines, "raw literal interior lines");
    }

    private static void TypeIdentity(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure(
            "namespace N; partial class F<T> { class Nested { int a; } } file class Local { }",
            "namespace N; partial class F<T> { int b; } class F { } file class Local { }");
        MetricFixture.Equal(5, result.Types.Count, "partial merge and file-local separation");
        MetricFixture.Equal(2, result.Types.Single(type => type.Name == "N.F`1").Locations.Count, "partial declarations");
        MetricFixture.Require(result.Types.Any(type => type.Name == "N.F`1.Nested"), "nested generic identity");
    }

    private static void InvalidInput(MetricFixture fixture)
    {
        MetricFixture.Reject(() => fixture.Measure("class {"), "Invalid C#");
        MetricFixture.Reject(() => fixture.Measure("#if NEVER\ninvalid !\n#endif\nclass F {}"), "Conditional compilation");
        MetricFixture.Reject(() => fixture.Measure("class F(int value) {}"), "Primary constructors");
        MetricFixture.Reject(() => fixture.Measure("System.Console.WriteLine(1);"), "top-level");
    }
}
