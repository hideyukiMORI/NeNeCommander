namespace NeNeCommander.Application.Settings;

/// <summary>Receives immutable settings snapshots when an ordered write attempt completes.</summary>
public interface ISettingsProgressObserver
{
    /// <summary>Reports the settings state current after one write completion.</summary>
    /// <param name="snapshot">Current immutable settings state.</param>
    public void SettingsProgressed(SettingsSnapshot snapshot);
}
