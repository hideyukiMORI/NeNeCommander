# Context-menu design and implementation proof - 2026-11-24

Status: mechanism design accepted; selected native framework prototype passed; product implementation pending.

Issue531 is open. The dedicated worktree is D:/NeNeCommander/worktrees/issue-531,
feat/531-context-menu, based on local technical pointer candidate76bff48. New-file
Issue544 remains a required dependency for its mouse entry; its local coreb593ef4
and existing provider evidence are retained separately. The beta.4 release candidate
f6c722c and pending exact publication approval are unchanged.

The invariant is exact displayed/executed command targeting through the existing
CommandPaletteSession/CommandCatalog/CommanderSession owners and existing operation
paths. Proposed ADR0091 adds closed menu surfaces and qualified context acquisition,
not a parallel dispatcher. Explorer reveal extends the existing Shell launcher and
STA scheduler with explicit path-handoff limits.

Live GitHub issue bodies, relevant owner/mapper/launcher source, the accepted pointer
ADR and the new-file ADR/quality/handoff were inspected. Background /design returned
access-management guidance only; no consent was changed. Tools-disabled ordinary
Claude advice was collected from an authored public brief with SHA-256
EDBB62D2FCF3995D0B17E93AEAB0DFE4694ACB371FE92A486261CD42B9FAC953. No repository source
or local file contents were sent. Advice, two invocations, raw JSON/stderr and all
investigation attempts are retained in D:/NeNeCommander/evidence/context-menu-20261124.
The journal records failed guessed-path/wildcard reads as well as the slow completed
worktree/file discovery. No render or native design acceptance is claimed.

Technical decisions and selected differences from the advice are explicit in
[ADR0091](../adr/0091-context-menu-through-command-selection.md). Stock native menu
heading/accessibility behavior and root/popup key ordering remain questions for a
bounded design/control prototype. Production implementation awaits independent
technical review. Real Explorer display is not run on hide's shared desktop.

Documentation-only scope: no product build, tests, mutation, full gate or CI starts.
The existing release Infrastructure02 process remains independently live. Preserve
this unfinished worktree/output; integration and unique-evidence checks precede any
completion cleanup. Every subsequent attempt and changed input is appended here.

12:16 independent review: three required clarifications are recorded and addressed
in the proposed ADR: surface-specific rendering/focus and shortcut absence; explicit
positionless ContextRequested rejection plus per-owner deferred Closed finalization;
and inheritance of existing non-interruptible Shell/normal-stop cancellation. The
proposal remains proposed, with no product source/build/test/CI claim. A re-review
will decide whether those mechanism clarifications are sufficient; native keyboard/
control ordering and visual acceptance remain separately unproved.

12:18 re-review: the clarified mechanism has no remaining mandatory design correction
and is technically accepted at that level. ADR proposed status is retained while the
bounded host prototype remains unproved: same-turn Click/Closed orders, submenu
Escape/repeat and root/popup input routing. If the queued finalizer runs before the
actual valid Invoke, redesign that boundary. UI presentation/accessibility, physical
input and implementation acceptance are not implied. No source change or product
build/test/CI has occurred. Full reviewed ADR hash and scope are in
independent-mechanism-review-02.json in the task evidence directory.

## Minimal native menu prototype — 12:26 to12:30 JST

A minimal standalone App copies only the pinned App SDK/project/build configuration
and desktop-isolation helper into outputs/context-menu-20261124/menu-prototype-01.
It does not reference product assemblies, use product settings, enumerate user paths,
or call Shell. Analyzers are disabled only for this diagnostic copy. The first
restore/build exits0 with0warnings/errors; exact commands are dotnet restore
D:/NeNeCommander/outputs/context-menu-20261124/menu-prototype-01/App/NeNeCommander.App.csproj -p:Configuration=Release
and dotnet build the same project -c Release --no-restore. Both logs and the receipt
are retained. This is bounded framework investigation, not a product build/test gate.

Attempt1 runs via pwsh -NoProfile -File
D:/NeNeCommander/evidence/context-menu-20261124/run-isolated-menu-01.ps1 -Locale en-US -Attempt 1.
It passes/exit0 on NeNeCommander531_20261124, with input desktop Default unchanged.
Both root and New-submenu UIA Invoke yield Click, Closing, later Closed, then queued
finalizer. Each submits once before cancellation; API Hide yields no submission and
does finalize cancellation. An explicit full automation name/help survives a long
target label; the root item reports MenuItem,398.4x32DIP and the submenu item96x32.
No screenshot/visual fit, actual product wording, physical input, or complete UIA
accessibility acceptance follows from those properties.

Attempt2 adds explicit native WM_KEYDOWN/UP messages to GetFocus's HWND only after
same-process and non-input-desktop guards. No SendInput or SwitchDesktop is used.
Diagnostic repeat handlers on BOTH window root and submenu presenter receive the
same initial Escape. Root sets held; the submenu mistakes that same routed event
for a repeat and suppresses it. Stage10 times out, exit1 from the launcher/child2.
This is a causal adapter-design failure, not an environment skip. All failed
events/source/build/launch receipts remain. The next attempt removes the second
handler: the observed root is already in the submenu's routed preview path.

Several read-only guessed-file/glob calls failed and were replaced by discovery;
the missing failure JSON for successful attempt1 is not a failed control run.
A release-ledger update script first failed JavaScript parsing before any shell
action, then the corrected invocation succeeded. No product source mutation or
public write occurred during any of these attempts.

Root-only attempt3 succeeds, child exit0/input desktop Default unchanged. Actual
Escape preview is observed once at window-root for each native key message.
First Escape collapses only the submenu; one held-repeat event is suppressed;
key-up clears the diagnostic held state; a fresh Escape closes/finalizes the root.
See menu-en-US-3-passed.json and isolated-launch-en-US-3.json. The ADR explicitly
replaces dual root/popup forwarding with the existing WindowFrame single route.

Build04 of the additional Enter/Space probe fails CS0649 because CRLF-based text
insertion did not match the LF diagnostic helper; XAML also emits WMC1509 after
the missing assembly. No App run follows that failed build. Source/log retained;
build05 corrects the insertion with an explicit match assertion. Product source
is still untouched; final product mapper/host tests remain required.

At12:34, attempt4 passes/exit0 after build05 succeeds with0warnings/errors.
The exact launcher command is pwsh -NoProfile -File
D:/NeNeCommander/evidence/context-menu-20261124/run-isolated-menu-01.ps1 -Locale en-US -Attempt 4.
The same root-only native Escape cases pass. Enter invokes once; a newly opened
menu receives and suppresses a repeated Enter without submitting/closing; Space
then invokes once. UIA root/submenu invocation and native Enter/Space all precede
Closed/finalization. Across six owners the submission counts are1,1,0,0,1,1.
Source hashes, sequence summary and hashed runtime/launch receipts are retained in
menu-prototype-04-summary.json. Normal input desktop remains Default. No product
mapper, physical modifier/IME, three-locale resources, visual fit or final host
acceptance is implied. Both same-turn callback orders and final state-qualified
cancellation/submission remain required deterministic implementation cases.

A necessary independent read-only review of the revised one-root-entry mechanism
and raw prototype evidence is requested from the existing reviewer. No new worker
implements or edits source. The accepted #544 core remains a required dependency;
its80-file change is being inspected before local integration, not sent publicly.

12:38 independent read-only review has no mandatory design correction and accepts
the single-root mechanism. The ADR becomes technically accepted before production
implementation. The reviewed ADR SHA-256 is
14BBD3900A8E2D8ADC1AE12B1703383598B2EA0748129E9EF0DBBD36652FA28A; subsequent differences
only record status/acceptance and this explicit proof limit. Attempt4's Enter KeyUp
precedes the repeat-marked event on the next owner. It does not prove uninterrupted
held-input transition. Final product mapper/repeat-owner integration, both callback
orders, stale/replaced/finalized/stopping owners, Shift+F10/Menu duplicates, IME,
modifier/focus, three-locale/long-target/disabled/accessibility/visible evidence remain.
No build/test/App run or edit was performed by the reviewer.

The read-only git merge-tree preview of accepted #544 b593ef4 from shared7ef9b96
finds four content conflicts: PROJECT_STATE, ADR index and en-US/ja-JP resources.
It changes no worktree/index. The new-file core has80changed files; integration must
preserve current pointer/language/theme/placement behavior and supply the third
shipped resource catalog. Product validation follows actual merged inputs; old
provider evidence is reused only if the relevant boundary inputs remain identical.

## #544 dependency integration — November24 13:05 JST

The exact accepted local coreb593ef498d299536c2091de0eec72cd8cdc78181 is merged into
the feature from61b27e4 without touching main or the release candidate. The four
expected conflicts are resolved: both additive PROJECT_STATE/ADR-index blocks stay,
current en-US/ja-JP catalogs retain every existing552key/value and import only the9
new-file keys. The incoming obsolete AddressEditorInput attached-key spelling is
not reintroduced. Simplified Chinese receives the same9keys, including editable
default 新建文本文档.txt. All3catalogs have561keys and unchanged prior values.

Selected integration01 restores/builds all3test projects with0warnings/errors.
Application164/165 and Presentation73/75 expose3integration fixture assumptions:
two24-command counts and a fixed23-step last-row navigation. Infrastructure53/53
passes, including owned Windows exclusive-create boundaries and WSL fake contracts.
The fixtures now expect25commands and advance through the actual row count to
prove clamping. Only those3changed cases rerun in integration02, allpass. The
imported CreateTextFile window-mode-freeze case separately passes1/1. Thus distinct
successful selected cases are Application166, Infrastructure53, Presentation75.
The two known old window-planner fixture failures from#530 are not claimed repaired.

Exact scripts/filters/commands/returns are run-integration-selected-01.ps1,
run-integration-selected-02.ps1, integration-selected-01/02.json and
integration-window-freeze-01.json/log in the task evidence directory. Default native
filesystem test temporary roots are under the dedicated D output; no live WSL tier
or shared desktop input is invoked. App locked restore and normal Release build
both exit0 with0warnings/errors. No full gate, mutation or CI is repeated here.

All FileOperations source in Application/Infrastructure, Domain Paths, the7existing
live-WSL source/ownership fixture files, live-WSL launcher and pinned build/package/
SDK declarations have an empty git diff againstb593ef4. Existing provider/live
evidence retains its original scope; this source comparison does not claim new
native UI acceptance. Changed session/catalog/keyboard/presentation consumers use
the selected combined-source tests above. Full shared-contract deep review remains
required after the actual context menu implementation is complete.

CS-013 Inspect exits1 with exactly the same4pointer-baseline findings (all location,
metric,value,limit fields equal),1599types/10087members. There is no new finding and
no clean-complexity claim. Several read-only guessed path/glob probes fail without
changing source; subsequent discovery uses actual paths. All failed build/test
attempts are retained. Context owner/menu/reveal product implementation is next;
the imported keyboard/palette new-file core alone does not complete either issue.
