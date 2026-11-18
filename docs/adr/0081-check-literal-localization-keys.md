# ADR-0081: Check literal localization keys against shipped catalogs

Status: accepted

Date: 2026-11-18

## Context

Issue #448 found that a view split changed a ResourceLoader string key to the
private field name. Compilation and presentation tests passed, but rendering
operation progress requested a missing resource and threw COMException
0x80073B17. Both catalogs still contained the intended key. Existing CS-023
requires resource-based UI text; its XAML checks did not verify these literal
references. QLT-007 requires executable positive and negative proof for a gate.

## Decision

- Extend the existing `eng/conformance.ps1` CS-023 check. For App C# direct
  `GetString("Key")` calls whose key is a simple ASCII identifier, require an
  ordinal, case-sensitive match in every shipped `.resw` catalog. App source
  with no catalog fails closed.
- Keep the scanner's existing raw-source approach. It does not resolve C#
  semantics, dynamic keys, aliases, escaped strings or comments. The constrained
  scope is stated in the coding rules; presentation-key tests remain necessary.
- Use the existing `eng/prove-gates.ps1` fixture materialization and conformance
  entry. Its full run and focused `-Rule CS-023` run prove the valid tree, absent
  catalogs, an unknown key, wrong case and a missing English translation.
- Register the proof in `docs/quality/GATE_PROOFS.md`. No threshold, suppression,
  gate cadence, dependency, public API or product rendering mechanism changes.

## Alternatives and consequences

Fixing only the literal leaves the same error invisible to compilation. Catching
and hiding the resource exception conceals a product defect. A second localization
wrapper, source generator or C# parser would introduce an unnecessary mechanism
for this bounded defect. The existing source scanner catches the observed class
of error without claiming complete localization verification.

## Proof

The new check rejects the old production call once per catalog. After the
one-literal correction, all five focused fixtures pass. App Release build and
15 selected move/progress cases pass. The actual public resource loader rejects
the old key and accepts the correct key. The [quality record](../quality/2026-11-18-operation-progress-resource.md)
owns commands, results and remaining desktop/environment limitations. ADR-0061
selects this bounded evidence instead of unrelated full or deep validation.
