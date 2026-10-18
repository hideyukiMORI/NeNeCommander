using System;
using System.Collections.Generic;

namespace NeNeCommander.Application.FileOperations;

/// <summary>Records one exactly compared file or directory and its immutable descendant evidence.</summary>
public abstract record VerifiedCopyEntry
{
    /// <summary>Restricts concrete cases to this assembly and its trusted friends.</summary>
    private protected abstract void RestrictDerivation();

    private protected VerifiedCopyEntry(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        Source = source;
        Target = target;
    }

    /// <summary>Gets the observed source entry.</summary>
    public VerifiedEntrySnapshot Source { get; }
    /// <summary>Gets the observed matching target entry.</summary>
    public VerifiedEntrySnapshot Target { get; }

    /// <summary>Creates file evidence after exact streamed comparison.</summary>
    public static VerifiedCopyEntry File(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target, FileContentHash content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new VerifiedFile(source, target, content);
    }

    /// <summary>Creates directory evidence with an owned immutable child list.</summary>
    public static VerifiedCopyEntry Directory(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target, IReadOnlyList<VerifiedCopyEntry> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        foreach (VerifiedCopyEntry child in children)
        {
            ArgumentNullException.ThrowIfNull(child);
        }
        return new VerifiedDirectory(source, target, children);
    }

    /// <summary>Represents a file and its SHA-256 evidence.</summary>
    public sealed record VerifiedFile : VerifiedCopyEntry
    {
        private protected override void RestrictDerivation()
        {
        }

        internal VerifiedFile(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target, FileContentHash content)
            : base(source, target)
        {
            Content = content;
        }

        /// <summary>Gets the content evidence agreed by both streams.</summary>
        public FileContentHash Content { get; }
    }

    /// <summary>Represents a directory's complete directly verified entry set.</summary>
    public sealed record VerifiedDirectory : VerifiedCopyEntry
    {
        private protected override void RestrictDerivation()
        {
        }

        internal VerifiedDirectory(VerifiedEntrySnapshot source, VerifiedEntrySnapshot target, IReadOnlyList<VerifiedCopyEntry> children)
            : base(source, target)
        {
            Children = new List<VerifiedCopyEntry>(children).AsReadOnly();
        }

        /// <summary>Gets the exact verified children.</summary>
        public IReadOnlyList<VerifiedCopyEntry> Children { get; }
    }
}
