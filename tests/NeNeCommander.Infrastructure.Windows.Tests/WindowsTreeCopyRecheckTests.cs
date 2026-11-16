using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeNeCommander.Application.FileOperations;
using NeNeCommander.Domain.Paths;
using NeNeCommander.Infrastructure.Windows.Drives;
using NeNeCommander.Infrastructure.Windows.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>
/// Proves the tree copy refuses when either the source inspection or the target admission alone fails,
/// both at the first admission and at the recheck immediately before each effect.
/// </summary>
[TestClass]
public sealed class WindowsTreeCopyRecheckTests
{
    /// <summary>Proves preflight refuses when only the source inspection fails.</summary>
    [TestMethod]
    public void PreflightWhenOnlySourceInspectionFailsRejectsProviderUnavailable()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileInfo source = new(root.WriteFile("source.txt", "source"));
        string targetText = root.Resolve("target.txt");
        WindowsTreeCopyAccess access = Access(new InspectionSequence(false), Drives(targetText, int.MaxValue));

        FileOperationFailureKind? failure = WindowsLocalTreeCopy.Preflight(source, Path(targetText), access);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, failure);
    }

    /// <summary>Proves preflight refuses when only the target drive is rejected.</summary>
    [TestMethod]
    public void PreflightWhenOnlyTargetDriveIsRejectedRejectsProviderUnavailable()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileInfo source = new(root.WriteFile("source.txt", "source"));
        string targetText = root.Resolve("target.txt");
        WindowsTreeCopyAccess access = Access(new InspectionSequence(true), Drives(targetText, 0));

        FileOperationFailureKind? failure = WindowsLocalTreeCopy.Preflight(source, Path(targetText), access);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, failure);
    }

    /// <summary>Proves a copy refuses at its first admission even when a later inspection would admit the source.</summary>
    [TestMethod]
    public void CopyWhenFirstSourceInspectionFailsRejectsWithoutCopying()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileInfo source = new(root.WriteFile("source.txt", "source"));
        string targetText = root.Resolve("target.txt");
        WindowsTreeCopyAccess access = Access(new InspectionSequence(false, true), Drives(targetText, int.MaxValue));

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(source, Path(targetText), access);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.IsFalse(File.Exists(targetText));
    }

    /// <summary>Proves a file or directory copy rechecks the source immediately before its effect.</summary>
    /// <param name="kind">Source entry kind.</param>
    [TestMethod]
    [DataRow("file")]
    [DataRow("directory")]
    public void CopyWhenSourceInspectionFailsAtRecheckRejectsWithoutTarget(string kind)
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        FileSystemInfo source = kind == "directory"
            ? new DirectoryInfo(root.CreateDirectory("source"))
            : new FileInfo(root.WriteFile("source.txt", "source"));
        string targetText = root.Resolve("target");
        InspectionSequence inspection = new(true, false);
        WindowsTreeCopyAccess access = Access(inspection, Drives(targetText, int.MaxValue));

        ProviderStepOutcome outcome = WindowsLocalTreeCopy.Copy(source, Path(targetText), access);

        Assert.AreSame(FileOperationFailureKind.ProviderUnavailable, outcome.Failure);
        Assert.AreEqual(2, inspection.Calls);
        Assert.IsFalse(File.Exists(targetText) || Directory.Exists(targetText));
    }

    /// <summary>Proves the local source inspection refuses a source whose drive is not admitted.</summary>
    [TestMethod]
    public void InspectLocalSourceWhenDriveIsRejectedReturnsFalse()
    {
        using TestOwnedTemporaryRoot root = TestOwnedTemporaryRoot.Create();
        string sourceText = root.WriteFile("source.txt", "source");

        bool admitted = WindowsLocalTreeCopy.InspectLocalSource(new FileInfo(sourceText), Drives(sourceText, 0));

        Assert.IsFalse(admitted);
    }

    private static WindowsTreeCopyAccess Access(InspectionSequence inspection, WindowsDriveBoundary drives)
    {
        return new WindowsTreeCopyAccess(path => path.CanonicalText, inspection.Inspect, drives);
    }

    private static WindowsDriveBoundary Drives(string path, int admittedQueries)
    {
        return new ScriptedDriveTypes(path, admittedQueries).Boundary();
    }

    private static FileSystemPath Path(string text)
    {
        return WindowsLocalOperationTestContext.ParsePath(text);
    }

    /// <summary>Answers source inspections in order and repeats the last answer.</summary>
    private sealed class InspectionSequence
    {
        private readonly bool[] _answers;

        internal InspectionSequence(params bool[] answers)
        {
            _answers = answers;
        }

        internal int Calls { get; private set; }

        internal bool Inspect(FileSystemInfo _)
        {
            bool answer = _answers[System.Math.Min(Calls, _answers.Length - 1)];
            Calls++;
            return answer;
        }
    }
}
