using System;
using Microsoft.UI.Xaml;

namespace NeNeCommander.App.Views;

/// <summary>Declares localized dependency-property targets using the window's explicit resource owner.</summary>
public sealed partial class LocalizedText : DependencyObject
{
    private static readonly DependencyProperty RegistrationProperty = DependencyProperty.RegisterAttached(
        "Registration", typeof(LocalizedElementText), typeof(LocalizedText), new PropertyMetadata(null));

    /// <summary>Identifies the attached Resources value.</summary>
    public static readonly DependencyProperty ResourcesProperty = DependencyProperty.RegisterAttached(
        "Resources", typeof(ViewTextResources), typeof(LocalizedText), new PropertyMetadata(null, OnChanged));

    /// <summary>Reads the target's Resources value.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <returns>The declared value.</returns>
    public static ViewTextResources? GetResources(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return (ViewTextResources?)target.GetValue(ResourcesProperty);
    }

    /// <summary>Declares the target's Resources value without changing interaction state.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <param name="value">The resource owner or localization key.</param>
    public static void SetResources(DependencyObject target, ViewTextResources? value)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(ResourcesProperty, value);
    }

    /// <summary>Identifies the attached TextKey value.</summary>
    public static readonly DependencyProperty TextKeyProperty = DependencyProperty.RegisterAttached(
        "TextKey", typeof(string), typeof(LocalizedText), new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Reads the target's TextKey value.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <returns>The declared value.</returns>
    public static string GetTextKey(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return (string?)target.GetValue(TextKeyProperty) ?? string.Empty;
    }

    /// <summary>Declares the target's TextKey value without changing interaction state.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <param name="value">The resource owner or localization key.</param>
    public static void SetTextKey(DependencyObject target, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(TextKeyProperty, value);
    }

    /// <summary>Identifies the attached ContentKey value.</summary>
    public static readonly DependencyProperty ContentKeyProperty = DependencyProperty.RegisterAttached(
        "ContentKey", typeof(string), typeof(LocalizedText), new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Reads the target's ContentKey value.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <returns>The declared value.</returns>
    public static string GetContentKey(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return (string?)target.GetValue(ContentKeyProperty) ?? string.Empty;
    }

    /// <summary>Declares the target's ContentKey value without changing interaction state.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <param name="value">The resource owner or localization key.</param>
    public static void SetContentKey(DependencyObject target, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(ContentKeyProperty, value);
    }

    /// <summary>Identifies the attached NameKey value.</summary>
    public static readonly DependencyProperty NameKeyProperty = DependencyProperty.RegisterAttached(
        "NameKey", typeof(string), typeof(LocalizedText), new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Reads the target's NameKey value.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <returns>The declared value.</returns>
    public static string GetNameKey(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return (string?)target.GetValue(NameKeyProperty) ?? string.Empty;
    }

    /// <summary>Declares the target's NameKey value without changing interaction state.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <param name="value">The resource owner or localization key.</param>
    public static void SetNameKey(DependencyObject target, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(NameKeyProperty, value);
    }

    /// <summary>Identifies the attached HelpTextKey value.</summary>
    public static readonly DependencyProperty HelpTextKeyProperty = DependencyProperty.RegisterAttached(
        "HelpTextKey", typeof(string), typeof(LocalizedText), new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Reads the target's HelpTextKey value.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <returns>The declared value.</returns>
    public static string GetHelpTextKey(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return (string?)target.GetValue(HelpTextKeyProperty) ?? string.Empty;
    }

    /// <summary>Declares the target's HelpTextKey value without changing interaction state.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <param name="value">The resource owner or localization key.</param>
    public static void SetHelpTextKey(DependencyObject target, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(HelpTextKeyProperty, value);
    }

    /// <summary>Identifies the attached PlaceholderKey value.</summary>
    public static readonly DependencyProperty PlaceholderKeyProperty = DependencyProperty.RegisterAttached(
        "PlaceholderKey", typeof(string), typeof(LocalizedText), new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Reads the target's PlaceholderKey value.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <returns>The declared value.</returns>
    public static string GetPlaceholderKey(DependencyObject target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return (string?)target.GetValue(PlaceholderKeyProperty) ?? string.Empty;
    }

    /// <summary>Declares the target's PlaceholderKey value without changing interaction state.</summary>
    /// <param name="target">Framework element carrying the declaration.</param>
    /// <param name="value">The resource owner or localization key.</param>
    public static void SetPlaceholderKey(DependencyObject target, string value)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.SetValue(PlaceholderKeyProperty, value);
    }

    private static void OnChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        _ = args;
        if (target is not FrameworkElement element)
        {
            throw new InvalidOperationException("Localized text requires a framework element.");
        }
        if (target.GetValue(RegistrationProperty) is not LocalizedElementText registration)
        {
            registration = new LocalizedElementText(element);
            target.SetValue(RegistrationProperty, registration);
        }
        registration.Update();
    }
}
