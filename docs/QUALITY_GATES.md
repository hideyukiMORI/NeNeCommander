# Quality Gates

Status: normative

`eng/check.ps1` (default `Merge` mode) is the single full definition of merge readiness. Individual tools and targeted tests are used during implementation; their success never substitutes for the complete integration gate. `-Mode Commit` performs lightweight conformance, security scans without negative fixtures, and whitespace checks; it does not build, run behavior tests, collect coverage, or claim merge readiness.

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

Both modes run secret, script-safety, workflow-supply-chain, NuGet-audit-policy, and adversarial-registry checks. Full negative proofs, build, behavior tests and coverage run at merge readiness, not on every edit/commit/push/PR update. The heavier mutation and CodeQL work runs through `eng/deep-review.ps1` every three days; security-sensitive changes also require deep review at integration readiness.

In `policy-foundation`, stages 3–9 are not silently skipped: production code is prohibited and the conformance gate proves that no solution or implementation is being represented as complete. The atomic transition described in `docs/PROJECT_STATE.md` activates all implementation stages.

### QLT-001 — One gate everywhere

- Status: **active**
- Enforcement: `eng/check.ps1` and CI workflow.

Local verification and CI share one script and the same mode definitions. The full default mode is the sole merge gate. Commit mode and targeted development diagnostics are explicitly incomplete evidence, not a second definition of done. A successful required CI gate need not be duplicated locally.

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

Live WSL, UNC, removable-drive, high-DPI, high-contrast, and packaged-app checks are explicit environment tests. A unit-test pass does not claim those behaviors were exercised. Release readiness requires recorded proof on the supported environment matrix.

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

### QLT-013 — Deep review is a separate mandatory tier

- Status: **active**
- Enforcement: `eng/deep-review.ps1` and scheduled CI.

The default branch runs deep security, adversarial, dependency, and mutation analysis every third UTC calendar day. It does not replace the PR gate. A release requires a successful deep-review result no older than 96 hours.

### QLT-014 — Restore and build evaluate the same configuration

- Status: **active**
- Enforcement: `eng/check.ps1`, `eng/conformance.ps1`, and negative gate proof.

The canonical gate performs its single locked restore with `Configuration=Release` before the Release build. Configuration-conditional runtime and build packs must be resolved on a clean runner; a default-configuration restore followed by a Release `--no-restore` build is prohibited.

### QLT-015 — Full validation is requested at integration readiness

- Status: **active**
- Enforcement: `eng/conformance.ps1`, negative gate fixtures, `quality.yml`, and the main ruleset.

Keep PRs draft during development. The sole normal full-CI trigger is `pull_request: ready_for_review`; Draft to Ready means the focused Issue is ready for its final integration check. Push, PR opened/synchronize, commit, and post-merge events must not start the full gate. A non-draft PR created directly must be converted to Draft then Ready to request validation. Further head changes require a fresh readiness transition; when main changes, update the branch and request fresh validation. The ruleset must retain required `canonical-gate` from GitHub Actions, strict up-to-date status checks, no bypass actors, and squash-only PR integration. Never use a skipped job or a lightweight success under the `canonical-gate` name. The job runs the complete default command against the PR merge candidate. Obsolete runs for the same PR may be cancelled. Separate scheduled/security/release deep-review obligations remain in force.

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

A gate change requires an ADR, positive and negative proof, updated documentation, and a passing old gate followed by the passing new gate when technically possible. Disabling a check to merge unrelated work is prohibited.
