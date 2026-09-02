# ADR-0003: Use CommunityToolkit.Mvvm generators

Status: accepted

Date: 2026-09-02

## Context

WinUI presentation needs property notification and commands. Hand-written implementations and competing MVVM frameworks create repetitive variation and divergent lifecycle behavior.

## Decision

Use CommunityToolkit.Mvvm source-generator attributes as the only view-model notification and relay-command mechanism. View models use composition and do not inherit from a project-owned base view model.

## Rejected alternatives

- Manual `INotifyPropertyChanged` and `ICommand`: repetitive and inconsistent.
- A custom base view model: hidden shared behavior and inheritance coupling.
- Multiple MVVM frameworks: overlapping mechanisms.

## Consequences

Presentation takes one pinned dependency only after the first view model requires it and then follows its generator constraints. Generated code is treated as deterministic build output. An empty presentation shell does not carry the dependency speculatively.

## Migration and removal

The package is added atomically with the first generated view model and its tests. It remains absent while no view model exists.

## Executable proof

Dependency allowlist, package lock, source scan for manual implementations, and presentation tests.
