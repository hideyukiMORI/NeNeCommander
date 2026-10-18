# ADR-0074: Compare visible entry names in natural numeric order

Status: accepted

Date: 2026-10-18

## Context

Issue #246 originally recorded a deferred suggestion. On 2026-10-18 hide selected
this work and explicitly chose natural order: `file1`, `file2`, `file10`.
The existing ordinal case-insensitive comparison instead puts `file10` before
`file2`. This decision replaces the name-comparison part of ADR-0053 and the name
tie-break inherited by ADR-0056. Their ownership, grouping and direction rules stay
in force, as does ADR-0067's persistence contract.

## Decision

Extend the private name comparison inside `EntryOrdering.Apply`, the sole visible
ordering projection. Do not add another ordering service, a provider-specific
comparison, a sort key, a settings option, a dependency or a public API.

Compare names from left to right:

1. When both current positions begin an ASCII digit run (`0` through `9`), compare
   the runs by their non-negative integer value. Ignore leading zeros for that
   comparison; compare significant lengths, then significant digits ordinally.
   Do not parse into a bounded integer or allocate a large numeric object.
2. If the values are equal, the shorter original digit run sorts first. This
   decision occurs before examining the following text: `file2 < file02 < file002`
   and `file2z < file02a`. An all-zero run has value zero, so `file0 < file00`.
3. Otherwise preserve ordinal case-insensitive text comparison. A prefix sorts
   before the longer name. Preserve framework handling of Unicode case, including
   supplementary characters; do not compare UTF-16 code units independently for
   case folding. Punctuation retains its ordinary ordinal position relative to
   digits and text, so `a! < a1 < aA`.
4. Only when the whole natural case-insensitive comparison ties, compare the
   original complete names ordinally, as before. `File2 < file2`, while later
   substantive differences still precede a case-only difference.

Only ASCII runs are numeric. Fullwidth and other Unicode digits remain text;
there is no normalization. Signs, decimal separators and grouping separators are
ordinary characters: `v1.2 < v1.10` compares successive runs, not decimal values.
Very long runs use the same rule without overflow. The comparison reads no locale,
environment, filesystem identity or provider capability.

The name key uses this comparison in either direction. The existing extension,
size and modification-time keys keep their primary comparisons and use natural
name ascending for ties, in both primary-key directions. Directories remain first.
The extension key itself remains ordinal case-insensitive; numeric extension text
is not a new key policy.

Windows local, UNC and WSL names share the same display ordering. This does not
change their distinct path identities or case semantics. `DirectoryListing`
continues to store its canonical ordinal snapshot. `PaneState` obtains its visible
sequence only through `EntryOrdering`; focus, selection, navigation and presentation
continue to consume that sequence through their existing owners.

## Rejected alternatives

- Keep lexical name ordering: inconsistent with hide's chosen numeric sequence.
- Use an operating-system logical comparison or current-culture collation: would
  introduce a platform/locale policy into a deterministic Application projection.
- Parse digit runs as Int64 or floating point: bounded range and precision do not
  express ordering for arbitrary filename digit runs.
- Normalize Unicode digits or filename text: changes the existing text distinction
  beyond the requested ASCII numeric ordering.
- Add a natural-sort checkbox, new sort key or provider-specific order: duplicates
  the existing name-order choice without a requested product need.
- Reorder `DirectoryListing` or sort rows in the view: changes snapshot ownership or
  creates a second visible order inconsistent with keyboard movement.

## Consequences

Existing saved name orders acquire the new comparison on the next projection;
schema 3 and accepted settings writes are unchanged. The default visible order
can differ from `DirectoryListing.Entries` when names contain numbers. Tests must
assert the intended distinction rather than claim those sequences always match.

Zero padding is an explicit secondary distinction within each numeric run, not
an instruction to merge names or identities. Unicode digit names remain lexical,
and no exact Windows Explorer ordering compatibility is promised.

## Migration and removal

Replace the old private name comparison in the existing projection. Update its
contract comment and affected documentation; keep one name comparison used by
every name key and name tie-break. There is no legacy switch, parallel path, data
migration, filesystem effect or change to keyboard bindings.

## Executable proof

Application tests specify ordinary and descending numeric order, equal-value
padding, all-zero and long runs, multiple runs, prefixes, punctuation, case ties,
Unicode text/digits, directory grouping and every existing key's name tie-break.
They also prove deterministic comparison order across mixed names and input
permutations, including the listing boundary, with no timing threshold.

Direct pane tests cover initial focus, movement, toggling, selection identity,
hidden-focus recovery and carried ordering through reads/history. Presentation
tests verify that displayed rows follow the same projection and preserve row
identity. The canonical listing remains unchanged.

Under ADR-0061 this is a bounded change to a pure existing comparison. Build the
affected Application/Presentation components and run their ordering and directly
affected consumer tests, changed-file formatting and the normal Commit checks.
No provider I/O, path identity, settings encoding, public contract or gate changes;
those unrelated suites, full/deep CI and live desktop/provider tiers are not
selected. Reuse successful proof while its relevant inputs remain unchanged.
