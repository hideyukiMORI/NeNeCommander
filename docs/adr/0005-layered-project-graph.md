# ADR-0005: Use the five-project layered graph

Status: accepted

Date: 2026-09-02

## Context

Implementation must begin without allowing WinUI, Windows, shell, and filesystem details to leak into product policy. Tests must address each owner independently while the executable retains one composition root.

## Decision

Use exactly the project graph declared in `eng/architecture.json`: Domain, Application, Infrastructure.Windows, Presentation.WinUI, and App, with five matching test projects. Dependencies point inward and App is the sole composition root. The initial implementation slice establishes validated filesystem paths, pane reduction, keyboard intent mapping, and the file-operation port before feature expansion.

## Rejected alternatives

- One executable project: permits boundary erosion and platform-coupled tests.
- Feature projects with peer references: encourage cycles and duplicated cross-cutting mechanisms.
- Dependency-injection framework: unnecessary before composition complexity proves a need.

## Consequences

Cross-layer contracts must be explicit and project references are mechanically allowlisted. Small changes may touch more files, but ownership and test scope remain deterministic.

## Migration and removal

The policy-foundation interlock changes to implementation only in the same change that creates the entire graph and activates all gates.

## Executable proof

`eng/architecture.json`, project-reference conformance, solution restore/build, and architecture tests.
