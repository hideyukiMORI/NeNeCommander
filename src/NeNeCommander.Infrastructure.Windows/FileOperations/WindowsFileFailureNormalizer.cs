using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Translates known Windows file HRESULT values into the canonical failure vocabulary.</summary>
public static class WindowsFileFailureNormalizer
{
    private const int AccessDeniedHResult = unchecked((int)0x80070005);
    private const int FileNotFoundHResult = unchecked((int)0x80070002);
    private const int NetworkNameNotFoundHResult = unchecked((int)0x80070043);
    private const int NetworkPathNotFoundHResult = unchecked((int)0x80070035);
    private const int PathNotFoundHResult = unchecked((int)0x80070003);

    /// <summary>Normalizes one adapter-caught HRESULT without widening access or retry scope.</summary>
    /// <param name="hResult">HRESULT captured from the expected Windows adapter exception.</param>
    /// <returns>The canonical fail-closed operation failure.</returns>
    public static FileOperationFailureKind Normalize(int hResult)
    {
        return hResult switch
        {
            AccessDeniedHResult => FileOperationFailureKind.AccessDenied,
            FileNotFoundHResult => FileOperationFailureKind.NotFound,
            PathNotFoundHResult => FileOperationFailureKind.NotFound,
            NetworkPathNotFoundHResult => FileOperationFailureKind.ProviderUnavailable,
            NetworkNameNotFoundHResult => FileOperationFailureKind.ProviderUnavailable,
            _ => FileOperationFailureKind.ProviderUnavailable,
        };
    }
}
