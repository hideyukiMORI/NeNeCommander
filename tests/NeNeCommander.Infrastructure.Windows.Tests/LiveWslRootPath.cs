using System;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal static class LiveWslRootPath
{
    private const string RootPrefix = "NeNeCommander-Live-";

    internal static bool IsAllowedRootShape(WslPath root)
    {
        return root.Parent is WslPath { LinuxPath: "/tmp" } &&
            root.LinuxPath[5..].StartsWith(RootPrefix, StringComparison.Ordinal) &&
            root.LinuxPath.Length > 5 + RootPrefix.Length;
    }

    internal static string[] Segments(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        string[] segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 || segments.Any(segment => segment is "." or ".." || segment.Contains('\\'))
            ? throw new InvalidOperationException("The live fixture path is not relative and contained.")
            : segments;
    }

    internal static WslPath Child(WslPath parent, string name)
    {
        return parent.Child(name) is PathParseSuccess { Path: WslPath child }
            ? child
            : throw new InvalidOperationException("The live fixture child name is invalid.");
    }
}
