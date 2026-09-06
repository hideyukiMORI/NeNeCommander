# 設定の永続化 引き継ぎ書 — 2026-09-06

Status: informational

## Current status

- Issue: [#74](https://github.com/hideyukiMORI/NeNeCommander/issues/74)
- Worktree: `C:\Users\info\WORKS\NeNeCommander-74`
- Branch: `feat/74-settings-persistence`
- Integration base: `991d7ffd24ba0e33a2f9a09a8b9c88d4eb2717cf`
- ADR: [`docs/adr/0040-atomic-settings-write-and-session-editor.md`](../adr/0040-atomic-settings-write-and-session-editor.md)
- Report: [`docs/reports/2026-09-06-settings-persistence-daily-report.md`](../reports/2026-09-06-settings-persistence-daily-report.md)

The approved Canvas handoff is [NeNe Commander UI Preview](https://claude.ai/design/p/ff811404-8e96-4dd0-92c1-320b3002b4b9?file=NeNe+Commander+UI+Preview.dc.html),
with review notes at `NeNe Commander UI Review.dc.html` in the same project. Approved scope is the
settings modal structure, `Ctrl+,` entry, two persisted choices, save-on-change explanation,
close/Escape without rollback, and separate persistent warning. Numeric mock styling and the
internal `保存 #1` label are not product requirements; the implementation uses existing semantic tokens.

## Canonical mechanisms and review entries

- `SettingsSession` owns the current selection, modal state, ordered write queue, typed persistence state,
  and shutdown observation. Its observation tail finishes the defect callback before `StopAsync` returns.
- `WindowsLocalSettingsStore.WriteCore` owns preflight, ancestor capture, fixed-temp creation, flush,
  pre-publish revalidation, atomic publish, post-publish baseline capture, and identity-checked cleanup.
- `SettingsWriteRejected.Effect` keeps `SettingsDirectoryEffect` separate from temporary residue.
  Cancellation is observed immediately before the first mutation; after mutation begins, the attempt
  completes or returns a typed rejection and effect.
- `CommanderSession.HandleAsync` is the one command route. `Ctrl+H` remains active-pane session state;
  `Ctrl+,` opens settings only in FileList / NavigationSurface. Modal and operation precedence freezes
  settings entry.
- `CommanderWindow.OperationProgressed` synchronously renders the exact reported pane snapshot under
  ADR-0019. Settings notifications coalesce to the latest session choice. The modal creates its scheme
  items and sets initial focus only on closed → open, preserving native radio focus during save-state renders.
- `CommanderApplication.ApplyColorScheme` replaces the one composition-root scheme dictionary and applies
  the matching closed appearance to the window content.

## Evidence and remaining work

Infrastructure project mutation reached 90.04% without threshold changes. Release build, affected tests,
coverage, conformance, security commit checks, real NTFS atomic-write cases, reparse/foreign-replacement cases,
late-cancellation effects, missing-parent retry, and Stop/defect ordering pass as recorded in the report.

Before Ready or merge:

1. Complete independent Draft review.
2. If #73 merges first, rebase onto that exact latest `main`; preserve `OperationAwaitingConflict` in pane/modal
   freeze and retain both conflict and settings render paths.
3. Run focused tests after any rebase or fix, then obtain exact-head deep review and dependency review.
4. Move Draft to Ready once and require the fresh canonical CI gate for that unchanged head/base.

Do not add a second writer, location resolver, command route, scheduler, theme mapping, dependency, waiver,
or threshold change. Do not claim the residual path-based reopen interval is race-free. Do not use current pane
hidden visibility as persisted input.
