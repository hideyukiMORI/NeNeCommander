# Bookmark heading accent observation — Issue #581

Status: selected technical verification complete; local commit and beta.5 integration follow

## Scope and invariant

Keep the accepted B heading design and the existing BookmarkBrowseView /
BookmarkKeyboardView / BookmarkFocusRestoration routes. A region switch must
paint its final accent once, without motion, while retained row selection and
search focus remain distinct. No provider, settings or new input route is added.

The previous 01:15 stop is superseded by today’s explicit continuation request.
The current clean baseline is307eb83; beta.5 worktree remains8d1c247 and main
ccb303b. Beta.4 remains unchanged. Independent read-only reconciliation finds
all182 App inputs equal to the last polish proof, so unchanged padding, search,
About and unrelated behavior evidence is reused. No product fix is claimed yet.

## Plan and initial observations

ADR-0061 selects only the affected App and native painting/focus boundary.
The previous168 geometry samples do not establish compositor or paint stability.
Inspect existing brush writes and native focus events, then collect a bounded
render sequence on the named non-input desktop. Preserve failed observations.
Do not inject shared-desktop input, change ordinary settings, or stop other Apps.

The heading adapter currently assigns only BorderBrush and Background. No
heading transition is declared in XAML. Microsoft’s Border.BackgroundTransition
contract says its default is null and property readback cannot establish the
intermediate animated color when a transition is present. Runtime values and
rendered samples, rather than assumed default behavior, will distinguish this.
Source: https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.border.backgroundtransition

Evidence: D:/NeNeCommander/evidence/bookmark-accent-20261125. The new isolated
source copy is D:/NeNeCommander/outputs/bookmark-accent-20261125/baseline-controls-01;
it derives from accepted-controls-02 without bin/obj and retains private fixture
settings/paths. Existing snapshots and trial01–03 remain untouched.

Initial reads include all mandatory normative documents, current581 design,
quality/handoff, beta.5/trial scope and current GitHub state. Sandbox Git ownership
refused ordinary status; exact-path per-command safe.directory permitted the
read without a global configuration change. Two misspelled file lookups were
corrected; neither represented a product failure. The session-start receipt
records input hashes, setup and the independent review findings.

## Diagnostic preparation attempts

Locked Release restore01 passes. Diagnostic build01 fails on two observer-only
CS0117 references to UIElement.GotFocusEvent, followed by cascading XAML errors
because the local assembly was not produced. The observer now subscribes to
the actual WindowFrame.GotFocus event and retains its original source/log.
No product source changes or suppression are used. Build02 reuses the unchanged
locked restore and existing verified dependency outputs.

## Reproduction and bounded correction

Baseline native ja-JP attempt1 succeeds on the dedicated non-input desktop and
preserves ordinary settings. It collects168 BGRA heading strips and762 Rendering
observations over14 phases at820/580 DIP. On every category-to-bookmark switch,
the requested bookmark mark is followed by a delayed old-category GotFocus, which
repaints the category before the queued request finally focuses the bookmark list.
Both rendering snapshots and the first RTB images show this old-region return.
The property callback's intermediate four-write combinations alone are not proof.
Runtime heading BackgroundTransition is null; translation/scale/opacity and fixed
heading geometry remain unchanged. The evidence isolates repaint reversal, not a
physical screen trajectory or all possible compositor effects.

The candidate queues the existing explicit BookmarkFocusRestoration request before
ShowFocus replaces marked rows. Native focus observers still cancel passive
restoration, but do not repaint while that explicit request remains pending.
Search, Tab, pointer and later native text input retain their existing cancellation
owners. Only BookmarkBrowseView changes; there is no timer, animation, new input
route or Presentation coordinator change.

Diagnostic corrected-controls-02 builds with0warnings/errors. Native ja-JP
attempt2 passes: all14 phases retain one correct Rendering color pair and one
exact heading-pixel hash across12 captures each. All168 captures and the raw event
sequence are preserved. Both launch receipts show exit0 and unchanged input
desktop/ordinary settings. Capture scheduling perturbs timing; this is bounded
native-control/render evidence, not physical keyboard or hide's screen acceptance.

The same candidate is now applied to the issue581 product worktree. Normal App
build, affected cancellation-boundary proof, format/policy and final review remain
pending. No source publication, beta.5 integration or release is claimed.

Additional observation limits: python is unavailable, so pixel analysis uses the
owned PowerShell/.NET script; the original native/build receipts are untouched.
Sandbox process inventory was denied; a read-only exact command inventory verified
the2-minute diagnostic build was running. It completed normally and no process
was terminated. Failed path/glob lookups are operator errors, not product failures.

## Independent review and cancellation boundary

Normal product build01 passes0warnings/errors. Changed-file whitespace passes;
Commit check passes112normative rules and24adversarial registrations. Complexity
has the same3findings (two parameter counts and one nesting finding); final field
comparison follows. Native3 passes the added rapid-switch, explicit search, native
Tab/text ownership, pointer-owner-to-search, close and reopen controls. Native4
passes the populated-list painting run:14phases/168images, one correct color pair
and one heading-pixel hash in every phase. These unchanged preliminary successes
are retained, not mistaken for proof of the next correction.

Independent read-only review identifies a missing cancellation boundary: when
Categories still holds actual focus, its late GotFocus is ignored during the
requested Bookmarks transition; pointer ownership can then cancel that request
without a new GotFocus. The requested mark and keyboard region can remain ahead
of actual focus. Native5 reproduces exactly this gap after an observed old-region
native notification: actual Categories focus is retained but its heading is wrong.
The observer invokes the existing pointer cancellation handler in that notification;
it sends no physical input and does not synthesize a new GotFocus. Its failed
receipt and pre-correction source remain in evidence.

CancelPendingFocus now reconciles actual category/list focus through the existing
OnRegionFocused route, in addition to its existing Search reconciliation. This
updates both marks and BookmarkKeyboardView's RegionFocused context, including
same-control cancellation with no later focus notification. The failed condition
will be rerun alongside only the affected cancellation and paint boundaries.

## Final selected evidence

Product build02 and diagnostic build05 succeed with0warnings/errors. Native6 now
passes50checks for the reproduced same-current-region cancellation and the next
keyboard switch; native7 passes156rapid/search/Tab/text/pointer-owner/close-reopen
checks; native8 passes64draft-return/live-language checks; native9 passes34control
checks and captures168BGRA strips across14phases/two widths. Native6–9 all exit0,
with normal settings unchanged and input desktop Default before/after. The earlier
native5 failure wrote its causal assertion, then failed to exit during the bounded
90s launcher interval; that exact owned child was terminated(exit3). Its teardown
limit remains recorded and is not labeled a normal shutdown or fixed product case.

One verifier is applied to both recorded before/after sequences. Baseline1 exits1
on the four category-to-list render reversals and three changing heading-pixel
phases(including search); initial correction2, populated4 and final9 all exit0.
Final9 has exactly one correct rendering color pair and one heading-pixel hash in
each phase. Geometry is separately checked only between each phase's first and
last capture timestamps:14fixed category/row rectangles. Rendering samples during
the explicit width change retain the previous phase label, so those resize samples
are excluded from geometry comparison, not from the raw evidence. There is no
claim of screen refresh cadence or a physical input/visible acceptance test.

final-source-parity-02.json freezes182App inputs(180exact diagnostic copies and
the two declared project/composition substitutions),4matching product dependency
DLLs and5fixture hashes. Only BookmarkBrowseView differs from the initial182product
inputs. Prior About, padding, localization and unaffected template proofs remain
applicable. Populated4's no-cancellation paint sequence is reused because the final
change only adds actual-region reconciliation at CancelPendingFocus; final6–9
exercise that changed boundary and the complete empty-list paint sequence.

Changed-file whitespace02 exits0. Complexity02 exits1 with exactly the same three
complete finding objects as the accepted bookmark-focus baseline; no new finding,
threshold change or clean-complexity claim. Earlier Commit policy passed, and the
normal repository-owned hook will run for the final source/document commit.

Exact commands (product commands run in the issue581 worktree):

- `dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore`
- `dotnet format whitespace src/NeNeCommander.App/NeNeCommander.App.csproj --no-restore --verify-no-changes --include src/NeNeCommander.App/Views/BookmarkBrowseView.cs`
- `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect`
- `pwsh -NoProfile -File eng/check.ps1 -Mode Commit` (preliminary policy01)
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-accent-20261125/run-corrected-controls-02.ps1 -Locale ja-JP -Attempt 6 -Scenario focus-same-region`
- The same runner with `-Attempt 7 -Scenario focus-ownership`, `-Attempt 8 -Scenario focus-drafts`, and `-Attempt 9 -Scenario focus-paint`.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-accent-20261125/analyze-heading-pixels.ps1 -Identifier ja-JP-9`
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-accent-20261125/verify-heading-paint.ps1 -Identifier ja-JP-9` (the identical verifier also runs on1/2/4).

The production diff remains one App adapter. No dependency, Application,
Presentation, Domain or infrastructure source changes, full gate, mutation or CI
are needed for this bounded correction. Release580 retains its separate final
candidate/freshness requirements. beta.4 remains unchanged. No new interactive
trial is created or launched by this observation task.

## Host-order rapid Tab correction

A second independent review checks the actual CommanderWindow order, not only the
mapped-action adapter. ObserveInput ran before Map and cancelled every initial Tab.
After actual-region reconciliation was added, this made a second mapped Tab forget
the first pending transition. The earlier direct Forward/Forward probe could not
observe this pre-mapping step. Native10 uses ObserveInput/Map/ForwardOutcome in the
same order as the host and reproduces the regression: two accepted mapped Tabs
settle on Categories rather than returning to Bookmarks. The failed receipt and
its exact two MappedBookmarkManagerAction results are retained.

The existing native-input notification now receives the same KeyboardInput already
owned by the host. Initial Tab cancellation moves there, after the canonical mapper
has yielded the key to native controls (or native composition/selector ownership).
Mapped Tabs supersede the queued request normally; native Tab still cancels it.
ObserveInput retains the pending confirmation Enter guard. All three existing host
notification sites pass the same input. No additional key mapping, context/modifier
table, forwarding route or event registration is added. The production scope is now
three App adapters, with the existing command map unchanged. Preliminary final
proof above remains historical; host-order verification and final parity follow.

## Accepted host-order evidence and search observer correction

Product build03 succeeds with0warnings/errors. Host-order native11 passes116checks:
two initial Tabs return to the original list; repeated Tab does not switch twice;
Control+Tab passes through and cancels the queued request; native Search Tab keeps
its owner while a list return is queued; pending Ctrl+Enter does not submit a draft.
Native12same-region passes50checks; native13other ownership passes156. Native14
(empty) and15(populated) pass22control assertions each and collect168images each.
Every final launch exits0 with ordinary settings and input desktop unchanged.
Native10's causal failure is followed by the same90s failed-observer teardown limit
as5; only its exact owned child is terminated, and neither is relabeled successful.

Strict verifier01 passes14 but rejects15's narrow/Search first frame: it still
contains the previous active-row heading. Raw evidence shows exactly one
old-to-inactive transition. The old Rendering sample is12626.0626ms; Search
GotFocus arrives12634.521ms, after which every Rendering sample is inactive.
Frame0capture starts12624.8652ms and ends12662.7662ms, crossing that notification;
frames1–11 are the same inactive pixels. Header positions/translation are fixed and
transition/implicit-animation fields are null. Independent read-only review confirms
this is no reversal or movement evidence. The observer's synchronous-first-frame
assumption was wrong; product code is unchanged by this correction.

Verifier02 permits the exact prior row-active state only before the first actual
Search GotFocus. All Rendering samples after that time and captures starting after
it must be inactive. Raw chronological captures may never return from inactive to
old; final inactive is mandatory. Other region switches retain the original strict
conditions. The original failed verifier15 and failed first freeze remain intact.
The same corrected verifier keeps baseline1red on the four region reversals and two
changing heading sequences, and accepts14/15. Synthetic negative inputs separately
check that search reversal and an old capture after notification are rejected; these
are explicitly synthetic observer proofs, not native runs.

`final-source-parity-03.json` freezes182App inputs/180exact diagnostic copies, the
two declared project/composition differences,4matching dependency DLLs and5fixture
hashes. Exactly the reviewed3App files differ from the initial product inputs.
All28measured category/row rectangle pairs remain fixed within their capture
intervals. Final11–15 provide366control assertions and336raw heading images.
No post-commit rebuild is required solely for source metadata. Unchanged prior
About/localization/padding/template and native8draft/live-language evidence is reused.

Changed Manager/host whitespace03 passes; Browse's unchanged whitespace02 is reused.
Complexity03 retains the exact same3finding objects. Source review finds no further
mandatory change. No dependency, other-layer source, public source update, CI,
main integration, visible trial or release accompanies this final proof.

Final observer commands beyond those already listed:

- The same `run-corrected-controls-02.ps1` runner with `-Attempt 11 -Scenario focus-rapid-host`, `-Attempt 12 -Scenario focus-same-region`, `-Attempt 13 -Scenario focus-ownership`, `-Attempt 14 -Scenario focus-paint`, and `-Attempt 15 -Scenario focus-paint-populated` (all `-Locale ja-JP`).
- The same pixel analyzer with identifiers `ja-JP-14` and `ja-JP-15`.
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-accent-20261125/verify-heading-paint-02.ps1 -Identifier ja-JP-1` (expected exit1); the same command for `ja-JP-14` and `ja-JP-15` (exit0).
- `pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-accent-20261125/freeze-final-proof-03.ps1` (exit0).

The reviewed correction is ready for its normal-hook local commit and integration
into the approved beta.5 candidate. Physical input, other scale/contrast conditions
and hide's visible acceptance remain distinct; this task does not claim them.

At08:05JST the final independent read-only review confirms verifier02/hash correspondence, both negative proofs, all182/180source facts,4DLLs, native11–15 and28geometry intervals. It requires no mandatory correction; physical-input/human-acceptance limits remain.
