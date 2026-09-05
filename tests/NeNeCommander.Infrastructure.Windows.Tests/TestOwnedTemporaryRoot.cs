using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Owns one unique directory beneath the operating-system temporary directory for Windows
/// integration tests. Setup and cleanup both re-resolve the root and refuse to act on any
/// path that is not a prefixed direct child of the temporary directory.
/// </summary>
internal sealed class TestOwnedTemporaryRoot : IDisposable
{
    private const string Prefix = "NeNeCommander-Test-";

    private readonly List<string> _junctions;
    private readonly List<string> _listingDeniedDirectories;
    private readonly string _fullPath;
    private readonly string _temporaryParent;

    private TestOwnedTemporaryRoot(string fullPath, string temporaryParent, WindowsLocalPath path)
    {
        _fullPath = fullPath;
        _temporaryParent = temporaryParent;
        _junctions = [];
        _listingDeniedDirectories = [];
        Path = path;
    }

    /// <summary>Gets the validated Windows local path of the owned root.</summary>
    internal WindowsLocalPath Path { get; }

    /// <summary>
    /// Creates a unique, empty, verified root. The operating system chooses the unique suffix;
    /// no test data depends on it.
    /// </summary>
    internal static TestOwnedTemporaryRoot Create()
    {
        string temporaryParent = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
        DirectoryInfo created = Directory.CreateTempSubdirectory(Prefix);
        string fullPath = System.IO.Path.GetFullPath(created.FullName);
        AssertOwnedRoot(fullPath, temporaryParent);
        AssertEmpty(fullPath);
        return FileSystemPath.Parse(fullPath) is PathParseSuccess { Path: WindowsLocalPath path }
            ? new TestOwnedTemporaryRoot(fullPath, temporaryParent, path)
            : throw new InvalidOperationException("The created test root is not a Windows local path.");
    }

    /// <summary>Resolves a direct child name to a full path that is verified to stay inside the root.</summary>
    internal string Resolve(string childName)
    {
        string childPath = System.IO.Path.GetFullPath(System.IO.Path.Join(_fullPath, childName));
        return childPath.StartsWith(_fullPath + "\\", StringComparison.OrdinalIgnoreCase)
            ? childPath
            : throw new InvalidOperationException("The requested child escapes the test root.");
    }

    /// <summary>Creates an empty file directly inside the root.</summary>
    internal string CreateFile(string childName)
    {
        string childPath = Resolve(childName);
        File.WriteAllBytes(childPath, []);
        return childPath;
    }

    /// <summary>
    /// Creates an empty file inside the root and marks it hidden through the filesystem, so the
    /// adapter reads a real attribute rather than an assumption about the name.
    /// </summary>
    internal string CreateHiddenFile(string childName)
    {
        string childPath = CreateFile(childName);
        File.SetAttributes(childPath, FileAttributes.Hidden);
        return childPath;
    }

    /// <summary>Creates an empty file inside the root and marks it a system file.</summary>
    internal string CreateSystemFile(string childName)
    {
        string childPath = CreateFile(childName);
        File.SetAttributes(childPath, FileAttributes.System);
        return childPath;
    }

    /// <summary>Creates a directory inside the root and marks it hidden through the filesystem.</summary>
    internal string CreateHiddenDirectory(string childName)
    {
        string childPath = CreateDirectory(childName);
        DirectoryInfo directory = new(childPath);
        directory.Attributes |= FileAttributes.Hidden;
        return childPath;
    }

    /// <summary>Writes text into a file inside the root, creating or replacing it.</summary>
    internal string WriteFile(string childName, string content)
    {
        string childPath = Resolve(childName);
        File.WriteAllText(childPath, content);
        return childPath;
    }

    /// <summary>
    /// Creates an NTFS junction inside the root that points at another directory inside the root.
    /// Junctions need no privilege, unlike symbolic links, so the fixture is deterministic on NTFS.
    /// </summary>
    internal string CreateJunction(string childName, string targetChildName)
    {
        string linkPath = Resolve(childName);
        string targetPath = Resolve(targetChildName);
        using Process mklink = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            ArgumentList = { "/c", "mklink", "/J", linkPath, targetPath },
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("The junction process could not be started.");
        mklink.WaitForExit();
        _junctions.Add(linkPath);
        return mklink.ExitCode == 0 && (new DirectoryInfo(linkPath).Attributes & FileAttributes.ReparsePoint) != 0
            ? linkPath
            : throw new InvalidOperationException("The junction fixture could not be created.");
    }

    /// <summary>Creates a directory directly inside the root.</summary>
    internal string CreateDirectory(string childName)
    {
        string childPath = Resolve(childName);
        _ = Directory.CreateDirectory(childPath);
        return childPath;
    }

    /// <summary>
    /// Creates a file whose name is legal for the NTFS namespace but rejected by the Win32 and
    /// project path models, using the extended-length prefix that bypasses Win32 normalization.
    /// </summary>
    internal void CreateFileWithUnrepresentableName(string childName)
    {
        if (childName.Trim('.').Length == 0 ||
            childName.Contains('\\', StringComparison.Ordinal) ||
            childName.Contains('/', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The unrepresentable name must be one direct child name.");
        }

        // Win32 normalization would strip the trailing character, so the path is joined verbatim
        // beneath the verified root instead of passing through full-path resolution.
        File.WriteAllBytes("\\\\?\\" + _fullPath + "\\" + childName, []);
    }

    /// <summary>Denies the current user the right to list a child directory until disposal.</summary>
    internal string DenyDirectoryListing(string childName)
    {
        string childPath = CreateDirectory(childName);
        ApplyListingRule(childPath, AccessControlModification.Add);
        _listingDeniedDirectories.Add(childPath);
        return childPath;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        string resolved = System.IO.Path.GetFullPath(_fullPath);
        AssertOwnedRoot(resolved, _temporaryParent);
        foreach (string deniedDirectory in _listingDeniedDirectories)
        {
            ApplyListingRule(deniedDirectory, AccessControlModification.Remove);
        }
        _listingDeniedDirectories.Clear();
        foreach (string junction in _junctions)
        {
            // A junction is removed as a link so its target contents are never deleted through it.
            Directory.Delete(junction);
        }
        _junctions.Clear();
        Directory.Delete(resolved, recursive: true);
    }

    private static void ApplyListingRule(string directoryPath, AccessControlModification modification)
    {
        SecurityIdentifier user = WindowsIdentity.GetCurrent().User ??
            throw new InvalidOperationException("The current Windows identity has no security identifier.");
        DirectoryInfo directory = new(directoryPath);
        DirectorySecurity security = directory.GetAccessControl();
        FileSystemAccessRule rule = new(user, FileSystemRights.ListDirectory, AccessControlType.Deny);
        if (!security.ModifyAccessRule(modification, rule, out _))
        {
            throw new InvalidOperationException("The listing access rule could not be modified.");
        }
        directory.SetAccessControl(security);
    }

    private static void AssertEmpty(string fullPath)
    {
        using IEnumerator<string> contents = Directory.EnumerateFileSystemEntries(fullPath).GetEnumerator();
        if (contents.MoveNext())
        {
            throw new InvalidOperationException("The created test root is not empty.");
        }
    }

    private static void AssertOwnedRoot(string fullPath, string temporaryParent)
    {
        string parent = System.IO.Path.GetDirectoryName(fullPath) ??
            throw new InvalidOperationException("The test root has no parent directory.");
        if (!System.IO.Path.TrimEndingDirectorySeparator(parent).Equals(
                System.IO.Path.TrimEndingDirectorySeparator(temporaryParent),
                StringComparison.OrdinalIgnoreCase) ||
            !System.IO.Path.GetFileName(fullPath).StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to use a root outside the owned temporary directory.");
        }
    }
}
