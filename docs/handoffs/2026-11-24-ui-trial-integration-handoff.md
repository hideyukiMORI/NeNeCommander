# 2026-11-24 UI trial integration handoff — Issue #564

Local technical verification is complete for the combined #530/#531/#544/#547/#548/#551
trial. This is not publication, main integration, release or human acceptance.
The broader #564 TODO remains open. beta.4 source/tag3909039 is unchanged.

Worktree: D:/NeNeCommander/worktrees/issue-564-ui-trial, branch test/564-ui-trial.
Evidence/output: D:/NeNeCommander/evidence/ui-trial-20261124 and
D:/NeNeCommander/outputs/ui-trial-20261124. Preserve all original unfinished feature
worktrees and outputs. No CUA/blue frame, shared input or normal-settings write.

The current source merges55147fc6ba,5314b3e75b (530/544 ancestry),547298cc56 and
548ee1859a. The same CommanderSession/CommandCatalog, pane owners, input mapper/
repeat guard and window route remain the only mechanisms. The cached active header
refreshes navigation availability/language independently of pane rows. Render
ownership now prevents native Closing reentry from resurrecting or overwriting a menu.
Window key declarations move intact to WindowAdjustmentKeyMap; immutable pane activity
facts replace duplicated admission helpers; header Click wiring belongs to its pane view.

Normal App and selected consumer builds pass0warnings/errors. Application admission
passes225distinct cases (224 plus corrected28-command count1), Presentation keymap
passes162. Earlier unaffected seam successes remain recorded. The three new complexity
violations are removed (299/296/269); exactly three older findings remain. Canonical
whitespace verification passes after7CRLF-only changes, with1368pre-normalization inputs
and exact before/after hashes retained. Do not rerun behavior merely for those line endings.

Real-control integration passes60checks each in English/Japanese/Simplified Chinese,
with all3process exits0 and ordinary settings/input desktop unchanged. Reentry attempts
03/04 establish two red ownership cases; corrected07/08 each pass4checks and exit0.
Attempt06's early-teardown0xC0000005 is retained and that earlier condition is not
claimed solved. Independent review accepts the repaired render boundary and responsibility
changes. See ../quality/2026-11-24-ui-trial-integration.md for exact commands and limits.

Next: save the pending local merge, connect current main's documentation without
changing product inputs, prepare a separately configured interactive trial with owned
D folders/private settings, then obtain hide's physical/visible acceptance before
feature publication/integration. No visible trial has been launched. No cleanup is
eligible while this work is unfinished; beta.4 cleanup is already complete.

Local integration saved as070f65952d349af07cd342eb545ee705316bd7e1. Current mainccb303b documentation is now connected additively; no product path differs from070f659.
