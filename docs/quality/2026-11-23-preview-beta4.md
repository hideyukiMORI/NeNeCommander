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
dependency inputs. Final results, analysis/ref-alert readback and packaging are pending.
