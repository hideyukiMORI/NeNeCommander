using System;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Owns the transient identity observation used by the final-entry capture proof.</summary>
internal sealed class FinalEntryCaptureRace
{
    private bool _active;
    private bool _duringEntry;
    internal bool ObservedChange { get; private set; }

    internal void Activate()
    {
        _active = true;
    }

    internal WslHandleFacts Read(string path)
    {
        WslHandleFacts facts = OwnedWslNamespace.ReadFacts(path);
        if (!_active)
        {
            return facts;
        }
        if (!path.EndsWith("\\file.txt", StringComparison.Ordinal))
        {
            if (_duringEntry)
            {
                _active = false;
            }
            _duringEntry = false;
            return facts;
        }
        if (_duringEntry)
        {
            ObservedChange = true;
            return VerifiedVerificationBoundaryTests.ChangedTime(facts);
        }
        _duringEntry = true;
        return facts;
    }
}
