# Daily report — runtime UI proof — 2026-09-05

Status: informational

## Scope

Issue #70 audits the outstanding WinUI release-matrix evidence without changing product behavior. It reuses the already built and tested Release executable from Issue #68 and performs only app-owned normal-desktop launches, app-window resize, read-only UI Automation inspection, and screenshot capture.

## Invariant and canonical mechanism

The proof touches only NeNe Commander and its own launched processes. It sends no keyboard input, changes no machine-wide accessibility/display setting, and closes no unrelated process. The original settings document is restored byte-for-byte. Product states remain reachable only through `KeyboardIntentMapper` → `DualPaneSession` → `DualPanePresenter` → `CommanderWindow`; no proof-only production command path was added.

## Runtime results

- Environment: window DPI 120 (125% scale); Windows high contrast was off.
- Narrow layout: every scheme retained a requested 900 × 600 dip window as 1125 × 750 physical pixels.
- UI Automation: both pane groups, addresses, statuses, and lists were present, on-screen, positive-sized, equal-height, and non-overlapping in every case.
- Color schemes: `nene-dark`, `ubuntu`, `monokai`, `solarized-dark`, `solarized-light`, `dracula`, `nene-black`, and `nene-light` all launched and rendered distinct screenshots. All eight were visually inspected; both panes, row focus, addresses, status text, and bottom key hints remained readable without clipping.
- Screenshot SHA-256 prefixes, in the order above: `558DCC79`, `87DDF611`, `BFD39B31`, `6445F94D`, `9EDE953B`, `684509B7`, `CDC496C4`, `FCBB36B`.
- Settings restoration: the SHA-256 before and after the proof was exactly `C417EF4E...E5B72`; every process terminated by the harness was one it launched.
- UIA command reachability: 91 elements were enumerated and none supported `InvokePattern`. The key-hint surface is descriptive, not an alternate command surface.

## Existing interaction-state proof

The exact product source had already passed all 400 tests with zero skips before this evidence-only branch. In particular, `DualPaneSessionTests` proves rename awaiting/submission, directory-name awaiting, delete confirmation, and running progress; `DualPanePresenterTests` proves the corresponding F2/F7/F8 modal presentation, operation tones, counts, and progress segments. Re-running the same tests after an evidence-only documentation edit would add no diagnostic value; the final canonical CI remains required.

## Explicit remaining environmental proof

- High contrast was not active. Enabling it would change the desktop-wide accessibility setting, so no high-contrast runtime claim is made.
- This machine supplied 125% DPI. The 100%, 150%, 200%, and 300% release matrix remains unexecuted; 125% must not be generalized to that matrix.
- F2, F7, F8, and F5/F6 progress states cannot be entered through app-limited UIA because the UI exposes no invokable command element. Synthetic keyboard input remains prohibited, so no actual-window interaction-state claim is made.

These are environmental proof gaps under QLT-009/QLT-011, not hidden test failures and not a product-specification decision. No product defect was observed in the executable states that were reached.

## Pending integration evidence

The documentation-only final head and latest base require the canonical Ready CI gate. Its identifier may be recorded in the PR body without a result-only commit.
