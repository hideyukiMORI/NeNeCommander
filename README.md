# NeNe Commander

NeNe Commander is a keyboard-first, dual-pane file manager for Windows 11 built with C#, .NET 10, and WinUI 3. It treats local Windows paths, UNC paths, and WSL distributions as explicit filesystem boundaries rather than interchangeable strings.

The repository is in its implementation stage. The first vertical slice establishes typed Windows, UNC, and WSL paths, deterministic Vim input, immutable pane transitions, and a fail-closed file-operation gateway before provider adapters are added.

## Start here

- Contributors and coding agents: [AGENTS.md](AGENTS.md)
- Tool compatibility entries: [AGENT.md](AGENT.md), [CLAUDE.md](CLAUDE.md), and [.github/copilot-instructions.md](.github/copilot-instructions.md)
- Product and engineering charter: [docs/PROJECT_CHARTER.md](docs/PROJECT_CHARTER.md)
- Current truth: [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md)
- Test and security law: [docs/TEST_STRATEGY.md](docs/TEST_STRATEGY.md) and [docs/SECURITY_MODEL.md](docs/SECURITY_MODEL.md)
- One-time clone setup: `pwsh -NoProfile -File ./eng/bootstrap.ps1`
- Development/commit policy checks: `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`; run focused behavior and affected-consumer tests during implementation.
- Full integration verification: `pwsh -NoProfile -File ./eng/check.ps1`; requested in CI by changing the final draft PR to Ready immediately before merge. See [verification workflow](docs/DEVELOPMENT_WORKFLOW.md).
- Deep security and adversarial verification: `pwsh -NoProfile -File ./eng/deep-review.ps1`
- Git and pull-request conventions: [docs/COMMIT_CONVENTIONS.md](docs/COMMIT_CONVENTIONS.md)

Technical documentation, identifiers, and comments are written in English. Commits use English Conventional Commit keywords with Japanese descriptions. User-facing text is stored in resources so localization remains possible.
