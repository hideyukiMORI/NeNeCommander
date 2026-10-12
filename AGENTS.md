# NeNe Commander Agent Constitution

Status: normative

This file is the mandatory entry point for every human or AI development session in this repository. The project codename is `NeNe Commander`.

## Non-negotiable operating rule

The repository is governed as code. Follow the normative documents and select verification from the actual change impact before running it (hide, 2026-10-12; ADR-0061). Documentation/comment-only work starts no CI and needs no build, product tests, coverage or mutation. Ordinary behavior changes need the affected build/tests and directly impacted boundaries. A substantial feature or shared-contract change needs the full canonical gate when focused proof cannot establish its wider impact:

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

Full quality and deep-review CI run only through explicit `workflow_dispatch`; no push, PR readiness or schedule starts them. Dependency review runs at readiness only when dependency inputs change. Record the selected scope and why it is necessary in the PR. A security-sensitive trust-boundary change may require deep review; release proof retains its own environmental and freshness requirements. A successful deep review already includes the full gate, so do not repeat quality for the same inputs.

Keep work in a draft PR until review-ready; Ready does not request full testing. Issue PRs may target main directly, with integration branches optional. Main still requires a PR and squash merge and blocks force pushes/deletion, but has no unconditional required CI check. Verify the applicable evidence before merging. Reuse successful evidence when its relevant code/tests/dependencies/tooling inputs are unchanged; commit, push, base movement or merge alone is not a reason to repeat it. Do not lower thresholds or suppress failures. Local commit hooks retain `eng/check.ps1 -Mode Commit`. See ADR-0061 and `docs/DEVELOPMENT_WORKFLOW.md`.

After a fresh clone, run `pwsh -NoProfile -File ./eng/bootstrap.ps1` once to verify the pinned SDK and enable the repository-owned Git hooks.

## Required reading order

Read these documents before editing the corresponding area:

1. [Project charter](docs/PROJECT_CHARTER.md) — product intent and non-goals.
2. [Project state](docs/PROJECT_STATE.md) — what may truthfully be changed now.
3. [Architecture constitution](docs/ARCHITECTURE_CONSTITUTION.md) — dependency and ownership law.
4. [Command model](docs/COMMAND_MODEL.md) — the sole execution paths for behavior.
5. [Project layout](docs/PROJECT_LAYOUT.md) — where every kind of code belongs.
6. [C# coding rules](docs/CODING_RULES.md) — mandatory language subset and design rules.
7. [Quality gates](docs/QUALITY_GATES.md) — executable definition of done.
8. [Test strategy](docs/TEST_STRATEGY.md) — mandatory behavioral, adversarial, coverage, and mutation proof.
9. [Security model](docs/SECURITY_MODEL.md) — threats, explicitly requested diagnostics, and response law.
10. [Development workflow](docs/DEVELOPMENT_WORKFLOW.md) — change procedure and evidence.
11. [Commit conventions](docs/COMMIT_CONVENTIONS.md) — Issue, branch, commit, and PR law.
12. [Keyboard model](docs/KEYBOARD_MODEL.md) — Vim-first navigation contract.
13. [Filesystem boundaries](docs/FILESYSTEM_BOUNDARIES.md) — Windows, UNC, and WSL safety.
14. [Design handoff](docs/DESIGN_HANDOFF.md) — boundary with Claude Design and ChatGPT design tools.
15. [Glossary](docs/GLOSSARY.md) — canonical terms; synonyms are not invented.

Architectural decisions live under [docs/adr](docs/adr/README.md). Temporary exceptions live under [docs/waivers](docs/waivers/README.md). Neither an ADR nor a waiver may disable compiler errors, nullable analysis, warnings-as-errors, the canonical gate, destructive-operation safety, or dependency-direction checks.

## Session protocol

1. Read this file and `docs/PROJECT_STATE.md` completely.
2. Read the normative documents relevant to the requested change.
3. Inspect the current tree and existing changes before editing; preserve unrelated work.
4. State the invariant and the single canonical mechanism affected by the change.
5. Add or update tests and conformance proof in the same change.
6. Run affected behavior and dependency-impact tests. Before merge, verify the evidence required by the selected ADR-0061 scope; request the full canonical gate only when the wider impact requires it.
7. For security-sensitive behavior, update and run the affected threat cases; request `./eng/deep-review.ps1` when the trust-boundary impact requires it. Do not run all-layer mutation for minor changes or tooling-only edits.
8. Report the exact commands run, their result, and any remaining environmental proof that cannot run locally.

## Absolute prohibitions

- Do not add production code while `docs/PROJECT_STATE.md` says `Production code: prohibited`.
- Do not introduce a second way to perform an existing operation.
- Do not access files, shell APIs, settings, time, randomness, or process-global state outside their declared boundary.
- Do not put domain or application decisions in XAML code-behind.
- Do not add a dependency, suppression, baseline, generated artifact, or broad utility module without its prescribed approval path.
- Do not claim a gate is active unless it executes in `eng/check.ps1` and CI.
- Do not add an unpinned GitHub Action, suppress a vulnerability advisory, commit a secret, or send repository content to an external review service without an accepted security ADR.
- Do not treat `\\wsl.localhost`, `\\wsl$`, UNC, removable media, or network paths as if they had local NTFS semantics.
- Do not hard-code final visual styling before an approved design handoff. Use semantic resources and tokens.

If a requested change conflicts with this constitution, stop that part of the change and propose an ADR. Convenience and deadlines are not exceptions.
