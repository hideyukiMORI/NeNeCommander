using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Resolves complete localized confirmation templates while retaining the presentation's exact disclosed set.</summary>
public sealed record ConflictModalText
{
    private readonly ImmutableList<ConflictOverwriteRow> _otherOverwriteRows;
    private ConflictModalText(ActiveConflictModal active, Func<string, string> localize)
    {
        ConflictOverwriteRow? first = active.Overwrite is TransferOverwriteCapability.QualifiedOverwrite qualified
            ? new ConflictOverwriteRow(qualified.Evidence, active.MetadataFormat, localize) : null;
        SourceText = first?.SourceText ?? Format(localize("ConflictSourcePath"), active.SourceText);
        TargetText = first?.TargetText ?? Format(localize("ConflictTargetPath"), active.ExistingTargetText);
        CandidateText = Format(localize("ConflictCandidatePath"), active.KeepBothCandidateText);
        UnavailableText = active.Overwrite is TransferOverwriteCapability.UnavailableOverwrite unavailable
            ? localize(UnavailableKey(unavailable.Reason)) : string.Empty;
        OverwriteActionName = Format(localize("ConflictOverwriteActionName"), active.ExistingTargetText);
        OverwriteScopeText = Format(localize("ConflictOverwriteScope"), active.OtherOverwriteConflicts.Count);
        _otherOverwriteRows = [.. active.OtherOverwriteConflicts.Select(conflict =>
            new ConflictOverwriteRow(((TransferOverwriteCapability.QualifiedOverwrite)conflict.Target.Overwrite).Evidence,
                active.MetadataFormat, localize))];
    }

    /// <summary>Gets the full source path and known file details.</summary>
    public string SourceText { get; }
    /// <summary>Gets the full target path and known file details.</summary>
    public string TargetText { get; }
    /// <summary>Gets the alternate path associated with KeepBoth.</summary>
    public string CandidateText { get; }
    /// <summary>Gets an explicit refusal explanation, or empty text when overwrite is admitted.</summary>
    public string UnavailableText { get; }
    /// <summary>Gets the accessible destructive action name including the exact current target.</summary>
    public string OverwriteActionName { get; }
    /// <summary>Gets the explicit scope wording for the count of other disclosed eligible files.</summary>
    public string OverwriteScopeText { get; }
    /// <summary>Gets the complete immutable set of other original file pairs shown before all-target consent.</summary>
    public IReadOnlyList<ConflictOverwriteRow> OtherOverwriteRows => _otherOverwriteRows;

    /// <summary>Resolves a display without changing consent, operation state or native focus.</summary>
    public static ConflictModalText Create(ActiveConflictModal active, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(localize);
        return new ConflictModalText(active, localize);
    }

    private static string UnavailableKey(TransferOverwriteUnavailableReason reason)
    {
        return reason switch
        {
            _ when reason == TransferOverwriteUnavailableReason.Folder => "ConflictOverwriteUnavailableFolder",
            _ when reason == TransferOverwriteUnavailableReason.UnsupportedLocation => "ConflictOverwriteUnavailableLocation",
            _ when reason == TransferOverwriteUnavailableReason.SameObject => "ConflictOverwriteUnavailableAlias",
            _ when reason == TransferOverwriteUnavailableReason.MultipleLinks => "ConflictOverwriteUnavailableLinks",
            _ when reason == TransferOverwriteUnavailableReason.SpecialAttributes => "ConflictOverwriteUnavailableAttributes",
            _ when reason == TransferOverwriteUnavailableReason.StreamsUnconfirmed => "ConflictOverwriteUnavailableStreams",
            _ when reason == TransferOverwriteUnavailableReason.ReservedTarget => "ConflictOverwriteUnavailableReservation",
            _ => "ConflictOverwriteUnavailableUnconfirmed",
        };
    }

    private static string Format(string template, object value)
    {
        return string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(template), value);
    }
}
