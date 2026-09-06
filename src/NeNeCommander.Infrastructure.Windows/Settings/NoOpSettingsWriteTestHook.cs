namespace NeNeCommander.Infrastructure.Windows.Settings;

internal sealed class NoOpSettingsWriteTestHook : ISettingsWriteTestHook
{
    internal static NoOpSettingsWriteTestHook Instance { get; } = new();

    private NoOpSettingsWriteTestHook()
    {
    }

    public void BeforeDirectoryCreation(string directoryPath)
    {
    }

    public void DirectoryCreated(string directoryPath)
    {
    }

    public void BeforeTemporaryCreation(string temporaryPath)
    {
    }

    public void TemporaryFlushed(string temporaryPath)
    {
    }

    public void TemporaryClosed(string temporaryPath)
    {
    }

    public void BeforePublish(string documentPath)
    {
    }

    public void Published(string documentPath)
    {
    }
}
