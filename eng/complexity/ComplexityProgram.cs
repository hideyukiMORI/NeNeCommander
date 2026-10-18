using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NeNeCommander.Complexity;

internal static class ComplexityProgram
{
    private static int Main(string[] arguments)
    {
        if (arguments.Length != 4) { Console.Error.WriteLine("Expected mode, repository, reference pack and output."); return 2; }
        string output = arguments[3];
        try
        {
            MetadataReference[] references = [.. Directory.GetFiles(arguments[2], "*.dll").Order(StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)),
                MetadataReference.CreateFromFile(typeof(SyntaxNode).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location)];
            if (arguments[0] == "SelfTest")
            {
                ComplexitySelfTest.Run(references, output);
                return 0;
            }
            if (arguments[0] != "Inspect") { throw new InvalidOperationException("Unknown inspection mode."); }
            IReadOnlyList<SourceProject> projects = SourceCatalog.Read(arguments[1]);
            MetricAnalysis analysis = new(projects, references);
            return WriteReport(analysis, projects, output);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new
            {
                status = "error",
                metricVersion = "cs013-v1",
                error = exception.Message,
            }));
            Console.Error.WriteLine(exception);
            return 2;
        }
    }

    private static int WriteReport(MetricAnalysis analysis, IReadOnlyList<SourceProject> projects, string output)
    {
        MetricFinding[] findings = analysis.Findings();
        object report = new
        {
            status = findings.Length == 0 ? "conformant" : "findings",
            metricVersion = "cs013-v1",
            parserVersion = typeof(CSharpSyntaxTree).Assembly.GetName().Version?.ToString(),
            sources = projects.SelectMany(project => project.Files).OrderBy(file => file.Path, StringComparer.Ordinal)
                .Select(file => new { project = file.Project, path = file.Path, sha256 = file.Hash }),
            unresolvedInvocations = analysis.UnresolvedInvocations,
            types = analysis.Types,
            members = analysis.Members,
            findings,
        };
        File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"[CS-013] Types {analysis.Types.Count}; members {analysis.Members.Count}; findings {findings.Length}; unresolved invocations {analysis.UnresolvedInvocations}.");
        foreach (IGrouping<string, MetricFinding> group in findings.GroupBy(finding => finding.Metric).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"[CS-013] {group.Key}: {group.Count()}");
        }
        return findings.Length == 0 ? 0 : 1;
    }
}
