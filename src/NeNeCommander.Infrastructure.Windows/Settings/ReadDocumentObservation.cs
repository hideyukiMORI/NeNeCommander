using NeNeCommander.Application.Settings;

namespace NeNeCommander.Infrastructure.Windows.Settings;

/// <summary>One read's typed outcome together with the document baseline it establishes.</summary>
internal sealed record ReadDocumentObservation
{
    internal ReadDocumentObservation(
        SettingsReadOutcome outcome,
        ExistingDocumentSnapshot document)
    {
        Outcome = outcome;
        Document = document;
    }

    internal SettingsReadOutcome Outcome { get; }

    internal ExistingDocumentSnapshot Document { get; }
}
