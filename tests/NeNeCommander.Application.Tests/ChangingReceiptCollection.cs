using System.Collections;
using System.Collections.Generic;

namespace NeNeCommander.Application.Tests;

/// <summary>Changes a receipt input on repeated enumeration to test snapshot-before-validation admission.</summary>
internal sealed class ChangingReceiptCollection<T> : IReadOnlyList<T>
    where T : class
{
    private readonly T _first;
    private readonly T _later;
    private int _enumerations;

    internal ChangingReceiptCollection(T first, T later)
    {
        _first = first;
        _later = later;
    }

    public int Count => 1;
    public T this[int index] => _enumerations == 0 ? _first : _later;

    public IEnumerator<T> GetEnumerator()
    {
        T value = _enumerations++ == 0 ? _first : _later;
        yield return value;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
