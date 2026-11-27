namespace NeNeCommander.Application.FileOperations;

/// <summary>Explains the first observed refusal without claiming unknown facts are safe.</summary>
public abstract record TransferOverwriteUnavailableReason
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private TransferOverwriteUnavailableReason()
    {
    }

    /// <summary>A source or destination is an observed folder.</summary>
    public static TransferOverwriteUnavailableReason Folder { get; } = new FolderReason();

    /// <summary>The path or observed filesystem is outside the admitted local NTFS scope.</summary>
    public static TransferOverwriteUnavailableReason UnsupportedLocation { get; } = new UnsupportedLocationReason();

    /// <summary>Source and destination are the same lexical path or native object.</summary>
    public static TransferOverwriteUnavailableReason SameObject { get; } = new SameObjectReason();

    /// <summary>A file has more than one observed hardlink.</summary>
    public static TransferOverwriteUnavailableReason MultipleLinks { get; } = new MultipleLinksReason();

    /// <summary>A file or ancestor has an excluded attribute or link shape.</summary>
    public static TransferOverwriteUnavailableReason SpecialAttributes { get; } = new SpecialAttributesReason();

    /// <summary>The complete default-only stream shape could not be confirmed; no additional stream is removed.</summary>
    public static TransferOverwriteUnavailableReason StreamsUnconfirmed { get; } = new StreamsUnconfirmedReason();

    /// <summary>Another batch entry reserves the target; a reservation is not an existing-object observation.</summary>
    public static TransferOverwriteUnavailableReason ReservedTarget { get; } = new ReservedTargetReason();

    /// <summary>Required current facts, rights, identity, path or preparation shape could not be confirmed.</summary>
    public static TransferOverwriteUnavailableReason Unconfirmed { get; } = new UnconfirmedReason();

    private sealed record FolderReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UnsupportedLocationReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record SameObjectReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record MultipleLinksReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record SpecialAttributesReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record StreamsUnconfirmedReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record ReservedTargetReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }

    private sealed record UnconfirmedReason : TransferOverwriteUnavailableReason
    {
        private protected override void RestrictDerivation()
        {
        }
    }
}
