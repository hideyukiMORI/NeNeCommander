using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NeNeCommander.Complexity;

internal static class CompilationCatalog
{
    internal static IReadOnlyDictionary<string, CSharpCompilation> Create(
        IReadOnlyList<SourceProject> projects, IReadOnlyList<MetadataReference> framework)
    {
        Dictionary<string, CSharpCompilation> compilations = new(StringComparer.Ordinal);
        List<SourceProject> pending = [.. projects];
        if (projects.Select(project => project.Name).Distinct(StringComparer.Ordinal).Count() != projects.Count)
        {
            throw new InvalidOperationException("[CS-013 input] Duplicate project identity.");
        }
        while (pending.Count != 0)
        {
            SourceProject? project = pending.FirstOrDefault(candidate => candidate.Dependencies.All(compilations.ContainsKey));
            if (project is null) { throw new InvalidOperationException("[CS-013 input] Missing or cyclic project reference."); }
            MetadataReference[] references = [.. framework, .. DependencyReferences(project, compilations)];
            CSharpCompilation compilation = CSharpCompilation.Create(project.Name,
                project.Files.Select(file => file.Tree), references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilations.Add(project.Name, compilation);
            _ = pending.Remove(project);
        }
        return compilations;
    }

    private static IEnumerable<MetadataReference> DependencyReferences(
        SourceProject project, IReadOnlyDictionary<string, CSharpCompilation> compilations)
    {
        HashSet<Compilation> dependencies = [];
        Stack<Compilation> pending = new(project.Dependencies.Select(name => compilations[name]));
        while (pending.TryPop(out Compilation? dependency))
        {
            if (!dependencies.Add(dependency)) { continue; }
            foreach (CompilationReference reference in dependency.References.OfType<CompilationReference>())
            {
                pending.Push(reference.Compilation);
            }
        }
        return dependencies.OrderBy(dependency => dependency.AssemblyName, StringComparer.Ordinal)
            .Select(dependency => dependency.ToMetadataReference());
    }
}
