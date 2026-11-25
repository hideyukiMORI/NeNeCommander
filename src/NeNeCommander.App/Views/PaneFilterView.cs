using System;
using Microsoft.UI.Xaml;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Adapts the two native filter controls to the one rendered editor and command route.</summary>
internal sealed class PaneFilterView
{
    private readonly PaneFilterControl _left;
    private readonly PaneFilterControl _right;
    private readonly Action<UserIntent> _forward;
    private readonly Action<PaneFilterClosed> _focusFileList;
    private PaneFilterState _rendered = PaneFilterState.Closed;
    private object? _renderOwner;
    private bool _stopping;

    internal PaneFilterView(FrameworkElement root, ViewTextResources resources,
        Action<UserIntent> forward, Action<PaneFilterClosed> focusFileList)
    {
        _forward = forward;
        _focusFileList = focusFileList;
        _left = new PaneFilterControl(root, resources, PaneSide.Left, forward);
        _right = new PaneFilterControl(root, resources, PaneSide.Right, forward);
    }

    internal bool IsComposing => _left.IsComposing || _right.IsComposing;

    internal bool ContainsInput(object? focused)
    {
        return _left.ContainsInput(focused) || _right.ContainsInput(focused);
    }

    internal void Render(CommanderSnapshot snapshot)
    {
        if (_stopping)
        {
            return;
        }
        object owner = new();
        _renderOwner = owner;
        _rendered = snapshot.Scopes.NameFilter;
        PaneSide? leftFocus = _left.Render(snapshot);
        if (!ReferenceEquals(owner, _renderOwner))
        {
            return;
        }
        PaneSide? rightFocus = _right.Render(snapshot);
        PaneSide? focus = leftFocus ?? rightFocus;
        if (ReferenceEquals(owner, _renderOwner) && focus is not null && _rendered is PaneFilterClosed closed)
        {
            _focusFileList(closed);
        }
    }

    internal void Forward(UserIntent intent)
    {
        if (!_stopping && _rendered is PaneFilterEditing editing)
        {
            _forward(PaneFilterPresenter.Qualify(intent, editing, ControlFor(editing.Side).CommittedText(editing)));
        }
    }

    internal PaneFilterEditSubmission? CaptureExternalExit(UserIntent intent)
    {
        return PaneFilterPresenter.CaptureExternalOwner(intent, _rendered) is PaneFilterEditing editing
            ? UserIntent.FinishPaneFilter(editing, PaneFilterEditAction.Blur, ControlFor(editing.Side).CommittedText(editing))
            : null;
    }

    private PaneFilterControl ControlFor(PaneSide side)
    {
        return side == PaneSide.Left ? _left : _right;
    }

    internal void Stop()
    {
        _stopping = true;
        _renderOwner = null;
        _left.Stop();
        _right.Stop();
    }
}
