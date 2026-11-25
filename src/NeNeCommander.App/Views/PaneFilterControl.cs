using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Renders one native inline editor without selecting rows, filtering names or owning a query.</summary>
internal sealed class PaneFilterControl
{
    private readonly FrameworkElement _root;
    private readonly ViewTextResources _resources;
    private readonly PaneSide _side;
    private readonly Action<UserIntent> _forward;
    private readonly FrameworkElement _bar;
    private readonly TextBox _input;
    private readonly Button _open;
    private readonly Button _clear;
    private readonly TextBlock _hint;
    private PaneFilterPresentation? _rendered;
    private int _renderDepth;
    private bool _stopping;

    internal PaneFilterControl(FrameworkElement root, ViewTextResources resources, PaneSide side, Action<UserIntent> forward)
    {
        _root = root;
        _resources = resources;
        _side = side;
        _forward = forward;
        string prefix = side == PaneSide.Left ? "Left" : "Right";
        _bar = Find<FrameworkElement>(prefix + "FilterBar");
        _input = Find<TextBox>(prefix + "FilterInput");
        _open = Find<Button>(prefix + "OpenFilter");
        _clear = Find<Button>(prefix + "ClearFilter");
        _hint = Find<TextBlock>(prefix + "FilterHint");
        _open.Click += (_, _) => Request(PaneFilterEntryAction.Open);
        _clear.Click += (_, _) => Request(PaneFilterEntryAction.Clear);
        _input.GotFocus += OnGotFocus;
        _input.LostFocus += OnLostFocus;
        _input.TextChanged += OnTextChanged;
        _input.TextCompositionStarted += OnCompositionStarted;
        _input.TextCompositionEnded += OnCompositionEnded;
    }

    internal bool IsComposing { get; private set; }

    internal bool ContainsInput(object? focused)
    {
        return ReferenceEquals(focused, _input);
    }

    internal PaneSide? Render(CommanderSnapshot snapshot)
    {
        if (_stopping)
        {
            return null;
        }
        PaneFilterPresentation presentation = PaneFilterPresenter.Present(snapshot, _side, _rendered);
        _rendered = presentation;
        _renderDepth++;
        try
        {
            RenderControls(presentation);
            if (presentation.FocusEditor && _input.FocusState == FocusState.Unfocused &&
                _input.Focus(FocusState.Programmatic) && ReferenceEquals(_rendered, presentation))
            {
                _input.SelectAll();
            }
        }
        finally
        {
            _renderDepth--;
        }
        return ReferenceEquals(_rendered, presentation) && !_stopping ? presentation.FileListFocusSide : null;
    }

    private void RenderControls(PaneFilterPresentation presentation)
    {
        _open.IsEnabled = presentation.IsEnabled;
        _clear.IsEnabled = presentation.IsEnabled;
        _input.IsEnabled = presentation.IsEnabled;
        _bar.Visibility = presentation.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        if (!presentation.IsEditing)
        {
            IsComposing = false;
        }
        if (!IsComposing && presentation.SynchronizeText && _input.Text != presentation.Text)
        {
            _input.Text = presentation.Text;
        }
        _hint.Text = PaneFilterPresenter.ComposeHint(presentation, _resources.GetString);
        ToolTipService.SetToolTip(_hint, _hint.Text);
        AutomationProperties.SetHelpText(_input, _hint.Text);
    }

    private void Request(PaneFilterEntryAction action)
    {
        if (!_stopping && _rendered is PaneFilterPresentation rendered)
        {
            _forward(UserIntent.RequestPaneFilter(_side, rendered.SourcePane, action));
        }
    }

    private void OnGotFocus(object sender, RoutedEventArgs args)
    {
        if (_renderDepth == 0 && _rendered is { IsEditing: false })
        {
            Request(PaneFilterEntryAction.Open);
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs args)
    {
        if (!_stopping && _renderDepth == 0 && _rendered?.Editor is PaneFilterEditing editor)
        {
            _forward(UserIntent.FinishPaneFilter(editor, PaneFilterEditAction.Blur, CommittedText(editor)));
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        ForwardCommittedText();
    }

    private void OnCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {
        IsComposing = true;
    }

    private void OnCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
    {
        IsComposing = false;
        ForwardCommittedText();
    }

    private void ForwardCommittedText()
    {
        if (!_stopping && _renderDepth == 0 && !IsComposing && _rendered?.Editor is PaneFilterEditing editor)
        {
            _forward(UserIntent.ChangePaneFilterText(editor, _input.Text));
        }
    }

    internal string CommittedText(PaneFilterEditing expected)
    {
        return ReferenceEquals(_rendered?.Editor, expected) && !IsComposing ? _input.Text : expected.Query.Text;
    }

    internal void Stop()
    {
        _stopping = true;
        IsComposing = false;
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Pane filter control '{name}' is missing.");
    }
}
