using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeNeCommander.Complexity;

internal sealed class MetricAnalysis
{
    internal MetricAnalysis(IReadOnlyList<SourceProject> projects, IReadOnlyList<MetadataReference> references)
    {
        IReadOnlyDictionary<string, CSharpCompilation> compilations = CompilationCatalog.Create(projects, references);
        List<MemberMetric> members = [];
        List<TypePart> parts = [];
        foreach (SourceProject project in projects)
        {
            foreach (SourceFile file in project.Files)
            {
                ReadFile(file, compilations[project.Name], members, parts);
            }
        }
        Members = [.. members.OrderBy(member => member.Location.Path, StringComparer.Ordinal).ThenBy(member => member.Node.SpanStart)];
        Types = [.. parts.GroupBy(part => part.Project + ":" + part.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new TypeMetric(group.First().Project, group.First().Name, [.. group]))];
        RecursionGraph graph = new();
        graph.Mark(Members);
        UnresolvedInvocations = graph.UnresolvedInvocations;
    }

    internal IReadOnlyList<MemberMetric> Members { get; }
    internal IReadOnlyList<TypeMetric> Types { get; }
    internal int UnresolvedInvocations { get; }

    internal MetricFinding[] Findings()
    {
        return [.. Members.SelectMany(member => member.Findings()).Concat(Types.SelectMany(type => type.Findings()))
            .OrderBy(finding => finding.Location.Path, StringComparer.Ordinal).ThenBy(finding => finding.Location.Line)
            .ThenBy(finding => finding.Location.Column).ThenBy(finding => finding.Metric, StringComparer.Ordinal)];
    }

    private static void ReadFile(SourceFile file, CSharpCompilation compilation, List<MemberMetric> members, List<TypePart> parts)
    {
        SemanticModel model = compilation.GetSemanticModel(file.Tree);
        foreach (SyntaxNode node in file.Tree.GetRoot().DescendantNodes())
        {
            if (node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            {
                INamedTypeSymbol symbol = model.GetDeclaredSymbol(node) as INamedTypeSymbol ??
                    throw new InvalidOperationException("[CS-013 input] Type identity could not be bound: " + file.Path);
                parts.Add(new TypePart(file, node, symbol));
            }
            if (CallableNodes.Contains(node)) { members.Add(new MemberMetric(file, node, model)); }
        }
    }
}
