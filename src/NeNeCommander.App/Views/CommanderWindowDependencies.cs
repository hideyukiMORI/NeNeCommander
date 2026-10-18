using System;
using NeNeCommander.Application.Sessions;
using NeNeCommander.Presentation.WinUI.Input;

namespace NeNeCommander.App.Views;

/// <summary>Carries the window host dependencies supplied by the sole application composition root.</summary>
public sealed record CommanderWindowDependencies
{
    private CommanderWindowDependencies(
        KeyboardIntentMapper keyboardIntentMapper,
        CommanderSession session,
        Action<Exception> defectObserver,
        TimeZoneInfo localZone)
    {
        KeyboardIntentMapper = keyboardIntentMapper;
        Session = session;
        DefectObserver = defectObserver;
        LocalZone = localZone;
    }

    /// <summary>Gets the canonical context-aware keyboard mapper.</summary>
    public KeyboardIntentMapper KeyboardIntentMapper { get; }

    /// <summary>Gets the coordinator over the pane, settings, and transient scope owners.</summary>
    public CommanderSession Session { get; }

    /// <summary>Gets the application callback that publishes unexpected task defects.</summary>
    public Action<Exception> DefectObserver { get; }

    /// <summary>Gets the time zone the composition root read once for row metadata.</summary>
    public TimeZoneInfo LocalZone { get; }

    /// <summary>Creates the complete dependency set without acquiring or owning additional state.</summary>
    /// <param name="keyboardIntentMapper">Canonical context-aware keyboard mapper.</param>
    /// <param name="session">Coordinator over the existing state owners.</param>
    /// <param name="defectObserver">Application callback for unexpected task defects.</param>
    /// <param name="localZone">Time zone already read by the composition root.</param>
    /// <returns>The immutable window dependency set.</returns>
    public static CommanderWindowDependencies Create(
        KeyboardIntentMapper keyboardIntentMapper,
        CommanderSession session,
        Action<Exception> defectObserver,
        TimeZoneInfo localZone)
    {
        ArgumentNullException.ThrowIfNull(keyboardIntentMapper);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(defectObserver);
        ArgumentNullException.ThrowIfNull(localZone);
        return new CommanderWindowDependencies(keyboardIntentMapper, session, defectObserver, localZone);
    }
}
