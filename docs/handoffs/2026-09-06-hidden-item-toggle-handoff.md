# Handoff — hidden-item toggle — 2026-09-06

Status: Draft implementation

Worktree: `C:\Users\info\WORKS\NeNeCommander-72`
Branch: `feat/72-hidden-item-toggle`
Base: verified `origin/main` / `1983ccc71e7e304e07f480d66b38f200168eca6a`

The design delegation from hide selects `Ctrl+H` for Issue #72. This is recorded as a delegated design decision, not as a claim that hide previously approved the individual key. The implementation adds `UserIntent.ToggleHiddenItems`, maps `Ctrl+H` only in `FileList`, routes through the existing pane session and reducer, and adds the generated localized hint. It does not persist the setting; Issue #74 owns that boundary.

Focused tests, commit-hook checks, security/conformance checks, push, and Draft PR creation were complete on the original base. Follow-up review fixes the chord-aware key-cap resource mapping, translates the Windows `Ctrl+H` control character without treating an unmodified backspace character as `h`, and adds freeze proof for the new intent in running, confirmation, and name-entry states.

After Issues #85 and #67 integrated, the branch rebased without conflict onto `1983ccc71e7e304e07f480d66b38f200168eca6a`. Locked restore, a zero-warning Release build, Application 188/188, Presentation.WinUI 76/76, Architecture 5/5, 100% Application branch coverage, and 96.53% Presentation.WinUI branch coverage pass on that base. The change is a non-destructive session keyboard/presentation route and changes no security boundary or gate, so no exact-head deep review is required. Final commit validation, updated Draft push, dependency review, Ready canonical CI, merge, runtime proof, and settings persistence remain pending.

Settings persistence remains a separate Issue #74 change and is not added through this handoff.
