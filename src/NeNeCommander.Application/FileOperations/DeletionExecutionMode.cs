namespace NeNeCommander.Application.FileOperations;

/// <summary>
/// Represents the closed deletion instruction passed to the provider port.
/// </summary>
public abstract record DeletionExecutionMode
{
    /// <summary>Gets the instruction that requires provider-guaranteed recycling.</summary>
    public static DeletionExecutionMode Recycle { get; } = new RecycleMode();

    /// <summary>Gets the instruction for explicitly confirmed permanent deletion.</summary>
    public static DeletionExecutionMode Permanent { get; } = new PermanentMode();

    private DeletionExecutionMode()
    {
    }

    private sealed record RecycleMode : DeletionExecutionMode;
    private sealed record PermanentMode : DeletionExecutionMode;
}
