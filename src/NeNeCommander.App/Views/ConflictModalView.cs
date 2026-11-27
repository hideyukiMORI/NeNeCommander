using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
    private readonly ViewTextResources _resources;
    private ActiveConflictModal? _rendered;
    private ActiveConflictModal? _displayed;
    private ActiveConflictModal? _ready;

    internal ConflictModalView(Border modal, DataTemplate template, Action<UserIntent> forward, ViewTextResources resources)
    {
        ArgumentNullException.ThrowIfNull(modal);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(resources);
        _resources = resources;
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
            RefreshText(active);
            return;
        }
        _rendered = active;
        _displayed = active;
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
        _displayed = null;
        _ready = null;
        _modal.Visibility = Visibility.Collapsed;
        if (_rendered is null)
        {
            _modal.Child = null;
        }
    }

    private void RefreshText(ActiveConflictModal active)
    {
        if (!ReferenceEquals(_displayed, active) && _modal.Child is FrameworkElement content)
        {
            _displayed = active;
            BindText(active, content);
        }
    }

    private void Bind(ActiveConflictModal active, FrameworkElement content)
    {
        BindText(active, content);
        CheckBox ordinaryScope = Find<CheckBox>(content, "ConflictApplyToAll");
        CheckBox overwriteScope = Find<CheckBox>(content, "ConflictOverwriteAll");
        Find<Button>(content, "ConflictSkip").Click += (_, _) => Forward(active, content, TransferConflictDecision.Skip, ordinaryScope);
        Find<Button>(content, "ConflictKeepBoth").Click += (_, _) => Forward(active, content, TransferConflictDecision.KeepBoth, ordinaryScope);
        Find<Button>(content, "ConflictOverwrite").Click += (_, _) => Forward(active, content, TransferConflictDecision.OverwriteContents, overwriteScope);
        Find<Button>(content, "ConflictCancel").Click += (_, _) => Forward(active, content, TransferConflictDecision.Cancel, ordinaryScope);
    }

    private void BindText(ActiveConflictModal active, FrameworkElement content)
    {
        ConflictModalText text = ConflictModalText.Create(active, _resources.GetString);
        Find<TextBlock>(content, "ConflictSource").Text = text.SourceText;
        Find<TextBlock>(content, "ConflictExistingTarget").Text = text.TargetText;
        Find<TextBlock>(content, "ConflictKeepBothCandidate").Text = text.CandidateText;
        Find<TextBlock>(content, "ConflictOverwriteUnavailable").Text = text.UnavailableText;
        Find<TextBlock>(content, "ConflictOverwriteUnavailable").Visibility = active.CanOverwrite ? Visibility.Collapsed : Visibility.Visible;
        Find<StackPanel>(content, "ConflictOverwritePanel").Visibility = active.CanOverwrite ? Visibility.Visible : Visibility.Collapsed;
        Find<StackPanel>(content, "ConflictOverwriteScopePanel").Visibility = active.CanOverwriteAll ? Visibility.Visible : Visibility.Collapsed;
        Find<TextBlock>(content, "ConflictOverwriteScope").Text = text.OverwriteScopeText;
        Find<ItemsControl>(content, "ConflictOverwriteOtherRows").ItemsSource = text.OtherOverwriteRows;
        Button overwrite = Find<Button>(content, "ConflictOverwrite");
        overwrite.IsEnabled = active.CanOverwrite;
        AutomationProperties.SetName(overwrite, text.OverwriteActionName);
    }

    private void Forward(ActiveConflictModal active, FrameworkElement content, TransferConflictDecision decision, CheckBox scopeControl)
    {
        if (ReferenceEquals(_ready, active) && IsCurrent(active, content) && _modal.Visibility == Visibility.Visible)
        {
            TransferConflictScope scope = scopeControl.IsChecked is true
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
