using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Owns native controls for exactly one rendered conflict without rebinding old gestures.</summary>
internal sealed class ConflictModalView
{
    private readonly Border _modal;
    private readonly DataTemplate _template;
    private readonly Action<UserIntent> _forward;
    private ActiveConflictModal? _rendered;
    private ActiveConflictModal? _ready;

    internal ConflictModalView(Border modal, DataTemplate template, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(modal);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(forward);
        _modal = modal;
        _template = template;
        _forward = forward;
    }

    internal void Render(ConflictModalPresentation presentation)
    {
        if (presentation is not ActiveConflictModal active)
        {
            Hide();
            return;
        }
        if (ReferenceEquals(_rendered?.SourceState, active.SourceState))
        {
            return;
        }
        _rendered = active;
        _ready = null;
        FrameworkElement content = (FrameworkElement)_template.LoadContent();
        Bind(active, content);
        if (!ReferenceEquals(_rendered, active))
        {
            return;
        }
        _modal.Child = content;
        if (!IsCurrent(active, content))
        {
            return;
        }
        _modal.Visibility = Visibility.Visible;
        if (IsCurrent(active, content))
        {
            _ = Find<Button>(content, "ConflictCancel").Focus(FocusState.Programmatic);
            if (IsCurrent(active, content))
            {
                _ready = active;
            }
        }
    }

    private void Hide()
    {
        _rendered = null;
        _ready = null;
        _modal.Visibility = Visibility.Collapsed;
        if (_rendered is null)
        {
            _modal.Child = null;
        }
    }

    private void Bind(ActiveConflictModal active, FrameworkElement content)
    {
        Find<TextBlock>(content, "ConflictSource").Text = active.SourceText;
        Find<TextBlock>(content, "ConflictExistingTarget").Text = active.ExistingTargetText;
        Find<TextBlock>(content, "ConflictKeepBothCandidate").Text = active.KeepBothCandidateText;
        Find<Button>(content, "ConflictSkip").Click += (_, _) => Forward(active, content, TransferConflictDecision.Skip);
        Find<Button>(content, "ConflictKeepBoth").Click += (_, _) => Forward(active, content, TransferConflictDecision.KeepBoth);
        Find<Button>(content, "ConflictCancel").Click += (_, _) => Forward(active, content, TransferConflictDecision.Cancel);
    }

    private void Forward(ActiveConflictModal active, FrameworkElement content, TransferConflictDecision decision)
    {
        if (ReferenceEquals(_ready, active) && IsCurrent(active, content) && _modal.Visibility == Visibility.Visible)
        {
            TransferConflictScope scope = Find<CheckBox>(content, "ConflictApplyToAll").IsChecked is true
                ? TransferConflictScope.All
                : TransferConflictScope.Current;
            _forward(UserIntent.ResolveConflict(active.SourceState, decision, scope));
        }
    }

    private bool IsCurrent(ActiveConflictModal active, FrameworkElement content)
    {
        return ReferenceEquals(_rendered, active) && ReferenceEquals(_modal.Child, content);
    }

    private static T Find<T>(FrameworkElement content, string name)
        where T : DependencyObject
    {
        return content.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Conflict control '{name}' is missing.");
    }
}
