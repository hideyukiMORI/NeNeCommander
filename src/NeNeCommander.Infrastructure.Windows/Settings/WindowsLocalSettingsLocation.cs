using System;
using System.IO;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Resolves the sole Windows location of the persisted settings document. This adapter is the
/// only place in the repository that reads the process environment, so no feature layer can
/// depend on an ambient folder (CS-010, ADR-0022).
/// </summary>
public static class WindowsLocalSettingsLocation
{
    private const string ProductFolderName = "NeNeCommander";
    private const string DocumentFileName = "settings.json";

    /// <summary>
    /// Resolves <c>%LOCALAPPDATA%\NeNeCommander\settings.json</c> as a validated Windows local path.
    /// </summary>
    /// <returns>The validated settings-document path.</returns>
    /// <exception cref="InvalidOperationException">
    /// The operating system reported a local application-data folder that is not a Windows local
    /// path, which is an impossible state rather than an expected failure.
    /// </exception>
    public static WindowsLocalPath Resolve()
    {
        string localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        string documentPath = Path.Join(localApplicationData, ProductFolderName, DocumentFileName);
        return FileSystemPath.Parse(documentPath) is PathParseSuccess { Path: WindowsLocalPath path }
            ? path
            : throw new InvalidOperationException("The resolved settings location is not a Windows local path.");
    }
}
