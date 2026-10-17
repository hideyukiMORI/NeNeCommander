using System;
using System.Globalization;
using NeNeCommander.Application.Identifiers;

namespace NeNeCommander.Infrastructure.Windows.Tests;

internal sealed class DeterministicIdentifierSource : IIdentifierSource
{
    private int _next;

    internal DeterministicIdentifierSource(int first = 1)
    {
        _next = first;
    }

    internal int IssuedCount { get; private set; }

    public Guid NextIdentifier()
    {
        Guid token = Guid.ParseExact(_next.ToString("x32", CultureInfo.InvariantCulture), "N");
        _next++;
        IssuedCount++;
        return token;
    }
}
