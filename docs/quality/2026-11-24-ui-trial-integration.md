# 2026-11-24 UI trial integration — Issue #564

Status: local integration in progress; no publication or human acceptance

## Scope and invariant

The local trial combines the existing #530 pointer entry, #531 context menu,
#544 empty text file, #547 opposite-pane navigation, #548 compact Locations and
#551 display transfer candidates. beta.4/3909039 remains published unchanged.
Source copies remain separate and clean; this work owns only
D:/NeNeCommander/worktrees/issue-564-ui-trial and matching ui-trial-20261124
output/evidence directories. No shared-desktop input, CUA, ordinary App termination
or user-settings write is permitted. A visible trial is not launched by this work.

The existing CommanderSession/CommandCatalog, pane owners, FileOperationGateway,
keyboard mapper/repeat guard and window-adjustment route remain the sole mechanisms.
Only overlapping composition is reconciled; no new operation or dependency is added.

## Exact candidates and reconciliation

Start: #551 at47fc6ba19cd03a6443ab30bc6d12384967f07506, already connected to
accepted main1d0fa30342dff7d92153d33577ca13a83a886c80. #531 at
4b3e75bb2ef17171e482d11f44ad2b3726b923b3 includes #530 and #544 by ancestry.
Its normal local merge is c6b8ec7a76b75e8ee27e9615991fec6d58adcd63.
Both Stop callbacks are retained before draining; the one repeat guard retains
window transfer, focus/menu and native single-shot ownership. The combined key
model has51keys and5modifiers;20window bindings leave230identified undeclared
combinations consumed. Three catalogs contain595unique keys after this merge.

#547 at298cc56f2fdb80022b797ff2c50a5209b10d656e is merged next. Its older
per-row target/opposite projection is reconciled to hide's accepted November23
compact-palette handoff: the captured header stays stable and the existing
CommandPaletteContext/Feedback names the actual opposite destination in selected
detail and UIA text. Four direction/filter cases and ADR0084 reflect this contract.
The command is added once to the shared catalog as PaletteOnly, preserving the
existing context-menu grouping contract. There are28palette commands,50FileList
and29NavigationSurface bindings. All3locale catalogs have601unique keys.
The old address PointerPressed handler is not reintroduced; PanePointerView owns
passive header taps and excludes the nested command Button from passive hit targets.

The header's navigation projection refreshes on every Render, independently of
cached pane rows, because passive readiness and language can change without a new
active-row projection. The existing pointer/selection cache remains intact.

#548 at ee1859a40eaa3183baf5b08e3eb37be847685f08 is still pending at this checkpoint.

## Attempts and selected evidence

All raw commands, exact arguments/times/results and retained failures are under
D:/NeNeCommander/evidence/ui-trial-20261124. No old component failure is relabeled
as a pass, and no whole canonical/deep-review/CodeQL result is claimed.

- Initial conflict-helper regex was too broad. XML parsing rejected it before any
  resource write. The only changed state document was restored from original merge
  stages; the corrected helper validates all writes before saving.
- Automatic approval initially rejected the #547 conflict helper because a comment
  replacement might damage executable code. No command executed. Read-only inspection
  established that the single DualPaneSession conflict is entirely XML documentation
  before the class; the revised helper requires identical complete class/method body
  and valid summary XML. That checked local action then succeeded.
- Locked Release restore succeeds. App build01 fails with three missing navigation
  variable references from an automatic merge and dependent XAML type-resolution
  diagnostics (193errors/1warning total). The navigation rendering correction above
  resolves the cause; App build02 succeeds with0warnings/0errors.
- Application test build01 exposes one old two-argument palette Open call. It is
  updated to explicit CommandSurface.Palette; build02 succeeds with0warnings/errors.
- Presentation builds01/02 succeed with0warnings/errors. Application seam tests01
  pass162/162. Presentation seam tests01 pass232/233; the only failure expects28
  navigation-surface bindings before Ctrl+E was added. The corrected29-count case
  passes1/1 in count02. The other232 successes are reused without another full run.

Commands are saved in restore-01.json, app-build-01/02.json,
application-build-01/02.json, presentation-build-01/02.json,
application-seams-01.json, presentation-seams-01.json and presentation-count-02.json.
The selected Application filter covers opposite navigation, captured menu/palette
ownership, pointer transitions and the directly shared window/text-file boundaries.
Presentation covers the combined keyboard, palette/menu/entry, opposite navigation,
pointer and window-adjustment consumers. Unchanged provider operations, mutation and
release proof are not rerun merely to combine branches (ADR0061/QLT015).

## Remaining proof and preservation

Finish #548 composition, then verify the affected final XAML/App and real-control
seams, including header-button readiness with a reused pane projection, language
refresh and pointer ownership. Review the combined ownership/render changes before
calling the trial ready. Physical mouse/key/IME, visible/native screen acceptance,
source publication, feature main integration and release are separate and pending.
Retain this incomplete worktree/output and all original feature candidates. Its
cleanup follows acceptance/integration and permanent artifact preservation; it is
not included in the completed beta.4 cleanup.
