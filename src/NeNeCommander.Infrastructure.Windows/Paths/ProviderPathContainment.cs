using System;
using NeNeCommander.Domain.Paths;

namespace NeNeCommander.Infrastructure.Windows.Paths;

/// <summary>Performs the sole provider-aware, segment-boundary operation-root check.</summary>
public static class ProviderPathContainment
{
    /// <summary>Evaluates a validated candidate against a validated operation root.</summary>
    /// <param name="root">Exact operation root.</param>
    /// <param name="candidate">Candidate path to contain.</param>
    /// <returns>The contained path or a closed rejection.</returns>
    public static PathContainmentOutcome Evaluate(FileSystemPath root, FileSystemPath candidate)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(candidate);
        return (root, candidate) switch
        {
            (WindowsLocalPath localRoot, WindowsLocalPath localCandidate) => EvaluateWindowsLocal(
                localRoot,
                localCandidate),
            (WindowsUncPath uncRoot, WindowsUncPath uncCandidate) => EvaluateWindowsUnc(
                uncRoot,
                uncCandidate),
            (WslPath wslRoot, WslPath wslCandidate) => EvaluateWsl(wslRoot, wslCandidate),
            _ => new RejectedPathContainment(PathContainmentFailureKind.ProviderMismatch),
        };
    }

    private static PathContainmentOutcome EvaluateWindowsLocal(
        WindowsLocalPath root,
        WindowsLocalPath candidate)
    {
        return root.Drive.Equals(candidate.Drive, StringComparison.OrdinalIgnoreCase)
            ? EvaluateCanonicalText(root, candidate, StringComparison.OrdinalIgnoreCase)
            : new RejectedPathContainment(PathContainmentFailureKind.ProviderMismatch);
    }

    private static PathContainmentOutcome EvaluateWindowsUnc(
        WindowsUncPath root,
        WindowsUncPath candidate)
    {
        return root.Server.Equals(candidate.Server, StringComparison.OrdinalIgnoreCase) &&
            root.Share.Equals(candidate.Share, StringComparison.OrdinalIgnoreCase)
            ? EvaluateCanonicalText(root, candidate, StringComparison.OrdinalIgnoreCase)
            : new RejectedPathContainment(PathContainmentFailureKind.ProviderMismatch);
    }

    private static PathContainmentOutcome EvaluateWsl(WslPath root, WslPath candidate)
    {
        if (!root.DistributionName.Equals(candidate.DistributionName, StringComparison.OrdinalIgnoreCase))
        {
            return new RejectedPathContainment(PathContainmentFailureKind.ProviderMismatch);
        }

        string boundary = root.LinuxPath.EndsWith('/') ? root.LinuxPath : root.LinuxPath + "/";
        return candidate.LinuxPath.Equals(root.LinuxPath, StringComparison.Ordinal) ||
            candidate.LinuxPath.StartsWith(boundary, StringComparison.Ordinal)
            ? new ContainedPath(candidate)
            : new RejectedPathContainment(PathContainmentFailureKind.OutsideRoot);
    }

    private static PathContainmentOutcome EvaluateCanonicalText(
        FileSystemPath root,
        FileSystemPath candidate,
        StringComparison comparison)
    {
        string boundary = root.CanonicalText.EndsWith('\\')
            ? root.CanonicalText
            : root.CanonicalText + "\\";
        return candidate.CanonicalText.Equals(root.CanonicalText, comparison) ||
            candidate.CanonicalText.StartsWith(boundary, comparison)
            ? new ContainedPath(candidate)
            : new RejectedPathContainment(PathContainmentFailureKind.OutsideRoot);
    }
}
