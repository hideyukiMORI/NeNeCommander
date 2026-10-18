using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.Presentation.WinUI.Tests;

/// <summary>Proves KeyboardBinding behavior and contracts.</summary>
[TestClass]
public sealed class KeyboardBindingTests
{
    /// <summary>Proves no context maps one keystroke to more than one intent (KBD-005).</summary>
    [TestMethod]
    public void BindingsForWhenContextIsDeclaredMapsEachKeystrokeToOneIntent()
    {
        KeyboardContext[] contexts =
        [
            KeyboardContext.FileList,
            KeyboardContext.NavigationSurface,
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
            KeyboardContext.AddressEntry,
        ];

        foreach (KeyboardContext context in contexts)
        {
            AssertKeystrokesAreUnique(context);
        }
    }

    /// <summary>Proves every declared binding is the intent the mapper emits for its keystroke.</summary>
    [TestMethod]
    public void BindingsForWhenDeclarationIsMappedEmitsTheDeclaredIntent()
    {
        KeyboardContext[] contexts =
        [
            KeyboardContext.FileList,
            KeyboardContext.NavigationSurface,
            KeyboardContext.Modal,
            KeyboardContext.TextEntry,
            KeyboardContext.AddressEntry,
        ];

        foreach (KeyboardContext context in contexts)
        {
            AssertDeclarationsAreMapped(context);
        }
    }

    /// <summary>Proves the file-list context declares every documented binding and nothing else.</summary>
    [TestMethod]
    public void BindingsForWhenContextIsFileListDeclaresTheDocumentedCount()
    {
        Assert.HasCount(45, KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList));
        Assert.HasCount(26, KeyboardIntentMapper.BindingsFor(KeyboardContext.NavigationSurface));
        Assert.HasCount(6, KeyboardIntentMapper.BindingsFor(KeyboardContext.Locations));
        Assert.HasCount(0, KeyboardIntentMapper.BindingsFor(KeyboardContext.WindowAdjustment));
        Assert.HasCount(2, KeyboardIntentMapper.BindingsFor(KeyboardContext.Modal));
        Assert.HasCount(1, KeyboardIntentMapper.BindingsFor(KeyboardContext.TextEntry));
        Assert.HasCount(3, KeyboardIntentMapper.BindingsFor(KeyboardContext.AddressEntry));
    }

    /// <summary>Proves every key identity names one distinct key-cap label resource.</summary>
    [TestMethod]
    public void LabelResourceKeyWhenKeyIsReadNamesOneDistinctResource()
    {
        AssertCharacterLabels();
        Assert.AreEqual("KeyLabelDown", KeyboardKey.Down.LabelResourceKey);
        Assert.AreEqual("KeyLabelUp", KeyboardKey.Up.LabelResourceKey);
        Assert.AreEqual("KeyLabelLeft", KeyboardKey.Left.LabelResourceKey);
        Assert.AreEqual("KeyLabelRight", KeyboardKey.Right.LabelResourceKey);
        Assert.AreEqual("KeyLabelBackspace", KeyboardKey.Backspace.LabelResourceKey);
        Assert.AreEqual("KeyLabelEnter", KeyboardKey.Enter.LabelResourceKey);
        Assert.AreEqual("KeyLabelPageDown", KeyboardKey.PageDown.LabelResourceKey);
        Assert.AreEqual("KeyLabelPageUp", KeyboardKey.PageUp.LabelResourceKey);
        Assert.AreEqual("KeyLabelTab", KeyboardKey.Tab.LabelResourceKey);
        Assert.AreEqual("KeyLabelSpace", KeyboardKey.Space.LabelResourceKey);
        Assert.AreEqual("KeyLabelEscape", KeyboardKey.Escape.LabelResourceKey);
        Assert.AreEqual("KeyLabelF2", KeyboardKey.F2.LabelResourceKey);
        Assert.AreEqual("KeyLabelF3", KeyboardKey.F3.LabelResourceKey);
        Assert.AreEqual("KeyLabelF4", KeyboardKey.F4.LabelResourceKey);
        Assert.AreEqual("KeyLabelF5", KeyboardKey.F5.LabelResourceKey);
        Assert.AreEqual("KeyLabelF6", KeyboardKey.F6.LabelResourceKey);
        Assert.AreEqual("KeyLabelF7", KeyboardKey.F7.LabelResourceKey);
        Assert.AreEqual("KeyLabelF8", KeyboardKey.F8.LabelResourceKey);
        Assert.AreEqual("KeyLabelUnmapped", KeyboardKey.Other.LabelResourceKey);
        AssertLabelResourceKeysAreDistinct();
    }

    /// <summary>Proves the binding query rejects an absent context.</summary>
    [TestMethod]
    public void BindingsForWhenContextIsNullThrowsArgumentNullException()
    {
        MethodInfo method = typeof(KeyboardIntentMapper).GetMethod(
            nameof(KeyboardIntentMapper.BindingsFor),
            BindingFlags.Public | BindingFlags.Static) ??
            throw new AssertFailedException("The binding query was not found.");

        TargetInvocationException failure = Assert.ThrowsExactly<TargetInvocationException>(
            () => method.Invoke(null, [null]));

        _ = Assert.IsInstanceOfType<ArgumentNullException>(failure.InnerException);
    }

    private static void AssertKeystrokesAreUnique(KeyboardContext context)
    {
        HashSet<string> keystrokes = [];
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
        {
            Assert.AreSame(context, binding.Context);
            Assert.IsTrue(
                keystrokes.Add(binding.Key.LabelResourceKey + " " + binding.Modifier.GetType().Name),
                "A keystroke is declared more than once in one context.");
        }
    }

    private static void AssertDeclarationsAreMapped(KeyboardContext context)
    {
        foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
        {
            KeyboardInput input = KeyboardInput.Create(
                binding.Key,
                binding.Modifier,
                KeyRepeatState.Initial,
                context);
            MappedKeyboardIntent mapped = Assert.IsInstanceOfType<MappedKeyboardIntent>(
                KeyboardMappingTestContext.CreateMapper().Map(input));
            Assert.AreSame(binding.Intent, mapped.Intent);
        }
    }

    private static void AssertLabelResourceKeysAreDistinct()
    {
        HashSet<string> labels = [];
        foreach (KeyboardContext context in new[] { KeyboardContext.FileList, KeyboardContext.NavigationSurface })
        {
            foreach (KeyBinding binding in KeyboardIntentMapper.BindingsFor(context))
            {
                _ = labels.Add(binding.Key.LabelResourceKey);
                Assert.StartsWith("KeyLabel", binding.Key.LabelResourceKey);
            }
        }
        Assert.IsNotEmpty(labels);
    }

    private static void AssertCharacterLabels()
    {
        Assert.AreEqual("KeyLabelLowerG", KeyboardKey.LowerG.LabelResourceKey);
        Assert.AreEqual("KeyLabelUpperG", KeyboardKey.UpperG.LabelResourceKey);
        Assert.AreEqual("KeyLabelH", KeyboardKey.H.LabelResourceKey);
        Assert.AreEqual("KeyLabelJ", KeyboardKey.J.LabelResourceKey);
        Assert.AreEqual("KeyLabelK", KeyboardKey.K.LabelResourceKey);
        Assert.AreEqual("KeyLabelL", KeyboardKey.L.LabelResourceKey);
        Assert.AreEqual("KeyLabelD", KeyboardKey.D.LabelResourceKey);
        Assert.AreEqual("KeyLabelR", KeyboardKey.R.LabelResourceKey);
        Assert.AreEqual("KeyLabelP", KeyboardKey.P.LabelResourceKey);
        Assert.AreEqual("KeyLabelU", KeyboardKey.U.LabelResourceKey);
        Assert.AreEqual("KeyLabelB", KeyboardKey.B.LabelResourceKey);
        Assert.AreEqual("KeyLabel1", KeyboardKey.One.LabelResourceKey);
        Assert.AreEqual("KeyLabel2", KeyboardKey.Two.LabelResourceKey);
        Assert.AreEqual("KeyLabel3", KeyboardKey.Three.LabelResourceKey);
        Assert.AreEqual("KeyLabel4", KeyboardKey.Four.LabelResourceKey);
        Assert.AreEqual("KeyLabel5", KeyboardKey.Five.LabelResourceKey);
        Assert.AreEqual("KeyLabel6", KeyboardKey.Six.LabelResourceKey);
        Assert.AreEqual("KeyLabel7", KeyboardKey.Seven.LabelResourceKey);
        Assert.AreEqual("KeyLabel8", KeyboardKey.Eight.LabelResourceKey);
        Assert.AreEqual("KeyLabel9", KeyboardKey.Nine.LabelResourceKey);
        Assert.AreEqual("KeyLabelComma", KeyboardKey.Comma.LabelResourceKey);
    }
}
