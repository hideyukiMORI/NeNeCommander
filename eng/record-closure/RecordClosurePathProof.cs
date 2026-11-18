using System;
using System.IO;
using System.Linq;

namespace NeNeCommander.RecordClosure;

/// <summary>Proves invalid probe names fail before producing any compilation artifact.</summary>
internal static class RecordClosurePathProof
{
    internal static void Run(RecordClosureCompiler compiler, string output)
    {
        string[] original = Files(output);
        string[] invalid = [string.Empty, ".", "..", "nested/probe", "nested\\probe", "probe:stream",
            Path.GetFullPath(Path.Join(output, "rooted-probe"))];
        foreach (string name in invalid)
        {
            Reject(compiler, name);
            if (!original.SequenceEqual(Files(output), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A rejected probe name produced an artifact.");
            }
        }
    }

    private static void Reject(RecordClosureCompiler compiler, string name)
    {
        try
        {
            _ = compiler.Accept(name, "public sealed record Probe;");
        }
        catch (ArgumentException exception) when (exception.ParamName == "name")
        {
            return;
        }
        throw new InvalidOperationException("An invalid probe name was accepted.");
    }

    private static string[] Files(string output)
    {
        return [.. Directory.GetFiles(output, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)];
    }
}
