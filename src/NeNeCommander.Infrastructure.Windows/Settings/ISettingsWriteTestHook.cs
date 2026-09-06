namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>Provides test-only observation points inside one synchronous settings write.</summary>
internal interface ISettingsWriteTestHook
{
    /// <summary>Runs immediately before cancellation is observed for directory creation.</summary>
    /// <param name="directoryPath">Settings document parent path.</param>
    public void BeforeDirectoryCreation(string directoryPath);

    /// <summary>Runs after directory creation returns and before its resulting chain is captured.</summary>
    /// <param name="directoryPath">Settings document parent path.</param>
    public void DirectoryCreated(string directoryPath);

    /// <summary>Runs immediately before cancellation is observed for temporary-file creation.</summary>
    /// <param name="temporaryPath">Fixed sibling temporary path.</param>
    public void BeforeTemporaryCreation(string temporaryPath);

    /// <summary>Runs after the owned temporary document has been flushed.</summary>
    /// <param name="temporaryPath">Fixed sibling temporary path.</param>
    public void TemporaryFlushed(string temporaryPath);

    /// <summary>Runs immediately before the destination is revalidated and published.</summary>
    /// <param name="documentPath">Settings destination path.</param>
    public void BeforePublish(string documentPath);

    /// <summary>Runs after the provider publish primitive has completed successfully.</summary>
    /// <param name="documentPath">Published settings destination path.</param>
    public void Published(string documentPath);
}
