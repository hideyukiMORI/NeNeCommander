using System;
using Microsoft.UI.Xaml.Data;

namespace NeNeCommander.App.Views;

/// <summary>
/// Resolves one semantic design resource key produced by the presentation into the resource the
/// application merged for the selected color scheme. The converter performs a lookup only: the
/// choice of key is made by a closed presentation value, so no visual policy lives in the view
/// (ARC-012, CMD-003). It exists because a XAML template cannot resolve a resource whose key is
/// data rather than markup.
/// </summary>
public sealed partial class SemanticResourceConverter : IValueConverter
{
    /// <summary>Returns the application resource named by the key.</summary>
    /// <param name="value">Resource key produced by a presentation value.</param>
    /// <param name="targetType">Framework target type; not used.</param>
    /// <param name="parameter">Framework parameter; not used.</param>
    /// <param name="language">Framework language tag; not used.</param>
    /// <returns>The resolved application resource.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return Microsoft.UI.Xaml.Application.Current.Resources[(string)value];
    }

    /// <summary>Not supported: a resolved resource is never converted back to its key.</summary>
    /// <param name="value">Framework value; not used.</param>
    /// <param name="targetType">Framework target type; not used.</param>
    /// <param name="parameter">Framework parameter; not used.</param>
    /// <param name="language">Framework language tag; not used.</param>
    /// <returns>This member always throws.</returns>
    /// <exception cref="NotSupportedException">Always, because the binding is one way.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException("A semantic resource is bound one way.");
    }
}
