using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeNeCommander.Application.Identifiers;
using NeNeCommander.Application.Settings;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.Execution;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Reads and atomically replaces the persisted settings document at one Windows local path.
/// Reads never create or repair it; writes reject detected baseline changes before publishing one
/// complete validated schema value through the provider's atomic replacement primitive.
/// </summary>
public sealed class WindowsLocalSettingsStore : ISettingsStore
{
    private readonly WindowsLocalIoExecutionBoundary _ioExecutionBoundary;
    private readonly SettingsStoreScope _scope;

    /// <summary>Initializes the store with the composed location of the settings document.</summary>
    /// <param name="documentPath">Validated Windows local path of the settings document.</param>
    /// <param name="ioExecutionBoundary">Sole scheduler for synchronous Windows filesystem work.</param>
    /// <param name="identifierSource">Source of one opaque candidate token per admitted write.</param>
    public WindowsLocalSettingsStore(
        WindowsLocalPath documentPath,
        WindowsLocalIoExecutionBoundary ioExecutionBoundary,
        IIdentifierSource identifierSource)
        : this(documentPath, ioExecutionBoundary,
            new WindowsLocalSettingsStoreDependencies(
                identifierSource, NoOpSettingsWriteTestHook.Instance, new WindowsDriveBoundary()))
    {
    }

    internal WindowsLocalSettingsStore(
        WindowsLocalPath documentPath,
        WindowsLocalIoExecutionBoundary ioExecutionBoundary,
        IIdentifierSource identifierSource,
        ISettingsWriteTestHook writeTestHook)
        : this(documentPath, ioExecutionBoundary,
            new WindowsLocalSettingsStoreDependencies(identifierSource, writeTestHook, new WindowsDriveBoundary()))
    {
    }

    internal WindowsLocalSettingsStore(
        WindowsLocalPath documentPath,
        WindowsLocalIoExecutionBoundary ioExecutionBoundary,
        WindowsLocalSettingsStoreDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(ioExecutionBoundary);
        ArgumentNullException.ThrowIfNull(dependencies);
        _ = SettingsWriteAttempt.TemporaryPath(documentPath, Guid.Empty);
        _ioExecutionBoundary = ioExecutionBoundary;
        _scope = new SettingsStoreScope(documentPath, dependencies);
    }

    /// <inheritdoc />
    public Task<SettingsReadOutcome> ReadAsync(CancellationToken cancellationToken)
    {
        return _ioExecutionBoundary.ExecuteAsync(() => ReadCoreAsync(cancellationToken)).Unwrap();
    }

    /// <inheritdoc />
    public Task<SettingsWriteOutcome> WriteAsync(
        UserSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _ioExecutionBoundary.ExecuteAsync(
            () => SettingsWriteAttempt.Run(_scope, settings, cancellationToken));
    }

    private async Task<SettingsReadOutcome> ReadCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            SettingsLocationSnapshot location = _scope.Locations.Capture();
            if (location is BlockedLocation blocked)
            {
                return RejectRead(blocked.Failure);
            }
            _scope.Baseline.RememberLocation(location);
            ReadDocumentObservation observation =
                await _scope.Documents.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (!_scope.Locations.Matches(location))
            {
                return RejectRead(SettingsWriteFailureKind.UnsafeLocation);
            }
            _scope.Baseline.Remember(observation.Document, location);
            return observation.Outcome;
        }
        catch (FileNotFoundException)
        {
            return ReadAbsent();
        }
        catch (DirectoryNotFoundException)
        {
            return ReadAbsent();
        }
        catch (UnauthorizedAccessException)
        {
            return RejectRead(SettingsWriteFailureKind.Unauthorized);
        }
        catch (IOException)
        {
            return RejectRead(SettingsWriteFailureKind.IoFailure);
        }
    }

    /// <summary>A missing document is absent only while the drive boundary still admits its path.</summary>
    private SettingsReadOutcome ReadAbsent()
    {
        if (!_scope.AdmitsDocument())
        {
            return RejectRead(SettingsWriteFailureKind.UnsafeLocation);
        }
        _scope.Baseline.RememberDocument(AbsentDocument.Instance);
        return SettingsReadOutcome.Absent();
    }

    /// <summary>An unreadable document blocks later writes with the same failure kind.</summary>
    private SettingsReadOutcome RejectRead(SettingsWriteFailureKind failure)
    {
        _scope.Baseline.Remember(
            new BlockedDocument(failure),
            new BlockedLocation(failure));
        return SettingsReadOutcome.Rejected(SettingsReadFailureKind.Unreadable);
    }
}
