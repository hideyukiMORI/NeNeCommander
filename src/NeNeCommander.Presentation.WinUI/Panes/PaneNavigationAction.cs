using System;
using System.Globalization;
using System.Linq;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Panes;

/// <summary>Projects the active-header action without owning navigation or another admission policy.</summary>
public sealed record PaneNavigationAction
{
    private static readonly string ShortcutResourceKey = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
        .Single(binding => binding.Intent == UserIntent.OpenCurrentLocationInOtherPane).KeyLabelResourceKey;

    private PaneNavigationAction(DualPaneSnapshot snapshot, PaneSide side)
    {
        IsParentEnabled = snapshot.CanInteractWithPanes && snapshot.Of(side).CanNavigateParent;
        IsVisible = snapshot.ActiveSide == side;
        IsEnabled = IsVisible && snapshot.CanOpenCurrentLocationInOtherPane;
        NameResourceKey = side == PaneSide.Left ? "OpenFolderInRightPaneFormat" : "OpenFolderInLeftPaneFormat";
    }

    /// <summary>Gets whether this header is active and its reserved slot displays the action.</summary>
    public bool IsVisible { get; }

    /// <summary>Gets Application readiness for the visible action, including the available same-location no-op.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets parent readiness for this explicit pane, whether active or passive.</summary>
    public bool IsParentEnabled { get; }

    private string NameResourceKey { get; }

    private static readonly string ParentShortcutResourceKey = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
        .Single(binding => binding.Intent == UserIntent.NavigateParent && binding.Modifier == KeyboardModifier.Alt).KeyLabelResourceKey;

    /// <summary>Resolves the parent command's localized name and canonical shortcut.</summary>
    public static string ComposeParentName(Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(localize);
        return string.Format(CultureInfo.InvariantCulture, localize("CommandEntryToolTipFormat"),
            localize("PaneNavigateParent"), localize(ParentShortcutResourceKey));
    }

    /// <summary>Projects the current snapshot for one permanent header without reading or changing either pane.</summary>
    /// <param name="snapshot">The same snapshot used to render the two file lists.</param>
    /// <param name="side">The header containing this action.</param>
    /// <returns>A complete immutable projection, with passive actions hidden and disabled.</returns>
    public static PaneNavigationAction Create(DualPaneSnapshot snapshot, PaneSide side)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(side);
        return new PaneNavigationAction(snapshot, side);
    }

    /// <summary>Resolves the complete directional tooltip and accessible name with the canonical shortcut.</summary>
    /// <param name="localize">Resolves the full sentence template and existing key label.</param>
    /// <returns>The same complete text for tooltip and accessibility, without ambient culture.</returns>
    public string ComposeName(Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(localize);
        return string.Format(CultureInfo.InvariantCulture, localize(NameResourceKey), localize(ShortcutResourceKey));
    }
}
