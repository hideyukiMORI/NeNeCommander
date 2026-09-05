# ADR-0035: Route WSL directory reads through the canonical query port

Status: accepted

Date: 2026-09-06

## Context

ADR-0004 requires Windows-side access to canonical `\\wsl.localhost` paths, while ADR-0010 provides the sole provider-neutral `IDirectoryReadPort`. The App previously composed `WindowsLocalDirectoryReader` directly, so a validated `WslPath` could not be navigated. Duplicating enumeration would let boundary, cancellation, name validation, failure normalization, and listing behavior drift between providers.

## Decision

Compose one `ProviderDirectoryReadPort` as the App's `IDirectoryReadPort`. It branches only on the validated path variant: `WindowsLocalPath` goes to `WindowsLocalDirectoryReader`, `WslPath` goes to `WslDirectoryReader`, and unsupported variants fail closed as `ProviderUnavailable` without probing another namespace.

Both readers use `WindowsDirectoryReadOperation`, the one bounded non-recursive enumeration algorithm, and the existing `WindowsLocalIoExecutionBoundary`. The shared production enumerator opens only the request's canonical namespace path and freezes each direct entry's name, kind, and attributes. The operation observes cancellation before enumeration and before each entry, counts every provider entry against the request boundary, derives children only with `FileSystemPath.Child`, counts rejected names as unrepresentable, and publishes only `DirectoryListing`.

Windows local visibility remains based on hidden/system attributes. WSL visibility follows the Linux dot-name convention; projected Windows attributes do not invent Linux hidden policy. Both adapters report every entry and leave filtering to the pane transition.

## Rejected alternatives

- Invoke `ls` through `wsl.exe`: this creates a shell query path, quoting surface, text protocol, and second enumeration mechanism.
- Parse canonical path strings again in the router: provider identity is already represented by the closed path type.
- Duplicate the existing enumeration loop in the WSL adapter: cancellation, bounds, and failure behavior could diverge.
- Treat WSL as ordinary UNC or use Windows hidden attributes: this loses Linux identity and visibility semantics.
- Add WSL reads to `FileOperationGateway`: directory enumeration is a query and must not acquire mutation authority.

## Consequences

- Pane sessions retain one provider-neutral port and can navigate a WSL path without provider-specific behavior.
- Windows local behavior moves to the shared operation without changing its public adapter contract.
- Windows UNC remains explicitly unsupported until it joins the same router in a separate provider Issue.
- Live WSL filesystem proof remains opt-in and must use a configured safe test-owned root; deterministic contract tests do not require a distribution.

## Migration and removal

The App composition replaces its concrete `WindowsLocalDirectoryReader` dependency with `ProviderDirectoryReadPort`. The old Windows-local enumeration loop is removed as the shared operation is introduced atomically.

## Executable proof

`ProviderDirectoryReadPortTests` prove exact provider routing and unsupported fail-closed behavior. `WslDirectoryReaderTests` prove Linux case identity, dot visibility, canonical child derivation, bounds, unrepresentable names, cancellation, expected failures, and adapter guards. Existing `WindowsLocalDirectoryReaderTests`, execution-boundary tests, architecture tests, coverage, mutation, security checks, and the canonical gate prove the shared migration.
