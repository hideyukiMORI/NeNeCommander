using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>
/// Copies and compares one Windows local entry tree beneath a target path. Reparse points are
/// never followed: a tree that contains one is reported before anything is written.
/// </summary>
public static class WindowsLocalTreeCopy
{
    /// <summary>Reports whether an entry is a symbolic link, junction, or other reparse point.</summary>
    /// <param name="entry">Existing entry.</param>
    /// <returns>True when the entry is a reparse point.</returns>
    public static bool IsReparsePoint(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return (entry.Attributes & FileAttributes.ReparsePoint) != 0;
    }

    /// <summary>Reports whether the entry or any entry beneath it is a reparse point, without following any.</summary>
    /// <param name="entry">Existing entry.</param>
    /// <returns>True when a reparse point exists in the tree.</returns>
    public static bool ContainsReparsePoint(FileSystemInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (IsReparsePoint(entry))
        {
            return true;
        }
        if (entry is not DirectoryInfo directory)
        {
            return false;
        }
        foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos("*", CreateDirectEntryOptions()))
        {
            if (ContainsReparsePoint(child))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Copies a file or a directory tree to a target path that must not exist yet.</summary>
    /// <param name="source">Existing entry that contains no reparse point.</param>
    /// <param name="targetText">Full target path.</param>
    public static void Copy(FileSystemInfo source, string targetText)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetText);
        if (source is DirectoryInfo directory)
        {
            DirectoryInfo target = Directory.CreateDirectory(targetText);
            foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos("*", CreateDirectEntryOptions()))
            {
                Copy(child, Path.Combine(target.FullName, child.Name));
            }
            return;
        }
        File.Copy(source.FullName, targetText, overwrite: false);
    }

    /// <summary>Compares a copied tree with its source by kind, entry set, and byte count.</summary>
    /// <param name="source">Existing source entry.</param>
    /// <param name="targetText">Full target path.</param>
    /// <returns>True when the target mirrors the source.</returns>
    public static bool Matches(FileSystemInfo source, string targetText)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetText);
        return source is DirectoryInfo directory
            ? MatchesDirectory(directory, targetText)
            : MatchesFile(source, targetText);
    }

    private static bool MatchesFile(FileSystemInfo source, string targetText)
    {
        FileInfo target = new(targetText);
        return target.Exists && source is FileInfo file && target.Length == file.Length;
    }

    private static bool MatchesDirectory(DirectoryInfo source, string targetText)
    {
        DirectoryInfo target = new(targetText);
        if (!target.Exists)
        {
            return false;
        }
        List<FileSystemInfo> children = [.. source.EnumerateFileSystemInfos("*", CreateDirectEntryOptions())];
        int targetCount = target.EnumerateFileSystemInfos("*", CreateDirectEntryOptions()).Count();
        if (targetCount != children.Count)
        {
            return false;
        }
        foreach (FileSystemInfo child in children)
        {
            if (!Matches(child, Path.Combine(target.FullName, child.Name)))
            {
                return false;
            }
        }
        return true;
    }

    private static EnumerationOptions CreateDirectEntryOptions()
    {
        return new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };
    }
}
