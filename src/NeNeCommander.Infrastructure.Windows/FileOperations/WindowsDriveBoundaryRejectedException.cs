using System.IO;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Stops a verified traversal when a local path no longer has local drive admission.</summary>
internal sealed class WindowsDriveBoundaryRejectedException : IOException;
