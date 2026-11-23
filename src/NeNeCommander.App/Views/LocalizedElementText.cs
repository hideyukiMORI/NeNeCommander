using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace NeNeCommander.App.Views;

/// <summary>Owns one element's loaded lifetime subscription and only its declared localized properties.</summary>
internal sealed class LocalizedElementText
{
    private readonly FrameworkElement _element;
    private readonly HashSet<DependencyProperty> _assigned = [];
    private ViewTextResources? _subscribed;

    internal LocalizedElementText(FrameworkElement element)
    {
        _element = element;
        _element.Loaded += OnLoaded;
        _element.Unloaded += OnUnloaded;
    }

    internal void Update()
    {
        ViewTextResources? resources = LocalizedText.GetResources(_element);
        Unsubscribe();
        if (resources is null)
        {
            foreach (DependencyProperty property in _assigned)
            {
                _element.ClearValue(property);
            }
            _assigned.Clear();
            return;
        }
        if (_element.IsLoaded)
        {
            _subscribed = resources;
            resources.Changed += OnLanguageChanged;
        }
        Render(resources);
    }

    private void Render(ViewTextResources resources)
    {
        Assign(LocalizedText.GetTextKey(_element), TextBlock.TextProperty, resources);
        Assign(LocalizedText.GetContentKey(_element), ContentControl.ContentProperty, resources);
        Assign(LocalizedText.GetNameKey(_element), AutomationProperties.NameProperty, resources);
        Assign(LocalizedText.GetHelpTextKey(_element), AutomationProperties.HelpTextProperty, resources);
        Assign(LocalizedText.GetPlaceholderKey(_element), TextBox.PlaceholderTextProperty, resources);
    }

    private void Assign(string key, DependencyProperty property, ViewTextResources resources)
    {
        if (key.Length != 0)
        {
            _ = _assigned.Add(property);
            _element.SetValue(property, resources.GetString(key));
        }
        else if (_assigned.Remove(property))
        {
            _element.ClearValue(property);
        }
    }

    private void Unsubscribe()
    {
        _subscribed?.Changed -= OnLanguageChanged;
        _subscribed = null;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        Update();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        Unsubscribe();
    }

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        if (_subscribed is ViewTextResources resources)
        {
            Render(resources);
        }
    }
}
