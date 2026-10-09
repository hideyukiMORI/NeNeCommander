# ADR-0055: Discover drives and WSL roots through one session-owned Locations picker

Status: accepted

Date: 2026-10-09

## Context

The charter's MVP scope includes drive and WSL-root discovery. Today a pane reaches another drive
or distribution only through typed address text (ADR-0044) or a bookmark (ADR-0041). ADR-0034
already lists WSL distribution roots through `IWslDistributionCatalog`, but nothing shows them.
Windows volumes are not enumerated anywhere. Every pane move must stay on the single navigation
route, every transient scope must take the ADR-0051 owner form, and `CommanderSession` is near its
300-logical-line limit.

## Decision

- **One Application port lists Windows volumes.** `IDriveCatalog` returns one closed
  `DriveCatalogOutcome`: `Succeeded` with an ordered, identity-unique list of `DriveLocation`
  values, each a `WindowsLocalPath` root, a closed `DriveKind` (`Fixed`, `Removable`, `Network`,
  `Optical`, `Unknown`), and an optional volume label; or `Failed` with a closed reason; or
  `Cancelled`. Only `WindowsDriveCatalog` in Infrastructure.Windows implements it. It calls
  `DriveInfo.GetDrives()` through the existing Windows local I/O execution boundary (ADR-0027),
  classifies through `DriveType` (`Fixed`, `Removable`, `Network`, and `CDRom` as `Optical`; `Ram`,
  `NoRootDirectory`, and `Unknown` as `Unknown`), treats a not-ready volume as `Unknown` rather
  than dropping it, reads the label only of a ready volume and lists the volume without a label
  when that read fails, parses each root through `FileSystemPath.Parse`, lists a repeated root
  once, and counts rather than shows a root that is not a `WindowsLocalPath` root. One internal
  enumerator behind an injectable seam is the only code that touches `DriveInfo`.
  `IWslDistributionCatalog` is reused unchanged.
- **One scope owner holds the picker.** `LocationsSession` joins `TransientScopeOwners` and its
  `LocationsState` joins `TransientScopeSnapshot` in the ADR-0051 form: `Open` with the pane
  snapshot and `InteractionOwnership`, `Validate` of an expected-state-qualified intent, total
  operations, closed results, no pane effect, no reference to the other owners. The state is
  `Closed`, `Loading`, or `Open` with two independent sections, drives and WSL roots, each either
  listed or failed with its reason, and one focus index over the listed items, drives first. Both
  catalogs are read concurrently while the state is `Loading`; the cancellation of either read
  closes the picker. A section failure never hides the other section and is never reported as
  success.
- **Selection navigates through the existing route.** Choosing an item sends the pane active at
  open through `DualPaneSession.NavigateAsync` exactly once, exactly as a submitted address does.
  The picker closes when the session accepts the selection, before the read starts; a read failure
  of the selected root appears through the pane's existing failure state. A network volume is
  listed by its drive-letter root, which parses as a `WindowsLocalPath` exactly as the same typed
  address does; no UNC location is derived from it. No second read path, retry, or
  picker-specific error channel exists.
- **Keys and catalog.** `Ctrl+G` opens the picker from the file list and navigation surface under
  the same admission as the bookmarks modal: no other transient scope, modal, editor, pending
  operation decision, or in-flight pane work. Inside the picker `Up`, `Down`, `j`, and `k` move the
  focus, `Enter` selects, `Escape` closes, a held `Enter` selects at most once, and every other
  identified key is consumed (KBD-002). These keys are declared in the canonical table under one
  keyboard context of the picker's own, because the existing modal context passes undeclared keys
  through to native controls. `OpenLocations` joins the ADR-0047 command catalog. Plain `G` chords
  stay with movement.
- **Presentation reuses the bookmarks modal structure.** The picker renders through existing
  semantic tokens and the localized resources; drive rows show the letter, label, and kind, WSL rows
  show the distribution name; `Loading`, per-section failure, an empty picker, and the count of
  unshown drive roots are explicit texts. No new token, color, style, or code-behind decision is
  added.

## Rejected alternatives

- Typing drive letters into the address editor only: it does not discover anything and leaves WSL
  distributions invisible.
- Putting drives into the bookmarks catalog: bookmarks are user-authored and persisted; volumes
  are environment facts that change at run time.
- A drive bar or pane header dropdown: it adds a mouse-first surface before the design handoff
  and a second navigation entry.
- Hiding not-ready or network volumes: the user loses the fact that they exist; the pane's typed
  failure already explains why one cannot be read.
- Enumerating in the view: it bypasses the port, the I/O boundary, and the test seam.

## Consequences

- The transient-scope records gain one owner and one state; `CommanderSession` adds dispatch
  lines only and stays within its limit, otherwise ADR-0051 absorbs the overflow.
- Infrastructure gains one adapter with an injectable `DriveInfo` seam for deterministic tests;
  real volume enumeration is not a gate tier.
- Network volumes are listed by their drive-letter root and read through the Windows local
  provider exactly as the same typed address is read today; shares without a drive letter are not
  discovered, and no UNC provider behavior is guessed.
- `COMMAND_MODEL.md` gains one registry row for Windows drive enumeration and one for the
  Locations picker scope; `GLOSSARY.md` gains "drive location" and "Locations picker";
  `KEYBOARD_MODEL.md` gains `Ctrl+G` and the picker's key table.

## Migration and removal

No stored data changes. Removing the picker removes its intents, owner, state, adapter, bindings,
catalog entry, modal, and tests together.

## Executable proof

Application tests prove open and close, every combination of section success, failure, and
cancellation, focus boundaries, a single `NavigateAsync` call per selection, freeze of other
intents while open, and ownership exclusion against the other scopes. Infrastructure tests prove
the kind classification, not-ready handling, root rejection counting, and execution through the
I/O boundary with an injected seam. Mapper tests prove the `Ctrl+G` context matrix and the modal
key ownership. Presentation tests prove row text and the loading and failed states. Thresholds are
unchanged.
