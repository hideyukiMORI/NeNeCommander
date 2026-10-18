using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.Input;
using NeNeCommander.Presentation.WinUI.Input;
using Windows.System;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardSort behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardSortTests
{
    /// <summary>
    /// Proves Ctrl+F3 and Ctrl+F4 sort only from the file list and the navigation surface, plain
    /// and otherwise modified F3 and F4 pass through there, and every owning context keeps them.
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenSortKeysArriveMapsControlChordOnlyOnPaneSurfaces()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext[] paneSurfaces = [KeyboardContext.FileList, KeyboardContext.NavigationSurface];
        KeyboardContext[] owningContexts =
        [
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
            KeyboardContext.AddressEntry,
            KeyboardContext.CommandPalette,
        ];

        foreach (KeyboardContext context in paneSurfaces)
        {
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F3, KeyboardModifier.Control, context), UserIntent.SortByName);
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F4, KeyboardModifier.Control, context), UserIntent.SortByExtension);
            foreach (KeyboardModifier modifier in new[] { KeyboardModifier.None, KeyboardModifier.Alt, KeyboardModifier.Other })
            {
                _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F3, modifier, context)));
                _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F4, modifier, context)));
            }
        }
        foreach (KeyboardContext context in owningContexts)
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F3, KeyboardModifier.Control, context)));
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F4, KeyboardModifier.Control, context)));
        }
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F3, KeyboardModifier.Control, KeyboardContext.WindowAdjustment)));
        _ = Assert.IsInstanceOfType<KeyboardConsumed>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F4, KeyboardModifier.Control, KeyboardContext.WindowAdjustment)));
    }

    /// <summary>Proves the raw F3 and F4 virtual keys translate under Control and name their chord caps.</summary>
    [TestMethod]
    public void TranslateKeyDataWhenSortChordArrivesMapsToSortIntentAndChordCap()
    {
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.F3, KeyboardKey.F3, UserIntent.SortByName);
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.F4, KeyboardKey.F4, UserIntent.SortByExtension);

        KeyBinding byName = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .Single(binding => binding.Intent == UserIntent.SortByName);
        KeyBinding byExtension = KeyboardIntentMapper.BindingsFor(KeyboardContext.NavigationSurface)
            .Single(binding => binding.Intent == UserIntent.SortByExtension);
        Assert.AreEqual("KeyLabelCtrlF3", byName.KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelCtrlF4", byExtension.KeyLabelResourceKey);
    }

    /// <summary>
    /// Proves Ctrl+F5 and Ctrl+F6 sort by size and modification time only from the file list and
    /// the navigation surface, other modifiers pass through there, and every owning context keeps
    /// them (ADR-0056).
    /// </summary>
    [TestMethod]
    [TestCategory("Adversarial")]
    [TestProperty("ThreatId", "ADV-013")]
    public void MapWhenMetadataSortKeysArriveMapsControlChordOnlyOnPaneSurfaces()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();
        KeyboardContext[] paneSurfaces = [KeyboardContext.FileList, KeyboardContext.NavigationSurface];
        KeyboardContext[] owningContexts =
        [
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
            KeyboardContext.AddressEntry,
            KeyboardContext.CommandPalette,
        ];
        KeyboardContext[] consumingContexts = [KeyboardContext.WindowAdjustment, KeyboardContext.Locations];

        foreach (KeyboardContext context in paneSurfaces)
        {
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F5, KeyboardModifier.Control, context), UserIntent.SortBySize);
            KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F6, KeyboardModifier.Control, context), UserIntent.SortByModified);
            foreach (KeyboardModifier modifier in new[] { KeyboardModifier.Alt, KeyboardModifier.Other })
            {
                _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F5, modifier, context)));
                _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F6, modifier, context)));
            }
        }
        foreach (KeyboardContext context in owningContexts)
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F5, KeyboardModifier.Control, context)));
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
                mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F6, KeyboardModifier.Control, context)));
        }
        foreach (KeyboardContext context in consumingContexts)
        {
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(
                mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F5, KeyboardModifier.Control, context)));
            _ = Assert.IsInstanceOfType<KeyboardConsumed>(
                mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F6, KeyboardModifier.Control, context)));
        }
    }

    /// <summary>
    /// Proves plain F5 and F6 keep their commands beside the new chords: copy and move in the file
    /// list, refresh and pass-through on the navigation surface.
    /// </summary>
    [TestMethod]
    public void MapWhenPlainF5AndF6ArriveKeepsCopyMoveAndRefresh()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F5), UserIntent.Copy);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F6), UserIntent.Move);
        KeyboardMappingTestContext.AssertMaps(mapper, KeyboardMappingTestContext.Input(KeyboardKey.F5, KeyboardContext.NavigationSurface), UserIntent.Refresh);
        _ = Assert.IsInstanceOfType<KeyboardPassThrough>(
            mapper.Map(KeyboardMappingTestContext.Input(KeyboardKey.F6, KeyboardContext.NavigationSurface)));
    }

    /// <summary>
    /// Proves the repeat guard on F5 and F6 also holds the chords: a held Ctrl+F5 or Ctrl+F6 does
    /// not keep reversing the order.
    /// </summary>
    [TestMethod]
    public void MapWhenMetadataSortChordRepeatsPassesThroughWithoutIntent()
    {
        KeyboardIntentMapper mapper = KeyboardMappingTestContext.CreateMapper();

        foreach (KeyboardKey key in new[] { KeyboardKey.F5, KeyboardKey.F6 })
        {
            _ = Assert.IsInstanceOfType<KeyboardPassThrough>(mapper.Map(KeyboardInput.Create(
                key,
                KeyboardModifier.Control,
                KeyRepeatState.Repeated,
                KeyboardContext.FileList)));
        }
    }

    /// <summary>Proves the raw F5 and F6 virtual keys translate under Control and name their chord caps.</summary>
    [TestMethod]
    public void TranslateKeyDataWhenMetadataSortChordArrivesMapsToSortIntentAndChordCap()
    {
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.F5, KeyboardKey.F5, UserIntent.SortBySize);
        KeyboardMappingTestContext.AssertRawControlKey(VirtualKey.F6, KeyboardKey.F6, UserIntent.SortByModified);

        KeyBinding bySize = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .Single(binding => binding.Intent == UserIntent.SortBySize);
        KeyBinding byModified = KeyboardIntentMapper.BindingsFor(KeyboardContext.NavigationSurface)
            .Single(binding => binding.Intent == UserIntent.SortByModified);
        Assert.AreEqual("KeyLabelCtrlF5", bySize.KeyLabelResourceKey);
        Assert.AreEqual("KeyLabelCtrlF6", byModified.KeyLabelResourceKey);
    }
}
