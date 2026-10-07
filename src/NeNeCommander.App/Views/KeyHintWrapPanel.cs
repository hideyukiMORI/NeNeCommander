using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace NeNeCommander.App.Views;

/// <summary>
/// Lays key hints out left to right and wraps them onto another line instead of clipping when the
/// available width runs out, as the window-adjustment handoff requires. The gaps are read from the
/// semantic spacing resources, so the panel declares no visual constant of its own (CS-023).
/// </summary>
public sealed partial class KeyHintWrapPanel : Panel
{
    private const string HintGapResourceKey = "SpacingOperationDetailGap";
    private const string LineGapResourceKey = "SpacingKeyHintGap";

    /// <summary>Measures every hint at its natural width and reports the wrapped extent.</summary>
    /// <param name="availableSize">Space the parent offers.</param>
    /// <returns>The extent of the wrapped hints.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size unbounded = new(double.PositiveInfinity, double.PositiveInfinity);
        foreach (UIElement child in Children)
        {
            child.Measure(unbounded);
        }
        return ExtentOf(Place(availableSize.Width));
    }

    /// <summary>Arranges every hint at the place the wrapped layout assigns it.</summary>
    /// <param name="finalSize">Space the parent assigned.</param>
    /// <returns>The assigned space.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        List<Rect> places = Place(finalSize.Width);
        for (int index = 0; index < places.Count; index++)
        {
            Children[index].Arrange(places[index]);
        }
        return finalSize;
    }

    private List<Rect> Place(double availableWidth)
    {
        double hintGap = SpacingOf(HintGapResourceKey);
        double lineGap = SpacingOf(LineGapResourceKey);
        List<Rect> places = [];
        double left = 0d;
        double top = 0d;
        double lineHeight = 0d;
        foreach (Size desired in Children.Select(static child => child.DesiredSize))
        {
            if (left > 0d && left + desired.Width > availableWidth)
            {
                left = 0d;
                top += lineHeight + lineGap;
                lineHeight = 0d;
            }
            places.Add(new Rect(left, top, desired.Width, desired.Height));
            left += desired.Width + hintGap;
            lineHeight = Math.Max(lineHeight, desired.Height);
        }
        return places;
    }

    private static Size ExtentOf(List<Rect> places)
    {
        double width = 0d;
        double height = 0d;
        foreach (Rect place in places)
        {
            width = Math.Max(width, place.Right);
            height = Math.Max(height, place.Bottom);
        }
        return new Size(width, height);
    }

    private static double SpacingOf(string resourceKey)
    {
        return (double)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }
}
