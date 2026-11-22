# Preview beta.3 preparation — 2026-11-22

Issue #522 follows hide's instruction to release the accepted current product
before six next-version changes. PR402 merged as08c2d9c with the identical
accepted3764d7e tree. [Bookmark acceptance](2026-11-22-bookmark-acceptance.md)
records the71 selected integration cases and the separate screen decision.

The single App Version becomes0.1.0-beta.3. The distribution README explains
display-only paths, Ctrl+L and keyboard Bookmark management, and discloses the
reported reactivation keyboard issue (#523). No production behavior, dependency,
gate, threshold or waiver changes are introduced by release preparation.

## Required release proof

Request one security-deep-review.yml on this candidate. It owns the canonical
gate, dependency audit, repeated adversarial cases and all protected mutation
layers. Verify successful CodeQL analysis plus zero open owned alerts separately.
QLT-013/SEC-009 require applicable success within96 hours. Do not duplicate full
quality. Dependency review is selected because the version changes a csproj.

The #94 one-time display/keyboard/high-contrast check is complete according to
hide's recorded November19 report; its exact selected scale was not recorded.
The November22 PR402 screen acceptance is distinct and now complete. New-host,
all-provider live and all-locale visual checks are not claimed.

Generate the shipping candidate only from accepted clean source through:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

Independently verify allpayload/ZIP entries, trackedinputs/locks, notices, PRI
locales/XBF, runtime/version/signature and extracted files. Publication requires
an exact-source tag and Preview prerelease with ZIP, manifest and checksums;
download public assets again and compare hashes. The subsequent completion
record owns actual commands/results/publication, never this preparation plan.

## Scope and retained work

Issues523–528 defer reactivation focus, cyclic pane focus, narrower palette,
one target-pane indication,125 Nib/Folio themes and selectable Japanese/English/
Chinese. AppSDK PR517 and native timing Issues491/453 remain separate.

Worktree: D:/NeNeCommander/worktrees/issue-522.
Temporary output: D:/NeNeCommander/outputs/release-beta3-20261122.
Permanent evidence: D:/NeNeCommander/evidence/release-beta3-20261122.
Official candidates: D:/NeNeCommander/releases/0.1.0-beta.3.
Keep current user app14492/issue-495 and shared settings. After integration and
complete artifact preservation, clean only exact completed task targets after
checking tracked/untracked/ignored files, unique artifacts, links and live refs.

## First release review and focused coverage correction

Deep review44934477990 on a215ef2 completed unsuccessfully. Canonical behavior
tests passed, but Application branch coverage was99.93% instead of required100%.
The one Application branch was ActivePaneBookmarkDefaults.From rejecting an
overlong folder leaf as a Bookmark display name. CodeQL's analysis step completed;
this does not replace the failed deep review. Its full log and report remain
under first-deep-review in the permanent evidence directory.

Issue549 adds two public CommanderSession registration cases for128/129-character
folder leaves. They assert the accepted/empty default name, complete original
path, active pane, single read and absence of settings writes. Product source is
unchanged. Seven registration cases pass; the Application layer now passes843/843
and records branch-rate1 with the same coverage.settings. No threshold, exclusion,
baseline or waiver changes. Changed-file formatting and the zero-warning/error
Application test build pass.

Commands executed from the release worktree:

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter FullyQualifiedName~CommanderSessionBookmarkRegistrationTests
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --coverage --coverage-settings eng/coverage.settings --coverage-output-format cobertura --coverage-output D:/NeNeCommander/evidence/release-beta3-20261122/application-after-fix.cobertura.xml
dotnet format whitespace tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --no-restore --include tests/NeNeCommander.Application.Tests/CommanderSessionBookmarkRegistrationTests.cs --verify-no-changes
```

The before-fix coverage uses the same arguments with application-before-fix as
the output name. It passes841 behavior cases while reproducing the coverage gap.
Only the failed Application layer was rerun locally to identify/close this gap.
One new release deep review is required for the changed test inputs.

hide also reported post-F6 focus returning to the first row; Issue550 owns the
next-version adjacent-item recovery. Issues530–548 hold the subsequent mouse,
context menu, drag/drop, previews, information, search, animation, layout,
system-monitor, archive, creation and navigation requests. None is silently
included in this shipping candidate.

The completed Bookmark worktrees398/430/431/445/457 were removed at03:34:45 after
all7004 ignored artifacts/1,582,125,646bytes were archived and every entry verified.
Branches/commits/evidence remain. Worktree495 stays retained for hide's live
screen-review app14492 and associated unfinished runtime cleanup.

## Reporting checkpoint

The November22 03:46 JST [daily report](../reports/2026-11-22-preview-beta3-daily-report.md) and [handoff](../handoffs/2026-11-22-preview-beta3-handoff.md) consolidate this release and all28 future requests. Monitor movement551 and global visibility/foreground hotkey552 are recorded after release preparation; no product change is included. Second deep review44936588484 on9ca919d remains in progress at the checkpoint. The documentation-only follow-up preserves all reviewed product/test/dependency/tool inputs; reuse successful evidence under ADR-0061. Actual release publication and final artifact proof remain pending.

## Resumed release review

The second deep review passed, but candidate CodeQL alerts154–156 remain open.
Issue553 records their source corrections, focused proof and required
supplemental validation in [the correction record](2026-11-22-release-codeql-corrections.md).
Default-branch0 alerts did not establish candidate acceptance. Publication remains pending.


Local supplemental proof is complete at15:06 JST. Original CodeQL reports0 findings;
remote upload/readback and publication remain held for hide's explicit payload
authorization after automatic approval rejection. The correction record and latest
handoff section contain the exact payload, diagnostic limits and remaining steps.
