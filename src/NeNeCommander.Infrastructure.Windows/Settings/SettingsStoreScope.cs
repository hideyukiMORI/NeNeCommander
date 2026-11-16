using NeNeCommander.Application.Identifiers;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>
/// Composes one settings store's document path, observers, baseline and write dependencies so a
/// read or write attempt works against the same owned state.
/// </summary>
internal sealed class SettingsStoreScope
{
    internal SettingsStoreScope(
        WindowsLocalPath documentPath,
        WindowsLocalSettingsStoreDependencies dependencies)
    {
        DocumentPath = documentPath;
        DriveBoundary = dependencies.DriveBoundary;
        IdentifierSource = dependencies.IdentifierSource;
        WriteTestHook = dependencies.WriteTestHook;
        Locations = new SettingsLocationObserver(documentPath, dependencies.DriveBoundary);
        Documents = new SettingsDocumentObserver(documentPath, dependencies.DriveBoundary);
        Baseline = new SettingsBaseline(Locations, Documents);
    }

    internal WindowsLocalPath DocumentPath { get; }

    internal WindowsDriveBoundary DriveBoundary { get; }

    internal IIdentifierSource IdentifierSource { get; }

    internal ISettingsWriteTestHook WriteTestHook { get; }

    internal SettingsLocationObserver Locations { get; }

    internal SettingsDocumentObserver Documents { get; }

    internal SettingsBaseline Baseline { get; }

    internal bool AdmitsDocument()
    {
        return DriveBoundary.AdmitsLocal(DocumentPath);
    }
}
