# Bookmark acceptance and current-main integration — 2026-11-22

hide exercised the previously verified candidate (PR402/addeb9e-equivalent
issue-495 binary) and accepted its current screen with release first. The
reactivation-keyboard report is not fixed or claimed reproduced. Next-version
Issues #523–#528 own reactivation focus, cyclic pane movement, palette width,
target-pane indication, Nib/Folio-aligned 125 themes and Japanese/English/Chinese
selection. Earlier statements that PR402 awaits hide acceptance are historical.

## Invariant and integration

The existing mapper/session/presenter and native input owners remain the only
command paths. Main800844c brings accepted ownership/reducer improvements and
the truncated-title tooltip. Only PROJECT_STATE conflicted; both histories were
kept. The two automatically merged CommanderWindow files add only the accepted
tooltip Loaded binding, initially disabled tooltip, named title TextBlock and
existing CommandPaletteView.BindTitleToolTip call. Bookmark and address input
ownership is unchanged. No dependency, gate, threshold or waiver is changed.

Selected ordinary integration proof exercises the interaction of the accepted
feature with current pane projections and operation ownership. Existing causal
and native proof remains attached to its original inputs; no new native or
human-perceived speed claim is made. A separate release deep review is required
by Issue522 and is not claimed here.

## Commands and outcomes

Executed from D:/NeNeCommander/worktrees/issue-398:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/bookmark-acceptance-20261122/run-combined.ps1
```

The preserved script and combined-commands.json contain every exact dotnet
argument array. Application, Presentation and Architecture test projects each
received locked Release restore and Release build (--no-restore, -m:2,
-p:UseSharedCompilation=false). Architecture builds the App consumer. Every
build has zero warnings/errors; locks and sources are unchanged by verification.

- Application: 30/30, CommanderSession bookmark ownership/navigation and address
  navigation, pane selection sessions and equal-sort projection boundaries.
- Presentation: 15/15, pane selection/visible ownership, bookmark rows and address
  editor presentation.
- App composition ownership: 26/26.

All 71 selected cases pass, no skips/failures. Prior broader feature/main evidence
is reused for unchanged inputs; no full local gate or hosted CI is repeated solely
for the merge. The release requires its own applicable deep review.

hide's screen process14492 was launched from the verified issue-495 output.
Its DLL hash is A4301CDBEBD68AA29725F4BB69A5639AD984331D9832E3F584EBA1D415F2E58B.
The process and shared settings remain owned by hide; cleanup must retain a live
referenced worktree. No mouse/key input or settings restoration was performed by
this integration.

Permanent evidence: D:/NeNeCommander/evidence/bookmark-acceptance-20261122.
The closing PR and completion record own actual main integration and cleanup.
Issue522 owns beta.3 packaging/publication; AppSDK PR517 and #491/#453 are separate.
