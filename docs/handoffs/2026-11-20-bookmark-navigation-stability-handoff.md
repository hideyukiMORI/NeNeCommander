# 2026-11-20 Bookmark navigation stability handoff

Status: selected source/native proof complete; PR402 hide acceptance remains.

## Current candidate and invariant

Issue #495 / PR496 uses `fix/495-bookmark-navigation-stability` at
`D:/NeNeCommander/worktrees/issue-495`, based on PR402's `76a281a` and targeting
`feat/398-bookmark-modal-scrim-keys`. PR402 remains Draft until hide screen acceptance.
Do not replace public beta.2 or claim main/Preview integration from this follow-up.

Stable Bookmark collections retain complete captures and update only changed rows.
The existing session/key map/focus owners remain. At08:14 hide explicitly replaced
inline address editing with display-only paths and a Ctrl+L input modal. One
AddressEditorView reuses the existing state/navigation owner. Expected-state cancel,
native IME, Tab containment and captured-list restoration retain their sole routes.
Path PointerPressed is consumed and uses the existing idle-only focus helper, so
F2/F7 name input retains focus. No new settings schema, parser or provider route.
Rules: ARC-001/004, CMD-003/009, KBD-001/003/005, CS-002/013/022,
ADR-0044/0061/0080. ADR0044, KEYBOARD_MODEL and DESIGN_HANDOFF reflect hide's decision.

## Evidence and next action

The [quality record](../quality/2026-11-20-bookmark-navigation-stability.md) owns
commands, failures, reviews and limits. Initial row8/Presentation45/Application19/
ownership24 cases pass. The modal revision's selected74 cases pass; overlapping
counts are not summed. Final App build has zero warnings/errors. Changed whitespace
exits0 with a workspace-loading warning. CS-013 has five unchanged unrelated findings.
No full CI, coverage, mutation or release was selected.

Native checks cover startup/list/Tab, Bookmark categories and Search/close, explicit
address modal, validation, Tab/Shift+Tab text, actual Japanese IME Enter/Escape,
captured-right navigation, scrim dismissal, path clicks and preserved F7 input.
The property-only focus attempt failed and remains recorded; the final pointer
handler passes and independent re-review has no required correction. This is
behavioral proof, not measured latency or hide's subjective screen acceptance.

Permanent evidence: `D:/NeNeCommander/evidence/bookmark-navigation-20261120/`.
It retains all attempts, native journals/images, exact command arrays and final1061
input hashes. App DLL: `A4301CDBEBD68AA29725F4BB69A5639AD984331D9832E3F584EBA1D415F2E58B`.
Settings remain428 bytes, SHA256
`7EA7AB2FB5B06A68D6015500A227BA7AEA3B7F1DA322430834B4B1361B2630EF`.
Never restore the old218-byte snapshot. All test-launched Apps including the former
6760 comparison ended under hide's08:03 authority; the final process read is empty.

Finish normal Commit checks, commit/push and update PR496 with this final scope and
proof. Its technical integration may target only the existing feature branch;
PR402's main/Preview acceptance remains separate. Retain unfinished worktrees and
all permanent evidence. #491/PR492 has zero native comparison samples; #453 remains
open. #386's bounded Dependabot policy and #397's truncated-title-only tooltip are
separate continuing work. Four old cleanup rejections remain below without retry.

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
