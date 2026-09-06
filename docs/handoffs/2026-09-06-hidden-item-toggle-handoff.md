# Handoff — hidden-item toggle — 2026-09-06

Status: Draft implementation

Worktree: `C:\Users\info\WORKS\NeNeCommander-72`
Branch: `feat/72-hidden-item-toggle`
Base: verified `origin/main` / `4b01d286cea2a823dddddd87aca0c611bbb2d0ff`

The design delegation from hide selects `Ctrl+H` for Issue #72. This is recorded as a delegated design decision, not as a claim that hide previously approved the individual key. The implementation adds `UserIntent.ToggleHiddenItems`, maps `Ctrl+H` only in `FileList`, routes through the existing pane session and reducer, and adds the generated localized hint. It does not persist the setting; Issue #74 owns that boundary.

Pending: run focused tests, commit-hook checks, the required security/conformance checks for the changed surface, push the branch, and create a Draft PR. Ready-for-review, canonical integration CI, merge, runtime proof, and settings persistence remain pending.

Do not modify the separate Issue #85 worktree or branch while continuing this handoff.
