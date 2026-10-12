# NeNe Commander

NeNe Commander is a keyboard-first, dual-pane file manager for Windows 11 built with C#, .NET 10, and WinUI 3. It treats local Windows paths, UNC shares, and WSL distributions as explicit filesystem boundaries rather than interchangeable strings, and it keeps the real safety and capability differences between them visible instead of guessing.

## Where the project stands

The repository is in its implementation stage; production code is permitted. Most of the MVP scope named in the [project charter](docs/PROJECT_CHARTER.md) is on `main`:

- Dual-pane navigation with Vim-first keys, an address input, per-pane history, selection, hidden-item display, and sorting by name, extension, size, or modification time. See the [keyboard model](docs/KEYBOARD_MODEL.md).
- Copy, move, rename, new folder, and delete through one fail-closed file-operation gateway with provider-declared policies, observable progress, cancellation between steps, and `Escape` to abandon a loading read.
- Session-owned transient surfaces: settings editor, command palette, bookmarks with fixed slots, window adjustment mode, and a Locations picker for drives and WSL distribution roots.
- Providers: Windows local paths (reads, mutations, transfers, launch), WSL distributions reached through `\\wsl.localhost` (reads, same-distribution mutations and transfers), UNC shares (directory reads only), and copy or move in both directions between a Windows local path and a WSL distribution.

Not yet on `main`: Windows recycle-bin deletion, transfers between WSL distributions, UNC mutations and launch, network-drive-specific provider behavior, and the hand-run on-screen release confirmation defined by ADR-0054. The single current truth, including the verified baseline, deep-review scores, and the next focused work, is [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md); numbers are deliberately not repeated here.

## Repository layout

- `src/` holds the five projects in dependency order: `Domain`, `Application`, `Infrastructure.Windows`, `Presentation.WinUI`, and the `App` host. Direction and ownership are law in the [architecture constitution](docs/ARCHITECTURE_CONSTITUTION.md) and the [project layout](docs/PROJECT_LAYOUT.md).
- `tests/` mirrors each project and adds `Architecture.Tests`; behavioral, adversarial, coverage, and mutation proof are required by the [test strategy](docs/TEST_STRATEGY.md).
- `eng/` is the executable gate: `check.ps1`, conformance and security proof, coverage thresholds, deep review, the live WSL tier, and the UI evidence harness.
- `docs/` holds the normative documents, [architecture decision records](docs/adr/README.md), [waivers](docs/waivers/README.md), design handoffs, daily reports, and session handoffs.

## Start here

- Contributors and coding agents: [AGENTS.md](AGENTS.md), the mandatory entry point and reading order
- Tool compatibility entries: [AGENT.md](AGENT.md), [CLAUDE.md](CLAUDE.md), and [.github/copilot-instructions.md](.github/copilot-instructions.md)
- Product and engineering charter: [docs/PROJECT_CHARTER.md](docs/PROJECT_CHARTER.md)
- Current truth: [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md)
- Execution paths and keys: [docs/COMMAND_MODEL.md](docs/COMMAND_MODEL.md) and [docs/KEYBOARD_MODEL.md](docs/KEYBOARD_MODEL.md)
- Filesystem safety across Windows, UNC, and WSL: [docs/FILESYSTEM_BOUNDARIES.md](docs/FILESYSTEM_BOUNDARIES.md)
- Test and security law: [docs/TEST_STRATEGY.md](docs/TEST_STRATEGY.md) and [docs/SECURITY_MODEL.md](docs/SECURITY_MODEL.md)
- Git and pull-request conventions: [docs/COMMIT_CONVENTIONS.md](docs/COMMIT_CONVENTIONS.md)

## Verification

- One-time clone setup: `pwsh -NoProfile -File ./eng/bootstrap.ps1` verifies the pinned SDK and enables the repository-owned Git hooks.
- Documentation/comment-only changes: review text/links/diff; no automatic CI or product tests. Local commit checks remain.
- Ordinary implementation: affected build/behavior and boundary tests, plus local `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`.
- Substantial/shared changes: request `gh workflow run quality.yml --ref <candidate-branch>` when focused proof cannot establish the wider impact. It executes the full `pwsh -NoProfile -File ./eng/check.ps1`.
- Necessary security integration/release: request `gh workflow run security-deep-review.yml --ref <candidate-branch>`. Deep review includes the full gate; do not also run quality for unchanged inputs. There is no periodic run. See [ADR-0061](docs/adr/0061-impact-scoped-verification.md).
- Live WSL tier: `pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1` with `NENE_COMMANDER_WSL_TEST_ROOT` pointing at an empty, dedicated directory inside a distribution; it is opt-in and owns all fixtures in that root and a separate Windows temporary root. Use an explicit `-Case` array of declared method names for affected cells only; the log and result must pass every selected cell. Omit `-Case` for the complete tier.
- Windows UI release evidence: `eng/ui-evidence/Invoke-UiEvidence.ps1`, used for the hand-run confirmation of ADR-0054.

Technical documentation, identifiers, and comments are written in English. Commits use English Conventional Commit keywords with Japanese descriptions. User-facing text is stored in resources so localization remains possible.
