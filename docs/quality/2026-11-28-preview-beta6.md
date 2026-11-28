# Preview beta.6 release evidence

Issue: [#594](https://github.com/hideyukiMORI/NeNeCommander/issues/594)

Status: published and verified. [Preview 0.1.0-beta.6](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.6) was published at 2026-11-28 19:55:40 JST from accepted main e25186725441c8c81e16a5e09f38c9f3422bcb62; completed-task cleanup is confirmed.

## Scope and invariant

hide requested the next Preview release on 2026-11-28. The authorized work covers
the candidate source, PR/main integration, official packaging, GitHub prerelease,
download verification and completed-task cleanup. This includes the integrated
selection, loaded-name filtering and file-content overwrite features. Folder
merging, clipboard integration, recursive/fuzzy search and Store packaging remain
outside this release work.

FileOperationGateway remains the sole transfer path. Overwrite still requires the
frozen source, exact displayed consent, current held-file identity and shape,
pre-write recovery materials, verified content and conditional source removal.
Successful handle ownership transfers remain live; failures release only the
owned resources. Native filter callbacks may stop or replace a render owner, so
both select-all and the returned list-focus effect require the current owner.

The App project is the sole version owner, now 0.1.0-beta.6. CodeQL-related
refactoring expresses failure-only resource cleanup, explicit absent consent,
direct receipt selection and the existing native render-owner check. It does not
add another operation route or alter the overwrite contract. Test localization
now reads the shipped catalogs while keeping independent fixed expected sentences;
69 template/argument contracts cover all three locales.

## Verification selection and evidence

Evidence root: D:/NeNeCommander/evidence/release-beta6-20261128.
Completed working copy: D:/NeNeCommander/worktrees/issue-594 (removed after preservation).
Original temporary output: D:/NeNeCommander/outputs/release-beta6-20261128 (relocated below).
Permanent distribution: D:/NeNeCommander/releases/0.1.0-beta.6/candidate-20261128-01.

ADR-0061 and the November 28 within-layer procedure apply. Required release
freshness remains 96 hours. Unchanged stages are mapped to their original
successful source/test/dependency/tool inputs; a commit or packaging step does not
trigger another complete product gate. Individual stages are not advertised as a
new all-green invocation of eng/check.ps1 or eng/deep-review.ps1.

PowerShell runners below use pwsh -NoProfile -File and their absolute path under
the evidence root. Python reconcilers use the existing local Python interpreter.
Their JSON receipts preserve exact arguments and timestamps.

| Command or record | Result and exact scope |
| --- | --- |
| run-domain-freshness-02.ps1 | Fresh Domain tests, 100% branches, three adversarial repetitions and Complete mutation 97.47%; 27 inputs unchanged and normal consumer restored. Prior Domain proof had exceeded 96 hours. |
| Build/conformance 11, run-focused-12.ps1 | Build has zero warnings/errors; 112 normative rules; Application 193, Infrastructure 601 and Presentation 119 tests passed. Application 76 and Infrastructure 349 adversarial cases each passed three times. |
| run-codeql-14.ps1 | Exact 1,623 inputs; compiler-traced and canonical buildless analyses each execute the unchanged security-and-quality suite. Canonical SARIF has 164 rules and two findings: dead success-path null assignments in a test fixture. Original 34-finding analysis remains preserved. Supplemental compiler-traced findings and extraction limits remain separate. |
| run-mutation-units-15.ps1 | Application TransferExecution 95.70% passed in 59.85 seconds. Four Infrastructure owners failed 83.46% in 1,039.30 seconds; five Presentation owners failed 89.09% in 39.91 seconds. Original thresholds/operators and every failure are retained; normal consumers restored after each unit. These are selected units, not whole-project scores. |
| run-native-reentry-16.ps1 -Locale en-US -Attempt 2 | Four real WinUI conditions / 25 checks passed: ordinary focus/select-all, synchronous GettingFocus stop, native hint-setter stop and nested render replacement. All 187 current App inputs are unchanged; expected fixture substitutions only; four normal dependency DLLs match. Input desktop/settings unchanged. No physical keyboard, IME or visible acceptance claim. |
| native-reentry-reconciliation-23.json | Exact native source/dependency correspondence and initial failed GotFocus assumption retained. GotFocus did not fire synchronously in that fixture; GettingFocus establishes the required actual synchronous callback. Fixture build failures and corrections remain recorded; analyzers were not disabled. |
| boundary-tests-29.json, boundary-infrastructure-30.log | Presentation 17 passed. Infrastructure initially 47/48 passed; DeleteOnClose alone did not establish delete-pending. After a real deletion request, both affected held-file tests passed. The original failure is retained. |
| run-final-scoped-31.ps1 | Current-owner/direct-consumer selection, including previously omitted post-content consumers: Infrastructure 405 and Presentation 152 tests passed with coverage; new adversarial cases repeated three times; format and conformance passed; 1,628 inputs unchanged. |
| selected-unit-coverage-corrected-31.json | Infrastructure owner files cover 57/62 branches (91.94%); Presentation files 154/158 (97.47%). The first custom parser misread Cobertura's capitalized boolean and is explicitly invalidated; corrected totals are cross-checked against all original XML branch totals. XML reports are unchanged. |
| run-final-correction-33.ps1 | Independent review found a 42-logical-line cancellation test. It is split into independent before/after cases with the same assertions; both pass with coverage and three adversarial repetitions. The direct provider-cancellation tests omitted by the earlier Application name filter are included. Final format/conformance pass; complexity retains only the pre-existing Help nesting finding. |
| CodeQL 26 / reconciliation 36 and 38 | All 164 rules execute with zero results. All 1,419 owned C# files match the source archive, and all 1,628 proof inputs and their path set match the current tree. Original diagnostics and failed earlier analyses remain. |
| Final mutation 34 | Infrastructure failed 88.9763779527559% after 15m09s; Presentation passed 94.54545455% after 49s. Both normal consumers restored; the original failed report is retained. |
| run-final-boundary-41.ps1 / coverage47 | The exact query-null refusal is delegated to the first existing owner factory, before parent-path admission. Same/foreign-parent refusal and real no-follow junction reason/handle/target proof pass. App/test builds have zero warnings/errors; 408 direct-consumer tests pass; the junction adversarial case passes three times; selected owner coverage remains 57/62 (91.94%). |
| run-mutation-final-42.ps1 / final-release-evidence50 | Infrastructure's unchanged four-owner scope passes 90.47619048% at the original 90% threshold. All final 1,628 inputs are unchanged and the normal consumer is restored. Application15 and Presentation34 are reused with exact relevant source/test/resource/tool path sets and hashes. |
| run-codeql-final-43.ps1 / reconcile-codeql-44.py | Final three changed inputs are included. All 164 rules execute with zero results; all 1,419 owned C# source-archive entries match. The 115 compilation-error, 18 extraction-error and 315 extraction-message diagnostics are unchanged from26. |

The final read-only review also accepts the equivalent query-null delegation and
its refusal-order/native-junction tests; no required correction remains.

The source-level independent review accepted cleanup ownership, nullable consent
and the preserved render guard. The later review accepted the added native
lifetime, cancellation, refusal and argument tests after the cancellation-test
split. CodeQL36 retains the identical normalized 115 compilation-error and 18
extraction-error diagnostics; seven failed-resolution/type notices disappear in
the corrected catalog tests. The 1,000 observed duplicate-type CS0436 notices
disappear in the clean snapshot with seven hash-verified existing references;
CS1701 remains capped at 1,000 observations. No complete-extraction claim is made.
Compiler-traced14 remains supplemental; ten changed/new C# test files differ from
that earlier snapshot, corrected explicitly in reconciliation38.

Test inspection of retained handles is read-only and bounded to proving
that the drive-root handle closes, which file writer/rename probes alone cannot
show. No threshold, exclusion, suppression or baseline is added.

The unchanged proof ledger from overwrite acceptance is
D:/NeNeCommander/evidence/transfer-conflicts-20261128/final-evidence-ledger-232.json.
Reconciliation38 verifies 1,590 unchanged prior inputs, 30 changed inputs and
eight added tests/helpers, with every difference mapped to current proof. All
15 dependency pin/lock inputs and shared tooling are unchanged. The two changed
project files change only the App version and embedded test catalogs; they add no
package or project dependency.

Its original failures and environmental limits remain. Successful unchanged
Application/Presentation/Infrastructure units are reused only with the recorded
input/consumer correspondence. The new owner changes use the focused tests,
native boundary observations and affected mutation above.

The final evidence ledger50 verifies the remaining three-input correction and
actual release freshness. The earliest applicable positive stage is November27
16:22 JST, within96hours; Domain02 is fresh. Positive stages inside earlier failed
or interrupted commands are identified individually. No new single successful
full-gate invocation or synthetic combined mutation score is claimed.

## Published release and completed cleanup

[PR #595](https://github.com/hideyukiMORI/NeNeCommander/pull/595) was squash-merged
as e25186725441c8c81e16a5e09f38c9f3422bcb62, exactly tree-equal to reviewed candidate
175ebd7be7f5dafbdda41256e9a1e564dc89c1c1. Candidate and main CodeQL analyses
2267898755 / 2267904411 each completed with 164 rules, zero results and zero open
alerts in API read-back. The dependency-review workflow 46136970508 succeeded.
No full quality/deep-review workflow was started for unchanged inputs.

| Command or record | Observed result |
| --- | --- |
| eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/outputs/release-beta6-20261128/candidate-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/release-beta6-20261128/Windows-SDK-license.rtf | Official locked restore/publish from clean accepted source; exit 0 in 84.5 seconds. The cache is the pinned original from the prior official ZIP, hash-verified in record 46. |
| verify-candidate.ps1 / candidate-verified-58.json | All 588 payload and ZIP files, 2,129 tracked source inputs, ten lock files, 51 original package notice files across 21 packages, executable metadata, 132 embedded XBF resources and 125 theme attributions match. |
| verify-extracted-final.ps1 / extraction-verified-58.json | Independent extraction: 588 files / 300,938,115 bytes, zero mismatches. |
| verify-pri-locales.ps1 / candidate-01-pri-locales.json | All 690 resources in each of en-US, ja-JP and zh-Hans match the final source catalog; zero mismatches. |
| verify-unmodified-startup.ps1 / startup-verified-59.json | The unmodified extracted binary creates a visible responsive WinUI window on its owned non-input desktop. First response 836.8 ms, two observations at least 3 seconds apart; owned child terminated and desktop closed. Input desktop and settings unchanged. This is not physical input or normal-shutdown proof. |
| release-preflight60 / release-create61 / published-tag62 | Annotated v0.1.0-beta.6 points to the exact accepted source; GitHub prerelease has exactly the verified ZIP/manifest/checksum assets. |
| published-assets-63.json | Unauthenticated public asset URLs are downloaded again. All three SHA-256 values and sizes match their pre-publication originals. ZIP 114,434,436 bytes, SHA-256 955e54f06b20d5551a8297d6ca5b77ea2ec50c9fc19e5fc2169165414e9421e9. |
| permanent-release-64.json | The three public bytes are preserved under D:/NeNeCommander/releases/0.1.0-beta.6/candidate-20261128-01 and their hashes rechecked. |
| preserve-original-trees-65.ps1 / cleanup-preserved-worktree-66.ps1 | All 47,571 files / 4,044,696,998 bytes and five link definitions are preserved by 23 same-volume NTFS directory renames. Every root ID and complete relative metadata inventory matches. The clean accepted worktree is removed; both original task paths are absent. Branches/commits and unrelated unfinished work remain. |

Permanent raw material is below
D:/NeNeCommander/evidence/release-beta6-20261128/completed-release-files-65.
The old output prefix maps to its outputs/release-beta6-20261128 child; generated
worktree roots map to worktree-ignored followed by their original relative path.
Earlier JSON/log paths remain historical observations. The original directory
objects and link definitions are retained, with no copied-byte hash claim for
this relocation and no guarantee that absolute old link targets remain reachable.
There is no cleanup approval hold. Completion documentation changes no product
input and starts no CI or product test.

Issue #94's one-time maintainer check was completed on November 19; its exact
display scale and independent captures were not recorded. Current physical
keyboard/IME/screen acceptance, every live provider environment and power-loss
durability are not established by the isolated native tests. Generated/compiler
analysis diagnostics and the original Help nesting finding are recorded
separately, not suppressed or described as new regressions.
