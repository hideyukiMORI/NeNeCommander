# Footer wrapping and normal key hints

Status: implementation handoff for Issue #454

Date: 2026-11-19

hide reported the right side of the footer disappearing at narrow widths and requested
natural multiple rows plus a setting for key-help visibility. The earlier Bookmark
consultation agreed on reuse of KeyHintWrapPanel with all hints visible before any
priority cap. The follow-up consultation inspected normal main 189bb61 and the exact
OperationBar, panel, hint presenter and settings keyboard contract.

## Concrete layout

OperationContent owns an information row followed by a hint row, each receiving the
available footer width. The border uses the existing DensityOperationBarHeight as a
minimum. Empty idle information consumes no row. Information flows in this order:
status with icon, name entry, progress segments, completed/total count and transfer
result pairs. Long status wraps without ellipsis. Result label/count pairs remain
together; their container can wrap. Collapsed elements consume no gap.

Key hints retain the existing key-cap template, semantic resources and canonical order.
The block remains right aligned and wraps at complete hint boundaries. There is no fixed
row count or hidden priority. Wide idle windows retain a single row when the content fits.
The panel measures children with finite width; ordinary key hints retain natural width.
No new color, size, gap or density value is introduced.

The settings checkbox reads "Show key hints in the footer" / "フッターにキー案内を表示".
Its visible and accessible explanation states that F1 and Enter/Esc for confirmation
or name entry remain. Hidden filters only normal footer hints to canonical OpenHelp;
it does not suppress operation progress, results, warnings or other transient surfaces.
Settings movement follows ADR-0077, adding this checkbox after the launch-hidden choice.
[ADR-0082](../adr/0082-footer-key-hint-visibility.md) owns behavior and schema version 4.

## Consultation provenance and corrections

The actual NeNe Commanderリナ resumed the prior local Claude consultation and returned
a read-only review with no permission denials. Her four corrections were: collapse the
empty upper row, wrap upper information as well as keys, retain right alignment, and amend
ADR-0077's exact focus order. The implementation adopts these corrections. This is design
agreement, not hide's native acceptance. The off-state exception will be explicit in the
screen-review record rather than hidden from the user.

The previous /design invocation was an access-management command in the installed CLI,
not a design consultation skill. The continued design critique/accessibility/handoff
consultation used local source and previously identified shell images. No cloud design
access was changed. A prior reference to HERO's 125/150/175% environment was withdrawn
as unrelated; it is not evidence for this project.

Raw prompt and response: D:/NeNeCommander/outputs/footer-20261119/ and
D:/NeNeCommander/evidence/footer-20261119/. The response's idle-empty statement is
independently consistent with the empty OperationStatusIdle resource. Its estimate that
the name-entry width fits 900 DIP remains an estimate until measured.

## Native acceptance plan

Observe Japanese and English at 900×600 and a wider size, at 100% and 200% and in high
contrast, including idle, running, completed, confirmation and long failure text.
Record footer and remaining file-list bounds, each hint's visibility, complete result
pairs, native name input, and the shared window-adjustment wrapping. Toggle via j/k or
arrows plus Space/Enter; verify immediate change, preserved F1 and confirmation guidance,
save failure warning, restart and return to prior setting. Check the explanatory text in UIA
and Narrator. Do not infer any of these results from source tests.

The [focused quality record](../quality/2026-11-19-footer-key-hints.md) now records
Japanese/Monokai native observations at wide and 954×658-pixel windows: complete normal
hints wrapping into two rows, the F1-only choice, native name input and Enter/Esc,
empty-pane focus return, running progress and completed result pairs. Settings opening
does not save; Space/Enter change the choice and restart retains it. Exact desktop
scale, English, high contrast, Narrator and long failure text remain separate limits.
The owned test app was closed and original settings bytes restored; the normal app remains.
