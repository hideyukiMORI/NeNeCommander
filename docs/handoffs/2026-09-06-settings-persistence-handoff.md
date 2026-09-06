# 設定の永続化 引き継ぎ書 — 2026-09-06

Status: informational

## Current status

- Issue: [#74](https://github.com/hideyukiMORI/NeNeCommander/issues/74)
- Worktree: `C:\Users\info\WORKS\NeNeCommander-74`
- Branch: `feat/74-settings-persistence`
- Integrated `main` base: `495ee51f77ac597b5780df102b0920105e8706ee`
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

## Evidence and remaining work

Infrastructure project mutation reached 90.58% without threshold changes. On integrated #73 base `495ee51f`,
Release build, Application 240, Infrastructure.Windows 209, Presentation.WinUI 83, Architecture 5,
branch coverage 100.00% / 100.00% / 92.77% / 93.29%, conformance, and security commit checks pass.
Real NTFS atomic-write cases, reparse/foreign-replacement cases, late-cancellation effects,
missing-parent retry, and Stop/defect ordering pass as recorded in the report.

Before Ready or merge:

1. Complete independent Draft review.
2. Obtain exact-head deep review and dependency review.
3. Move Draft to Ready once and require the fresh canonical CI gate for that unchanged head/base.

The unexecuted live WSL release tier is tracked by [Issue #93](https://github.com/hideyukiMORI/NeNeCommander/issues/93).
The unexecuted Windows high-contrast, DPI, narrow-width, eight-scheme, and keyboard-modal matrix is tracked by
[Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94). A required skipped cell keeps release
readiness incomplete; it is not a passing result.

Do not add a second writer, location resolver, command route, scheduler, theme mapping, dependency, waiver,
or threshold change. Do not claim the residual path-based reopen interval is race-free. Do not use current pane
hidden visibility as persisted input.
