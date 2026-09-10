using NeNeCommander.Application.Launching;

namespace NeNeCommander.Application.Tests;

/// <summary>Test-only future launch outcome used to prove fail-closed dispatch.</summary>
internal sealed record UnsupportedFileLaunchOutcome : FileLaunchOutcome;
