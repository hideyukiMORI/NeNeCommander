using System;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Renders the existing OperationBarView controls and forwards their typed intents.</summary>
internal sealed class OperationBarView
{
    private readonly Grid _root;
    private readonly ViewTextResources _resources;
    private readonly Action<UserIntent> _forward;
    private ActiveConflictModal? _renderedConflictModal;
    private readonly Border _conflictModal;
    private readonly TextBlock _conflictSource;
    private readonly TextBlock _conflictExistingTarget;
    private readonly TextBlock _conflictKeepBothCandidate;
    private readonly CheckBox _conflictApplyToAll;
    private readonly Button _conflictCancel;
    private readonly Border _operationBar;
    private readonly Path _operationWarningIcon;
    private readonly Path _operationNameEntryIcon;
    private readonly TextBlock _operationStatus;
    private readonly Border _nameEntryFrame;
    private readonly TextBox _nameEntry;
    private readonly Panel _transferResultSegments;
    private readonly TextBlock _transferResultNotTransferredCount;
    private readonly TextBlock _transferResultCopiedCount;
    private readonly TextBlock _transferResultVerifiedCount;
    private readonly TextBlock _transferResultSourceDeletedCount;
    private readonly TextBlock _transferResultSourcePartiallyDeletedCount;
    private readonly ItemsControl _operationProgressSegments;
    private readonly TextBlock _operationDetailCount;
    private readonly TextBlock _operationProgressSeparator;
    private readonly TextBlock _operationTotal;
    private readonly StackPanel _operationContent;
    private readonly ItemsControl _operationKeyHints;
    private readonly KeyHintWrapPanel _information;
    private readonly Grid _statusGroup;
    private readonly StackPanel _counts;

    internal OperationBarView(Grid root, ViewTextResources resources, Action<UserIntent> forward)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(forward);
        _root = root;
        _resources = resources;
        _forward = forward;
        _conflictModal = Find<Border>("ConflictModal");
        _conflictSource = Find<TextBlock>("ConflictSource");
        _conflictExistingTarget = Find<TextBlock>("ConflictExistingTarget");
        _conflictKeepBothCandidate = Find<TextBlock>("ConflictKeepBothCandidate");
        _conflictApplyToAll = Find<CheckBox>("ConflictApplyToAll");
        _conflictCancel = Find<Button>("ConflictCancel");
        _operationBar = Find<Border>("OperationBar");
        _operationWarningIcon = Find<Path>("OperationWarningIcon");
        _operationNameEntryIcon = Find<Path>("OperationNameEntryIcon");
        _operationStatus = Find<TextBlock>("OperationStatus");
        _nameEntryFrame = Find<Border>("NameEntryFrame");
        _nameEntry = Find<TextBox>("NameEntry");
        _transferResultSegments = Find<Panel>("TransferResultSegments");
        _transferResultNotTransferredCount = Find<TextBlock>("TransferResultNotTransferredCount");
        _transferResultCopiedCount = Find<TextBlock>("TransferResultCopiedCount");
        _transferResultVerifiedCount = Find<TextBlock>("TransferResultVerifiedCount");
        _transferResultSourceDeletedCount = Find<TextBlock>("TransferResultSourceDeletedCount");
        _transferResultSourcePartiallyDeletedCount = Find<TextBlock>("TransferResultSourcePartiallyDeletedCount");
        _operationProgressSegments = Find<ItemsControl>("OperationProgressSegments");
        _operationDetailCount = Find<TextBlock>("OperationDetailCount");
        _operationProgressSeparator = Find<TextBlock>("OperationProgressSeparator");
        _operationTotal = Find<TextBlock>("OperationTotal");
        _operationContent = Find<StackPanel>("OperationContent");
        _operationKeyHints = Find<ItemsControl>("OperationKeyHints");
        _information = Find<KeyHintWrapPanel>("OperationInformation");
        _statusGroup = Find<Grid>("OperationStatusGroup");
        _counts = Find<StackPanel>("OperationCounts");
        Find<Button>("ConflictSkip").Click += OnConflictSkip;
        Find<Button>("ConflictKeepBoth").Click += OnConflictKeepBoth;
        _conflictCancel.Click += OnConflictCancel;
    }

    internal void Render(DualPanePresentation presentation)
    {
        _operationStatus.Text = _resources.GetString(presentation.OperationStatus.ResourceKey);
        AutomationProperties.SetAutomationId(_operationContent, presentation.DeletionConfirmationAutomationId);
        RenderTone(presentation.Tone);
        RenderDetail(presentation.Detail);
        if (!ReferenceEquals(_operationKeyHints.ItemsSource, presentation.KeyHints))
        {
            _operationKeyHints.ItemsSource = presentation.KeyHints;
        }
        RenderNameEntry(presentation.NameEntry);
        RenderInformationVisibility();
        FocusNameEntry();
        RenderConflict(presentation.ConflictModal);
    }

    private void RenderInformationVisibility()
    {
        _statusGroup.Visibility = _operationStatus.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        _counts.Visibility = _operationDetailCount.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        _information.Visibility = _information.Children.Any(static child => child.Visibility == Visibility.Visible)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RenderTone(OperationBarTone tone)
    {
        _operationBar.Background = ResolveBrush(tone.SurfaceBrushResourceKey);
        _operationBar.BorderBrush = ResolveBrush(tone.BorderBrushResourceKey);
        Brush foreground = ResolveBrush(tone.ForegroundBrushResourceKey);
        _operationStatus.Foreground = foreground;
        _operationDetailCount.Foreground = foreground;
        _operationProgressSeparator.Foreground = foreground;
        _operationTotal.Foreground = foreground;
        _operationWarningIcon.Stroke = foreground;
        _operationNameEntryIcon.Stroke = foreground;
        RenderToneIcon(tone.Icon);
    }

    private void RenderToneIcon(OperationBarIcon icon)
    {
        _operationWarningIcon.Visibility = icon == OperationBarIcon.Warning
            ? Visibility.Visible
            : Visibility.Collapsed;
        _operationNameEntryIcon.Visibility = icon == OperationBarIcon.NameEntry
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RenderDetail(OperationDetail detail)
    {
        _operationProgressSegments.Visibility = detail is OperationProgressDetail
            ? Visibility.Visible
            : Visibility.Collapsed;
        _transferResultSegments.Visibility = detail is TransferResultDetail
            ? Visibility.Visible
            : Visibility.Collapsed;
        switch (detail)
        {
            case OperationItemCountDetail count:
                _operationProgressSegments.ItemsSource = null;
                _operationDetailCount.Text = count.Count.ToString(CultureInfo.CurrentCulture);
                _operationProgressSeparator.Text = string.Empty;
                _operationTotal.Text = string.Empty;
                break;
            case OperationProgressDetail progress:
                _operationProgressSegments.ItemsSource = progress.Segments;
                _operationDetailCount.Text = progress.Completed.ToString(CultureInfo.CurrentCulture);
                _operationProgressSeparator.Text = _resources.GetString("OperationProgressSeparator");
                _operationTotal.Text = progress.Total.ToString(CultureInfo.CurrentCulture);
                break;
            case TransferResultDetail result:
                _operationProgressSegments.ItemsSource = null;
                _operationDetailCount.Text = string.Empty;
                _operationProgressSeparator.Text = string.Empty;
                _operationTotal.Text = string.Empty;
                _transferResultNotTransferredCount.Text = result.NotTransferred.ToString(CultureInfo.CurrentCulture);
                _transferResultCopiedCount.Text = result.Copied.ToString(CultureInfo.CurrentCulture);
                _transferResultVerifiedCount.Text = result.Verified.ToString(CultureInfo.CurrentCulture);
                _transferResultSourceDeletedCount.Text = result.SourceDeleted.ToString(CultureInfo.CurrentCulture);
                _transferResultSourcePartiallyDeletedCount.Text = result.SourcePartiallyDeleted.ToString(CultureInfo.CurrentCulture);
                break;
            default:
                _operationProgressSegments.ItemsSource = null;
                _operationDetailCount.Text = string.Empty;
                _operationProgressSeparator.Text = string.Empty;
                _operationTotal.Text = string.Empty;
                break;
        }
    }

    private void RenderNameEntry(NameEntryPresentation nameEntry)
    {
        if (nameEntry is not ActiveNameEntry active)
        {
            _nameEntryFrame.Visibility = Visibility.Collapsed;
            return;
        }
        if (_nameEntryFrame.Visibility == Visibility.Collapsed)
        {
            _nameEntry.Text = active.InitialText.Resolve(_resources.GetString("NameEntryTextFileDefault"));
            _nameEntryFrame.Visibility = Visibility.Visible;
        }
    }

    private void FocusNameEntry()
    {
        if (_nameEntryFrame.Visibility == Visibility.Visible)
        {
            _ = _nameEntry.Focus(FocusState.Programmatic);
            _nameEntry.SelectAll();
        }
    }

    private void RenderConflict(ConflictModalPresentation conflictModal)
    {
        if (conflictModal is not ActiveConflictModal active)
        {
            _conflictModal.Visibility = Visibility.Collapsed;
            _renderedConflictModal = null;
            return;
        }
        bool isNewConflict = !ReferenceEquals(_renderedConflictModal, active);
        _conflictSource.Text = active.SourceText;
        _conflictExistingTarget.Text = active.ExistingTargetText;
        _conflictKeepBothCandidate.Text = active.KeepBothCandidateText;
        _conflictModal.Visibility = Visibility.Visible;
        if (isNewConflict)
        {
            _conflictApplyToAll.IsChecked = false;
            _ = _conflictCancel.Focus(FocusState.Programmatic);
        }
        _renderedConflictModal = active;
    }

    private void OnConflictSkip(object _, RoutedEventArgs args)
    {
        ForwardConflictDecision(TransferConflictDecision.Skip);
    }

    private void OnConflictKeepBoth(object _, RoutedEventArgs args)
    {
        ForwardConflictDecision(TransferConflictDecision.KeepBoth);
    }

    private void OnConflictCancel(object _, RoutedEventArgs args)
    {
        ForwardConflictDecision(TransferConflictDecision.Cancel);
    }

    private void ForwardConflictDecision(TransferConflictDecision decision)
    {
        TransferConflictScope scope = _conflictApplyToAll.IsChecked is true
            ? TransferConflictScope.All
            : TransferConflictScope.Current;
        _forward(UserIntent.ResolveConflict(decision, scope));
    }

    private static Brush ResolveBrush(string resourceKey)
    {
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }

    private T Find<T>(string name)
        where T : DependencyObject
    {
        return _root.FindName(name) is T control
            ? control
            : throw new InvalidOperationException($"Operation bar control '{name}' is missing.");
    }
}
