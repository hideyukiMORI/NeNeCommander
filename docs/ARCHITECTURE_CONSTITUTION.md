# Architecture Constitution

Status: normative

This document defines invariants. Examples explain the law but do not create alternatives. `eng/architecture.json` is the machine-readable projection of the project graph.

## Dependency direction

```text
NeNeCommander.App
  -> NeNeCommander.Presentation.WinUI
  -> NeNeCommander.Infrastructure.Windows

NeNeCommander.Presentation.WinUI
  -> NeNeCommander.Application

NeNeCommander.Infrastructure.Windows
  -> NeNeCommander.Application
  -> NeNeCommander.Domain

NeNeCommander.Application
  -> NeNeCommander.Domain

NeNeCommander.Domain
  -> BCL only
```

Dependencies always point inward. The application executable is the only composition root. Tests may reference only their system under test and that system's inward dependencies.

### ARC-001 — One canonical mechanism per concern

- Status: **active**
- Enforcement: architecture manifest, conformance scan, tests, and review.

Each concern has exactly one approved mechanism. Existing mechanisms are extended or replaced atomically; competing implementations are prohibited. This applies especially to commands, state transitions, paths, results, settings, logging, time, filesystem mutations, keyboard mapping, and dependency construction.

### ARC-002 — Physical dependency graph

- Status: **active**
- Enforcement: `eng/architecture.json`, project-reference validation, and architecture tests.

Namespaces do not constitute a boundary. Every layer is a separate project and every reference must be explicitly permitted by the manifest. Cycles are prohibited.

### ARC-003 — Platform-free domain

- Status: **active**
- Enforcement: dependency allowlist and forbidden-API scan.

Domain code depends only on the .NET base class library. It contains no WinUI, Windows App SDK, filesystem I/O, shell, registry, process, environment, clock, random, network, JSON, logging, or dependency-injection API.

### ARC-004 — One owner for mutable state

- Status: **active**
- Enforcement: reducer tests, immutable public surfaces, and review.

Each mutable concept has one owner. Pane state is changed only by `PaneReducer`; operation lifecycle is changed only by its coordinator. Other components receive immutable snapshots and emit intents.

### ARC-005 — One filesystem mutation gateway

- Status: **active**
- Enforcement: forbidden-API scan and dependency boundaries.

All copy, move, rename, create-directory, and delete behavior passes through `FileOperationGateway`. Presentation and application features must not call `System.IO`, Win32 file mutation APIs, shell operations, WSL commands, or provider-specific mutation APIs directly.

### ARC-006 — Explicit composition

- Status: **active**
- Enforcement: construction scan and composition tests.

`NeNeCommander.App` is the only composition root. Service locators, mutable global containers, ambient context, runtime assembly scanning, reflection-driven registration, and hidden singleton access are prohibited.

### ARC-007 — Determinism by injection

- Status: **active**
- Enforcement: forbidden-API scan and deterministic tests.

Time, identity generation, filesystem capabilities, and other nondeterministic inputs enter through explicit interfaces. Application behavior must not directly read the clock, generate random values, inspect process environment, or depend on enumeration order.

### ARC-008 — Validate once at the boundary

- Status: **active**
- Enforcement: strong-type APIs and boundary tests.

Untrusted strings are parsed once into validated types. Internal code accepts those types and does not repeatedly parse, normalize, trim, or reinterpret the same value.

### ARC-009 — Closed typed outcomes

- Status: **active**
- Enforcement: API review and exhaustive outcome tests.

Expected success, cancellation, collision, capability denial, access denial, not-found, and provider-unavailable cases use one closed result model. Exceptions are reserved for defects and impossible states after boundary normalization.

### ARC-010 — Structured asynchronous work

- Status: **active**
- Enforcement: compiler, analyzers, conformance scan, and cancellation tests.

I/O is asynchronous end to end. Work is awaited by its owner, cancellation is explicit, UI-thread affinity is isolated, and background exceptions are observed. Blocking waits and unowned fire-and-forget tasks are prohibited.

### ARC-011 — Provider boundaries are explicit

- Status: **active**
- Enforcement: `FileSystemPath` parsing and provider contract tests.

Windows local, Windows UNC, and WSL locations are closed provider variants with explicit capability sets. Behavior branches on the validated provider kind, never on ad hoc prefix checks spread through features.

### ARC-012 — Visual design is an adapter concern

- Status: **active**
- Enforcement: XAML token scan and design-handoff review.

Business behavior, keyboard intent, and pane state do not depend on colors, spacing, typography, animation, or a particular visual composition. Final design enters through semantic resources and presentation-only templates.

### ARC-013 — Architecture outranks convenience

- Status: **active**
- Enforcement: canonical gate and ADR review.

A shortcut that violates a dependency, ownership, or safety invariant is invalid even when it is smaller or faster. If the model cannot express a legitimate requirement, change the constitution by ADR before changing code.

## Protected invariants

These cannot be waived: dependency direction, one mutation gateway, no silent destructive behavior, warnings-as-errors, nullable analysis, no suppressions, reproducible dependency resolution, and the canonical gate.
