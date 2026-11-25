# Pane name-filter progress report — 2026-11-25

Issue #545 now filters each pane's already-loaded names through its existing reducer.
Produced `/`, the header and palette open one native inline editor; Enter keeps the
latest committed condition, editor Escape clears it, and ordinary blur keeps it without
stealing focus. The same visible projection owns focus, selection and operation targets.
The feature remains separate from beta.5 and is not published or merged.

Technical acceptance includes Application 1,273 cases with 100% branch coverage,
Presentation 570 with 97.422969%, 35 affected architecture cases, normal App build,
conformance/record closure and three repeated filter adversarial runs. Independent
review has no required correction. Native tests exposed and causally corrected Enter
focus loss and pending TextChanged overwrite/exit loss. Final isolated native scenarios
and wide/narrow Japanese, English and Chinese images pass; no shared desktop input or
normal-settings change occurs. Physical-key and actual-IME acceptance remain unrun.

At the 10,000-entry listing boundary with every entry selected, measured parse/reducer
transitions have local medians of 0.20–5.31 ms and p95 of 0.22–7.67 ms across four query
shapes. This excludes XAML rendering and is not a total keystroke-latency claim.

The [quality record](../quality/2026-11-25-pane-name-filter.md) preserves every failed
attempt, exact command and source/diagnostic parity. The [handoff](../handoffs/2026-11-25-pane-name-filter-handoff.md)
records the invariant, ownership changes, unfinished publication/integration and
retained D work. No cleanup is due for this unfinished task.
