# ADR-0084: Open the active location in the opposite pane through existing navigation

Status: accepted

Date: 2026-11-22

Issue #547. hide requested this operation and continued milestone work on November22.
This proposal refines ADR-0013's active-only intent routing for one explicitly named
cross-pane navigation intent. No reader, provider, state owner or execution path is added.
Accepted for the technical routing/cancellation contract by the design owner under
hide's current continuation instruction, after an independent read-only review
and correction of host cancellation reachability and provider-aware path identity.
The November22 22:07 header implementation review accepts the engineering integration
in [the design handoff](../design/2026-11-22-other-pane-header-handoff.md). Native screen
acceptance and source publication remain separate.

## Context

DualPaneSession already owns both PaneSessions, active-side routing and
NavigateAsync(side, path). The old default forwards pane intents only to the active
session. The request is to open its CURRENT LISTED DIRECTORY in the other pane,
with a shortcut and a direction-clear icon, preserving source focus/selection and
activation. It must never be interpreted as copying or moving selected files.

## Decision

- Add one closed UserIntent.OpenCurrentLocationInOtherPane. DualPaneSession captures
  the source's currently listed canonical location and its other side synchronously,
  then calls its existing NavigateAsync(side, path). PaneSession and PaneReducer keep
  sole ownership of read, navigation, history, sort, visibility, selection and focus.
  This action never changes the active side; the existing explicit activation and
  ADR0090 qualified pointer routes retain their own ownership. No temporary activation is used.
- Expose one derived readiness predicate on DualPaneSnapshot for this action and use
  it in session dispatch and Presentation. The source must have PaneContentListed;
  neither pane may be PaneLoading or PaneLaunching; no file operation may be running
  or awaiting name, confirmation or conflict. Completed/rejected operations do not
  freeze it. Retained listed content after read failure/cancellation/abandonment is
  the visible source location, not the failed/pending target. An absent or failed
  opposite pane is allowed. The predicate owns no state and performs no effects.
- If the opposite pane already lists an equal canonical path, do nothing: no read,
  snapshot replacement, focus/selection clearing, history entry or status change.
  This case remains an available idempotent command; no separate retry semantics
  or refresh is inferred. Comparison uses the sole FileSystemPathIdentityComparer.Instance:
  Windows/UNC case differences compare equal, WSL distribution-name case is ignored,
  and WSL path-component case is preserved. Record equality/text comparison is not used.
- When navigation succeeds, only the destination receives the normal first-visible
  focus and selection clearing; its own sort/hidden settings and successful-location
  history follow existing rules. The source snapshot and active side remain intact.
  Failure/cancellation retain destination content/focus/selection/history and show
  the existing typed activity. Provider refusal is the existing read outcome.
- The host keeps its existing AsyncWorkOwner admission: while this navigation task
  runs, only Escape may interrupt; ordinary commands and Tab are refused. This is
  not a claim that source-side UI navigation remains available while waiting. Direct
  Application API activation/navigation still cannot retarget the captured read,
  and existing latest-read identity discards stale completions.
- Escape must reach the admitted read although its destination remains passive.
  DualPaneSession records one closed Idle/Pending cancellation-route state for this
  intent; Pending captures destination side and the exact PaneLoading instance
  created by the existing NavigateAsync. It owns no read, token, provider task or
  cancellation implementation. An active-pane read retains first priority. If the
  active pane is not reading and the captured destination still has the identical
  PaneLoading instance, Escape routes to that PaneSession.HandleAsync(Escape).
  Its existing PaneRead.Abandon releases the waiter immediately, cancels the read's
  token and observes/discards late provider completion. Otherwise Escape keeps its
  usual active-pane route; a stale capture must not swallow Escape or cancel a
  newer unrelated read. File-operation/modal precedence remains higher.
- Completion, abandonment and fault clear this captured route in finally only if
  the same Pending state is still owned. A newer capture cannot be cleared by an
  old completion. Synchronously completed/no-op reads create no pending route.
  This explicitly refines ADR-0058's active-only Escape for this one owned passive
  read; it neither cancels arbitrary passive reads nor expands AsyncWorkOwner.
- CommanderSession retains existing modal/scope precedence. The new intent never
  leaks through settings, bookmarks, palette, address, Locations, help or window
  adjustment ownership. A validated palette submission closes before normal dispatch.
  The source/destination readiness is re-evaluated at execution, not trusted from UI.
- Ctrl+E is the proposed normal FileList/NavigationSurface shortcut. E already exists
  in the canonical translated key model. Add its control character U+0005 handling
  and canonical label; the raw virtual E remains a pass-through precursor. Plain e,
  text editing and IME remain native. Repeats use the existing KeyboardRepeatGuard,
  retained across context changes and released by a fresh initial E, without another
  guard. No new modifier class or key translation owner is needed.
- F1/help, footer and palette use the existing canonical binding/label data. The
  palette must describe this command's destination unambiguously; do not label the
  passive navigation as an active-pane effect. As reconciled on November24 with
  hide's accepted November23 compact-palette handoff, the common strip names the
  captured active/opposite context once. This command's selected detail and UIA
  name explicitly name the destination through CommandPaletteContext/Feedback;
  selection, filtering and zero results preserve the common captured heading.
  This supersedes this ADR's earlier per-row target/opposite projection without
  changing command ownership or dispatch. The command remains an idempotent
  available entry when both locations already match.
- One active-header action points toward the opposite pane. Pointer activation emits
  the same intent through the normal forwarder and keeps keyboard focus. The
  existing modal overlay owns blocking; readiness supplies enabled state. It must
  say that it opens the current folder, not files or selected items, in tooltip/UIA
  wording with direction and shortcut. The approved tool-free consultation informs
  the 24-DIP reserved header target and 16-DIP folder/arrow geometry in the design
  handoff. The passive slot remains; the active button projects the sole readiness
  predicate. Same-location stays enabled under the contract above. Existing semantic
  styles/tokens are mandatory; native screen acceptance remains pending.

## Rejected alternatives

- Activate the other pane, navigate and activate back: changes observable active
  state and lets reentrancy, failure or a late read misroute later input.
- Read through a new port/service, copy PaneState, or assign the other listing:
  duplicates existing navigation or shares source-owned focus/selection/history.
- Open the focused child instead of the current directory: changes the requested
  operation and can accidentally launch files.
- Supersede either pending read or infer failed-target retry: obscures visible
  location and cancellation ownership. An explicit idle admission is predictable.
- Use a private key/click handler to decide source/destination: violates CMD-003/009.
- Hide native proof limits or start shared-desktop input: TST-003 still applies.

## Consequences and migration

Refine CMD-002/006, Keyboard Model, ADR-0013 routing and ADR-0058 Escape with this one
exception. Add the intent, derived readiness, existing-route dispatch and canonical
presentation/keyboard projections. No old overload, compatibility path, provider
policy, dependency, saved schema, new mechanism-registry row or new I/O boundary is
needed. The existing rows remain the single owners.

The earlier transmission holds are historical: hide approved #542 publication and
the exact Claude brief at21:34 JST. #542 is integrated as PR557/main b82aa1e; the
2,593-byte brief received one tool-free consultation. This feature remains a local
candidate; its source publication, native acceptance and integration are separate.

## Executable proof

Focused Application cases: both directions; active snapshot/selection/focus intact;
passive own sort/hidden/history/focus; no file mutation or launch; canonical same-
location no-op; source absence; both sides' loading/launching refusal; retained source
failure/cancel/abandon content; absent destination; provider failure/cancellation;
Windows/UNC case-equivalent same-location no-op and distinct WSL path casing;
UNC/WSL canonical paths through the same fake read port; active change during read;
stale later navigation; running/name/confirmation/conflict and each transient scope.
Palette execution must revalidate the captured snapshots and route the same intent.

A bounded Presentation test must compose the actual AsyncWorkOwner with the actual
CommanderSession and pending directory fake: accepted command, refused Tab/second
command, accepted Escape, immediate waiter release with source selection retained,
target token cancellation and late outcome discard. It must also prove unrelated
passive reads, a newer target read and an active read retain their declared Escape
priority, and completion/fault cannot leave a stale cancellation route. This is
direct consumer evidence for the newly refined cancellation contract. The owner
implementation itself remains unchanged; its existing admission/lifetime proof is reused.
A controlled continuation test must specifically order A abandonment, a new B capture,
A finally, then Escape, and prove B is still abandonable. This detects old-finally
clearing of a new capture that ordinary stale-provider tests do not cover.

Focused Presentation cases: Ctrl+E from both normal contexts; no unrelated-context
mapping; no raw/character double effect; held-key suppression across context changes;
fresh key accepted; existing bookmark e/IME/text routes; canonical F1/hints/labels;
correct active/destination wording and active-only icon projection. Test baseline
failures first where possible, then affected App/test builds, changed formatting and
CS-013 comparison. No unrelated full suite or mutation is justified by this routing
addition alone; ADR-0061 scope is reviewed against the final implementation.

An isolated native session must still verify icon click/focus, keyboard dispatch,
direction, no file effects and narrow/high-contrast layout. No shared-desktop input
is permitted and no such session is presently available. Record that proof as
pending rather than claiming the helper or unit tests establish it.
