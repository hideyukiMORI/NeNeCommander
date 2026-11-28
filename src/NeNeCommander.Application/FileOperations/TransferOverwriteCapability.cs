using System;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Retains either qualified original overwrite observations or one explicit refusal, never contradictory fields.</summary>
public abstract record TransferOverwriteCapability
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private TransferOverwriteCapability()
    {
    }

    /// <summary>Retains the provider's original observations; execution still requires fresh admission.</summary>
    public static TransferOverwriteCapability Available(TransferOverwriteEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new QualifiedOverwrite(evidence);
    }

    /// <summary>Retains an observed refusal without creating overwrite evidence.</summary>
    public static TransferOverwriteCapability Unavailable(TransferOverwriteUnavailableReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        return new UnavailableOverwrite(reason);
    }

    /// <summary>Contains the exact admitted original pair, not a permanent capability lease.</summary>
    public sealed record QualifiedOverwrite : TransferOverwriteCapability
    {
        internal QualifiedOverwrite(TransferOverwriteEvidence evidence)
        {
            Evidence = evidence;
        }

        /// <summary>Gets the original source and target observations.</summary>
        public TransferOverwriteEvidence Evidence { get; }

        private protected override void RestrictDerivation()
        {
        }
    }

    /// <summary>Contains the first applicable refusal or explicit lack of complete proof.</summary>
    public sealed record UnavailableOverwrite : TransferOverwriteCapability
    {
        internal UnavailableOverwrite(TransferOverwriteUnavailableReason reason)
        {
            Reason = reason;
        }

        /// <summary>Gets the reason overwrite is not offered.</summary>
        public TransferOverwriteUnavailableReason Reason { get; }

        private protected override void RestrictDerivation()
        {
        }
    }
}
