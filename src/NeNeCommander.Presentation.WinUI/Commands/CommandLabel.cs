using System;
using NeNeCommander.Application.Input;

namespace NeNeCommander.Presentation.WinUI.Commands;

/// <summary>Pairs one canonical intent with its sole localized command-label resource.</summary>
public sealed record CommandLabel
{
    internal CommandLabel(UserIntent intent, string resourceKey)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        Intent = intent;
        ResourceKey = resourceKey;
    }

    /// <summary>Gets the canonical intent.</summary>
    public UserIntent Intent { get; }

    /// <summary>Gets the localized title resource shared by hints and command search.</summary>
    public string ResourceKey { get; }
}
