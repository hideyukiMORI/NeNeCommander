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

22:34 update: current branch7701aa5 contains the local integration and documentation-only
main connection. Interactive trial is built at
D:/NeNeCommander/outputs/ui-trial-20261124/interactive-trial-01/App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe.
Its normal analyzers pass0warnings/errors;176source copies and476output files match,
and exact-binary startup on a non-input desktop responds twice. Only that owned child
was terminated; no normal-shutdown claim. Settings/input desktop remain unchanged.
Private settings and owned left/right folders live under interactive-trial-01. No Fixture
sources/fake providers are in this interactive copy. Its Japanese trial guide describes
six features. hide's22:34 visible-launch question is pending; do not show a window or send
shared input without the answer. No ordinary App is terminated. The source is not pushed.

22:38 visible-trial update: hide explicitly selects「試遊画面を開く」for the prepared
private-settings copy. Initial preflight stops before process creation because the
guard names resources.pri while this build declares NeNeCommander.App.pri; the exact
manifest paths correct that guard without changing any product/output bytes. The
next attempt verifies the seven product exe/dll/pri hashes and opens exactly one
responsive window titled「NeNe Commander / UI trial #564」at22:38:22JST. Owned PID37744
was created22:38:20 and is deliberately left running for hide. No input is sent and
no ordinary App is terminated. Normal settings hash remains561932A6530A6317499BAB59AD0735C8469869A15C6E6DA9BE3507B37AF557CE.
The visible-launch permission is resolved; do not ask again. This observation is not
hide's physical/visible acceptance. Preserve the live binary/output and private
settings; do not rebuild, delete or stop this trial while hide is using it. See
interactive-visible-launch-01.json and interactive-launch-preflight-01.json.

23:09 icon/parent revision: hide's22:41/22:43 requests are implemented locally under
ADR0090's qualified pane route. Bookmarks/Settings/Help are vectors with existing
localized hover/name/shortcuts; each pane has its own parent arrow before the path.
Root/absent/stale owner rejects before activation. Existing navigation, work/modal
admission and asynchronous focus ownership are retained. App build0warnings/errors,
129distinct selected Application cases and47Presentation cases pass across retained
attempts. Test-only CA1062/MSTEST0017/ToString fixture failures are recorded. Whitespace
passes; complexity has exactly3existing findings. Native attempt1 passes52checks but
uses the wrong help-close fixture route; corrected English2/Japanese3/Chinese4 each
pass56checks with exit0 and normal-settings/input-desktop fingerprints unchanged.
Two later comment-only clarifications reuse successful executable proof. Exact
commands, failed attempts, raw results and rendering images remain in the quality
record/evidence root. Final independent receipt check, interactive02 and local save
remain next. Preserve live trial01 and all earlier work. #532 drag/drop remains
unimplemented and out of scope; no additional feature publication or release.

23:10 independent read-only final reconciliation: no required correction. The original
MSTEST0017 finding is resolved;129distinct Application/47Presentation successes,
identical3complexity findings,176App inputs/4DLLs/7fixture identities and all three
56-check/exit0 native receipts reconcile. Physical input and human visible acceptance
remain separate. Save this local revision and prepare interactive02 next.
