namespace NeNeCommander.Infrastructure.Windows.Tests;

/// <summary>Names the source and the produced target of one live move, both relative to the run child.</summary>
internal sealed record LiveWslTreeMove
{
    private LiveWslTreeMove(string sourceRelativePath, string targetRelativePath)
    {
        SourceRelativePath = sourceRelativePath;
        TargetRelativePath = targetRelativePath;
    }

    /// <summary>Gets the moved source path relative to the run child.</summary>
    internal string SourceRelativePath { get; }

    /// <summary>Gets the produced target path relative to the run child.</summary>
    internal string TargetRelativePath { get; }

    /// <summary>Creates the source and target names of one live move.</summary>
    /// <param name="sourceRelativePath">Moved source path relative to the run child.</param>
    /// <param name="targetRelativePath">Produced target path relative to the run child.</param>
    /// <returns>The move names.</returns>
    internal static LiveWslTreeMove Create(string sourceRelativePath, string targetRelativePath)
    {
        return new LiveWslTreeMove(sourceRelativePath, targetRelativePath);
    }
}
