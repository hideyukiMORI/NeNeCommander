# ADR-0006: Use MSTest on Microsoft.Testing.Platform

Status: accepted

Date: 2026-09-02

## Context

The repository requires one test runner, native .NET 10 orchestration, WinUI compatibility, code coverage, deterministic filtering, and no overlapping adapters.

## Decision

Use `MSTest.Sdk` 4.4.0 with the .NET 10 `Microsoft.Testing.Platform` runner selected in `global.json`. Use its Default extension profile for Microsoft code coverage and TRX support. VSTest mode and other test frameworks are prohibited. The pin was upgraded from 4.3.3 on 2026-09-05 after the official stable release review; the runner mechanism and extension profile did not change.

## Rejected alternatives

- Mixing MTP and VSTest: unsupported at solution scope and produces divergent CLI behavior.
- Multiple test frameworks: duplicates analyzers, naming, discovery, and lifecycle conventions.
- A third-party coverage collector: unnecessary while the chosen SDK supplies deterministic Cobertura output.

## Consequences

All test commands use the .NET 10 MTP form and every test project uses the same SDK. WinUI tests use Windows targets; non-UI layers remain portable.

## Migration and removal

None. This is the initial test platform.

## Executable proof

`global.json`, the CFG-002 pin-drift negative proof, test project SDK declarations, canonical test execution, minimum-test enforcement, and coverage verification.
