# ADR-0011: Windows integration tests own an isolated temporary root

Status: accepted

Date: 2026-09-03

## Context

CS-018 keeps `System.IO` and other platform APIs inside `NeNeCommander.Infrastructure.Windows`. The conformance scan enforced that boundary on every `.cs` file under `src` and `tests`, which also prohibited the Windows local integration tier that `docs/QUALITY_GATES.md` and TST-011 require: real filesystem behavior proved inside a unique test-owned temporary root. Without platform APIs, the Infrastructure test project could neither create fixtures nor verify cleanup.

## Decision

Narrow the CS-018 scan to permit `System.IO` in exactly two locations: `src/NeNeCommander.Infrastructure.Windows/` and `tests/NeNeCommander.Infrastructure.Windows.Tests/`. Every other production and test project remains prohibited.

Inside the Infrastructure test project, filesystem fixtures are created only through `TestOwnedTemporaryRoot`:

- the root is created by the operating system beneath its temporary directory with the `NeNeCommander-Test-` prefix and is verified empty before use;
- setup and disposal both re-resolve the root and refuse any path that is not a prefixed direct child of the temporary directory;
- child paths are resolved and verified to remain inside the root before a file, directory, or access rule is created;
- access-rule fixtures are recorded and removed before the root is deleted.

## Rejected alternatives

- Exposing a fixture factory from production code: adds test-only production surface and a second filesystem path.
- Creating fixtures through PowerShell before the test run: separates setup from the assertion that verifies it and hides the contract from the test project.
- Leaving the tier unimplemented: leaves adapter behavior for denied, missing, hidden, and unrepresentable entries unproved.

## Consequences

- Infrastructure tests may touch only the verified temporary root. WSL, UNC, home, repository, and mount roots remain prohibited by TST-011 and are not reachable through the harness.
- The unique root suffix is chosen by the operating system for isolation; no assertion depends on it.
- A negative gate proof keeps `System.IO` rejected in every other test project.

## Migration and removal

No prior mechanism exists. Live WSL and UNC integration tiers require their own dedicated opt-in roots and a further ADR before they use this harness.

## Executable proof

`eng/conformance.ps1` CS-018 scan, the `platform-api-outside-infrastructure` negative proof in `eng/prove-gates.ps1`, `docs/quality/GATE_PROOFS.md`, and `TestOwnedTemporaryRoot` usage in `WindowsLocalDirectoryReaderTests`.
