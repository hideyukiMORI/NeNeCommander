# 設定の永続化 引き継ぎ書 — 2026-09-06

Status: informational

## Current status

- Issue: [#74](https://github.com/hideyukiMORI/NeNeCommander/issues/74) (closed by [PR #92](https://github.com/hideyukiMORI/NeNeCommander/pull/92))
- Worktree: `C:\Users\info\WORKS\NeNeCommander-74`
- Branch: `feat/74-settings-persistence`
- Integrated `main` base: `495ee51f77ac597b5780df102b0920105e8706ee`
- Final feature head: `ddec3badf02d933236b98b259c9f47d5e767c24e`
- Squash merge: `b6096d6a1a1bdda24c04bd770ebd0d516ced9c11`
- ADR: [`docs/adr/0040-atomic-settings-write-and-session-editor.md`](../adr/0040-atomic-settings-write-and-session-editor.md)
- Report: [`docs/reports/2026-09-06-settings-persistence-daily-report.md`](../reports/2026-09-06-settings-persistence-daily-report.md)

The approved Canvas handoff is [NeNe Commander UI Preview](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+UI+Preview.dc.html),
with review notes at `NeNe Commander UI Review.dc.html` in the same project. Approved scope is the
settings modal structure, `Ctrl+,` entry, two persisted choices, save-on-change explanation,
close/Escape without rollback, and separate persistent warning. Numeric mock styling and the
internal `保存 #1` label are not product requirements; the implementation uses existing semantic tokens.

## Canonical mechanisms and review entries

- `SettingsSession` owns the current selection, modal state, ordered write queue, typed persistence state,
  and shutdown observation. Its raw completion callback publishes each defect once through the existing
  host observer and releases the queue tail in `finally`.
- `WindowsLocalSettingsStore.WriteDocument` owns preflight, ancestor capture, fixed-temp creation, flush,
  pre-publish revalidation, atomic publish, post-publish approved-chain verification and document linkage,
  and identity-and-byte-checked cleanup.
  Matching read preflight and document I/O use the same Windows local scheduler. Temporary validation
  includes exact serialized bytes, and document file reparse entries are rejected.
- `SettingsWriteRejected.Effect` keeps `SettingsDirectoryEffect` separate from temporary residue.
  Cancellation is observed immediately before the first mutation; after mutation begins, the attempt
  completes or returns a typed rejection and effect.
- `CommanderSession.HandleAsync` is the one command route. `Ctrl+H` remains active-pane session state;
  `Ctrl+,` opens settings only in FileList / NavigationSurface. Modal and operation precedence freezes
  settings entry, including integrated #73's `OperationAwaitingConflict` state.
- The file-list hint remains binding-derived: the modifier-label helper names `Ctrl+,`, and
  `KeyHintPresenter` places its localized Settings intent after `Ctrl+H` and before `Escape`.
- `CommanderWindow.OperationProgressed` synchronously renders the exact reported pane snapshot under
  ADR-0019. Settings notifications coalesce to the latest session choice. The modal creates its scheme
  items and sets initial focus only on closed → open, preserving native radio focus during save-state renders.
- Conflict and settings overlays share the existing native-control modal deferral: Enter and Space stay
  with the focused button or selector, while Escape remains the canonical mapped close/cancel route.
- `CommanderApplication.ApplyColorScheme` replaces the one composition-root scheme dictionary and applies
  the matching closed appearance to the window content.

## Integration evidence and remaining release work

Infrastructure project mutation reached 90.58% without threshold changes. On integrated #73 base `495ee51f`,
Release build, Application 240, Infrastructure.Windows 209, Presentation.WinUI 86, Architecture 5,
branch coverage 100.00% / 100.00% / 92.77% / 93.29%, conformance, and security commit checks pass.
Real NTFS atomic-write cases, reparse/foreign-replacement cases, late-cancellation effects,
missing-parent retry, and Stop/defect ordering pass as recorded in the report.

The first exact-head deep workflow `34029758777` ran against head `617b48c`. Domain 96.98%,
Application 95.17%, and Infrastructure.Windows 90.46% mutation passed, while Presentation.WinUI
88.99% failed the 90% break threshold. The candidate stayed Draft. The tests-only commit `ddec3bad`
then added direct proof for all eight scheme labels and selection state, save and warning resources,
visibility, typed null/blank guards, and standard test `FileStream` ownership. It also made the three
CodeQL review threads obsolete without suppression.

Corrected head `ddec3bad` passed dependency review `34031503892` and exact-head deep workflow
`34031541883`. The final deep mutation scores were Domain 95.98%, Application 95.58%,
Infrastructure.Windows 90.46%, and Presentation.WinUI 90.67%; CodeQL analysis, evidence upload,
open security/dependency/secret alert checks, and unresolved review-thread checks all passed.
The unchanged head/base then passed canonical Ready workflow `34033250650` and was squash-merged
as `b6096d6a` on 2026-09-06. Issue #74 is closed.

The first docs-closure canonical workflow `34034018423` then exposed a scheduler-dependent Application
test wait: after the first planned settings write was released, two duplicated helpers allowed only 20
`Task.Yield` turns before asserting that the second write had started. Issue #97 / PR #98 replaced all four
uses with one test-store generation signal. Count inspection and signal capture share the same lock, as do
write recording and signal advancement, so a signal fired before or after awaiter registration is retained.
The existing tests still prove immediate intent return, no second write before predecessor completion,
ordered writes, and latest-state retention. Production, security, workflow, dependencies, thresholds, and
suppressions are unchanged. Dependency run `34034884120` and canonical Ready run `34034893392` passed at
head `f023be12`; PR #98 was squash-merged as `894b999d`, and Issue #97 is closed.

The unexecuted live WSL release tier is tracked by [Issue #93](https://github.com/hideyukiMORI/NeNeCommander/issues/93).
The unexecuted Windows high-contrast, DPI, narrow-width, eight-scheme, and keyboard-modal matrix is tracked by
[Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94). A required skipped cell keeps release
readiness incomplete; it is not a passing result.

Do not add a second writer, location resolver, command route, scheduler, theme mapping, dependency, waiver,
or threshold change. Do not claim the residual path-based reopen interval is race-free. Do not use current pane
hidden visibility as persisted input.
