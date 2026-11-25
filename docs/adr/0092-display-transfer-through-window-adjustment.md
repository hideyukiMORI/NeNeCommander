# ADR-0092: Transfer the normal window through the existing adjustment route

Status: accepted

Date: 2026-11-24

Issue #551. The delegated engineering owner accepts this technical mechanism on
November24 at17:35JST after independent review and correction of the three required
qualification/minimum details. It authorizes scoped implementation, not product or
screen acceptance, publication or inclusion in beta.4.
The current baseline is accepted release inputf6c722c, connected by8f34e7c.

## Invariant and sole mechanism

Extend WindowAdjustmentSession/WindowAdjustmentPlanner and the existing synchronous
WindowAdjustmentRoute/AppWindowPlacementAdapter only. No second dispatcher, window
port, timer, polling loop, native import, global shortcut, display reconfiguration
or settings writer is introduced. Application chooses every destination and both
rectangles from immutable facts; App translates SDK reads, checks the exact apply
qualification and executes only the chosen calls. Domain has no display concepts.
ADR0050 scope admission and pane freeze remain. Editors, modals, operations, reads
and launches prevent entry; no draft is cancelled to make transfer possible.
ADR0089 tracking, startup correction, close capture and complete settings queue
remain the only persistence path. A transfer writes no settings by itself.

## Interaction

Inside Ctrl+W, n requests the next display and p the previous display. Ctrl+Right
and Ctrl+Left are standard-key aliases. Only the canonical mapper declares them;
letters use produced characters, no Control/Alt, and IME/dead-key input passes
through unchanged. Held keys are consumed after the initial transfer, including
owner replacement. Ordinary arrows/h/j/k/l and +/- retain their existing repeats.
Bindings generate the two helper hints and keyboard-help entries in all3locales.

Sort distinct work-area rectangles by signed left, top, width and height. Cycle
and wrap through that order, independent of OS enumeration and display numbers.
Co-located duplicate rectangles are one geometric destination, represented by the
lowest current opaque display identity. The current display is the SDK's nearest
DisplayArea for this window, matched by identity within the same fresh snapshot;
its rectangle determines the current geometric slot. A single distinct rectangle
is a typed NoOtherDisplay refusal. Negative coordinates and staggered layouts use
the same order. The UI says next/previous, never claims purely right/left movement.

Transfer admits only a visible Restored own window. Maximized/minimized/non-
overlapped/unavailable keep explicit refusals; r, transfer, m is the deliberate
sequence for a maximized window. Hidden does not show or activate. Escape always
leaves the mode and returns focus to the captured pane, regardless of read/apply
failure. This is the engineering choice from the consultation's allowed normal-
only alternative, not a claim that maximized transfer was proved.

## Fresh facts and platform limits

The adapter reads display identities, work areas and reported scales for transfer
requests only. Existing movement/startup remains independent of the new scale API.
Use the pinned Windows SDK projection of DisplayInformationInterop.GetForMonitor
with Win32Interop.GetMonitorFromDisplayId in the already-declared App window
boundary. No handwritten native import or Infrastructure dependency is added.
The documented minimum is Windows11 build22621; the existing App boundary performs
an explicit platform-version guard before this API. Earlier supported product OS
versions receive typed DisplayInformationUnavailable for transfer only. This ADR
explicitly declares that bounded capability read in App, not arbitrary OS probing.
COM failure, missing identity, empty work area, inconsistent snapshot or non-finite/
nonpositive scale is closed unavailability, never a guessed scale or fallback API.

An immutable owned display snapshot holds opaque identities and validated area/
scale facts. Platform handles never leave App. Source and destination scales come
from this same snapshot, never the immediately stale XamlRoot scale. The API
reports whole-monitor scale through GetScaleFactorForMonitor; exact custom-scaling
precision is not established. Logical-size preservation means conversion by those
reported scales, with final native observations recorded separately. The snapshot
is read per request and is neither saved nor cached across key presses. No display
event subscription or automatic transfer on reconnect is introduced.

Extend the existing qualified WindowAdjustmentRequest with this closed transfer
fact input (NotRequested, Unavailable, or Available including visibility); ordinary
actions supply NotRequested. There is one canonical planner entry. Closed snapshot
and factory guards are migrated atomically with callers; there is no compatibility
overload, nullable failure marker or second input path.

## Geometry chosen entirely in Application

For source physical dimension d and reported source/target scale s/t, choose final
size min(target work-area dimension, round(d / s * t, AwayFromZero)). Each dimension
must be positive. Center that final rectangle with floor of the remaining whole-
pixel space; wide intermediates must keep signed coordinates and endpoints exact.
Reject non-finite/out-of-range arithmetic before narrowing. Do not retain relative
position: centering gives a predictable reachable landing in gaps/staggered layouts.

For each final dimension f, the first request dimension is round(f * s / t,
AwayFromZero), at least1. Its origin is the final centered origin. The plan carries
both exact rectangles, the expected source placement and the exact display facts;
the adapter calculates none of them. This compensates the observed OS scale resize
without assuming it always occurs. Apply first MoveAndResize(initial), then one
MoveAndResize(final) after qualification. There is no repeat-until-fit or delay.

For each dimension, a nonzero declared preferred minimum remains that exact physical
pixel value in both requests. With no declared minimum, the initial floor is
round(32*s, AwayFromZero) and the final floor is round(32*t, AwayFromZero), each at
least1. Both requested rectangles must respect their respective effective minimum;
the final must fit the target work area. If those facts
cannot hold together, refuse TargetCannotFit before any move. Do not silently exceed
the work area, lower the minimum or enlarge a too-small source as a side effect.
Rounded dimensions cannot become zero. Clamp only the planned final fit, not invalid
scale, identity, arithmetic or minimum inputs.

## Qualified application and partial effects

A transfer plan is a returned value, never a replayable snapshot command. The
existing route owns one synchronous in-flight apply receipt. Reentrant geometry
commands cannot replace it or start another apply. A mode leave/host stop invalidates
further effects; it must still be observable during SDK callbacks. No wait or
background continuation is added. The planned decision carries the exact new open
state so a later apply result can update only that state, never a replacement.

The route receipt binds the incoming rendered open state, the newly decided open
state and this exact plan. During that receipt only, Leave may translate that exact
incoming owner to the decided owner, provided Current is still the decided owner.
Thus a reentrant Escape from the view, which has not rendered the new outcome yet,
can leave before the next SDK effect. Any other stale owner remains a no-op. A
reentrant render may already carry the decided owner, which uses ordinary qualified
Leave. No matching-by-value or general stale-owner fallback is permitted.

CommanderWindow stops this route through WindowAdjustmentView.Stop before calling
FinishTracking. Stop invalidates the current receipt and blocks further reads,
applies, Forward, Render and focus return. In particular, an outer Forward that
resumes after a reentrant close must not repaint or refocus the closing window.
Expected-owner qualification is also checked before any placement/API read.

Immediately before the first call, compare the own window's visibility, Restored
state and source bounds plus current/source/target display identities, work areas
and reported scales with the plan. Include both declared preferred minimum values
and the complete initially observed display membership/identity facts. Do not
compare the stale XAML scale as if it were native destination scale. A mismatch
yields a typed not-applied refusal. Immediately after the first call, require the
same owner, visibility and presenter, the intended target identity, unchanged
target work area/scale and unchanged declared preferred minimum values before the
final call. Re-enumeration compares observed identities, not an index that could
name a replacement. An observed change stops; no alternate display or adjusted
geometry is chosen in App. A changed minimum stops without recalculation.

Once execution enters the first changing SDK call, no subsequent exception or
failed read may promise no effect: even that first call can throw after changing
the window. Every such failure returns a partial/uncertain apply receipt, not a
no-effect promise, rollback or successful transfer. Only a refusal before entering
the first changing call may be reported as not applied.

After the final call, read the resulting physical rectangle and target identity.
A typed applied receipt requires exact planned bounds, matching current target
identity/work area/reported scale, unchanged declared minima, the same current
decided owner, non-stopping lifetime, visible and Restored state. Otherwise report
an unconfirmed effect; if its owner has gone stale, the session ignores that receipt.
Application's existing window-adjustment owner records the qualified apply receipt
in its outcome. Planned wording never says moved; confirmed wording describes only
this immediate observation and does not promise future OS stability. A stale receipt
changes no owner or focus. The existing defect observer retains unexpected SDK
defects; expected topology/state changes use typed refusal/partial outcomes. No
hidden retry, rollback, forced focus, Show/Activate, implicit maximize/restore or
normal-settings write is allowed.

The read-to-SDK-call race remains: display topology is not locked. Reentrant host
closing or a changed topology can leave the first rectangle applied. This is an
explicit bounded effect, not transactional multi-monitor movement.

## Evidence, visual handoff and verification

Background advice proposes n/p, geometric cycling and logical-size preservation.
Engineering selects centered landing, normal-only admission, refusal rather than
minimum-overflow, standard-key aliases and qualified apply receipts. Preserve the
existing semantic helper, full localized status/automation text and
focus sink. No new color, spacing constant, overlay or visual family is introduced.
The [engineering handoff](../design/2026-11-24-monitor-transfer-handoff.md) enumerates
the hint/status states; final screen acceptance remains hide's separate observation.

The unshown prototype's24ordered-pair/size cases all reached exact final bounds
inside the target area at its native DPI, with no activation/visibility/foreground
or normal-settings change. One first request (175% source to150% upper destination,
oversized window) remained2987x1680 instead of2560x1440 before the final call; target
identity was already correct. This supports two bounded calls, not a timing theorem.
Earlier36/24observations and all counterexamples remain in the quality record.

Select focused Application model/planner/session tests, Presentation translator/
mapper/repeat/hint/outcome tests, normal App build, conformance/record closure,
changed formatting and complexity. Prove order permutations/duplicates/wrap,
negative/extreme coordinates, finite/overflow/rounding/minimum rules, every refusal,
ordinary-command compatibility, stale/replaced owner and partial/late receipt.
The actual route needs isolated owned-window tests for the two SDK calls, read
failures, destination loss/state change between calls, callback close/reentrancy,
tracking and no automatic settings write. Tests must distinguish API-driven proof
from physical keys and visible repaint. New sources stay local until reviewed.

No provider or filesystem trust boundary changes. Reuse unrelated Domain/provider/
dependency/mutation proof; do not start a full gate solely for this bounded extension.
If implementation widens a shared contract beyond focused proof, document that
specific impact before expanding verification. No thresholds or exclusions change.
Actual display unplug/reconnect, 100/200% and custom scale, visible maximize/restore,
Narrator, physical hold/focus and screen acceptance remain environmental limits.

## Sources

- [ADR0050](0050-window-adjustment-mode.md) and [ADR0089](0089-persist-window-placement.md).
- [Investigation and raw proof](../quality/2026-11-22-monitor-transfer-investigation.md).
- [GetForMonitor contract](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.display.interop/nf-windows-graphics-display-interop-idisplayinformationstaticsinterop-getformonitor).
- [RawPixelsPerViewPixel](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawpixelsperviewpixel).

## Responsive helper contract — accepted November24

The helper has one bounded vertical ScrollViewer body containing icon/title, the
complete wrapped outcome sentence, then the seven geometry hint groups in their
canonical order. Only this helper uses a standard vertical StackPanel for those
groups, with existing SpacingKeyHintGap and unchanged key/description templates.
The SDK ScrollBarSize and ScrollViewerScrollBarMargin reserve a permanent gutter.
Horizontal scrolling and text trimming are disabled; no font reduction or new
visual token is introduced. The original shared KeyHintWrapPanel is unchanged.

A fixed footer always shows the generated Esc/Done and PgUp/PgDn/Read help groups
once each. Its height is independent of overflow. The existing nonvisual focus
sink is in this fixed area; the body has no tab stop, interaction focus or automatic
focus-follow scrolling. PageUp/PageDown are repeatable, unmodified Presentation
view actions from the canonical binding table, never Application geometry actions.
They preserve session outcome/owner, pane rows/selection/focus and settings.

WindowAdjustmentView uses one TryResetScroll path after complete open rendering,
on LayoutUpdated and after a page action updates layout. A new rendered owner
clears any old pending/issued reset receipt. Once its exact current/rendered owner
survives, the scroll is loaded and its viewport is positive, pending is cleared
before one nonanimated ChangeView to0. Only a surviving owner receives the issued
receipt; callers recheck the lifetime before further work. Closed/Stop clear both,
and Stop unsubscribes. Unchanged result text does not require a later layout event.
An issued receipt does not itself prove asynchronous completion. No product timer,
retry loop, ViewChanged retry or overflow mutation is introduced.

Page actions qualify the owner before/after UpdateLayout and require that owner's
issued receipt. They advance half the current viewport to leave readable overlap,
and clamp the destination to0..ScrollableHeight before the same ChangeView call.
This conforms to the [SDK offset contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.scrollviewer.changeview).
The scrollbar/wheel operate that same body. Physical delivery/hold and speech remain
separate environmental proof.

The baseline at500x400physical pixels/current system text scale on attached125%,
150% and175% displays found19English/1Japanese clipped layouts among52per locale.
Later concrete counterexamples include a full-viewport page gap and a wrapping
panel measured at232DIP then arranged at its smaller natural width, adding a row
outside its reported extent. The [quality record](../quality/2026-11-22-monitor-transfer-investigation.md)
retains all failures, diagnostic calibration and chosen corrections. Standard
vertical grouping costs some scrolling on wide windows but has one stable reading
order and content-driven height. Background text-design consultation supports it;
independent read-only engineering reviews resolve the required design questions.
These reviews are not human screen acceptance.

Selected real-control proof must show complete text at page stops (every body
TextBlock is fully visible at least once), reachable final row, return to exact0,
identical-result new-owner reset, fixed footer/client bounds, no glyph/scrollbar
overlap, no horizontal overflow, retained sink/panes/settings and no post-stop or
stale-owner repaint. The diagnostic observes asynchronous completion within a
bounded interval without resending the request.500x400is a measurement condition,
not a declared product minimum. Smaller clients,100%/200%/custom display scales,
system text scaling, screen reader and physical/visible acceptance remain explicit
limits until observed. No broader support claim is inferred from this matrix.
