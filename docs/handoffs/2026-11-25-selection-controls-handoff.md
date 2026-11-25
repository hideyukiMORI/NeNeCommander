# Selection controls handoff — 2026-11-25

Status: selected technical implementation complete; public publication pending.
Worktree D:/NeNeCommander/worktrees/issue-562; branch feat/562-selection-controls;
base0e0829c. This work is separate from frozen beta.5/Issue580 and must not enter
that cutoff. ADR0093 and the quality record describe the single existing reducer,
selection calculator, mapper/translator and repeat-owner mechanism.

Ctrl+A selects visible entries; produced*inverts; palette ClearSelection uses idle
Escape's existing clear transition. Six Shift navigation keys replace a clamped
inclusive anchor range. Bulk operations clear the anchor and preserve focus.
Provider identity and exact visible-owned paths remain; passive panes, text owners,
busy/modal admission and existing pointer behavior retain their contracts.

Evidence: D:/NeNeCommander/evidence/selection-20261125. Application selected05passes227;
explicit catalog selected08passes4. Presentation08passes225. App06build0warnings/errors;
closure01passes152abstract/140closedroots; final structural03format/conformance pass.
Complexity03retains exactly the three pre-existing findings. Final test-only fixes
receive their own format and normal commit-hook checks. Original red11failures,
stale expectations, test-mode conformance and complexity findings remain preserved.
See [quality](../quality/2026-11-25-selection-controls.md) for commands and boundaries.

Independent source review has two mandatory proof fixes, both implemented and proven:
31-row ordered catalog and actual classifier→modifier→key→mapper text/search routing.
No product change follows source review. Physical keyboard/layout/IME and visible
acceptance remain unperformed. No native input or normal settings were modified.

Automatic approval rejected only the concrete Issue562body write before execution.
Exact body issue-body-02.md SHA256 E16998F287D0997B8B4C35A4C1E1DAC801091653C8DD237566856547C2D5BC19
and publication-hold-01.json retain it; the asynchronous approval question remains
unanswered. Do not retry by another mechanism. No source push, PR, merge or release
is performed. Keep all worktree/output copies until acceptance, integration and
permanent evidence preservation. No cleanup rejection or eligible cleanup exists.

Beta.5 remains the priority: its changed-input mutation and final CodeQL/native-menu
proof are tracked in its own current quality/handoff, not this feature's evidence.
