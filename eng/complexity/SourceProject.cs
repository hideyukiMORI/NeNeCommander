using System.Collections.Generic;

namespace NeNeCommander.Complexity;

internal sealed class SourceProject
{
    internal SourceProject(string name, IReadOnlyList<string> dependencies, IReadOnlyList<SourceFile> files)
    {
        Name = name;
        Dependencies = dependencies;
        Files = files;
    }

    internal string Name { get; }
    internal IReadOnlyList<string> Dependencies { get; }
    internal IReadOnlyList<SourceFile> Files { get; }
}
