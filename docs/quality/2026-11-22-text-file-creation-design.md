# Empty text-file creation investigation — 2026-11-22

Issue #544. Status: proposed contract and isolated OS experiment, no product implementation.
Base main7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2; dedicated branch
`docs/544-text-file-creation-design`. ADR-0061 scope is documentation plus an
external exploratory primitive probe. No product build/test/coverage/mutation/CI.

## Invariant and existing mechanism

A submitted single filename creates only one empty direct child of the frozen
location, never overwrites or removes an existing entry, and reports a completed
effect even when later refresh fails. Existing name entry, FileOperationGateway,
provider router, Windows I/O scheduler and PaneReducer remain the owners.
[ADR-0085](../adr/0085-create-empty-text-file.md) is a proposal requiring review.

## Source inspection

Read #544's current GitHub body, the normative command/provider/security/keyboard/
workflow rules, ADR-0020/0027 and the existing source/consumer tests. Frozen hashes
of22 source/test inputs are in the permanent evidence source-manifest.json.
The current name entry carries a literal seed; localized default text therefore
needs a closed presentation text variant, not a hard-coded Application filename.
Shift is not represented in KeyboardModifier. Ctrl+F7 is the proposed shortcut.
Directory creation uses Exists followed by Directory.CreateDirectory; that code
is not a suitable exclusive-file primitive and was not changed in this task.
WindowsFileFailureNormalizer does not map native80/183 to Conflict globally;
the creation boundary must own exact collision interpretation. Native directory
collision returns5 in this environment and must not be blindly equated with every
access-denied error. The scheduler deliberately does not cancel admitted steps.

## Isolated primitive experiments

Permanent evidence: `D:/NeNeCommander/evidence/text-file-creation-20261122/`.
Owned fixtures: `D:/NeNeCommander/outputs/text-file-creation-20261122/fixture-01`
and `fixture-02`. Both roots were absent before creation. Every sentinel, link and
link destination belongs to these two dedicated roots. No user App/settings,
shared-desktop input, WSL/UNC/removable-media path or external content is involved.

Observed runtime is .NET10.0.15 in PowerShell, Windows10.0.26300. This is not a
pinned-SDK product build. The probe's Add-Type/DllImport code is exploratory code
outside the repository, not the source-generated LibraryImport implementation
required for a future production boundary.

| Probe | Result | Interpretation |
|---|---|---|
| First BCL script compile | exit1, CS8632 | Nullable annotation context missing; no fixture or mutation started. Original script/log retained as attempt1. |
| BCL File.OpenHandle(CreateNew, Write, None) | script exit0;9 contract cells pass,1 fail,1 residual observed | Dangling final link is followed and creates its absent target. Script completion is explicitly not an all-pass test. |
| Native CREATE_NEW plus OPEN_REPARSE_POINT | exit0;10 contract cells pass,1 residual observed | Dangling/live final links are refused; both native flags still follow parent links. |

Each script covers absent target (0 bytes), ordinary/hidden/readonly/Unicode
sentinels, a directory with a preserved child, a file appearing after a prior
absence check,16 pairs of simultaneous contenders, a live final symlink, a dangling
final symlink, and a parent-link redirection demonstration. The16 pairs are one
matrix cell: every pair had exactly one successful creator and a0-byte result.
This nondeterministic OS observation does not replace controlled scheduler/barrier
product tests. The native script rejects any fail/unexpected cell with exit2;
the deliberate parent-link observation is a limit, not a safety pass.

BCL dangling-link result: Created=true, target changed from absent to present,
link remained. Native dangling-link result: Created=false, Win32 error80, target
remained absent and link remained. Existing-directory native result: error5 with
its sentinel/hash unchanged. Parent-link redirection is observed in both versions.
No fixture is deleted or altered to conceal these results.

Exact executable commands, from any directory (paths are deliberately explicit):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/text-file-creation-20261122/probe-exclusive-create-attempt1.ps1 -FixtureRoot D:/NeNeCommander/outputs/text-file-creation-20261122/fixture-01 -Report D:/NeNeCommander/evidence/text-file-creation-20261122/exclusive-create-result.json
pwsh -NoProfile -File D:/NeNeCommander/evidence/text-file-creation-20261122/probe-exclusive-create.ps1 -FixtureRoot D:/NeNeCommander/outputs/text-file-creation-20261122/fixture-01 -Report D:/NeNeCommander/evidence/text-file-creation-20261122/exclusive-create-result.json
pwsh -NoProfile -File D:/NeNeCommander/evidence/text-file-creation-20261122/probe-native-exclusive-create.ps1 -FixtureRoot D:/NeNeCommander/outputs/text-file-creation-20261122/fixture-02 -Report D:/NeNeCommander/evidence/text-file-creation-20261122/native-exclusive-create-result.json
```

These are historical commands. Do not rerun against the retained roots: the probes
require absence, and no new run is justified unless a primitive or unresolved claim
changes. The first failed script and log were preserved before adding #nullable
enable. No test was retried into success: that failed attempt never reached I/O;
the BCL behavioral failure remains in its original result. The native variant is a
changed candidate with separate fixture/output. No WSL capability is inferred.

## Investigation-only failures and records

Several guessed file paths were absent (NameEntry under Operations, DualPaneSession
under Sessions, OperationPresenter, WindowsIoExecutor, ProviderStepOutcome under
Infrastructure, ProviderPathContainment outside Paths, and a Conformance test project).
Windows rg glob arguments also failed with error123; directory-based rg/rg --files
located the actual files. One attempted python summary read could not find python;
PowerShell parsed the saved JSON instead. A first PowerShell summary emitted an
empty projection; direct file inspection and a new variable confirmed the full
8879-byte saved BCL result and its failure. These were read/discovery/reporting
errors, not product or primitive behavior changes. All raw experiment logs remain.

An initial non-elevated git status reported dubious ownership; command-scoped
safe.directory allowed read-only inspection without changing global configuration.
The primary main is clean. This task's D worktree was created from that exact main.
Two progress-message timestamps were mistyped and corrected in the next update;
probe JSON UTC times and the acquired clock values are the chronology evidence.

## Required next proof and limits

Technical independent review, accepted provider contract, implementation at every
port/projection, baseline negative cases and focused consumer builds/tests precede
one justified deep-review candidate. Required WSL live cells and isolated native
input remain unexecuted. No new visual layout is approved or shipped. Mouse menu
integration belongs with #530/#531; keyboard/palette-only completion is insufficient.
The existing final-ancestor-query-to-path-open race remains explicit. A final-file
no-follow flag is not a parent-chain lock. No full security guarantee is claimed.

#542 public push and #547 Claude transmission remain pending after automatic
approval rejection. This work performs no external write and does not retry either
rejection through another payload, branch or service. Main/beta.3 are unchanged.

Sources read November22: [FileMode](https://learn.microsoft.com/en-us/dotnet/api/system.io.filemode?view=net-10.0),
[CreateFileW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew),
[symbolic-link effects](https://learn.microsoft.com/en-us/windows/win32/fileio/symbolic-link-effects-on-file-systems-functions).

## Additional primitive boundaries — 20:37 JST

Only previously unobserved boundaries were run in fixture-03; the earlier cells
were not repeated. All6 cells pass: hardlink conflict, live/dangling directory-link
conflict, a directory appearing after an absence check, a255-character leaf at a
561-character local path, and refusal of a256-character leaf without adding an
entry. The late directory returns5; the overlong leaf returns123. These raw codes
must be interpreted at the creation boundary; the positive long-path case does
not expand the canonical domain's name or path admission rules.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/text-file-creation-20261122/probe-native-create-boundaries.ps1 -FixtureRoot D:/NeNeCommander/outputs/text-file-creation-20261122/fixture-03 -Report D:/NeNeCommander/evidence/text-file-creation-20261122/native-boundaries-result.json
```

The command exits0. The script and log are retained with the exact JSON. As before,
this is OS primitive evidence only. The current process has no
NENE_COMMANDER_WSL_TEST_ROOT value; no live WSL mutation was started. No inference
about another process's configuration, registered distributions or 9P capability
is made from that absence. Native UI input is also unexecuted.

## Independent review clarification — 20:40 JST

The read-only reviewer requested a fixed completion scope and unambiguous late-
cancellation result. The proposal now makes Windows local AND WSL the feature's
completion scope: missing selected live WSL proof keeps the whole feature
unaccepted/unshipped, with no evidence-controlled runtime switch. A successful
admitted creation returns Succeeded, one FileCreated and progress1/1 even when
cancellation arrives during the step, so the existing preferred-focus route stays
reachable. Other failures/cancellation before admission remain closed and effect-
free. Final technical review is still pending; no product implementation was made.

The documentation commit387a92e passed the normal commit hook:112 normative rules,
24 registered adversarial IDs, security/whitespace/message convention exit0.
The separate new-document relative-link check passes. No product validation is
claimed by these hook results. The original investigation input hashes are unchanged.

## Technical contract accepted — 20:42 JST

The independent read-only reviewer confirmed the corrected contract has no
remaining mandatory correction. They checked the original primitive scripts/results,
existing gateway/provider/scheduler and all22 source hashes (0 mismatches). The
design owner accepts ADR-0085 technically under the current continuation request.
The feature is still unimplemented and unshipped; selected live WSL, deep review,
isolated native input and mouse-entry integration remain required.

Reviewer discovery-only failures were retained in the review: initial git ownership
refusal, then command-scoped safe.directory with a user-ignore-file permission
warning; wrong ADR0027/CommandCatalog paths corrected with rg --files; truncated
reads bounded and repeated; one call aborted before dispatch when a message arrived.
The primary agent also had one read call aborted before dispatch on that message.
No product or filesystem mutation occurred in those review activities.

The next implementation branch is feat/544-create-empty-text-file in the same
issue-544 worktree, starting from the accepted documentation. The documentation
branch/commit are retained. The source-manifest is the pre-implementation baseline;
subsequent product input changes require their own evidence and cannot reuse it as
proof of the final code.
