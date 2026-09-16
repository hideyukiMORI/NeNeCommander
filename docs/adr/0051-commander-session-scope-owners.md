# ADR-0051: Split transient scopes out of `CommanderSession` into scope owners

Status: proposed

Date: 2026-09-16

Proposed by the NeNe Commander design owner under hide's delegated authority for its own Issue
(number assigned at filing), as the prerequisite of ADR-0050 (Issue #100). The split was listed
as a follow-up candidate awaiting hide's approval in the 2026-09-11 handoff; it is proposed now
because the next change that grows `CommanderSession` cannot honour QLT-010 without it.

## Context

`CommanderSession` coordinates the dual-pane session, the settings session, bookmark navigation,
the address editor (ADR-0044), and the command palette (ADR-0047). Measured on main `f7d06c1`
it holds 324 logical lines inside the type against the CS-013 limit of 300 (blank lines,
comments, `using` directives, the namespace line, and lone braces excluded). CS-013 is enforced
by review, not by the gate, and the same count finds five other types above the limit
(`WindowsLocalSettingsStore` 580, `CommanderWindow.xaml.cs` 509, `FileOperationGateway` 412,
`WindowsLocalFileOperationAdapter` 327, `BookmarkManagerView` 312). Gating CS-013 across the
repository is therefore a decision of its own, already listed with the analyzer-enforcement
candidate awaiting hide's approval; this ADR neither gates the rule nor exempts those types. It
applies QLT-010 to the one type the next change would grow: `CommanderSession` is brought under
the limit before ADR-0050 adds a scope to it.

The scopes inside the type have different shapes. The command palette (48 logical lines) and the
address editor (68) are each one closed state record, an admission rule, an expected-state
validation, and a hand-back to the session's private dispatch or to a pane route. Bookmark
navigation (99) is not a scope of its own: it is routing over `SettingsSession` and both panes
with an in-progress flag, and it stays. Dispatch precedence (`HandleAsync`,
`DispatchIdleIntentAsync`, `NavigateAsync`) reads every scope and stays.

`SettingsSession` already shows the owner shape the repository uses for such a scope: it owns
its state under one lock, exposes operations that return its snapshot, marks caller-only
transitions `internal`, knows no other owner, and is constructed at the composition root before
the window. The palette and address scopes differ from it in one way: their admission and
validation read the pane snapshot and whether another scope owns input, so an owner in that
shape must receive those facts as arguments instead of holding the other owners.

Two constructor signatures are already at the CS-013 four-parameter limit and would break with
one more scope: `CommanderSession` once each owner is a parameter, and
`CommanderSnapshot(panes, settings, addressEditor, commandPalette)`.

Three orderings inside the current code are observable behavior and must survive the move: the
address editor state is set after `ActivateOtherPane` completes and from the snapshot read
before it; the address editor closes before `DualPaneSession.NavigateAsync` starts; and the
palette closes before the validated intent is dispatched, without an intermediate closed
notification.

## Decision

- **Two scope owners, `CommandPaletteSession` and `AddressEditorSession`, take the state and
  validation of their scopes.** Each owns its closed state record (`CommandPaletteState`,
  `AddressEditorState`) under one lock, exposes its current state, and exposes the operations
  its scope needs: `Open` with the pane snapshot the admission rule reads, `Validate` of an
  expected-state-qualified intent against the current state, the current pane snapshot, and one
  closed `InteractionOwnership` value (`ScopeOwnsInput` or `AnotherScopeOwnsInput`) that
  `CommanderSession` derives from the settings and the other scope's state, and `Close`. Each
  returns a closed result that tells the caller what happened and, for a qualified submission,
  which intent or which parsed target the session must now route. No parameter list exceeds
  four, no parameter is a boolean, and neither owner references `DualPaneSession`,
  `SettingsSession`, or the other owner's types, and neither performs a pane effect.
- **`CommanderSession` keeps dispatch, precedence, freeze, and every pane effect, and the
  owner's state is final before any effect.** It still decides which scope owns the next intent,
  still calls its one private dispatch path for a validated palette intent exactly once, still
  calls `DualPaneSession.NavigateAsync` for a parsed address target, still owns `NavigateAsync`'s
  external-navigation guard, and still owns bookmark navigation. An owner's result is returned
  only after the owner's state has changed, so the session performs the pane effect against an
  already closed or already open scope; the three orderings named in Context are preserved and
  their existing tests keep passing. No behavior, intent, outcome, or snapshot property changes.
- **Owners and scope states travel as one closed record each, written in the repository's
  explicit-constructor form.** `CommanderSession` is constructed from `DualPaneSession`,
  `SettingsSession`, and `TransientScopeOwners`, a sealed record holding the palette and address
  owners; `CommanderSnapshot` is constructed from `DualPaneSnapshot`, `SettingsSnapshot`, and
  `TransientScopeSnapshot`, a sealed record holding `AddressEditor` and `CommandPalette`. Both
  records have an explicit `internal` constructor with `ArgumentNullException.ThrowIfNull`
  guards and get-only properties, in the form `CommanderSnapshot` already uses; positional
  records and primary constructors stay prohibited (CS-014), and no factory is added because the
  records carry no invariant beyond non-null members (CS-008). Both records are the single place
  a later transient scope is added, so ADR-0050 adds its window-adjustment owner and state there
  without touching either constructor's arity. Readers of the two moved snapshot properties in
  App and tests follow the new path; no delegating properties are kept.
- **This ADR supersedes the ownership sentences of ADR-0047 and ADR-0044 and replaces the
  registry row.** ADR-0047's "`CommanderSession` is the sole palette interaction owner" now
  reads: `CommandPaletteSession` owns palette state, admission, and qualified validation, and
  `CommanderSession` remains the sole dispatcher of a validated palette intent. ADR-0044's
  "`CommanderSession` owns one address-editor state" now reads the same way for
  `AddressEditorSession` and address navigation. `COMMAND_MODEL.md` row "command-palette
  catalog, captured scope, availability, and qualified routing" is replaced by
  "command-palette state, admission, and qualified validation | `CommandPaletteSession` with
  `CommandCatalog`" plus "dispatch of a validated palette intent | `CommanderSession`", and a new
  row "address-editor state, admission, and submission validation | `AddressEditorSession`" is
  added. Every other sentence of ADR-0044 and ADR-0047 stays in force.
- **The count is measured and recorded, not gated.** After the change `CommanderSession` holds
  at most 300 logical lines by the count above; the PR records the measured number for
  `CommanderSession` and both owners. No per-type test is added, because a check that applies to
  one type while five others exceed the limit would be a baseline in all but name; making CS-013
  a gate for every type is the separate decision named in Context.

## Rejected alternatives

- Extracting only the palette: leaves about 285 lines, so the next scope would breach the limit
  again immediately.
- Extracting bookmark navigation instead: it is pane routing with an in-progress flag rather
  than a state-and-validation scope, and moving it would move pane effects out of the dispatcher.
- Giving each owner a reference to `DualPaneSession` or to the other owners: breaks the
  `SettingsSession` shape, creates a dependency ring between owners, and lets an owner decide
  from state it does not own.
- Passing the other scope's state type to an owner: `CommandPaletteSession` would depend on
  `AddressEditorState`; the closed `InteractionOwnership` value carries the one fact it needs.
- A boolean "other scopes closed" parameter: CS-002 prohibits boolean mode parameters.
- Letting the owners dispatch: reintroduces the cycle from owner back into `CommanderSession`'s
  private dispatch that ADR-0047 placed inside the session on purpose.
- Passing each owner as its own constructor parameter: reaches the CS-013 four-parameter limit
  now and breaks on the next scope.
- Keeping delegating `AddressEditor` and `CommandPalette` properties on `CommanderSnapshot`:
  two ways to read one state.
- A CS-013 waiver: QLT-010 requires the fix before merge, and the extraction is small and
  mechanical.
- A logical-line test for `CommanderSession` in `Application.Tests`: it would need `System.IO`,
  which CS-018 confines to Windows infrastructure, and it would be a one-type carve-out.

## Consequences

`CommanderSession` drops to an estimated 220 to 230 logical lines. Two owners of roughly 40 and
60 lines and two records appear under `Application/Sessions`. Five construction sites change
(the composition root, the session test factory, two null-guard tests, and one Presentation test
that builds a session), the null-guard reflection tests take the new constructor signatures, and
two App reads of the moved snapshot properties follow the new path. Production Domain,
Infrastructure.Windows, and Presentation code, and every filesystem, provider, settings,
resource, and dependency boundary, are unchanged. No native or process boundary is touched, so
no Issue-specific security deep review is required beyond the scheduled tier. `docs/GLOSSARY.md`
gains "transient scope" and "scope owner".

## Migration and removal

Add `CommandPaletteSession`, `AddressEditorSession`, `InteractionOwnership`,
`TransientScopeOwners`, and `TransientScopeSnapshot` under `Application/Sessions`; move the
palette and address state fields, admission rules, validation, and helper members into the
owners; change the two constructors and their five construction sites; update the App reads of
`CommandPalette` and `AddressEditor`; rewrite the ADR-0047 and ADR-0044 sentences named above
with a superseding note that points here; replace and add the `COMMAND_MODEL.md` rows; and add
the glossary terms, all in one change. Nothing else is removed.

## Executable proof

Every existing `CommanderSessionTests`, `CommandPalettePresenterTests`, and `NullGuardTests`
case passes with only construction sites changed, including the three ordering tests named in
Context. New null-guard cases cover both owners, both records, and `InteractionOwnership`.
Mutation scores of Application and Presentation stay at or above their thresholds in the
exact-head deep review, since the moved validation keeps its assertions. The PR records the
measured logical line counts. The canonical gate passes at Draft-to-Ready.
