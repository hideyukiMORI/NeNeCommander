using System;

namespace NeNeCommander.Application.Input;

/// <summary>
/// Represents the confirmation of a name-entry modal carrying the untrusted text the user typed.
/// Validation belongs to the request the session builds from it, never to the host.
/// </summary>
public sealed record NameSubmission : UserIntent
{
    internal NameSubmission(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Gets the untrusted name text exactly as entered.</summary>
    public string Name { get; }
}
