using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace NeNeCommander.App.Views;

/// <summary>
/// Copies a row's automation identifier and name onto its realized list container. A WinUI
/// style setter cannot carry a binding, so the container receives them when it is prepared.
/// </summary>
internal static class RowAutomationIdentity
{
    internal static void Attach<TRow>(
        ListViewBase list,
        Func<TRow, string> automationId,
        Func<TRow, string> automationName,
        Func<TRow, string>? automationDescription = null)
        where TRow : class
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(automationId);
        ArgumentNullException.ThrowIfNull(automationName);
        list.ContainerContentChanging += (_, args) =>
        {
            if (args.InRecycleQueue || args.Item is not TRow row)
            {
                return;
            }

            AutomationProperties.SetAutomationId(args.ItemContainer, automationId(row));
            AutomationProperties.SetName(args.ItemContainer, automationName(row));
            if (automationDescription is not null)
            {
                AutomationProperties.SetHelpText(args.ItemContainer, automationDescription(row));
            }
        };
    }
}
