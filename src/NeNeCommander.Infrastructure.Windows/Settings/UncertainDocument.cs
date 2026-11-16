namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>A published document whose identity could not be linked to the owned temporary entry.</summary>
internal sealed record UncertainDocument : ExistingDocumentSnapshot
{
    internal static UncertainDocument Instance { get; } = new();

    private UncertainDocument()
    {
    }
}
