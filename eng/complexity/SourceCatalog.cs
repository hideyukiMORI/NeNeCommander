using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NeNeCommander.Complexity;

internal static class SourceCatalog
{
    internal static IReadOnlyList<SourceProject> Read(string root)
    {
        RejectLink(root);
        string manifestPath = Path.Combine(root, "eng", "architecture.json");
        RejectAncestors(root, manifestPath);
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        List<SourceProject> projects = [];
        HashSet<string> admitted = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement entry in manifest.RootElement.GetProperty("projects").EnumerateArray())
        {
            SourceProject project = ReadProject(root, entry, directories);
            Admit(project, admitted);
            projects.Add(project);
        }
        SourceFile[] engineering = [.. Enumerate(Path.Combine(root, "eng"), null).Select(path => ReadFile(root, "Engineering", path))];
        if (projects.Count == 0 || engineering.Length == 0) { throw Input("Empty source catalog."); }
        projects.Add(new SourceProject("Engineering", [], engineering));
        VerifyAllSources(root, admitted, directories);
        return projects;
    }

    private static SourceProject ReadProject(string root, JsonElement entry, HashSet<string> directories)
    {
        string name = entry.GetProperty("name").GetString() ?? throw Input("Missing project name.");
        string relative = entry.GetProperty("path").GetString() ?? throw Input("Missing project path.");
        string projectPath = Contained(root, relative);
        if (!File.Exists(projectPath)) { throw Input("Missing project: " + relative); }
        RejectAncestors(root, projectPath);
        string directory = Path.GetDirectoryName(projectPath) ?? throw Input("Missing project directory.");
        _ = directories.Add(directory);
        string[] dependencies = [.. entry.GetProperty("projectReferences").EnumerateArray().Select(value => value.GetString() ?? throw Input("Missing dependency."))];
        SourceFile[] files = [.. Enumerate(directory, directory).Select(path => ReadFile(root, name, path))];
        if (files.Length == 0) { throw Input("Empty project: " + name); }
        return new SourceProject(name, dependencies, files);
    }

    private static void Admit(SourceProject project, HashSet<string> admitted)
    {
        foreach (SourceFile file in project.Files)
        {
            if (!admitted.Add(file.Path)) { throw Input("Overlapping source projects: " + file.Path); }
        }
    }

    private static void VerifyAllSources(string root, HashSet<string> admitted, HashSet<string> directories)
    {
        foreach (string scope in new[] { "src", "tests" })
        {
            string directory = Path.Combine(root, scope);
            if (!Directory.Exists(directory)) { throw Input("Missing scope: " + scope); }
            RejectLink(directory);
            foreach (string child in Directory.GetDirectories(directory))
            {
                VerifyDirectory(root, child, admitted, directories);
            }
            if (Directory.GetFiles(directory, "*.cs").Length != 0) { throw Input("Unassigned source in scope root: " + scope); }
        }
    }

    private static void VerifyDirectory(string root, string directory, HashSet<string> admitted, HashSet<string> directories)
    {
        foreach (string path in Enumerate(directory, directories.Contains(directory) ? directory : null))
        {
            if (!admitted.Contains(Relative(root, path))) { throw Input("Unassigned source: " + Relative(root, path)); }
        }
    }

    private static IEnumerable<string> Enumerate(string directory, string? projectDirectory)
    {
        RejectLink(directory);
        foreach (string path in Directory.GetFileSystemEntries(directory).Order(StringComparer.Ordinal))
        {
            if (IsGenerated(path, projectDirectory)) { continue; }
            RejectLink(path);
            if (Directory.Exists(path))
            {
                foreach (string nested in Enumerate(path, projectDirectory)) { yield return nested; }
            }
            else if (string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }
    }

    private static bool IsGenerated(string path, string? projectDirectory)
    {
        return projectDirectory is not null && Path.GetDirectoryName(path) == projectDirectory &&
            Path.GetFileName(path) is "bin" or "obj";
    }

    private static SourceFile ReadFile(string root, string project, string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024) { throw Input("Source exceeds one MiB: " + Relative(root, path)); }
        return new SourceFile(project, Relative(root, path), File.ReadAllText(path));
    }

    internal static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) { throw Input("Source links are unsupported."); }
    }

    private static void RejectAncestors(string root, string path)
    {
        string? current = path;
        while (current is not null && !string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
        {
            RejectLink(current);
            current = Path.GetDirectoryName(current);
        }
    }

    private static string Contained(string root, string relative)
    {
        string path = Path.GetFullPath(Path.Combine(root, relative));
        string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { throw Input("Project path escapes the repository."); }
        if (!relative.Replace('\\', '/').StartsWith("src/", StringComparison.Ordinal) &&
            !relative.Replace('\\', '/').StartsWith("tests/", StringComparison.Ordinal)) { throw Input("Project outside src/tests."); }
        return path;
    }

    private static string Relative(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }

    private static InvalidOperationException Input(string message)
    {
        return new InvalidOperationException("[CS-013 input] " + message);
    }
}
