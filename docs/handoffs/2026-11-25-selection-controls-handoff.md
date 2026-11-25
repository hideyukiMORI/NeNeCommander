# Selection controls handoff — 2026-11-25

Status: integrated by PR #585; Issue #562 closed; completed copies archived and removed. No cleanup hold remains.
Accepted source is mainfb4f2db, exactly tree-equal to reviewed1842c0e. Original
feat/562-selection-controls at e76950a and integration branch1842c0e are retained.
Published beta.5 excludes this later feature. ADR0093 and the quality record describe the single existing reducer,
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


## Active checkpoint — November 26, 07:35 JST

Resume on feat/562-selection-integration at 92beeed in the existing issue-562 worktree.
The original feature branch e76950a is preserved. Accepted main 9603e90 is reconciled
through three cherry-picks; only two documentation insertion conflicts were resolved,
keeping every record. Normal hooks pass. 1,361/1,367 prior inputs match; the six accepted
main differences receive focused consumer/build proof. Current commands/receipts and
the isolated native fixture are under evidence/selection-20261126 and outputs/selection-20261126.
The native attempt is not yet accepted. Physical input/IME/visible acceptance remain
unperformed. No public write, PR/main integration or feature cleanup is claimed.
Continue from verification-01.json and preserve all original failures and evidence.


## Final local checkpoint — November 26, 07:45 JST

Normal App build zero warnings/errors, Application boundary 88 and Presentation 110
pass. Final native attempts 5/6/7 pass 169 each with actual settings/effective locale
checked, exact native ListView row/focus binding, unchanged passive pane and native
address Ctrl+A. Parity03 has 180/182 exact App inputs, two declared substitutions and
four matching dependency DLLs. Normal settings path/length/hash and input desktop
remain unchanged. Old failure4 and both audit/preparation setup errors remain in the
quality record and original receipts. The uppercase tag mismatch is fixture-only;
the subsequent JSON-property-order audit false alarm is corrected structurally.

Source integration review requires no product correction. Final evidence review accepts the declared technical scope with no further correction. All product source is still92beeed; only these
three documents are changed. Source push, draft PR, main squash, Issue closure and
cleanup remain. No new release is planned by this task. Physical input/IME/speech,
uninterrupted physical repeat and pixel/human acceptance are not claimed.

## Completed checkpoint — November 26, 08:00 JST

PR585 is squash-merged asfb4f2db and Issue562 is closed; main-sync-01.json proves
complete candidate/accepted tree equality and a clean fast-forward. The following
completion-only Issue586 records the publication, connector403/CLI fallback and cleanup.
Successful product/native proof is unchanged and is not rerun for these documents.

All5,817 disposable files/581,927,878 bytes are preserved and individually verified in
evidence/selection-20261126/completed-selection-artifacts-01.zip. The original raw
evidence directories, retained branches and commits remain. At07:59:36JST the issue-562
worktree and outputs/selection-20261125 and outputs/selection-20261126 are all absent.
Archive hash, exact inventory, unchanged metadata, clean source, main integration,
zero links and zero active references are checked before removal. See
cleanup-completion-01.json and the quality record for exact paths/hash/commands.

No cleanup remains for this selection task. #545 name filtering is the next local
implementation awaiting accepted-main reconciliation/publication. Preserve every
unfinished task and its evidence. Physical input/IME/speech/pixel/human acceptance
remains unclaimed, and no new release is part of this completion.
