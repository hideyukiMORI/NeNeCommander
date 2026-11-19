# 2026-11-20 Dependabot test SDK update policy — Issue #386

Status: local configuration verification passed; integration pending.

## Problem, invariant and canonical mechanism

ADR-0048 requires Microsoft.NET.Test.Sdk to match the version resolved by the pinned
MSTest.Sdk. Independent version proposals repeatedly ask for an incompatible pin.
Issue #386 requests suppressing those routine proposals while keeping security
updates eligible. The existing .github/dependabot.yml is the sole configuration
owner; no package, lock file, runner, workflow trigger or repository security
setting changes.

The NuGet block now ignores the three version-update SemVer types for the exact
Microsoft.NET.Test.Sdk name. It defines no versions range, wildcard dependency
match, alert dismissal or security-update exclusion. The comment cites ADR-0048
and explains that the companion pin changes with MSTest.Sdk.

## Evidence for the security-update boundary

Reviewed the [official options reference](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference#ignore)
and pinned upstream commit `e2506780490c0ed92dd72f5d4e81ed988b885d8f` on 2026-11-20.
The [ignore-condition implementation](https://github.com/dependabot/dependabot-core/blob/e2506780490c0ed92dd72f5d4e81ed988b885d8f/common/lib/dependabot/config/ignore_condition.rb)
returns only explicit version ranges for a security-only job, before interpreting
update types. This configuration has no such ranges. The
[upstream specification](https://github.com/dependabot/dependabot-core/blob/e2506780490c0ed92dd72f5d4e81ed988b885d8f/common/spec/dependabot/config/ignore_condition_spec.rb)
also asserts that version-type restrictions still allow patch, minor and major
security updates. These are inspected upstream sources, not locally executed
upstream tests or a guarantee that the next hosted job has already run.

The exact upstream source/spec, Git blob IDs, SHA256 values, before/after YAML and
access time are retained in
`D:/NeNeCommander/evidence/dependabot-test-sdk-20261120/`.
The GitHub connector can read those sources. An immutable commit fetch was used
after initial default-branch discovery. Sandbox command discovery did not expose
the existing user Python; its normal environment was found, but PyYAML and the
available Node runtime's yaml package are absent. No dependency was installed and
no custom YAML parser was introduced. The short YAML block is reviewed directly.

## Selected verification

ADR-0061 tooling/configuration scope: review the exact eight-line YAML addition,
its parent mapping and three supported values; confirm all other configuration
and dependency files remain unchanged; execute
`pwsh -NoProfile -File eng/check.ps1 -Mode Commit`, which runs repository
conformance, security conformance with unchanged negative fixtures omitted, and
Git whitespace checks. The normal commit hook remains enabled.

No product code changes, so no restore, product build/tests, coverage, mutation,
full/deep CI or release is selected. Hosted Dependabot execution is a later
observation; no synthetic local result substitutes for it. Existing PR456 is a
separate proposal and is not closed or merged by this config change.

## Results

At 08:01-08:02 JST the selected Commit-mode check returned exit0:
112 normative rules pass; security conformance reports 24 registered adversarial
mappings and no secret/workflow-supply-chain violation; whitespace passes. This
checks the mappings, not execution of those 24 behavior cases. validation.json
and commit-scope.log retain exact timing/command/output. The only non-document
diff is the reviewed eight-line ignore block; all dependency pins remain equal
to main 1627d55. No ignored output was reported in the worktree.

The existing dependency-review workflow includes .github/dependabot.yml in its
Ready-event path list. Therefore that narrow hosted dependency check must also
complete before integration; no full or deep-review workflow is dispatched.
The normal commit hook will perform its prescribed policy check. Its result,
Draft/Ready transition, dependency check, squash merge and cleanup are recorded
below after observation.
