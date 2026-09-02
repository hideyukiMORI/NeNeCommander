# Test Strategy

Status: normative

Tests are executable specifications. Production code is not accepted because it appears correct; it is accepted only when positive, negative, boundary, failure, concurrency, and adversarial behavior are proved at the owning layer.

### TST-001 — Every behavior has an owning test tier

- Status: **active**
- Enforcement: project layout, test mapping, and canonical gate.

Domain invariants use unit and deterministic property tests. Application orchestration uses command, reducer, cancellation, and failure-injection tests. Every provider implements one shared contract suite. Presentation uses mapper, view-model, accessibility-state, and resource tests. Live OS behavior uses isolated integration tests.

### TST-002 — Positive and negative paths are inseparable

- Status: **active**
- Enforcement: review and threat-case mapping.

Every accepted input class has a successful example and every rejected invariant has a minimal counterexample. A feature test set that proves only success is incomplete.

### TST-003 — Tests are deterministic

- Status: **active**
- Enforcement: forbidden-API scan and repeated deep test.

Tests use injected clocks, identifiers, schedulers, providers, and seeded data. Wall time, sleeps, network state, random seeds, machine order, test order, and user directories are prohibited. A flaky test is a product defect and may not be retried into success.

### TST-004 — Property cases use one deterministic generator

- Status: **active**
- Enforcement: test utilities and seed reporting.

Boundary-heavy types use the project-owned deterministic case generator with a fixed default seed and an explicit replay seed in failure output. A second property-testing framework or hidden random source is prohibited.

### TST-005 — Provider contracts are shared

- Status: **active**
- Enforcement: provider contract suite.

Local Windows, UNC, and WSL adapters run the same capability-aware contract. Provider-specific tests add behavior; they do not replace the common contract.

### TST-006 — Concurrency and cancellation are controlled

- Status: **active**
- Enforcement: deterministic scheduler tests and adversarial cases.

Race tests advance explicit barriers rather than relying on timing. They prove reentrancy rejection, cancellation at every operation boundary, active-pane changes during work, stale enumeration results, and late provider completion.

### TST-007 — Failures are injected at every side-effect boundary

- Status: **active**
- Enforcement: port fakes and command tests.

Each port supports deterministic failure at preflight, first item, middle item, verification, source deletion, progress publication, and cancellation observation. Tests prove the exact typed outcome and completed effects.

### TST-008 — Mutation testing measures assertion strength

- Status: **active**
- Enforcement: pinned Stryker.NET and three-day deep review.

Stryker.NET runs at `Complete` mutation level with no baseline. Domain and Application must score at least 95%; Infrastructure and Presentation must score at least 90%. Surviving mutants are fixed with behavior tests or simpler code; they are not excluded to satisfy the threshold.

### TST-009 — Coverage is branch-based and ratcheted

- Status: **active**
- Enforcement: canonical implementation gate.

Domain and Application require 100% branch coverage. Testable Infrastructure and Presentation logic requires at least 90%. Generated framework code is the only default exclusion. A new exclusion requires an ADR and cannot hide product logic.

### TST-010 — Adversarial cases are first-class tests

- Status: **active**
- Enforcement: `eng/adversarial-cases.json` mapping and filtered test run.

Every registered threat ID appears in at least one test using `TestProperty("ThreatId", "ADV-NNN")` and category `Adversarial`. Removing or renaming a case without updating the threat model fails conformance.

### TST-011 — Destructive tests prove containment first

- Status: **active**
- Enforcement: test harness and cleanup assertions.

Mutation tests use a unique resolved OS temporary root. Live WSL and UNC tests require explicit dedicated roots. Setup and cleanup both reject roots, homes, repositories, mount roots, ancestors, links that escape the root, and ambiguous provider identity.

### TST-012 — Test output is complete evidence

- Status: **active**
- Enforcement: CI logs and deep-review report.

A failed seed, threat ID, provider, capability set, operation step, and typed outcome are emitted without secrets. Scheduled reports record tool versions, commit, start time, conclusion, and every skipped environmental tier.

## Required test matrix for each command

Every application command proves: valid success; each validation failure; each provider capability denial; empty and multi-item selection; cancellation before, during, and after the last side effect; collision decisions; partial completion; progress ordering; repeated invocation; active-pane changes; and normalized adapter failure.

## Deep-review cadence

`.github/workflows/security-deep-review.yml` runs every third UTC calendar day and on manual dispatch. It runs the canonical gate, adversarial tests three consecutive times, vulnerability review, and mutation testing. Any single failure fails the workflow; there are no automatic retries that convert failure into success.
