# 2026-11-24 UI trial integration handoff — Issue #564

Current checkpoint: November24 23:15 JST. HEAD27efbdc records productcd3ecab and
verified visible interactive02 (PID49036). No required review correction remains.
The interactive02 cutoff is accepted at23:20; human visible acceptance remains separate; no feature push/main merge
or new tag. Preserve the running interactive02 and all unfinished feature worktrees.
The later sections give current exact paths/receipts; earlier pending statements
are historical attempts and do not supersede this checkpoint.

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

23:15 interactive02: product revision is saved ascd3ecab83a4215dd4172047a39c122bd13bdbb12
with the normal conformance/security/commit-message hooks passing. The configured
copy builds0warnings/errors with ordinary analyzers. Its178App inputs include176
exact copies, with only declared project/composition substitutions;4dependency DLLs
and all476output files match. No Fixture source or fake provider enters this copy.
The old trial's dedicated preferences are copied with stable source/destination
hash143542E3EC8DB78FC152C2D2297FB798A1AC175209EC69027FE557D47E965905;
normal settings are never copied. The manifest hash is
80387725DA54DE816CD54ACF2EE4A9B5DE396D0B0EB7B96774CB23799B49244E.

`verify-interactive-startup-01.ps1` verifies interactive02's complete manifest and
observes the exact unmodified binary twice more than3s apart on a non-input desktop.
It terminates only that owned startup child; normal shutdown is not claimed. Its
settings/input fingerprints remain unchanged. Then the standing visible-trial
request and this explicit revision request are fulfilled by
`launch-interactive-trial-02.ps1`: one responsive window is observed23:14:48JST,
PID49036, HWND77925544, title「NeNe Commander / UI trial #564 - icons and parent」.
No input is sent, no ordinary App is stopped, and normal-settings hashes match.
Preserve the running binary, its owned folders and private settings at
D:/NeNeCommander/outputs/ui-trial-20261124/interactive-trial-02.
See interactive-trial-parity-02.json, interactive-startup-02.json and
interactive-visible-launch-02.json. All original source/fixture/trial01 outputs remain.

hide's visible acceptance of this revision and the next release's cutoff are pending.
At23:15 an optional scope question offers this trial's features as the next cutoff,
adding drag/drop, or deciding later. No answer, new release authorization, public
feature push, PR, main merge or new tag is inferred. beta.4 remains already published.

23:20 release cutoff: hide answers「今回の試遊版で区切る（推奨）」to the23:15
scope question. The next release includes the current trial02 feature set; no new
feature is added to that cutoff. Drag/drop#532 and the remaining broad#564TODO stay
separate. Proceed with the existing PR/main integration and release-proof/package
workflow. This answer establishes scope; the exact public payload and final release
steps remain concrete actions with their applicable authorization/evidence checks.
The read-only reuse assessment matches1252of1317previous context-menu inputs, with65
changed and none missing. Domain's declared27proof inputs are all unchanged;
Infrastructure's own286source/test inputs also match, but transitive Application
relevance must be assessed before claiming its old mutation proof for the final
candidate. Application26changed/27new and Presentation24changed/11new paths remain
material. No new runtime verification, public source push or tag has started here.
