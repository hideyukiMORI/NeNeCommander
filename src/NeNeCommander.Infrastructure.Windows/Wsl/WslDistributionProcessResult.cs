using System;

namespace NeNeCommander.Infrastructure.Windows.Wsl;

/// <summary>Captures the exit code and bounded standard output of one list invocation.</summary>
internal sealed record WslDistributionProcessResult
{
    internal WslDistributionProcessResult(int exitCode, string standardOutput)
    {
        ArgumentNullException.ThrowIfNull(standardOutput);
        ExitCode = exitCode;
        StandardOutput = standardOutput;
    }

    /// <summary>Gets the process exit code.</summary>
    internal int ExitCode { get; }

    /// <summary>Gets the bounded standard output.</summary>
    internal string StandardOutput { get; }
}
