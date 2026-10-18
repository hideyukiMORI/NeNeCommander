using System;
using System.IO;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Injects the existing target-leaf change at its exact third/fourth admission observation.</summary>
internal sealed class TargetLeafAdmissionRace
{
    private readonly TestOwnedTemporaryRoot _root;
    private readonly string _change;
    private readonly DateTime _modified;
    private int _firstLeafReads;

    internal TargetLeafAdmissionRace(TestOwnedTemporaryRoot root, string change, DateTime modified)
    {
        _root = root;
        _change = change;
        _modified = modified;
    }

    internal bool Changed { get; private set; }

    internal WslHandleFacts Read(string path)
    {
        if (path.EndsWith("\\a.txt", StringComparison.Ordinal))
        {
            _firstLeafReads++;
            if (_firstLeafReads == 3)
            {
                ChangeTarget();
            }
            else if (_change == "transient-metadata" && _firstLeafReads == 4)
            {
                File.SetLastWriteTimeUtc(_root.Resolve("windows\\directory\\a.txt"), _modified);
            }
        }
        return OwnedWslNamespace.ReadFacts(path);
    }

    private void ChangeTarget()
    {
        if (_change == "bytes")
        {
            _ = _root.WriteFile("windows\\directory\\a.txt", "other");
        }
        File.SetLastWriteTimeUtc(_root.Resolve("windows\\directory\\a.txt"),
            _change == "bytes" ? _modified : _modified.AddTicks(10000000));
        Changed = true;
    }
}
