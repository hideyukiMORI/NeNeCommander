# ADR-0064: Open WSL files through the existing Windows Shell handoff

Status: accepted

Date: 2026-10-12

Issue #224. This implements hide's instruction to continue the remaining product
work through the existing design and implementation authority.

ADR-0065 supersedes this decision's UNC-launch rejection and Application provider allowlist. Its WSL path admission and remaining handoff contract stay active.

## Context

`OpenFocused` already owns file activation, and Windows can access WSL files
through the distribution-qualified namespace. The missing behavior is a Windows
association handoff for a focused WSL file, not Linux application discovery.
Ordinary Shell paths can normalize names that the WSL filesystem preserves;
passing arbitrary Linux names or assuming extended-length Shell support would
therefore be unsafe.

Microsoft documents [Windows access to WSL files](https://learn.microsoft.com/en-us/windows/wsl/filesystems)
and the distinction between [filesystem and Shell path support](https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation).
The bounded admission below is a product decision, not a claim that every Windows
application can open every admitted WSL file.

## Decision

Extend ADR-0046's one `IFileLauncher` to accept `FileSystemPath`. `PaneSession`
admits Windows-local and WSL files, still routes directories to navigation, and
rejects arbitrary UNC files before the port. `WindowsShellFileLauncher` enforces
the same provider restriction when called directly. A provider root is refused.

For WSL only, every Linux path segment must satisfy the existing canonical Windows
name rules and the canonical namespace path must be shorter than 260 characters.
`WslPath.HasWindowsCompatibleNames` exposes this immutable lexical fact using the
parser's existing segment predicate. It does not parse the path again, manufacture
a Windows location, access a drive, or change WSL parsing/identity. Invalid names,
including an invalid ancestor, and excessive length return `ProviderUnavailable`
before scheduling. Windows-local admission remains unchanged.

One Shell call receives the exact ordinary canonical path and its canonical
parent, with `UseShellExecute = true`, no arguments and no verb. Do not replace
names, copy a temporary file, invoke `wsl.exe`/`xdg-open`, choose another provider,
or retry with another namespace. ADR-0060's extended-length filesystem-I/O rule
continues to govern reads and mutations; this separate path-activation contract
uses only names admitted for the ordinary Shell representation.

All existing scheduling, cancellation, wrapper disposal, pane freeze, typed
outcome and content-preservation behavior remains in the same implementation.
Acceptance means the Shell accepted the handoff; it does not prove that the
associated application opened the file. Normal association semantics, including
executables, shortcuts and reparse resolution, and the documented path race in
ADR-0046 remain. There is no identity or mutation preflight and no new credential
source. An associated application owns any subsequent access or edits.

## Alternatives rejected

- A second launcher/provider router would duplicate the same Shell operation.
- Linux command dispatch needs a separate application-selection contract and is
  outside this change.
- Passing all Linux names or extended-length paths to arbitrary associations
  would assume semantics that the Shell/application does not promise.
- Re-parsing the WSL path as a fabricated Windows path would violate ARC-008.
- A separate Windows-name validator would duplicate the canonical path rules.

## Migration and proof

Widen the port and its test fakes atomically; add no dependency, state variant,
command, presentation resource or composition path. This supersedes ADR-0046's
WSL rejection and Windows-local-only port type; its remaining contract holds.
Update CMD-002, SEC-002, ADV-019, filesystem boundaries and the glossary.

Under ADR-0061, build the affected projects and prove path-name classification,
Windows/WSL single dispatch, all lexical/provider refusals, length boundaries,
cancellation around scheduling/handoff, failures, repeat freeze and preserved
pane state. Include directly affected presentation and architecture tests plus
Commit checks. The Shell API and execution owner are unchanged; this bounded
provider admission is established by focused adversarial tests without another
all-layer deep/full run. Actual desktop associations remain separate release
environmental proof under Issue #94 and are not claimed by delegate-based tests.
