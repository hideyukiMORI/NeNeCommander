using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace NeNeCommander.Complexity;

internal static class ComplexitySelfTest
{
    internal static void Run(IReadOnlyList<MetadataReference> references, string output)
    {
        MetricFixture fixture = new(references);
        MetricBoundaryProof.Run(fixture);
        FlowMetricProof.Run(fixture);
        NativeSignatureProof.Run(fixture);
        CompilationCatalogProof.Run(fixture, references);
        SourceCatalogProof.Run(fixture, output);
        File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new
        {
            status = "passed",
            metricVersion = "cs013-v1",
            cases = fixture.Passed,
        }, new JsonSerializerOptions { WriteIndented = true }));
        System.Console.WriteLine("[CS-013] Self-test passed: " + fixture.Passed.Count + " fixture groups.");
    }
}
