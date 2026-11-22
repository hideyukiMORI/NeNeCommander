using System;
using System.Collections.Generic;

namespace NeNeCommander.Application.Settings;

/// <summary>Identifies one shipped next-launch interface language; arbitrary tags cannot enter the host.</summary>
public abstract record InterfaceLanguage
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    /// <summary>Gets the unmodified platform language at next launch.</summary>
    public static InterfaceLanguage System { get; } = new SystemLanguage();

    /// <summary>Gets Japanese interface text.</summary>
    public static InterfaceLanguage Japanese { get; } = new JapaneseLanguage();

    /// <summary>Gets English interface text.</summary>
    public static InterfaceLanguage English { get; } = new EnglishLanguage();

    /// <summary>Gets Simplified Chinese interface text.</summary>
    public static InterfaceLanguage SimplifiedChinese { get; } = new SimplifiedChineseLanguage();

    /// <summary>Gets the closed choices in editor order without exposing mutable storage.</summary>
    public static IReadOnlyList<InterfaceLanguage> All { get; } = Array.AsReadOnly<InterfaceLanguage>(
        [System, Japanese, English, SimplifiedChinese]);

    /// <summary>Gets the exact persisted identifier; System is not a platform language tag.</summary>
    public abstract string Identifier { get; }

    private InterfaceLanguage()
    {
    }

    private sealed record SystemLanguage : InterfaceLanguage
    {
        private protected override void RestrictDerivation()
        {
        }

        public override string Identifier => "system";
    }

    private sealed record JapaneseLanguage : InterfaceLanguage
    {
        private protected override void RestrictDerivation()
        {
        }

        public override string Identifier => "ja-JP";
    }

    private sealed record EnglishLanguage : InterfaceLanguage
    {
        private protected override void RestrictDerivation()
        {
        }

        public override string Identifier => "en-US";
    }

    private sealed record SimplifiedChineseLanguage : InterfaceLanguage
    {
        private protected override void RestrictDerivation()
        {
        }

        public override string Identifier => "zh-Hans";
    }
}
