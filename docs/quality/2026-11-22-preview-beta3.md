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
