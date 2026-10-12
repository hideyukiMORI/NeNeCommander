# Quality Gates

Status: normative

ADR-0061 selects verification by change impact. `eng/check.ps1` (default `Merge` mode) remains the single full validation definition, requested for substantial features/shared changes when focused proof is insufficient. It is not required for every PR. Documentation starts no CI; isolated changes use affected tests; tooling changes use the affected tool's proofs. `-Mode Commit` remains local conformance/security/whitespace validation and does not claim product-test coverage.

## Gate sequence

The canonical gate stops at the first failing stage:

1. pinned SDK and repository-state validation;
2. policy and conformance validation;
3. locked dependency restore evaluated with the Release build configuration;
4. formatting verification;
5. Release build with warnings as errors;
6. deterministic unit and contract tests;
7. architecture and conformance tests;
8. coverage threshold verification;
9. clean generated-output and dependency-lock proof.

Both modes run secret, script-safety, workflow-supply-chain, NuGet-audit-policy and adversarial-registry checks. Full negative proofs/build/tests/coverage run only when the selected scope requires the full gate. Mutation and CodeQL run through explicitly requested `eng/deep-review.ps1` for justified security-sensitive integration or release proof. There is no periodic run. Deep review includes the full gate and satisfies it for unchanged inputs.

In `policy-foundation`, stages 3–9 are not silently skipped: production code is prohibited and the conformance gate proves that no solution or implementation is being represented as complete. The atomic transition described in `docs/PROJECT_STATE.md` activates all implementation stages.

### QLT-001 — One gate everywhere

- Status: **active**
- Enforcement: `eng/check.ps1` and CI workflow.

Local verification and CI share one full script and the same mode definitions. ADR-0061 determines when the full gate is required; targeted evidence is sufficient for a bounded change when its recorded scope establishes the impact. Commit mode alone does not prove behavior. Successful proof is reused while its relevant inputs remain unchanged.

### QLT-002 — Compiler and analyzer findings fail the build

- Status: **active**
- Enforcement: `Directory.Build.props`, `.editorconfig`, and conformance scan.

Nullable analysis, compiler warnings, .NET analyzers, and adopted style diagnostics are errors. Warning suppressions, severity downgrades outside the root configuration, and warning baselines are prohibited.

### QLT-003 — Formatting is verified, not repaired by CI

- Status: **active**
- Enforcement: `dotnet format --verify-no-changes` during implementation.

Formatting is deterministic from the root `.editorconfig`. Contributors format before committing; the gate never mutates source to obtain success.

### QLT-004 — Dependencies are reproducible

- Status: **active**
- Enforcement: pinned SDK, central package versions, package lock files, and locked restore.

The SDK is exact. Package sources are cleared and explicitly declared. Every package is allowlisted by project and centrally versioned. Lock-file drift fails the gate.

### QLT-005 — Behavior is proved at its owner

- Status: **active**
- Enforcement: matching unit, property, contract, and integration tests.

Every behavior change adds proof at the lowest owning layer. Bug fixes include a test that fails without the fix. Tests assert observable contracts, not private implementation details.

### QLT-006 — Architecture is executable

- Status: **active**
- Enforcement: `eng/architecture.json`, `eng/conformance.ps1`, and architecture tests.

Project existence, references, package use, forbidden APIs, source form, presentation boundaries, and token use are machine checked. A prose-only architecture rule is incomplete unless marked impossible with an explicit review proof.

### QLT-007 — Every custom gate has a negative proof

- Status: **active**
- Enforcement: `eng/prove-gates.ps1` and `docs/quality/GATE_PROOFS.md`.

A custom check is not trusted until a minimal invalid fixture makes it fail for the intended rule and a valid fixture passes. Gate changes update both proofs in the same change.

### QLT-008 — Coverage cannot ratchet downward

- Status: **active**
- Enforcement: coverage report at implementation stage.

Initial implementation starts at 100% branch coverage for Domain and Application and 90% branch coverage for provider and presentation logic that can run without live UI or OS state. Exclusions are limited to generated code and framework startup, are listed centrally, and require an ADR. Thresholds may increase but not decrease.

### QLT-009 — Environmental proof is named honestly

- Status: **active**
- Enforcement: CI job separation and release checklist.

Live WSL, UNC, removable-drive, high-DPI, high-contrast, and packaged-app checks are explicit environment tests. A unit-test pass does not claim those behaviors were exercised. A check that has not run is recorded as not run, never as passed. Live UNC proof of the ADR-0057 directory reader has not run: no live UNC tier exists yet, and the gate proves that reader only through the injected enumerator.

The Windows UI release confirmation is defined by ADR-0054: hide uses the product daily at the development scale and, before a release is declared, on a day hide is present, sets one monitor to 100 or 200 percent and switches high contrast on once, exercises the panes, address, status, key hints, the `Ctrl+W` helper, and the `F2`/`F7`/`F8` modals, and restores the settings. Issue #94 tracks that single confirmation. When evidence is recorded it is taken with `eng/ui-evidence/Invoke-UiEvidence.ps1` in `-Mode Observe`, which sends no input; the launcher is not part of `eng/check.ps1` or CI, never establishes merge readiness, and records every skipped cell with its reason (ADR-0052). Narrator speech evidence, 150 and 300 percent cells, the taskbar seam, the eight-scheme matrix at every scale, and a dedicated environment are not release requirements.

### QLT-010 — Greenfield means no baseline

- Status: **active**
- Enforcement: repository scan.

Analyzer, formatting, architecture, dependency, coverage, and test-failure baselines are prohibited. Existing violations are fixed before merge; they are not grandfathered.

### QLT-011 — UI changes prove interaction states

- Status: **active**
- Enforcement: presentation tests and design review.

Every UI change proves keyboard focus, selection, inactive-pane rendering, busy, cancellation, error, high contrast, localization expansion, and relevant design-token states. Snapshot images alone are insufficient.

### QLT-012 — The verified tree is reproducible

- Status: **active**
- Enforcement: clean-tree comparison in CI.

Restore, generation, build, and test must not modify committed sources or lock files. Build output stays in ignored locations. Generated differences fail CI.

### QLT-013 — Deep review is requested for the applicable risk

- Status: **active**
- Enforcement: `eng/deep-review.ps1` and manual CI.

Request deep security, adversarial, dependency and mutation analysis only when the security impact or release requires it. It includes full canonical validation; do not request that again for the same inputs. A release retains the requirement for successful deep-review evidence no older than 96 hours. No schedule runs these tiers.

### QLT-014 — Restore and build evaluate the same configuration

- Status: **active**
- Enforcement: `eng/check.ps1`, `eng/conformance.ps1`, and negative gate proof.

The canonical gate performs its single locked restore with `Configuration=Release` before the Release build. Configuration-conditional runtime and build packs must be resolved on a clean runner; a default-configuration restore followed by a Release `--no-restore` build is prohibited.

### QLT-015 — Verification follows the actual change impact

- Status: **active**
- Enforcement: `eng/conformance.ps1`, focused negative fixtures, CI workflows, PR evidence and the main ruleset.

Follow ADR-0061. Quality CI is `workflow_dispatch` only and always runs the unchanged full command when explicitly requested. Deep review is also manual-only. Dependency review runs at PR readiness only for the protected dependency-path set. Documentation-only changes start no CI. Do not substitute skipped jobs or synthetic success for actual evidence.

Main retains required PRs, squash-only integration, resolved threads, no bypass actors and force-push/deletion protection. It has no unconditional required status check: review the selected evidence before merging. Record the chosen scope and justification in the PR. Only relevant input changes invalidate evidence; workflow stage, new commit identifiers or unchanged docs do not. No daily integration or Draft-to-Ready full run is mandatory.

## Test tiers

| Tier | Runs in canonical PR gate | Purpose |
|---|---:|---|
| unit and property | yes | domain values, reducers, policies, key mapping |
| provider contract | yes | every adapter against the same behavioral contract using isolated fakes/temp roots |
| Windows local integration | yes on Windows CI | real filesystem and shell behavior in a test-owned temp root |
| WSL live integration | opt-in CI/release | registered distro and dedicated `NENE_COMMANDER_WSL_TEST_ROOT` |
| UNC/removable hardware | release matrix | capability and failure behavior on controlled infrastructure |
| WinUI accessibility/visual | release matrix | focus, narrator, DPI, themes, high contrast, design states |

## Changing a gate

A gate change requires an ADR, updated documentation and positive/negative proof of the affected tooling. Under ADR-0061, use the bounded proof tools and local Commit checks; do not run unrelated product suites merely because a gate changed. Changing test expectations to conceal a failure in unrelated work is prohibited.
