using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Presentation.WinUI.Panes;

namespace NeNeCommander.App.Views;

/// <summary>Updates the existing operation bar's evidence once per outcome or language without taking focus.</summary>
internal sealed class TransferResultView
{
    private readonly ScrollViewer _viewport;
    private readonly ItemsControl _rows;
    private readonly ViewTextResources _resources;
    private FileOperationOutcome? _rendered;
    private string _language = string.Empty;

    internal TransferResultView(ScrollViewer viewport, ItemsControl rows, ViewTextResources resources)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(resources);
        _viewport = viewport;
        _rows = rows;
        _resources = resources;
    }

    internal void Render(OperationDetail detail)
    {
        if (detail is not TransferResultDetail result)
        {
            _rendered = null;
            _rows.ItemsSource = null;
            _viewport.Visibility = Visibility.Collapsed;
            return;
        }
        string language = _resources.LanguageTag;
        if (ReferenceEquals(_rendered, result.Outcome) && string.Equals(_language, language, StringComparison.Ordinal))
        {
            return;
        }
        _rendered = result.Outcome;
        _language = language;
        IReadOnlyList<string> facts = TransferResultFacts.Compose(result, _resources.GetString);
        _rows.ItemsSource = facts;
        _viewport.Visibility = facts.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
}
