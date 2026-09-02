namespace NeNeCommander.Application.Tests;

internal abstract record ScriptedCallbackPoint
{
    internal static ScriptedCallbackPoint AfterInspection { get; } = new AfterInspectionPoint();
    internal static ScriptedCallbackPoint AfterPreflight { get; } = new AfterPreflightPoint();
    internal static ScriptedCallbackPoint AfterCopy { get; } = new AfterCopyPoint();
    internal static ScriptedCallbackPoint AfterVerification { get; } = new AfterVerificationPoint();
    internal static ScriptedCallbackPoint AfterDeletion { get; } = new AfterDeletionPoint();

    private ScriptedCallbackPoint()
    {
    }

    private sealed record AfterInspectionPoint : ScriptedCallbackPoint;
    private sealed record AfterPreflightPoint : ScriptedCallbackPoint;
    private sealed record AfterCopyPoint : ScriptedCallbackPoint;
    private sealed record AfterVerificationPoint : ScriptedCallbackPoint;
    private sealed record AfterDeletionPoint : ScriptedCallbackPoint;
}
