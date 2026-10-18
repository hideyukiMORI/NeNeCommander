using System.Linq;

namespace NeNeCommander.Complexity;

internal static class FlowMetricProof
{
    internal static void Run(MetricFixture fixture)
    {
        fixture.Check("cognitive boundary 10/11", () => CognitiveBoundary(fixture));
        fixture.Check("nesting boundary 3/4", () => NestingBoundary(fixture));
        fixture.Check("if else-if else weights", () => Score(fixture, "if(a) {} else if(b) {} else {} return x;", 3, 1));
        fixture.Check("logical runs parentheses and negation", () => Logical(fixture));
        fixture.Check("switch catch ternary and jump weights", () => Constructs(fixture));
        fixture.Check("nested local lambda and anonymous contexts", () => NestedCallables(fixture));
        fixture.Check("direct indirect recursion and overload separation", () => Recursion(fixture));
        fixture.Check("partial cross-file recursion order", () => PartialRecursion(fixture));
    }

    private static void CognitiveBoundary(MetricFixture fixture)
    {
        foreach (int score in new[] { 10, 11 })
        {
            MemberMetric member = fixture.Method(string.Concat(Enumerable.Repeat("if(a) { x++; }", score)) + "return x;");
            MetricFixture.Equal(score, member.Cognitive, "cognitive boundary value");
            MetricFixture.Equal(score == 11 ? 1 : 0, member.Findings().Count(finding => finding.Metric == "cognitive"), "cognitive finding");
        }
    }

    private static void NestingBoundary(MetricFixture fixture)
    {
        foreach (int depth in new[] { 3, 4 })
        {
            MemberMetric member = fixture.Method(string.Concat(Enumerable.Repeat("if(a) {", depth)) + "x++;" + new string('}', depth) + "return x;");
            MetricFixture.Equal(depth, member.Nesting, "nesting boundary value");
            MetricFixture.Equal(depth * (depth + 1) / 2, member.Cognitive, "nested weighting");
            MetricFixture.Equal(depth == 4 ? 1 : 0, member.Findings().Count(finding => finding.Metric == "nesting"), "nesting finding");
        }
    }

    private static void Logical(MetricFixture fixture)
    {
        Score(fixture, "bool y = a && (b && c); return x;", 1, 0);
        Score(fixture, "bool y = a && b || c && a; return x;", 3, 0);
        Score(fixture, "bool y = a && !(b && c); return x;", 2, 0);
        Score(fixture, "bool y = x is > 0 and < 10 or 20 and 30; return x;", 3, 0);
    }

    private static void Constructs(MetricFixture fixture)
    {
        Score(fixture, "switch(x) { case 1: break; case 2: break; default: break; } return x;", 1, 1);
        Score(fixture, "return x switch { 0 => a ? 1 : 2, _ => 3 };", 3, 2);
        Score(fixture, "try { if(a) {} } catch(System.Exception) { if(b) {} } finally {} return x;", 4, 2);
        Score(fixture, "while(a) { for(int i=0;i<x;i++) { if(b) break; } } return x;", 6, 3);
        Score(fixture, "goto Done; Done: return x;", 1, 0);
        Score(fixture, "using (var s = new System.IO.MemoryStream()) { lock(s) {} } return x;", 0, 0);
    }

    private static void NestedCallables(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("class F { void Outer() { void Local() { if(true){} } System.Action a = () => { if(true){} }; System.Action b = delegate { if(true){} }; } }");
        MemberMetric outer = result.Members.Single(member => member.Name == "F.Outer()");
        MetricFixture.Equal(6, outer.Cognitive, "outer nested context");
        MetricFixture.Equal(2, outer.Nesting, "outer nested depth");
        foreach (MemberMetric nested in result.Members.Where(member => member != outer))
        {
            MetricFixture.Equal(1, nested.Cognitive, "independent nested callable");
            MetricFixture.Equal(1, nested.Nesting, "independent nested depth");
        }
    }

    private static void Recursion(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("class F { int R(int x) { return R(x)+R(x); } int A() { return B(); } int B() { return A(); } int M() { return M(1); } int M(int x) { return x; } T G<T>(T x) { return G<T>(x); } }");
        foreach (MemberMetric member in result.Members)
        {
            int expected = member.Name.StartsWith("F.M(", System.StringComparison.Ordinal) ? 0 : 1;
            MetricFixture.Equal(expected, member.Cognitive, "cycle/overload: " + member.Name);
        }
    }

    private static void Score(MetricFixture fixture, string body, int cognitive, int nesting)
    {
        MemberMetric result = fixture.Method(body);
        MetricFixture.Equal(cognitive, result.Cognitive, "flow cognitive");
        MetricFixture.Equal(nesting, result.Nesting, "flow nesting");
    }

    private static void PartialRecursion(MetricFixture fixture)
    {
        string[] sources = ["partial class F { partial void R() { R(); } }", "partial class F { partial void R(); }"];
        foreach (string[] ordered in new[] { sources, sources.Reverse().ToArray() })
        {
            MetricAnalysis result = fixture.Measure(ordered);
            MetricFixture.Equal(1, result.Members.Count(member => member.Recursive), "partial implementation cycle");
            MetricFixture.Equal(1, result.Members.Sum(member => member.Cognitive), "partial recursion once");
        }
    }
}
