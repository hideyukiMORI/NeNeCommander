using NeNeCommander.Application.Panes;
using NeNeCommander.Application.Settings;

namespace NeNeCommander.Application.Sessions;

/// <summary>Combines the two progress channels observed by the application-session host.</summary>
public interface ICommanderProgressObserver : IDualPaneProgressObserver, ISettingsProgressObserver;
