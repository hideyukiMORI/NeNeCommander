using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Queries recycle capability and executes one guarded Shell recycle without a fallback.</summary>
internal sealed class WindowsRecycleBin
{
    private const uint RequiredAttributes = 0x40000000 | 0x20;
    private readonly IWindowsRecycleShell _shell;

    internal WindowsRecycleBin(IWindowsRecycleShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        _shell = shell;
    }

    internal DeletionCapability GetCapability(WindowsLocalPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try
        {
            return WindowsRecycleAdmission.Inspect(path) is null && QueryCapability(path)
                ? DeletionCapability.Recycle
                : DeletionCapability.PermanentOnly;
        }
        catch (COMException)
        {
            return DeletionCapability.PermanentOnly;
        }
        catch (UnauthorizedAccessException)
        {
            return DeletionCapability.PermanentOnly;
        }
        catch (IOException)
        {
            return DeletionCapability.PermanentOnly;
        }
    }

    internal ProviderStepOutcome Recycle(FileEntrySnapshot source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        FileOperationFailureKind? refusal = WindowsRecycleAdmission.Revalidate(source);
        if (refusal is not null)
        {
            return ProviderStepOutcome.Failed(refusal);
        }
        try
        {
            if (GetCapability((WindowsLocalPath)source.Path) != DeletionCapability.Recycle)
            {
                return ProviderStepOutcome.Failed(FileOperationFailureKind.ProviderUnavailable);
            }
            using WindowsRecycleSession session = OpenSession(source, cancellationToken);
            return session.Execute(cancellationToken);
        }
        catch (COMException exception)
        {
            return ProviderStepOutcome.Failed(WindowsFileFailureNormalizer.Normalize(exception.HResult));
        }
        catch (UnauthorizedAccessException)
        {
            return ProviderStepOutcome.Failed(FileOperationFailureKind.AccessDenied);
        }
        catch (IOException exception)
        {
            return ProviderStepOutcome.Failed(WindowsFileFailureNormalizer.Normalize(exception.HResult));
        }
    }

    internal WindowsRecycleSession OpenSession(FileEntrySnapshot source, CancellationToken cancellationToken)
    {
        IWindowsShellEntry entry = _shell.OpenEntry((WindowsLocalPath)source.Path);
        IWindowsShellOperation? operation = null;
        try
        {
            operation = _shell.OpenOperation();
            return new WindowsRecycleSession(operation, entry, new WindowsRecycleProgress(source, cancellationToken));
        }
        finally
        {
            if (operation is null)
            {
                WindowsRecycleShell.Release(entry);
            }
        }
    }

    private bool QueryCapability(WindowsLocalPath path)
    {
        IWindowsShellEntry entry = _shell.OpenEntry(path);
        try
        {
            return entry.GetAttributes(RequiredAttributes, out uint attributes) == 0 &&
                (attributes & RequiredAttributes) == RequiredAttributes && _shell.QueryRecycleBin(path) == 0;
        }
        finally
        {
            WindowsRecycleShell.Release(entry);
        }
    }
}
