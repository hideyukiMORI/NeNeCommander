# ADR-0094: Filter loaded pane names through the existing visible-state owner

Status: accepted

Date: 2026-11-25

Issue: #545. This feature is excluded from beta.5. The local basis combines selection
2cfc3b2 and the reviewed release correction b72bbcc; only this task receives that merge.

## Context and invariants

PaneReducer is the only owner of pane state changes, and PaneState's one visible
projection determines focus, selected operation targets, range anchors and rows.
A query must never leave a hidden selection or a second filtered row collection in
the view. Filter changes read the loaded listing only; providers and FileOperationGateway
remain unchanged. Editing is native text input with exact scope ownership, not a new
route for file commands. Both panes retain independent applied queries.

## Chosen single mechanism

Add one immutable PaneNameFilterQuery value, created once from native committed text.
It retains the exact text for caret/IME stability and privately compiles nonempty
Unicode-whitespace-separated terms. Every term must be a literal substring of the
complete entry name, using OrdinalIgnoreCase. No wildcard, regex, fuzzy ranking,
path interpretation, culture-sensitive collation, width/kana folding or Unicode
normalization is performed. Canonically distinct Unicode sequences remain distinct;
unpaired UTF-16 is handled as literal text rather than sent through normalization.
All non-null text is meaningful query input; no new settings or provider format exists.

PaneState owns the applied query. Extend its existing SelectVisible projection to
apply hidden-item eligibility and then the query over existing sorted entries.
PaneReducer alone applies/replaces/clears that query, retaining visible selection
by provider identity, retaining a visible anchor, and recovering focus through the
existing forward-then-backward policy. Clearing never restores dropped marks or an
old anchor. Empty matches have no focus. All/range/invert use the resulting same
VisibleEntries. No filtering or selection calculation occurs in App/XAML.

Refresh and same-location navigation retain this pane's query; true location changes
clear it. Refresh preserves existing clear-selection policy and rebinds focus/anchor
to the new owned visible paths. Apply the carried query before final preferred-focus
and removed-focus recovery. Explicit creation/rename may create a name outside the
query: report its completed operation honestly and do not bypass the filter to show it.
Successful opposite-pane navigation follows the destination pane's same rule.
Cancelled/failed reads keep the retained listing and its query with existing qualifiers.

Use one new PaneFilterSession for temporary native editing, composed alongside the
other declared transient scopes. It owns Closed/Editing and exact expected-state
admission; it does not own a duplicate applied query or mutate panes. An Editing state
captures the addressed side and current source pane. Qualified updates resolve to one
accepted query change; CommanderSession applies it through DualPaneSession/PaneSession
and PaneReducer, then reconciles the edit capture to the accepted new pane snapshot.
A stale edit owner cannot update/clear another folder, side or later editor. Open,
text-change, Enter, Escape and pointer clearing share this route; the palette supplies
only the same open/clear commands. No reentrant UI or local callback changes PaneState.

Native focus loss ends editing without a focus-return effect. Commit and Blur carry
the latest committed native text in the existing PaneFilterEditSubmission. Validation
requires the exact editor, active side, exact SourcePane and ordinary CanEdit admission
before applying it through the same reducer. Exact stale-pane Blur only retires its
editor; Cancel ignores carried text and clears the query. Reconcile cannot reopen a
closed owner. The native control supplies text only for its matching rendered editor
outside composition; otherwise it supplies that owner's accepted query. Ongoing editing
never assigns the last applied value back into native Text, because TextChanged can be
deferred. Entry and non-editing render still synchronize the native property.

Blur therefore preserves the latest committed query. Explicit Enter returns focus once while preserving query; Escape in
the editor clears query and returns focus once. IME owns Escape while composing and
only committed text applies. Pointer interactions that leave the editor must finish
its exact scope before admitting their existing qualified interaction; stale blur
must not close a replacement editor or reactivate the previous pane. Other modal,
busy, loading, launch and stopping owners retain precedence. The exact handoff and
returned-state identity must be proved in host order, not only by direct mapper calls.

In the filtered file list, idle Escape retains existing selection/anchor clearing
first; only when no selection cleanup remains does it clear the query. Explicit
ClearSelection continues to clear marks only. A pending gg, read, operation or modal
still consumes its existing Escape. Produced / in FileList opens editing; Ctrl+F stays
unassigned. Native text/IME contexts retain their ordinary character/navigation rules.

## Necessary composition preparation

Current measured CommanderSession is296logical lines against300. TransientScopeOwners
and TransientScopeSnapshot already have five-parameter findings. Do not add another
oversized constructor or hide added logic in partial files.

Before the new scope, extract only the existing bookmark navigation coordination
into one CommanderBookmarkCommands component: its navigation-in-progress latch,
direct/manager navigation and existing failure recovery stay together. It receives
the same panes/settings owners from the App composition root; only CommanderSession
invokes its internal command methods. The root retains dispatch/precedence and reads
that one latch for admission. No second bookmark state or execution route exists.
The constructor may take this fourth explicit dependency; no service locator or
ambient callback is added. Existing bookmark/abort/failure/busy proof covers the move.

Group per-pane transient composition (address editor, Locations and name-filter editor)
into one immutable PaneScopeOwners carrier and corresponding snapshot. The outer
transient carriers then take this group plus command palette, window adjustment and
keyboard help. Preserve the current public snapshot accessors through one private
carrier; do not expose duplicate public paths or duplicate stored states. This is
constructor grouping only, not a dynamic registry, parallel scope manager or new
lifetime. The App remains the only composition root.

## Presentation and engineering adaptations

Use one inline bar below each virtualized list and above its existing count strip.
The bar stays while the native input owns focus or the applied query is nonempty.
Existing semantic text/spacing/focus/border tokens apply; no animation or new brand
values. A header action and palette command expose the same / entry. Enter returns
to the list without opening an item. A labeled clear button invokes the sole clear
command. Narrow layout wraps complete labels; no meaning is hidden behind an icon.

The existing count strip remains authoritative for visible files/folders, explicit
selection and loaded/retained qualifiers. A localized filter summary may show matches
of hidden-eligible loaded entries, with clear names and accessibility text rather than
an unexplained bare ratio. Do not call zero eligible rows an empty folder: hidden
items or incomplete/unrepresentable entries may exist. Existing empty/read-failure
states remain, and query-no-match is explicitly about the currently loaded eligible
entries. No new live announcements or timer are added. Parent navigation is a header
action, not an item counted or filtered as a synthetic '..' row.

Claude's actual Skill(design) invocation returns access-management-only capabilities;
no grant/revoke or remote publication occurs. Ordinary authored-brief advice and local
pane-filter.html are references, not an approved final visual specification. The
engineering handoff records adopted behavior and rejected empty/count assumptions.

## Alternatives rejected

- View-only filtering: displayed rows and selected operation targets could disagree.
- Provider enumeration per keystroke: duplicates existing listing and cancellation.
- A second query in settings/UI state: adds conflicting ownership and persistence.
- Editing flags in PaneState or unqualified LostFocus writes: mix native input lifetime
  with durable pane state and can reanimate stale text after modal/read changes.
- A modal filter dialog: hides immediate results and requires a second applied-query
  indicator. An inline editor still uses the established transient ownership mechanism.
- Restoring marks on clear or cancelling to the old query: makes hidden operation targets
  reappear and incorrectly implies selection undo.
- Enlarging existing constructor/type limits, partial-file evasion or broad utilities:
  the necessary cohesive extraction/grouping keeps existing canonical owners.

## Proof and migration

No dependency/schema/provider change. Add query/reducer red proof before behavior:
AND terms/case/Unicode/extension literals; empty/single/zero/bounded/hidden/sorted
listings; exact owned identity; filtering drops selection/anchor permanently; focus
recovery; refresh versus true navigation; creation outside query; both panes; range,
all, invert and actual operation sources. Prove no extra provider read per query.

Add exact scope lifecycle, stale updates/blur, passive activation, busy/modal/read/
launch/operation admission and existing bookmark extraction boundaries. Test actual
classifier→mapper→host ownership for /, text Ctrl+A, Space, */slash and IME dismissal;
held Enter/Escape and focus-loss order must not replay or open a file. Palette/F1/
three locales/count qualifiers and architecture composition require direct consumer
proof. Keep native rendered/focus observations on the owned non-input desktop with
private settings; no shared input or physical/screen-reader acceptance claim.

ADR0061 selects affected layers and direct consumers. This adds a shared input scope
and relocates orchestration, so full canonical proof is needed if focused evidence
cannot establish the complete precedence impact; decide concrete scope before running.
Do not start redundant release proof or mutate beta.5's frozen worktree. Preserve all
failed attempts, reuse only matching inputs, and clean D copies only after accepted
integration and permanent evidence preservation.

## Technical acceptance

The selected design is technically accepted with the local implementation and final
independent read-only review. It does not claim hide's final visual acceptance, actual
IME/physical-key proof or publication. The [quality record](../quality/2026-11-25-pane-name-filter.md)
records causal failures before the retained-activity, equal-count projection, Enter-focus
and pending-text fixes, the selected layer coverage, native ordering and measurement.
A same-pane interaction captured before a pending-text flush is refused if the flush
changes its SourcePane; the host never retargets that old request. Native LostFocus
followed by a later Click captures the updated source and is independently observed.
