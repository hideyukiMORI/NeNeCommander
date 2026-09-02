# Design Handoff Contract

Status: normative

Claude Design or ChatGPT design tooling will supply the final visual direction. Engineering owns behavior, information hierarchy, accessibility, semantic resources, and integration constraints; the design tool owns the approved visual specification within those constraints.

## What engineering prepares

- A functional dual-pane layout with semantic regions and stable automation identifiers.
- Complete interaction states: inactive, active, focused, selected, busy, disabled, warning, error, conflict, and destructive confirmation.
- A semantic resource dictionary whose keys describe purpose rather than a literal value.
- Localization-ready text, keyboard hints from the canonical key map, and representative long strings.
- Windows light, dark, high-contrast, 100–300% scaling, narrow-window, and touch-target constraints.
- A fixture showing local, UNC, and WSL paths, long names, hidden items, permissions, progress, and partial failures.

## What the design handoff must return

- Component inventory and annotated states.
- Token values for color, typography, spacing, radii, elevation, density, and motion.
- Pane hierarchy, focus treatment, selection treatment, progress and error treatment.
- Assets with source, license, export dimensions, and high-contrast behavior.
- Responsive and DPI behavior plus accessibility notes.

## Integration law

1. Token values are changed in theme resource dictionaries, not copied into views.
2. Views bind to semantic keys such as `PaneActiveSurfaceBrush`; keys such as `Blue500` are prohibited at the feature layer.
3. A design change may not change command semantics, focus order, keyboard mappings, safety confirmation, or provider policy without a separate product ADR.
4. Generated raster mockups are references, not implementation assets, unless explicitly exported and licensed for use.
5. The initial implementation uses neutral placeholder token values. It must not invent a competing brand system.

## Required semantic token families

`Surface`, `Text`, `Border`, `Focus`, `Selection`, `Status`, `Operation`, `Spacing`, `Typography`, `Radius`, `Elevation`, `Density`, and `Motion` are the only top-level token families. Adding a family requires updating the design fixture and token conformance test.
