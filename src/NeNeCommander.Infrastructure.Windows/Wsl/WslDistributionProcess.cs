using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeNeCommander.Infrastructure.Windows.Wsl;

/// <summary>Runs the fixed WSL distribution-list process invocation.</summary>
internal sealed class WslDistributionProcess : IWslDistributionProcess
{
    private readonly Func<ProcessStartInfo, Process?> _start;

    internal WslDistributionProcess()
        : this(static startInfo => Process.Start(startInfo))
    {
    }

    internal WslDistributionProcess(Func<ProcessStartInfo, Process?> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        _start = start;
    }

    public async Task<WslDistributionProcessResult> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using Process? process = _start(CreateStartInfo());
        return process is null
            ? throw new Win32Exception("The WSL distribution process did not start.")
            : await CompleteAsync(process, cancellationToken);
    }

    internal static ProcessStartInfo CreateStartInfo()
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "wsl.exe",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.Unicode,
            StandardOutputEncoding = Encoding.Unicode,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--list");
        startInfo.ArgumentList.Add("--quiet");
        return startInfo;
    }

    private static async Task<WslDistributionProcessResult> CompleteAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        void Terminate()
        {
            KillOwnedProcess(process);
        }

        using CancellationTokenRegistration registration = cancellationToken.Register(Terminate);
        Task<string> standardOutput = ReadBoundedAsync(process.StandardOutput, Terminate, CancellationToken.None);
        Task<string> standardError = ReadBoundedAsync(process.StandardError, Terminate, CancellationToken.None);
        Task exit = process.WaitForExitAsync(CancellationToken.None);
        await Task.WhenAll(standardOutput, standardError, exit);
        cancellationToken.ThrowIfCancellationRequested();
        string output = await standardOutput;
        return new WslDistributionProcessResult(process.ExitCode, output);
    }

    internal static async Task<string> ReadBoundedAsync(
        TextReader reader,
        Action terminate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(terminate);
        char[] buffer = new char[1024];
        StringBuilder output = new();
        int read = await reader.ReadAsync(buffer, cancellationToken);
        while (read != 0)
        {
            if (output.Length > WslDistributionCatalog.OutputCharacterBoundary - read)
            {
                terminate();
                throw new InvalidDataException("WSL distribution process output exceeded its boundary.");
            }
            _ = output.Append(buffer, 0, read);
            read = await reader.ReadAsync(buffer, cancellationToken);
        }
        return output.ToString();
    }

    private static void KillOwnedProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (InvalidOperationException) when (process.HasExited)
        {
            return;
        }
    }
}
