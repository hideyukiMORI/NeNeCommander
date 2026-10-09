# ADR-0052: Record Windows UI release evidence through one admitted-input harness

Status: accepted

Date: 2026-10-09

## Context

QLT-009 requires release readiness to rest on recorded proof on the supported environment matrix,
and QLT-011 requires every UI change to prove its interaction states. Issue #94 owns the Windows
release matrix: Windows-reported 100/150/200/300 percent scaling, the 900-by-600-DIP narrow state,
eight color schemes, high contrast, keyboard-reached modals, the window-adjustment helper, Narrator,
a cross-scale move, and the refused taskbar seam. Every cell needs a real window, real UI Automation
reads, and for most cells synthetic keyboard input.

Earlier evidence was taken by hand-written drivers outside the repository. The Issue #70 run sent no
input but changed the production settings document in the operator's own profile. The Issue #100
driver sent input while hide was present; its foreground guard compared only a window handle, its
default binary pointed at a worktree that no longer exists, and its stop path released held keys
after it had already lost foreground ownership. On 2026-09-03 an unguarded SendKeys run captured
hide's own typing into the product. None of these drivers recorded a screenshot hash, verified the
binary's commit, or protected the settings document, so none of their output satisfies the Issue
#94 acceptance record. hide confirmed on 2026-10-08 that no dedicated interactive Windows
environment exists yet; the host has no 100, 200, or 300 percent display, one interactive session,
and no test account.

The product resolves its settings document through `WindowsLocalSettingsLocation` from the
`LocalApplicationData` known folder. A `LOCALAPPDATA` environment override does not relocate it, so
settings isolation is a property of the profile or machine that runs the harness, not of the
harness.

## Decision

Add one repository-owned environment launcher under `eng/ui-evidence/` that records Issue #94
cells. It is the sole mechanism for synthetic input against the product and for release UI
evidence. It joins neither `eng/check.ps1` nor CI; like `eng/run-live-wsl-tests.ps1` under
ADR-0043 it is a separately recorded tier whose unexecuted cells remain unexecuted.

- **Owned process.** The harness starts the Release `NeNeCommander.App.exe` given by an absolute
  `-Binary` path, records its SHA-256 and `ProductVersion`, and refuses to start when
  `-ExpectedCommit` is given and the embedded `+<sha>` does not match. It derives the root window
  handle from the owned process identifier, records the process identifier, session identifier, and
  handle, and closes only that window through the UI Automation window pattern. It never
  activates, moves, or sends to any other window.
- **Admission before every input.** Input is sent only in `-Mode Input` with `-AllowInput` and an
  operator-supplied `-EnvironmentId` naming the dedicated environment. Immediately before each
  send, a pure admission function receives the observed foreground root handle, its process and
  session identifiers, the UI Automation focused element's process identifier and automation
  identifier, and the presence of the mode or modal element the cell expects, and compares them with
  the owned values and the cell's expectations. Any mismatch stops the run before the send. After a
  stop no further `SendInput` call occurs for any reason, including cleanup.
- **No held keys.** Each key is sent as one `SendInput` call containing both the down and the up
  event. The harness offers no held-key primitive, so a stop cannot leave a synthetic key pressed in
  the foreground application. Cells that need key repeat are out of this harness's scope.
- **Owned effects only.** Filesystem effects are confined to `-TestRoot`, an absolute path that is
  empty or absent at start. The settings document is read through the same known-folder resolution
  as the product; its bytes or absence and hash are recorded at start. It is written only when the
  operator asserts `-OwnedProfile`, and it is restored to the recorded bytes or absence at the end
  with the restored hash recorded. Without that assertion scheme-switching cells are skipped with
  the reason `profile-not-owned`. The harness changes no display scale, high-contrast state,
  taskbar setting, or other account.
- **Honest record.** Each run writes `evidence.json` and `summary.md` without a byte-order mark.
  The record carries the harness file hashes, repository head, binary hash and version, operating
  system build, session, user SID, environment identifier, every monitor's physical rectangle and
  DPI, the high-contrast flag, taskbar rectangle and edge, Narrator state, and the settings
  document hash. Each cell carries its identifier, mode, `PASS`, `FAIL`, or `SKIP` with a reason,
  scheme, scaling, window bounds in DIP and physical pixels read both by `GetWindowRect` and the
  UI Automation bounding rectangle, owned process and handle, focus and bounds checks, screenshot
  path and SHA-256 with the capture method, and start and end times. A skipped or failed required
  cell keeps Issue #94 and release readiness open; a screenshot alone is never a pass.
- **Observe mode.** `-Mode Observe` sends no input. It may resize the owned window through
  `SetWindowPos` to the 900-by-600-DIP narrow state and read layout facts. `-Preflight` records the
  environment without starting the product.
- **Scanned like every other script.** The harness functions live in a PowerShell module, so the
  SEC-011 script scan parses `.psm1` files under `eng/` as well as `.ps1` files, and the SEC-005 secret
  scan reads `.psm1` text like `.ps1`. This widens the gate's input and changes no rule, threshold,
  or suppression.

## Rejected alternatives

- Keep ad-hoc drivers outside the repository: their guards, defaults, and records diverged from
  the Issue #94 contract and cannot be reviewed or reproduced.
- Add a production test hook or a second key map so that evidence bypasses the keyboard route:
  Issue #94 forbids it, and it would prove a path users never take.
- Guard by window handle only: a handle does not prove the process, session, or focused control,
  and the 2026-09-03 and 2026-10-07 runs show that keystrokes then land in the wrong window.
- Release held keys on stop: a release sent after ownership is lost reaches another application.
- Redirect settings with a `LOCALAPPDATA` override: the product uses the known-folder API, so the
  override isolates nothing and misleads the record.
- Join the canonical gate: a build machine without a dedicated display matrix would turn skips
  into merge evidence.
- Automate machine-wide scaling, high contrast, or taskbar changes to supply missing cells: this
  changes the operator's desktop and is excluded by Issue #94.

## Consequences

- Issue #94 cells are recorded only through this harness in a dedicated environment identified by
  `-EnvironmentId`. The host's shared console is not such an environment; hide's explicit go is
  required for any input run there, and the admission function stops on the first foreign focus.
- The dedicated environment must preconfigure Windows-reported scaling, high contrast, and display
  topology; the harness reads and records them read-only. Cells the environment cannot supply stay
  `SKIP` with `environment-required`.
- Settings isolation depends on running under a test-owned profile or guest; `-OwnedProfile` is an
  operator assertion that is recorded, not verified.
- Cross-scale move and taskbar-seam cells depend on multi-display topology that a single-display
  guest cannot supply; they are registered as environment-required until an identified topology
  exists.
- The admission function is pure and self-tested without Win32; the Win32 and UI Automation reads
  are separate functions that are exercised only in the dedicated environment.
- Admission is observed immediately before each send, but the observation and the `SendInput` call
  are two steps; a focus change between them can still deliver one chord to another window. The
  harness narrows that window to one atomic chord and never repairs it with further input.

## Migration and removal

`D:/NeNeCommander/evidence-100/Invoke-WindowAdjustmentEvidence.ps1` stays outside the repository
as an observation reference only and is not run again. Its recorded 125 percent evidence for
Issue #100 remains valid for the unchanged implementation. When every Issue #94 cell is recorded
and release proof is complete, this harness remains the mechanism for re-recording after UI
changes; removing it requires a superseding ADR.

## Executable proof

`eng/ui-evidence/selftest.ps1` proves that the admission function refuses a foreign foreground
root handle, a foreign process identifier, a foreign session, a focused element owned by another
process or carrying an unexpected automation identifier, and a missing expected mode or modal
element, and admits only the fully matching observation. Commit-mode and canonical gates prove
format, conformance, and that `src/` and `tests/` are unchanged. The Issue #94 record proves each
cell in the dedicated environment; this ADR claims no cell as passed.
