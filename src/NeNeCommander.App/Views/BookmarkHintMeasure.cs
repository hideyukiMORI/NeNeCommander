using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace NeNeCommander.App.Views;

/// <summary>
/// Measures passive key-hint content using the same loaded templates as the interactive band.
/// Its subtree has no automation children, interactive controls, input or focus target.
/// </summary>
public sealed partial class BookmarkHintMeasure : ItemsControl
{
    /// <summary>Hides the measurement-only text from the accessible control and content trees.</summary>
    /// <returns>A leaf peer for the inert measurement host.</returns>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new MeasurementPeer(this);
    }

    private sealed partial class MeasurementPeer : FrameworkElementAutomationPeer
    {
        internal MeasurementPeer(BookmarkHintMeasure owner)
            : base(owner)
        {
        }

        protected override IList<AutomationPeer>? GetChildrenCore()
        {
            return null;
        }

        protected override bool IsControlElementCore()
        {
            return false;
        }

        protected override bool IsContentElementCore()
        {
            return false;
        }
    }
}
