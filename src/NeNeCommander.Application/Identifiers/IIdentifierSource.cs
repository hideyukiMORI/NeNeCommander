using System;

namespace NeNeCommander.Application.Identifiers;

/// <summary>Supplies an opaque candidate token for one provider-owned settings or transfer preparation attempt.</summary>
public interface IIdentifierSource
{
    /// <summary>Returns the next candidate token without asserting filesystem ownership or uniqueness.</summary>
    /// <returns>An opaque candidate token.</returns>
    public Guid NextIdentifier();
}
