# NeNe Commander Agent Constitution

Status: normative

This file is the mandatory entry point for every human or AI development session in this repository. The project codename is `NeNe Commander`.

## Non-negotiable operating rule

The repository is governed as code, not as advice. Every change MUST follow the normative documents below and MUST pass the one canonical gate:

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

There is no alternative build, test, formatting, lint, or conformance path. A change is not complete while this command fails. A rule may not be bypassed locally, suppressed inline, or weakened to make a change pass.

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
9. [Security model](docs/SECURITY_MODEL.md) — threats, scheduled diagnostics, and response law.
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
6. Run `./eng/check.ps1` from the repository root.
7. For security-sensitive behavior, run `./eng/deep-review.ps1` and update the applicable threat cases.
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
