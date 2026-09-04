using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;

namespace NeNeCommander.App.Views;

/// <summary>
/// Resolves one localization resource key produced by the presentation into the localized text for
/// the current language. The converter performs a lookup only; no user-facing text is assembled
/// here or in the template (CS-025). It exists because a XAML template cannot use <c>x:Uid</c> for
/// text whose resource key is data rather than markup.
/// </summary>
public sealed partial class LocalizedTextConverter : IValueConverter
{
    private static readonly ResourceLoader Resources = new();

    /// <summary>Returns the localized text named by the key.</summary>
    /// <param name="value">Localization resource key produced by a presentation value.</param>
    /// <param name="targetType">Framework target type; not used.</param>
    /// <param name="parameter">Framework parameter; not used.</param>
    /// <param name="language">Framework language tag; not used.</param>
    /// <returns>The localized text, which is empty when the resource declares no text.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return Resources.GetString((string)value);
    }

    /// <summary>Not supported: localized text is never converted back to its key.</summary>
    /// <param name="value">Framework value; not used.</param>
    /// <param name="targetType">Framework target type; not used.</param>
    /// <param name="parameter">Framework parameter; not used.</param>
    /// <param name="language">Framework language tag; not used.</param>
    /// <returns>This member always throws.</returns>
    /// <exception cref="NotSupportedException">Always, because the binding is one way.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException("Localized text is bound one way.");
    }
}
