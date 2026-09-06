using System;
using NeNeCommander.Infrastructure.Windows.Settings;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class ScriptedSettingsWriteTestHook : ISettingsWriteTestHook
{
    internal Action<string> OnBeforeDirectoryCreation { get; init; } = _ => { };

    internal Action<string> OnDirectoryCreated { get; init; } = _ => { };

    internal Action<string> OnBeforeTemporaryCreation { get; init; } = _ => { };

    internal Action<string> OnTemporaryFlushed { get; init; } = _ => { };

    internal Action<string> OnBeforePublish { get; init; } = _ => { };

    internal Action<string> OnPublished { get; init; } = _ => { };

    public void BeforeDirectoryCreation(string directoryPath)
    {
        OnBeforeDirectoryCreation(directoryPath);
    }

    public void DirectoryCreated(string directoryPath)
    {
        OnDirectoryCreated(directoryPath);
    }

    public void BeforeTemporaryCreation(string temporaryPath)
    {
        OnBeforeTemporaryCreation(temporaryPath);
    }

    public void TemporaryFlushed(string temporaryPath)
    {
        OnTemporaryFlushed(temporaryPath);
    }

    public void BeforePublish(string documentPath)
    {
        OnBeforePublish(documentPath);
    }

    public void Published(string documentPath)
    {
        OnPublished(documentPath);
    }
}
