# Selection controls handoff — 2026-11-25

Status: selected technical implementation complete; Issue scope published with approval; feature source/PR/main integration pending.
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

The original automatic rejection is retained in publication-hold-01.json. Its exact
Issue body was later explicitly approved and published; the checkpoint below
supersedes that old pending question. No feature-source push, PR, merge or release
is performed. Keep all worktree/output copies until acceptance, integration and
permanent evidence preservation. No cleanup rejection or eligible cleanup exists.

Beta.5 remains the priority: its changed-input mutation and final CodeQL/native-menu
proof are tracked in its own current quality/handoff, not this feature's evidence.

Correction at10:59JST: feature source is committed2cfc3b2. Final review's host-order
translation test came after complexity03and introduced cognitive11. Its identical
24context/key cases are now parameterized; selected translation10passes57/57,
build/format10pass and complexity05has exactly the original three findings, exit1.
Only this test changes; final-inputs02maps1366unchanged hashes. Earlier complexity03
must not be cited as measuring the final reviewed test. Initial UTF-8/cp932 setup
failure and unchanged09execution are retained and explicitly corrected in the quality
record. The following normal-hook correction commit is the latest local selection head.

## Current publication checkpoint — November 25, 21:31 JST

The earlier Issue-body permission hold is resolved. hide explicitly approved the
exact saved issue-body-02.md at 19:59 JST, and the publication completed at 20:07 JST.
issue-readback-03.json retains the exact matching body. A fresh read-only GitHub
connector observation at 21:29 JST confirms the same open Issue #562 and published
scope. That historical proposal body is not a claim that feature source was pushed.

The local implementation and final test-only correction are saved at fa5422b.
Source publication, PR/main integration and physical/visible acceptance remain
pending. beta.5 is separately published from 5727258 and excludes this feature.
Its completed-copy cleanup explicitly excludes issue-562 and selection outputs.
This checkpoint changes documentation only: successful product/build evidence is
reused without additional product tests or CI. No new public write is performed.
The first documentation script failed before writing because universal-newline
reading altered one CRLF in the comparison; raw UTF-8 decoding confirms exact text.
That failed attempt and its no-change commit result remain recorded separately.
