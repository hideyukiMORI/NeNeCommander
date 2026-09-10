using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Runs the one bounded <c>wsl.exe --distribution &lt;name&gt; --exec</c> invocation the live
/// harness is allowed to make. Exactly three verbs are accepted: the read-only ADR-0049
/// <c>stat</c> oracle, the <c>ln -s</c> link fixture, and the <c>unlink</c> that removes a link
/// entry the Windows namespace cannot remove. Shell execution is disabled, every token is fixed,
/// and paths travel as separate argument-list entries after <c>--</c>, so no path can be read as
/// an option or as shell text.
/// </summary>
internal static class LiveWslDistributionCommand
{
    private const int OutputBoundary = 4096;

    private static readonly string[] AcceptedVerbs = ["stat", "ln", "unlink"];

    internal static string Run(string distribution, IReadOnlyList<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distribution);
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count == 0 || Array.IndexOf(AcceptedVerbs, arguments[0]) < 0)
        {
            throw new InvalidOperationException("The live WSL command verb is not accepted.");
        }
        using Process process = Process.Start(CreateStartInfo(distribution, arguments)) ??
            throw new InvalidOperationException("The bounded live WSL command did not start.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 &&
            standardError.Length <= OutputBoundary &&
            standardOutput.Length <= OutputBoundary
            ? standardOutput.Trim()
            : throw new InvalidOperationException("The bounded live WSL command failed.");
    }

    private static ProcessStartInfo CreateStartInfo(string distribution, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "wsl.exe",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--distribution");
        startInfo.ArgumentList.Add(distribution);
        startInfo.ArgumentList.Add("--exec");
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        return startInfo;
    }
}
