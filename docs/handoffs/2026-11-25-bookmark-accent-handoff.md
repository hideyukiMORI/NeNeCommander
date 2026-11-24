# Bookmark accent continuation — 2026-11-25

Status: active; hide resumed at07:06 JST and has not stopped the current session.
The earlier01:15stop is historical. Current work fixes Issue581 before returning
to the approved Issue580 beta.5 release cutoff. The [quality record](../quality/2026-11-25-bookmark-accent.md)
owns all attempts and exact commands. No new feature is added to the cutoff.

## Current source and scope

Worktree: D:/NeNeCommander/worktrees/issue-581, branch fix/581-bookmark-focus-about,
base307eb83. Current product delta is three App files: BookmarkBrowseView,
BookmarkManagerView and CommanderWindow.xaml.cs. Existing B heading design,
padding/search accents, About and canonical mapper/session routes remain.
C main is cleanccb303b; the prepared issue580 release worktree is clean8d1c247.
There is no new feature push/PR/main integration/tag/release yet.

The cause is measured: marked-row replacement delivers old-region native focus
notifications after the requested heading has painted. Registering the existing
request before repaint and ignoring incidental focus during that request removes
the return. Independent review also exposed two boundaries, both reproduced:

1. Same-current-region pointer cancellation can leave the heading/context ahead
   of actual focus without another GotFocus. Cancellation now reconciles actual
   search/category/list focus through the existing region notification route.
2. Pre-map cancellation of every Tab makes two rapid mapped Tabs act as one after
   reconciliation. Initial Tab cancellation now belongs to the existing native
   input notification after the canonical Map has yielded. The same KeyboardInput
   reaches all three existing host notification sites. Pending Enter protection stays.

No new timer, animation, mapping table, input route, dependency or other layer
source is added. No design consultation is needed to restore the already accepted
no-motion behavior.

## Evidence and remaining actions

Evidence: D:/NeNeCommander/evidence/bookmark-accent-20261125.
Diagnostic sources: D:/NeNeCommander/outputs/bookmark-accent-20261125.
Baseline and all failed receipts are retained. Native5 and10 have causal failures;
their failing observer exit also times out, so the bounded launcher terminates only
its exact owned child. This is not a successful shutdown claim.

- Product build03 and diagnostic07:0warnings/errors.
- Native11 host-order boundaries116checks; native12 same-region50; native13 other
  cancellation paths156; all exit0 and retain normal settings/input desktop.
- Prior native8 draft/live-language64 and native9paint168images pass. Production
  paint code/XAML is unchanged since9; final14/15 use actual host ordering for both empty/populated lists and pass
  chronological verifier02:336images/28fixed geometry phases. Search may make one
  old-to-inactive change before native GotFocus; no later old state or reversal is allowed.
- Formatting02(Browse) and03(Manager/host) pass. Complexity03 is the exact same
  three pre-existing finding objects. Normal Commit hooks remain enabled.
- Final source review requires no correction. final-source-parity-03 freezes182App
  inputs/180exact copies/4matchingDLLs/5fixtures. Final11–15 give366checks. Commit
  with the normal hook, then integrate into issue580. Keep original failed observer
  and verifier receipts; see quality for their corrected observation assumptions.

Next release work: use the accepted interactive02 feature scope plus the requested
581 correction. Keep#532drag/drop and broad#564remaining work out. Reconcile changed
release proof inputs, reuse successful unchanged stages under ADR0061, and complete
required final coverage/mutation/CodeQL/ref alerts/package/extracted startup/freshness
before any release claim. Read current GitHub state and final concrete payload before
external writes; old public beta.4 permissions do not authorize a different payload.

## Shared environment and preservation

No blue CUA, shared-desktop key/mouse injection, ordinary settings modification or
other application termination. All probes use the named non-input desktop and
owned settings/paths. They are native-control/render evidence, not physical input
or hide's visible acceptance. Do not reuse old PIDs or stop existing trials by name.
Interactive04 is still uncreated. Keep interactive03 and all unfinished feature,
release and diagnostic copies. Cleanup follows integration and verified permanent
preservation; no cleanup has been rejected or is due in this unfinished step.