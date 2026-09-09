# ADR-0048: Execute the mutation tier through an isolating VSTest host

Status: accepted

Date: 2026-09-10

Accepted under hide's delegated implementation authority and adopted by the NeNe Commander design owner after the 2026-09-09 mutation-runner diagnostics for Issues #99 and #101.

## Context

TST-008 requires Stryker.NET at `Complete` mutation level with fixed thresholds: Domain and Application 95%, Infrastructure and Presentation 90%. ADR-0006 selects MSTest on Microsoft.Testing.Platform (MTP) as the sole test runner and prohibits VSTest mode, so `stryker-config.json` used `test-runner: mtp` and the TST-008 security rule protected that value.

Diagnostics on `feat/101-command-palette` head `358c294` and on main `ca99288` established that the pinned Stryker.NET 4.16.0 MTP runner does not measure what TST-008 claims:

- The MTP runner starts four test-server processes and switches the active mutant by writing its id to a memory-mapped file `stryker-mutant-<slot>.txt`. Its trace log contains no isolation or restart for any mutant. A static initializer runs once per process, so only the first static mutant handled by each process can manifest; later ones are reported Survived, or Killed by a test failure that belongs to an unrelated mutant.
- The MTP runner also records almost every mutant as covered by every test (200/201, 871/873, 816/816, and 492/561 mutants across the four layers), so kill attribution comes from whichever tests fail in a reused host rather than from per-test coverage. The VsTest runner attributes coverage per test and starts a fresh host for each static mutant (37 host logs for 35 static mutants).
- On identical source and an identical mutant set, the MTP runner reported 50 static Presentation mutants Survived and 43 Killed; the VsTest runner reported 90 Killed and 3 Survived. Of 41 static mutants Killed by both, 29 carried an MTP `killedBy` set that belongs to an unrelated mutant.
- Ground truth from source: no test asserts the `ResourceKey` of `OperationStatus.MoveAwaitingConflict` or `CopyAwaitingConflict`, and no test passes `null` to `KeyboardInput.Create`. The VsTest runner reported those mutants Survived; the MTP runner reported them Killed. An MTP run restricted to `OperationStatus.cs` reported 35/35 Killed, which is therefore a false pass.
- Re-measuring main `ca99288` with the isolating runner gives Domain 95.02%, Application 94.85%, Infrastructure.Windows 90.74%, and Presentation.WinUI 89.10%. Application and Presentation do not meet their thresholds; every earlier MTP-based pass (for example run `34239166946`) was inflated by false kills and is not evidence of assertion strength.

Upstream tracks the defect as stryker-net Issue #3742; PR #3695 ("run static mutants in a dedicated test-server process") is open and unreleased as of 4.16.0 (2026-07-03).

A gate whose verdicts depend on process scheduling does not satisfy the constitution's rule that a claimed gate must actually execute what it claims. The thresholds, mutation level, no-baseline rule, and exclusion prohibition are not the defect and do not change.

## Decision

- **Every canonical test command stays on MTP exactly as ADR-0006 states.** `dotnet test`, coverage, the commit hook, the canonical gate, and CI do not change. The `global.json` `test.runner` value and each test project's `TestingExtensionsProfile` remain.
- **Each test project named by `eng/security-policy.json` `mutationProjects` additionally references `Microsoft.NET.Test.Sdk`**, pinned centrally in `Directory.Packages.props` at the version MSTest.Sdk 4.4.0 itself resolves (18.9.0), so the VSTest adapter and `testhost` are always present in the test output. `UseVSTest` is never set. The package graph and therefore every `packages.lock.json` are identical for the gate build and the mutation build, so `--locked-mode` restore and central transitive pinning cover the mutation run.
- **`stryker-config.json` selects `test-runner: vstest`.** This file is the single declaration of the mutation runner; `eng/deep-review.ps1` passes no runner flag. The TST-008 security rule protects `vstest` instead of `mtp` and additionally requires the `Microsoft.NET.Test.Sdk` reference in every mutation test project, so the isolating host cannot silently regress.
- **Thresholds, `Complete` level, `break-on-initial-test-failure`, the per-layer `breakAt` values, and the baseline and exclusion prohibitions do not change.** Layers whose truthful score is below threshold are corrected in the same change with behavior tests for surviving mutants, as TST-008 already requires.
- This is the only VSTest use in the repository and exists solely because the pinned MTP runner cannot isolate mutants or attribute coverage per test. ADR-0006's prohibition of VSTest mode continues to govern test execution outside the mutation tier.

## Rejected alternatives

- Adding tests to kill MTP static survivors: the mutant never manifests in a reused host, so no test can kill it; the score moves only with scheduling luck.
- Reshaping production code so that no literal lives in a static initializer: shapes the product around a tool defect, needs a new conformance rule to stay effective, and leaves the non-static attribution defect in place.
- Waiting for the upstream fix: no release date; two integration candidates are blocked and every mutation verdict on main is untrustworthy meanwhile.
- Building Stryker from the open upstream PR: an unpinned, unreleased dependency is prohibited.
- Enabling `UseVSTest` conditionally for the Stryker build through a property or environment variable: MSTest.Sdk then removes `TestingExtensionsProfile`, `dotnet test` fails against the `global.json` runner, and the package graph differs between the gate and the mutation build, so locked restore could not cover the mutation run.
- Passing `--test-runner vstest` only in `eng/deep-review.ps1` while the config keeps `mtp`: two declarations of the runner, and any local run from the config alone would again be untruthful.
- Switching the whole solution to VSTest: reverses ADR-0006, changes the canonical CLI and coverage extension profile, and is broader than the defect.

## Consequences

Mutation scores are re-baselined truthfully for all four layers, and the deep-review tier becomes independent of process scheduling. Application and Presentation gain behavior tests for previously unmeasured argument guards and resource keys. The Domain margin at 95.02% is zero, so Domain survivors should be reduced in the same change where a behavior test is cheap. Historical MTP-based mutation results are recorded as non-evidence in the state document. Three test-only packages enter each mutation test project's lock file; no production project changes.

## Migration and removal

When a released, pinned Stryker.NET version isolates static mutants and attributes coverage per test under MTP, `stryker-config.json` returns to `test-runner: mtp`, the `Microsoft.NET.Test.Sdk` references and the TST-008 reference check are removed, and this ADR is marked superseded. The condition is reviewed at each Stryker pin change.

## Executable proof

The TST-008 security rule fails when `test-runner` is not `vstest` or a mutation test project lacks the `Microsoft.NET.Test.Sdk` reference. `dotnet restore --locked-mode` passes with the committed lock files. The MTP canonical gate passes unchanged. An exact-head `security-deep-review` run with the isolating runner passes every layer at the existing thresholds, and its Presentation report shows the `OperationStatus` `AwaitingConflict` resource-key mutants and the `KeyboardInput.Create` null-guard mutants Killed by the tests added in this change.
