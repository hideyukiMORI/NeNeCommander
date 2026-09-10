using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Reads the Linux inode, hard-link count, and change time of one entry with a read-only
/// <c>stat</c> inside the configured distribution. This is the oracle the ADR-0049 live read-only
/// cells compare the Windows-side identity against. It never mutates, is never consulted by
/// production code, and is never an identity source for the harness itself.
/// </summary>
internal sealed record LiveWslStatFact
{
    private const int OutputBoundary = 4096;

    private LiveWslStatFact(long inode, long linkCount, long changeSeconds)
    {
        Inode = inode;
        LinkCount = linkCount;
        ChangeSeconds = changeSeconds;
    }

    internal long Inode { get; }

    internal long LinkCount { get; }

    internal long ChangeSeconds { get; }

    internal static LiveWslStatFact Read(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        using Process process = Process.Start(CreateStartInfo(path)) ??
            throw new InvalidOperationException("The read-only live stat process did not start.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 &&
            standardError.Length <= OutputBoundary &&
            standardOutput.Length is > 0 and <= OutputBoundary
            ? Parse(standardOutput)
            : throw new InvalidOperationException("The read-only live stat query failed.");
    }

    private static ProcessStartInfo CreateStartInfo(WslPath path)
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

        // Fixed tokens only, no shell, and the argument list keeps the validated Linux path one
        // argument. `--` stops option parsing so no path can be read as a switch.
        startInfo.ArgumentList.Add("--distribution");
        startInfo.ArgumentList.Add(path.DistributionName);
        startInfo.ArgumentList.Add("--exec");
        startInfo.ArgumentList.Add("stat");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("%i %h %Z");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(path.LinuxPath);
        return startInfo;
    }

    private static LiveWslStatFact Parse(string standardOutput)
    {
        string[] fields = standardOutput.Trim().Split(' ');
        return fields.Length == 3 &&
            long.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out long inode) &&
            long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out long linkCount) &&
            long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out long changeSeconds)
            ? new LiveWslStatFact(inode, linkCount, changeSeconds)
            : throw new InvalidOperationException("The read-only live stat record has an unexpected shape.");
    }
}
