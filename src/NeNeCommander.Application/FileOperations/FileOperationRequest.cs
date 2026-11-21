using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents one immutable request accepted by the filesystem mutation gateway.
/// </summary>
public abstract record FileOperationRequest
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private const int MaximumSourceCount = 10000;

    internal FileOperationRequest(IReadOnlyList<FileSystemPath> sources)
    {
        Sources = SourceSnapshot.Create(sources);
    }

    /// <summary>Gets the owned ordered source set, which public collection reads cannot change.</summary>
    public IReadOnlyList<FileSystemPath> Sources { get; }

    private protected static FileOperationRequestFailureKind? ValidateSourceSet(
        IReadOnlyList<FileSystemPath> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Count == 0)
        {
            return FileOperationRequestFailureKind.EmptySources;
        }
        if (sources.Count > MaximumSourceCount)
        {
            return FileOperationRequestFailureKind.TooManySources;
        }

        HashSet<FileSystemPath> identities = new(FileSystemPathIdentityComparer.Instance);
        foreach (FileSystemPath source in sources)
        {
            if (source is null)
            {
                return FileOperationRequestFailureKind.NullSource;
            }
            if (!identities.Add(source))
            {
                return FileOperationRequestFailureKind.DuplicateSource;
            }
        }
        return null;
    }

    private protected static FileOperationRequestFailureKind? ValidateTransfer(
        IReadOnlyList<FileSystemPath> sources,
        FileSystemPath destination)
    {
        FileOperationRequestFailureKind? failure = ValidateSourceSet(sources);
        return failure ??
            (sources.Any(source => FileSystemPathIdentityComparer.Instance.Equals(source, destination))
                ? FileOperationRequestFailureKind.DestinationIsSource
                : null);
    }

    private sealed record SourceSnapshot : IReadOnlyList<FileSystemPath>, ICollection<FileSystemPath>
    {
        private readonly List<FileSystemPath> _sources;

        private SourceSnapshot(IEnumerable<FileSystemPath> sources)
        {
            _sources = [.. sources];
        }

        public int Count => _sources.Count;

        public FileSystemPath this[int index] => _sources[index];

        bool ICollection<FileSystemPath>.IsReadOnly => true;

        internal static SourceSnapshot Create(IEnumerable<FileSystemPath> sources)
        {
            return new SourceSnapshot(sources);
        }

        public IEnumerator<FileSystemPath> GetEnumerator()
        {
            return ((IEnumerable<FileSystemPath>)_sources).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        bool ICollection<FileSystemPath>.Contains(FileSystemPath source)
        {
            return _sources.Contains(source);
        }

        void ICollection<FileSystemPath>.CopyTo(FileSystemPath[] array, int arrayIndex)
        {
            _sources.CopyTo(array, arrayIndex);
        }

        void ICollection<FileSystemPath>.Add(FileSystemPath source)
        {
            throw new NotSupportedException();
        }

        void ICollection<FileSystemPath>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<FileSystemPath>.Remove(FileSystemPath source)
        {
            throw new NotSupportedException();
        }
    }
}
