# ADR-0034: Discover WSL distributions through one bounded process boundary

Status: accepted

Date: 2026-09-06

## Context

ADR-0004 and FS-003 require registered WSL distributions to be discovered through `IWslDistributionCatalog`, but no port or adapter existed. Raw `wsl.exe` output is untrusted, distribution identity is case-insensitive while names remain case-preserving, redirected WSL output uses UTF-16, and cancellation must not leave an unowned child process. Discovery must not become a general shell execution facility or an alternate file-mutation path.

## Decision

Add the provider-neutral `IWslDistributionCatalog` in Application with one closed success, cancellation, or expected-failure outcome. A successful outcome owns an immutable ordered snapshot of unique `WslPath` roots. Invalid roots, child paths, null entries, and duplicate distribution identities cannot be constructed as success.

Implement the port once as `WslDistributionCatalog` in Infrastructure.Windows. Its dedicated `WslDistributionProcess` invokes only `wsl.exe` with separate fixed `--list` and `--quiet` argument tokens, disables shell execution and window creation, redirects both streams, and decodes them as UTF-16. Each stream is limited to 64 KiB while reading, and at most 256 non-empty output lines are accepted. Each line passes through `FileSystemPath.Parse` and must be exactly a `WslPath` root. One invalid line, excess boundary, process or stream I/O failure, or nonzero exit returns a closed failure without a partial list.

Cancellation registers termination only for the process started by that discovery call. Both redirected streams and process exit are awaited without cancellation after termination is requested; only then does the call publish `Cancelled`. An internal process-start seam exists solely so tests can start and terminate their own short-lived processes while production construction always uses the fixed start information.

## Rejected alternatives

- Start `wsl.exe` from feature or presentation code: this would duplicate provider discovery and violate the platform boundary.
- Use a command string or shell: it creates a quoting and injection surface where no dynamic argument is needed.
- Return raw distribution strings: callers could bypass canonical WSL root parsing and identity rules.
- Accept valid lines while dropping malformed lines: a partial snapshot would hide hostile or ambiguous provider output.
- Use unbounded `ReadToEndAsync`: output is untrusted and must be bounded before publication and continued allocation.
- Kill without awaiting on cancellation: this leaves process and redirected-stream lifetime unowned.

## Consequences

- Discovery has no filesystem mutation authority and cannot execute a caller-supplied command or argument.
- Provider order and first-seen casing are preserved; later case variants are ignored as the same distribution identity.
- An empty successful list represents no registered distributions. Provider absence and malformed output remain distinguishable failures.
- WSL directory enumeration, file operations, root-picker UI, and opt-in live mutation proof remain separate work.

## Migration and removal

This implements the mechanism already reserved by ADR-0004; there is no legacy discovery implementation to remove. Future consumers must depend on `IWslDistributionCatalog` and may not invoke `wsl.exe` directly.

## Executable proof

`WslDistributionCatalogOutcomeTests` prove immutable valid roots and invalid snapshot rejection. `WslDistributionCatalogTests` prove valid, empty, duplicate, invalid, excessive, failed, and cancelled process outcomes. `WslDistributionProcessTests` prove the exact non-shell invocation plus bounded stream, successful completion, active cancellation, oversized-output termination, and failed-start behavior using only processes started by the test. Architecture, security, focused mutation, and the canonical gate complete integration proof.
