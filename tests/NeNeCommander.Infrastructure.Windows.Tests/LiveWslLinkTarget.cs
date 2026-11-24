using System;
using System.Collections.Generic;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Declares whether an owned fixture link must preserve an owned entry or an absent target.</summary>
internal abstract record LiveWslLinkTarget
{
    private LiveWslLinkTarget(WslPath path)
    {
        Path = path;
    }

    internal WslPath Path { get; }

    internal abstract bool Matches(
        IReadOnlyDictionary<string, LiveWslRootEntry> ownedEntries, LiveWslRootMechanism mechanism);

    internal static LiveWslLinkTarget Existing(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new OwnedTarget(path);
    }

    internal static LiveWslLinkTarget Absent(WslPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new AbsentTarget(path);
    }

    private sealed record OwnedTarget : LiveWslLinkTarget
    {
        internal OwnedTarget(WslPath path) : base(path)
        {
        }

        internal override bool Matches(
            IReadOnlyDictionary<string, LiveWslRootEntry> ownedEntries, LiveWslRootMechanism mechanism)
        {
            return ownedEntries.TryGetValue(mechanism.ResolvePath(Path), out LiveWslRootEntry? entry) &&
                LiveWslRootFileSystem.VerifyEntries([entry], mechanism.FileSystem) is LiveWslRootCheckAccepted;
        }
    }

    private sealed record AbsentTarget : LiveWslLinkTarget
    {
        internal AbsentTarget(WslPath path) : base(path)
        {
        }

        internal override bool Matches(
            IReadOnlyDictionary<string, LiveWslRootEntry> ownedEntries, LiveWslRootMechanism mechanism)
        {
            return !ownedEntries.ContainsKey(mechanism.ResolvePath(Path)) && mechanism.FileSystem.Find(Path) is null;
        }
    }
}
