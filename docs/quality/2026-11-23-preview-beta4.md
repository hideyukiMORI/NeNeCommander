# Preview beta.4 preparation — Issue571

Status: preparing; no new tag or release has been published.

hide requested release preparation on2026-11-23 after accepting the theme picker,
immediate language switching, compact palette/context, window placement and the
left/right reproduction correction for Issue523. The frozen starting product is
bf7938e. Mainb82aa1e already contains cyclic focus, adjacent post-move focus and
direct pane counts. Other unaccepted feature branches and AppSDK PR517 are excluded.

## Invariant and scope

Existing focused PRs remain the integration boundaries and use ordinary squash
merges. The release source must preserve the accepted product tree. The single App
Version and distribution README change for beta.4, including schema6 and the old
binary incompatibility. No runtime route, dependency, threshold or waiver changes.
Only eng/publish-release.ps1 creates a clean-source shipping candidate.

QLT-013/SEC-009 require applicable successful deep-review evidence within96hours;
the new settings/theme/window contracts and shared consumers require fresh combined
release proof. One security-deep-review dispatch includes the canonical gate,
coverage, adversarial checks, dependency audit and mutation. Do not also run quality
for unchanged inputs. CodeQL completion and zero open owned alerts are read back
for the actual candidate ref; default-main0 alone is insufficient.

The accepted focused tests and native observations remain reusable and keep their
original limits. Issue94 is closed with hide's November19 report; the exact display
scale and independent captures were not recorded. No new shared-desktop automation,
screen capture, input injection or user settings restoration is authorized here.

## Preparation and remaining work

Dedicated source: D:/NeNeCommander/worktrees/issue-571,
branch chore/571-preview-beta4. Permanent proof:
D:/NeNeCommander/evidence/release-beta4-20261123.

Initial readback: mainb82aa1e; PR559/565/568/569/570 are open Drafts with expected
heads91e06f5/6182b0d/510d53de/856cb6f/fa06b21, no reviews or unresolved threads.
The GitHub connector cannot create issues (403 integration permission); the existing
authenticated gh created Issue571 with the same prepared body. This is a connector
permission limit, not an automatic approval rejection.

Version/README preparation, final-source proof, focused-PR integration, release deep
review, CodeQL readback, package/ZIP/PRI/notice/source comparison and completion
cleanup remain to be recorded from actual results. Packaging alone is not publication.
Current App/settings and beta.3 assets remain separate. Unfinished worktrees/outputs
are retained; no cleanup rejection or hide cleanup request exists at this checkpoint.

## Focused integration and release run

All six feature PRs use ordinary exact-head squash merges without an admin bypass,
force push or branch deletion. Their existing focused proof and hide acceptance are
recorded in the final PR descriptions. No review or unresolved thread was present.
The local main is synchronized to e0a0616 and is clean.

| PR | Frozen product head | Accepted main | Tree comparison |
|---|---|---|---|
|559 language|91e06f5|5e92f70|equal|
|565 themes|6182b0d|f2716d2|equal|
|568 compact palette/context|510d53de|7ad6e67|equal|
|569 immediate language|856cb6f|f0ff65e|equal|
|570 saved placement|fa06b21|72d9fad|equal|
|572 reactivation input|bf7938e|e0a0616|equal|

After each squash, the next stacked head contains the complete original predecessor.
Its predecessor tree is exactly the newly accepted main tree. Only after checking
both facts, an ancestry-only `git merge --strategy=ours --no-ff --no-commit origin/main`
connects that head to main. The normal commit hooks run; before/after trees must be
identical. This does not resolve differing product files by discarding main changes.
PR connection/ready/merged/tree-proof JSON and the exact guarded scripts are retained
in the permanent evidence directory. The preparation script first failed on an empty
PowerShell result's Count under StrictMode, then rejected a mistyped expected SHA;
neither attempt reached mutation. The corrected invocation passed every guard.

Issue523 is published as PR572. Release PR573 remains Draft and now targets main.
Its initial9121dc0 and connected2af817e have the identical complete tree; only the
version, README and this release record differ from the accepted product.

```powershell
gh workflow run security-deep-review.yml --repo hideyukiMORI/NeNeCommander --ref chore/571-preview-beta4
```

Run45188453598 started at22:53:43 JST on9121dc0. CodeQL initialization succeeded;
the deep-review step is still running at23:01 JST. No quality workflow was dispatched.
Later history/document-only updates do not invalidate its unchanged code/test/tool/
dependency inputs. The results and necessary corrections are recorded below.

## First release failure and corrections

Run45188453598 failed: the canonical tests found two stale Application fixtures.
The theme command increased the catalog to24; the bookmark ownership assertion
still expected23. The reflective UserSettings.Create guard supplied three arguments
after optional placement became the fourth parameter. Both expectations now match
the accepted contracts. No production guard or threshold is weakened.

CodeQL analysis2230334706 ran164 security-and-quality rules and reported four owned
alerts157–160 on the candidate ref: three cs/invalid-string-formatting findings in
PaneCountsLine and one cs/linq/missed-where in PaneEntryCounts. Its buildless extraction
also logged restore failures and8 unresolved types; an empty API error field is not
proof of a perfect extraction. The original SARIF/logs are retained. Issue574 tracks
these findings separately from the stale test fixtures in Issue571.

The normal three-value format genuinely received five values. SARIF also combines
all strings returned by the test Localize callback, including unavailable, three-value
and five-value templates at the one-value wrapper calls. The correction explicitly
passes3/5/1 values and uses the same local Format helper pattern as existing presenters,
retaining InvariantCulture and complete translated sentences. The one-pass directory
count uses Count(predicate), without adding enumeration or filesystem reads. No
query exclusion, suppression, baseline, resource change or public API is introduced.
See the [official query explanation](https://codeql.github.com/codeql-query-help/csharp/cs-invalid-string-formatting/)
and [query source](https://github.com/github/codeql/blob/main/csharp/ql/src/API%20Abuse/FormatInvalid.ql).

Selected Application36 and Presentation35 tests pass. Their full directly affected
assemblies pass933 and403 tests respectively, with branch coverage100% and96.078431%.
Application/Presentation test builds and the final App build have0 warnings/errors.
Changed-file whitespace checks and git diff --check pass. The new regression rejects
a five-value template in the ordinary three-value context; the preserved old product
DLL fails that single test as expected, then the fixed DLL is restored and hash-checked.
All12 real templates across three locales have exact3/5/1/1 placeholder sets and
InvariantCulture sample output. Independent read-only review requires no correction.

CS-013 Inspect still exits1 with exactly the same five findings, including locations,
values and limits, as the archived accepted input result; it is not claimed clean.
The comparison is counts-complexity-comparison.json. Source input hashes, commands,
test/coverage/build logs, old-DLL regression and independent review are retained in
the permanent evidence directory. The full same-suite local CodeQL analysis remains
in progress. A corrected release deep review is required because code/tests changed;
the failed first run is not reused as successful release proof.

## Integrated-feature cleanup

At23:19 JST the completed issue523/525/526/527/528 worktrees and their eight dedicated
outputs were removed after confirming integration, clean saved source, no links and
no running references. All29,115 generated files (5,105,771,514 bytes) are preserved
with every file hash verified in integrated-feature-artifacts.zip (SHA256
eaec33d9c42e2d99fe9236e3d957adb3f753046e01b1def139adfd63790683ff).
Branches, commits and permanent evidence remain. The archive manifest, exact bounded
deletion script and completion receipt identify every target. Unfinished other tasks
and the active issue571 release source/output remain. No cleanup rejection occurred.
