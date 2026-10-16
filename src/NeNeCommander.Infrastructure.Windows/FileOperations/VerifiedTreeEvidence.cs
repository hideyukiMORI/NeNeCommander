using System.Collections.Generic;
using System.IO;
using System.Linq;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Revalidates immutable verification evidence without replacing its original baseline.</summary>
internal static class VerifiedTreeEvidence
{
    internal static bool Matches(VerifiedCopyEntry entry, VerifiedTreeAccess access)
    {
        return access.Capture(entry.Source.Path) == entry.Source &&
            access.Capture(entry.Target.Path) == entry.Target &&
            (entry switch
            {
                VerifiedCopyEntry.VerifiedFile file => MatchesFile(file, access),
                VerifiedCopyEntry.VerifiedDirectory directory =>
                    Membership(directory.Source.Path, directory.Children.Select(child => child.Source.Path), access) &&
                    Membership(directory.Target.Path, directory.Children.Select(child => child.Target.Path), access) &&
                    directory.Children.All(child => Matches(child, access)),
                _ => false,
            }) &&
            access.Capture(entry.Source.Path) == entry.Source && access.Capture(entry.Target.Path) == entry.Target;
    }

    internal static bool MatchesFile(VerifiedCopyEntry.VerifiedFile file, VerifiedTreeAccess access)
    {
        return access.Find(file.Source.Path) is FileInfo && access.Find(file.Target.Path) is FileInfo &&
            VerifiedFileContent.Read(access.Resolve(file.Source.Path)) == file.Content &&
            VerifiedFileContent.Read(access.Resolve(file.Target.Path)) == file.Content;
    }

    internal static bool Membership(FileSystemPath path, IEnumerable<FileSystemPath> expected, VerifiedTreeAccess access)
    {
        HashSet<FileSystemPath> names = new(expected, FileSystemPathIdentityComparer.Instance);
        return names.SetEquals(access.Children(path));
    }

    internal static bool TargetMatches(VerifiedCopyEntry entry, VerifiedTreeAccess access)
    {
        return access.Capture(entry.Target.Path) == entry.Target &&
            (entry switch
            {
                VerifiedCopyEntry.VerifiedFile file => access.Find(file.Target.Path) is FileInfo &&
                    VerifiedFileContent.Read(access.Resolve(file.Target.Path)) == file.Content,
                VerifiedCopyEntry.VerifiedDirectory directory => Membership(directory.Target.Path,
                    directory.Children.Select(child => child.Target.Path), access),
                _ => false,
            }) && access.Capture(entry.Target.Path) == entry.Target;
    }
}
