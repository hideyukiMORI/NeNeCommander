namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>No entry exists at the settings document path.</summary>
internal sealed record AbsentDocument : ExistingDocumentSnapshot
{
    internal static AbsentDocument Instance { get; } = new();

    private AbsentDocument()
    {
    }
}
