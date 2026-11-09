# Keyboard help engineering handoff

Status: approved direction A for Issue #359

Date: 2026-11-09

This handoff prepares the design consultation for the `F1` keyboard help surface decided in
[ADR-0076](../adr/0076-keyboard-help-surface.md). Engineering owns the content, behavior,
accessibility and integration constraints below. The visual direction is chosen by hide; final
token values follow [DESIGN_HANDOFF.md](../DESIGN_HANDOFF.md).

## Content inventory

Generated from the canonical key map; nothing is hand-written.

1. Title: `キー操作` / `Keyboard shortcuts`, with a one-line note `Esc または F1 で閉じる`.
2. Groups, in key-map order, each with a localized heading:
   - 移動 (Normal-mode movement): `j`/`Down`, `k`/`Up`, `h`/`Backspace`/`Alt+Up`, `l`/`Enter`,
     `Alt+Left`, `Alt+Right`, `gg`, `G`, `Ctrl+D`/`PageDown`, `Ctrl+U`/`PageUp`, `Tab`, `Space`,
     `Ctrl+H`, `Ctrl+F3`–`Ctrl+F6`, `Escape`.
   - ファイル操作 (File commands): `F2`, `F5`, `F6`, `F7`, `F8`, `Ctrl+L`, `Ctrl+R`, `Ctrl+,`,
     `Ctrl+P`, `Ctrl+B`, `Ctrl+W`, `Ctrl+G`, `Ctrl+1`–`Ctrl+9`, `F1`.
   - ウィンドウ調整 (Window adjustment, `Ctrl+W`): `h`/`j`/`k`/`l` and arrows, `+`, `-`, `m`, `r`,
     `Escape`/`Ctrl+W`.
   - 場所の選択 (Locations picker, `Ctrl+G`): `j`/`Down`, `k`/`Up`, `Enter`, `Escape`.
   - コマンドパレット (Command palette, `Ctrl+P`): its declared keys.
3. Each row: one or more key caps (aliases share a row) and the localized action label. Key caps
   reuse the operation bar's key-cap treatment.

Representative long strings: the longest Japanese action label and `Ctrl+F3`–`Ctrl+F6` ranges must
fit at the narrow window size without clipping.

## States and behavior

Closed and open only. No search, selection, execution, animation, busy state or error state. Opens
only from the file list or navigation surface (same admission as `Ctrl+P`). While open both panes
are frozen and visible behind the transparent scrim. `Escape`, `F1` and a scrim tap close it.
Scrolling keys: `Up`/`Down`, `j`/`k`, `PageUp`/`PageDown`, `Ctrl+U`/`Ctrl+D`. Focus returns to the
file list of the pane active at entry.

## Accessibility and environment constraints

- Automation identifier `KeyboardHelp`; each row exposes key text then action text.
- Light, dark, high contrast, 100–300 % scaling and the narrow window (900×600 DIP) must remain
  readable; the scrolling region is the only focus stop.
- Colors only through existing semantic tokens (`SurfacePane`, `TextPrimary`, `TextSecondary`,
  `TextKeyHint`, `BorderSubtle`, `FocusRing`); no new color token family.

## Selected direction

hide selected direction A on 2026-11-09: a centered raised panel on the transparent scrim, like the
settings modal. At normal width the groups flow in two columns (movement and window adjustment on
the left, file commands, Locations picker and command palette on the right); below the narrow
breakpoint used by the operation bar they stack in one column. The panel scrolls vertically, its
height is capped by the window with the window's outer spacing, and the title and the close note
stay fixed while the groups scroll. The panel reuses the settings modal's surface, border, corner
radius and padding tokens and the operation bar's key-cap treatment.

## Directions considered

- **A. Centered panel (recommended).** A raised panel in the middle of the window like the settings
  modal, two columns of groups at normal width, one column at narrow width, vertical scroll.
  Closest to existing modals; reads as a reference sheet.
- **B. Right-side drawer.** A full-height panel on the right third of the window, one column,
  vertical scroll. The left pane stays fully visible, so the help can be read next to the files it
  describes.
- **C. Top compact sheet.** A palette-like sheet near the top of the window with groups as
  horizontal tabs switched by `Left`/`Right` (adds keys). Smallest footprint, but hides most rows
  behind tabs.

B and C were not selected.
