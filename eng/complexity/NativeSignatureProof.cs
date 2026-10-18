using System.Linq;

namespace NeNeCommander.Complexity;

internal static class NativeSignatureProof
{
    internal static void Run(MetricFixture fixture)
    {
        fixture.Check("native imported and mapped COM signatures", () => Native(fixture));
        fixture.Check("source-defined attribute spoof refused", () => Spoofed(fixture));
        fixture.Check("managed mirror and overload remain owned", () => Managed(fixture));
        fixture.Check("COM Guid mapping and import body required", () => Shape(fixture));
    }

    private static void Native(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("""
            using System;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            using Import = System.Runtime.InteropServices.LibraryImportAttribute;
            partial class Imports {
                [Import("synthetic", EntryPoint="operation")] static partial int P(int a,int b,int c,int d,int e);
                [DllImport("synthetic")] static extern int D(int a,int b,int c,int d,int e);
            }
            [GeneratedComInterface, Guid("11111111-1111-1111-1111-111111111111")]
            partial interface IExternal { int M(int a,int b,int c,int d,int e); }
            [GeneratedComClass] partial class Implementation : IExternal {
                public int M(int a,int b,int c,int d,int e) { return a; }
            }
            """);
        MetricFixture.Equal(4, result.Members.Count(member => member.Native is not null), "native signatures");
        MetricFixture.Equal(0, result.Findings().Count(finding => finding.Metric == "parameters"), "native parameter scope");
        MetricFixture.Require(result.Members.All(member => member.Parameters == 5), "native raw counts preserved");
    }

    private static void Spoofed(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("""
            namespace System.Runtime.InteropServices {
                class LibraryImportAttribute : System.Attribute { public LibraryImportAttribute(string name) {} }
            }
            partial class F {
                [System.Runtime.InteropServices.LibraryImport("synthetic")]
                static partial int Fake(int a,int b,int c,int d,int e);
            }
            """);
        MetricFixture.Require(result.Members.All(member => member.Native is null), "metadata identity rejects spoof");
        MetricFixture.Equal(1, result.Findings().Count(finding => finding.Metric == "parameters"), "spoof keeps finding");
    }

    private static void Managed(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("""
            using System.Runtime.InteropServices.Marshalling;
            [GeneratedComClass] partial class Mirror : IManaged {
                public int M(int a,int b,int c,int d,int e) { return a; }
                public int M(int a,int b,int c,int d,int e,int f) { return a; }
            }
            interface IManaged { int M(int a,int b,int c,int d,int e); }
            """);
        MetricFixture.Equal(3, result.Findings().Count(finding => finding.Metric == "parameters"), "managed mirror not exempted");
    }

    private static void Shape(MetricFixture fixture)
    {
        MetricAnalysis result = fixture.Measure("""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            [GeneratedComInterface] partial interface IMissingGuid { int M(int a,int b,int c,int d,int e); }
            [GeneratedComInterface, Guid("11111111-1111-1111-1111-111111111111")]
            partial interface IExternal { int M(int a,int b,int c,int d,int e); }
            [GeneratedComClass] partial class Implementation : IExternal {
                public int M(int a,int b,int c,int d,int e) { return a; }
                public int M(int a,int b,int c,int d,int e,int f) { return a; }
                [LibraryImport("synthetic")] static int Body(int a,int b,int c,int d,int e) { return a; }
            }
            """);
        MetricFixture.Equal(2, result.Members.Count(member => member.Native is not null), "only exact native interface and implementation");
        MetricFixture.Equal(3, result.Findings().Count(finding => finding.Metric == "parameters"), "invalid native shapes remain owned");
    }
}
