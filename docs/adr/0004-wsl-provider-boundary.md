# ADR-0004: Model WSL as a provider boundary

Status: accepted

Date: 2026-09-02

## Context

NeNe Commander must navigate and mutate WSL directories, including cross-boundary operations. WSL paths differ from local NTFS paths in case sensitivity, links, permissions, availability, metadata, performance, and delete behavior.

## Decision

Parse `\\wsl.localhost` and `\\wsl$` inputs into a dedicated `WslPath` provider variant. Render `\\wsl.localhost` canonically. Discover distributions through the infrastructure `IWslDistributionCatalog`; access content through one Windows-side WSL provider adapter. Do not use shell commands as an alternate mutation engine.

## Rejected alternatives

- Treat WSL as generic UNC: hides provider-specific safety and capability semantics.
- Store raw path strings: spreads parsing and prefix checks across features.
- Execute `cp`, `mv`, or `rm` through `wsl.exe`: creates a second operation path and unsafe quoting surface.

## Consequences

Capabilities and failure modes are explicit. Cross-provider move is copy, verify, then delete. WSL live testing needs an installed distribution and a dedicated opt-in root.

## Migration and removal

None. This is the initial WSL model.

## Executable proof

Parser property tests, provider contract tests, cross-provider operation tests, and opt-in live WSL integration tests.
