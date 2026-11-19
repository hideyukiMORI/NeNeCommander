# 2026-11-20 Bookmark navigation stability handoff

Status: source candidate verified; native acceptance pending. Issue #495.

## Current candidate and invariant

Branch `fix/495-bookmark-navigation-stability` starts from PR402's `76a281a`.
The worktree is `D:/NeNeCommander/worktrees/issue-495`; intended PR base is
`feat/398-bookmark-modal-scrim-keys`. Keep both this follow-up and PR402 Draft.
PR402 still requires hide's screen acceptance before main/Preview integration.

The sole Bookmark session/key map/focus owner remains. Stable browse collections
replace only changed marks/captures, preserve complete category membership and
prevent native focus reentry. The existing window/pane views restore the active
file list and implement the accepted Ctrl+L-only address-edit entry. Settings and
filesystem/navigation policy are unchanged. Rules: ARC-001/004, CMD-003/009,
KBD-001/003/005, CS-002/013/022, ADR-0044/0061/0080.

## Proof and next work

The [quality record](../quality/2026-11-20-bookmark-navigation-stability.md) owns
exact commands, initial failures, review and limits. Eight row-source cases,
45 Presentation boundaries, 19 Application cases and 24 view-ownership cases
pass. Final App build has zero warnings/errors; changed whitespace and normal
Commit checks pass. Complexity exit1 is five unchanged pre-existing findings;
none is in the changed source. No full CI, coverage, mutation or release ran.

Permanent evidence is `D:/NeNeCommander/evidence/bookmark-navigation-20261120/`.
It contains the inherited source/patch/hash snapshot, all attempt logs and script
arguments, final complexity JSON, 1,059 frozen input hashes, final DLL hash and
current settings facts. Reuse this proof while its relevant inputs are unchanged.

After hide unlocks the desktop, select an actual returned candidate window using
the computer-use skill. Execute the bounded startup/category/close/j-k/Tab/Ctrl+L/
text/IME matrix in the quality record. Clicking a passive address must not start
editing. No bookmark save/delete is required. Do not infer smoothness or response
time from the unit tests. PR402 screen acceptance is distinct from agent checks.

At 07:53:31 settings remain 428 bytes, SHA256
`7EA7AB2FB5B06A68D6015500A227BA7AEA3B7F1DA322430834B4B1361B2630EF`.
Never restore the old 218-byte snapshot. Current registered bookmarks belong to
hide. App DLL SHA256 is `F2952117222AC403837450337481A476EA9AE0E93499AD528135DF9B153DAB64`.

## Retained process and work

The earlier launch is PID6760, path
`D:/NeNeCommander/worktrees/issue-398/src/NeNeCommander.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe`,
started 07:28:11. Its initial window inventory was empty; a later process read
found a native window. Automatic approval rejected the scoped Stop-Process before
execution because the live window might contain state. Keep it pending hide's
answer to the earlier request; do not retry termination through another mechanism.
No UI input was sent to this process in this resumed source-verification work.

Keep issue-495's worktree/output and the unaccepted issue-398/430/431/445/457
worktrees. #491's separate comparison remains Draft PR492 with zero native samples;
#453 remains open. Do not merge this fix to main or replace the public beta.

## ! hide実行待ち：後片付け

Four older cleanup targets were rejected by automatic approval as blocked by
policy before this task. They are not retried here. The earlier evidence/archive
checks and ready-to-run script remain the owning record; execution must recheck
accepted state, unsaved content, links and active references. Targets:

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
