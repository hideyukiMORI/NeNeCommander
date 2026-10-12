# ADR-0065: Open UNC files through the existing Windows Shell handoff

Status: accepted

Date: 2026-10-12

Issue #226. This implements hide's instruction to continue the remaining product
work through the existing design and implementation authority.

## Context

ADR-0057 supports UNC listings and names the implicit Windows authentication
boundary. ADR-0046 and ADR-0064 already provide one typed file-association handoff,
but a focused UNC file is still refused in Application. Opening a user-selected
file is path activation, not a mutation that depends on SMB entry identity.

Microsoft documents the [Shell association contract](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.useshellexecute?view=net-10.0)
and [ShellExecuteEx behavior](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecuteexw).
The Shell and associated application own resolution, authentication, prompts and
subsequent application behavior; successful handoff does not prove successful
opening. Filesystem and Shell path support remain distinct under ADR-0064.

## Decision

`PaneSession` keeps the sole file/directory decision in `OpenFocused`: directories
navigate, files reach the existing `IFileLauncher` once. All three currently parsed
providers can now be launched, so remove the Application provider allowlist and its
refusal helper. Path admission belongs to the existing `WindowsShellFileLauncher`,
which still fails closed for unsupported variants and roots when called directly.

Admit `WindowsUncPath` only when it has a parent and its canonical path is shorter
than 260 UTF-16 code units. The canonical parser already enforces Windows segment
rules. Retain the Windows-local and WSL admission policies unchanged. One existing
scheduled Shell call receives the exact canonical filename and canonical parent,
`UseShellExecute = true`, no arguments, and no verb. Do not map a drive, substitute
a server alias, copy to a temporary file, use a command interpreter, retry, or
fall back to another namespace/provider. No identity/existence preflight is added.

Extend the launcher's existing native-error normalization: 53, 67, 59, 64, 121,
1231 and 1232 become `ProviderUnavailable`; 1326 and 1219 join 5 as `AccessDenied`.
The existing not-found, association and generic `ShellRejected` outcomes remain.
These are the same network/error classes already accepted for UNC reads, expressed
through the launch outcome rather than a filesystem mutation outcome.

Opening a UNC file may authenticate to its server with the current Windows logon
session, as navigating to that share already does. NeNe Commander does not read,
supply, prompt for, retain or log credentials, and adds no credential retry. The
Shell or associated application may display its own authentication/security UI.
Do not claim that such OS-owned UI is suppressed. The synchronous Shell call may
block on OS/network behavior. Cancellation can prevent queued work and is checked
again immediately before invocation; it cannot interrupt or undo a started call.
Wrapper disposal, accepted-result meaning, pane freeze/content preservation and
the path-resolution race of ADR-0046 remain unchanged.

## Alternatives rejected

- A second UNC launcher would duplicate the existing association mechanism.
- Retaining provider admission in both Application and Infrastructure would leave
  a redundant allowlist after all parsed providers became supported.
- Treating UNC activation as a local mutation would incorrectly assume SMB entry
  identity, atomicity, deletion or metadata semantics.
- Credential collection, drive mapping or a local-copy fallback would widen the
  operation beyond the selected path and introduce separate lifetimes.
- Assuming extended-length support in arbitrary associations would contradict the
  bounded ordinary-Shell policy already adopted for WSL.

## Migration and proof

This supersedes the UNC-launch prohibitions of ADR-0046, ADR-0057 and ADR-0064 and
their Application-side provider decision. Other provider capabilities and the
single launch port/API/scheduler/lifetime remain unchanged. Update CMD-002,
SEC-002, ADV-019, filesystem boundaries, the glossary and README atomically.

Under ADR-0061, build the affected consumers and run focused launch adapter,
pane dispatch/navigation/freeze, status projection and architecture tests. Cover
exact UNC filename/parent delivery including share-root parents and metacharacters,
259/260 length boundaries, root refusal before scheduling, every new native-error
row without retry, and the existing local/WSL cancellation and outcome behavior.
Commit checks retain the conformance/security registry. This uses an existing
Shell execution mechanism and an already-supported UNC namespace/credential
boundary; focused adversarial proof establishes the bounded addition without a
new all-layer full/deep run. Deterministic tests inject the Shell start delegate,
touch no network and launch no desktop application. Live UNC and actual desktop
association proof remain unexecuted environmental work, separate from these tests.
