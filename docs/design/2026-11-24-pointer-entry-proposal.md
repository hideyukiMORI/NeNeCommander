# Pointer entry design reconciliation — Issue530

Status: visual proposal; technical mechanism accepted and locally implemented in
ADR0090 with selected owning/control proof recorded. No visible acceptance.

The public Issue requests beginner-friendly file-list clicking and recognizable
Bookmarks/Settings/Help entry points. Right-click and drag/drop remain separate.
Read-only inspection uses accepted mainaf17755; beta.4's frozen candidate is not
changed. [ADR-0090](../adr/0090-pointer-pane-interaction.md) owns proposed behavior.

## Consultation evidence

At08:33 JST the background /design command returns: it manages project access and
produced no design. No consent/revoke command is executed. The following ordinary
text consultation succeeds at08:35 JST with tools disabled, safe mode, empty MCP
configuration, no file access and no session persistence. It receives only public
Issue requirements and general authored constraints, not repository source,
screenshots or private data. It is advice, not a Claude Design artifact.

Permanent evidence: D:/NeNeCommander/evidence/pointer-entry-20261124/
(design-brief.txt, design-consult-brief.txt, both invocation/raw-result records,
design-payload.json and design-advice.txt). Working output remains in the task's
D output directory. No model version is made part of this design contract.

## Proposed adoption and corrections

| Advice | Engineering disposition |
|---|---|
| One trailing strip, Bookmarks then Settings then Help | Proposed before the existing caption controls; retain the caption adapter's bounded drag area. No per-pane duplicate. |
| Existing icon set, semantic colors and localized names | Reuse framework symbols and current token families; exact fit/states still need host proof. |
| Plain/Ctrl/Shift/Ctrl+Shift row selection and blank-space clearing | Captured-owner intent and sole reducer, with explicit anchor lifetime in ADR-0090. |
| Double click opens only the same row | Require same listing/entry between gestures and current admission; reuse the existing OpenFocused route. |
| One roving Tab group with arrows | Not adopted as a private map. Ordinary Tab must keep pane switching; canonical F10 entry and native button Tab navigation are proposed. |
| Close returns to toolbar button | Retain each existing surface's captured-pane focus-return contract. |
| Scrim never dismisses, Escape prompts on drafts | Not adopted; each existing modal keeps its documented Escape/scrim behavior. Background pane input remains frozen. |
| Busy delay/progress timer | Not adopted; use existing work ownership/disabled projection without a new timing service. |
| New minimum width | Not adopted; measure the existing supported narrow geometry and adapt resources/layout if it does not fit. |

Current visual direction is retained. Active pane already has number/header/border
indicators; this issue must not restyle every row or reinterpret focus versus
explicit marks. The icon group is always present independently of footer-key-hint
visibility. Tooltips contain complete localized command labels plus canonical
shortcuts; accessible names identify commands, with shortcuts exposed separately.
High-contrast resources, focus rings and current theme switching remain required.

## Remaining evidence

Choose exact semantic sizes/glyphs only during the approved integration and verify
normal/hover/pressed/focus/disabled state, caption hit-test separation, long labels
in three locales, existing minimum/narrow bounds, large text/DPI and hidden-footer
mode. Do not claim native pointer/keyboard/focus, screen-reader speech or actual
visible acceptance from pure tests or unshown control invocation.

## hide feedback adopted at22:43 JST

Replace the three text entries with original vector bookmark/gear/help icons in
square DensityRowHeight targets; use DensityKindIconSize artwork and existing
native button focus/hover/pressed/disabled resources. Localized names and canonical
shortcut tooltips stay unchanged. This follows the earlier consultation and hide's
explicit request. Keep the caption drag and system controls separate.

Each pane receives an upward arrow before its path, using the existing semantic
PaneNavigationButtonStyle/target/icon dimensions. Its localized name is「上の階層へ」
(Japanese), “Go to parent folder” (English), “转到上级文件夹” (Simplified Chinese),
with the canonical Alt+Up tooltip. It addresses that pane through ADR0090's qualified
route. Roots/absent content and busy operation/read states disable it; all modal
owners still reject it. This amendment adds no drag-and-drop or new visual theme.
