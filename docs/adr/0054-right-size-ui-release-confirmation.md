# ADR-0054: Right-size the Windows UI release confirmation

Status: accepted

Date: 2026-10-09

## Context

The 2026-09-02 constitution made accessibility, high contrast, and DPI scaling structural
requirements and QLT-009 demanded recorded proof on a supported environment matrix. Issue #70 and
Issue #94 then grew that matrix into Windows-reported 100/150/200/300 percent scaling in separate
environments, eight color schemes at each, high contrast, Narrator speech evidence, a cross-scale
move, a taskbar seam, and a dedicated interactive VM or test account with per-key admitted
synthetic input. Those requirements were written by the AI sessions that authored the constitution;
hide never asked for them. On 2026-10-09 hide pointed this out and decided: when a scaling or
high-contrast look is needed, hide changes one monitor's scale in Windows settings themselves, on a
day hide is present, and feature work from the MVP list comes first. No dedicated environment is
prepared.

For a personal keyboard-first file manager this is the ordinary level of care. Mandatory
accessibility conformance (WCAG, Section 508, EN 301 549) applies to products sold into
organizations that require it; a one-time look at another scale and at high contrast catches the
WinUI layout faults that matter here. The honesty rule stays: a check that has not run is not
claimed.

## Decision

- The Windows UI release confirmation is: hide uses the product daily at the development display
  scale; before a release is declared, on a day hide is present, one monitor is set to 100 or 200
  percent in Windows settings, the product is placed on it and the panes, address, status, key
  hints, the `Ctrl+W` helper, and the `F2`/`F7`/`F8` modals are exercised once by hide, high
  contrast is switched on once and the same screens are looked at, and the scale and contrast are
  restored. hide owns the settings change; no automation changes a display or accessibility setting.
- Evidence, when recorded, is taken with `eng/ui-evidence/Invoke-UiEvidence.ps1` in `-Mode
  Observe` after hide has changed the setting; it sends no input. ADR-0052 remains the sole
  mechanism for any synthetic input and for recorded UI evidence, and its `-Mode Input` is used only
  when hide asks for an unattended recording.
- Narrator speech evidence, 150 and 300 percent cells, the taskbar-seam cell, the eight-scheme
  matrix at every scale, and a dedicated VM or test account are not release requirements. They
  become candidates only if hide decides to distribute the product to other people, through a new
  ADR.
- Issue #94 is rewritten to this confirmation and stays open until it has run once; until then the
  state documents say that the pre-release confirmation has not run. QLT-009 keeps its identifier
  and its rule that environment checks are named honestly; the paragraph describing the matrix is
  replaced by this confirmation. QLT-011 and KBD-002 are unchanged: interaction states, focus,
  high-contrast resource use, and modal key ownership remain proven by presentation and mapper
  tests, which the gate already runs.

## Rejected alternatives

- Keep the matrix and only lower its priority: the documents would keep describing a requirement
  nobody owns, and every session would re-derive the same environment plan.
- Delete QLT-009: the honesty rule is independent of the matrix size and is kept.
- Treat the harness as wasted: it is the right tool for the Observe recording and for any future
  distribution decision, and it cost no production change.

## Consequences

- No Hyper-V guest, test account, ISO, elevation, or display-topology work is planned.
- hide's own display and accessibility settings are changed only by hide, by hand, when hide
  chooses; the automation prohibition on changing them stands.
- Feature work from the charter's MVP list (sorting, drive and WSL-root discovery, UNC and
  removable media, cross-provider transfer) proceeds ahead of any UI release confirmation.
- The existing 125 percent evidence from Issues #70 and #100 remains valid for the unchanged
  implementation and is not repeated.

## Migration and removal

`docs/QUALITY_GATES.md` QLT-009, `docs/PROJECT_CHARTER.md`, `docs/KEYBOARD_MODEL.md`, and the
Issue #94 body are updated in the same change; `docs/DESIGN_HANDOFF.md` keeps its design
constraints because they describe what the token system must tolerate, not a test matrix.
`eng/ui-evidence/cells.json` keeps its `environment-required` cells as capabilities of the recorder,
not as requirements. Superseding this decision requires a new ADR that names who the product is
distributed to.

## Executable proof

No executable change. Conformance keeps the same rule count and identifiers. The confirmation,
when it runs, is recorded on Issue #94 with the Observe-mode evidence and hide's visual review.
