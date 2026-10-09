using NeNeCommander.Application.FileOperations;

namespace NeNeCommander.Infrastructure.Windows.FileOperations;

/// <summary>Translates known Windows file HRESULT values into the canonical failure vocabulary.</summary>
public static class WindowsFileFailureNormalizer
{
    // Win32 error codes as HRESULT_FROM_WIN32 values, grouped by the canonical failure they produce.
    private const int AccessDeniedHResult = unchecked((int)0x80070005); // ERROR_ACCESS_DENIED (5)
    private const int LogonFailureHResult = unchecked((int)0x8007052E); // ERROR_LOGON_FAILURE (1326)
    private const int SessionCredentialConflictHResult = unchecked((int)0x800704C3); // ERROR_SESSION_CREDENTIAL_CONFLICT (1219)
    private const int FileNotFoundHResult = unchecked((int)0x80070002); // ERROR_FILE_NOT_FOUND (2)
    private const int PathNotFoundHResult = unchecked((int)0x80070003); // ERROR_PATH_NOT_FOUND (3)
    private const int NetworkPathNotFoundHResult = unchecked((int)0x80070035); // ERROR_BAD_NETPATH (53)
    private const int NetworkNameNotFoundHResult = unchecked((int)0x80070043); // ERROR_BAD_NET_NAME (67)
    private const int NetworkUnreachableHResult = unchecked((int)0x800704CF); // ERROR_NETWORK_UNREACHABLE (1231)
    private const int HostUnreachableHResult = unchecked((int)0x800704D0); // ERROR_HOST_UNREACHABLE (1232)
    private const int SemaphoreTimeoutHResult = unchecked((int)0x80070079); // ERROR_SEM_TIMEOUT (121)
    private const int NetworkNameDeletedHResult = unchecked((int)0x80070040); // ERROR_NETNAME_DELETED (64)
    private const int UnexpectedNetworkErrorHResult = unchecked((int)0x8007003B); // ERROR_UNEXP_NET_ERR (59)

    /// <summary>Normalizes one adapter-caught HRESULT without widening access or retry scope.</summary>
    /// <param name="hResult">HRESULT captured from the expected Windows adapter exception.</param>
    /// <returns>The canonical fail-closed operation failure.</returns>
    public static FileOperationFailureKind Normalize(int hResult)
    {
        return hResult switch
        {
            AccessDeniedHResult => FileOperationFailureKind.AccessDenied,
            LogonFailureHResult => FileOperationFailureKind.AccessDenied,
            SessionCredentialConflictHResult => FileOperationFailureKind.AccessDenied,
            FileNotFoundHResult => FileOperationFailureKind.NotFound,
            PathNotFoundHResult => FileOperationFailureKind.NotFound,
            NetworkPathNotFoundHResult => FileOperationFailureKind.ProviderUnavailable,
            NetworkNameNotFoundHResult => FileOperationFailureKind.ProviderUnavailable,
            NetworkUnreachableHResult => FileOperationFailureKind.ProviderUnavailable,
            HostUnreachableHResult => FileOperationFailureKind.ProviderUnavailable,
            SemaphoreTimeoutHResult => FileOperationFailureKind.ProviderUnavailable,
            NetworkNameDeletedHResult => FileOperationFailureKind.ProviderUnavailable,
            UnexpectedNetworkErrorHResult => FileOperationFailureKind.ProviderUnavailable,
            _ => FileOperationFailureKind.ProviderUnavailable,
        };
    }
}
