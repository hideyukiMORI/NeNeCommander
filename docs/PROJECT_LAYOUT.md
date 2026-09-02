# Project Layout

Status: normative

`eng/architecture.json` is the machine-readable project and reference allowlist. When implementation begins, the repository uses exactly this shape:

```text
src/
  NeNeCommander.App/
  NeNeCommander.Presentation.WinUI/
  NeNeCommander.Application/
  NeNeCommander.Domain/
  NeNeCommander.Infrastructure.Windows/
tests/
  NeNeCommander.Domain.Tests/
  NeNeCommander.Application.Tests/
  NeNeCommander.Infrastructure.Windows.Tests/
  NeNeCommander.Presentation.WinUI.Tests/
  NeNeCommander.Architecture.Tests/
eng/
  architecture.json
  check.ps1
  conformance.ps1
  prove-gates.ps1
docs/
  adr/
  quality/
  waivers/
```

## Placement law

| Content | Sole location |
|---|---|
| invariants, value objects, provider kinds, closed outcomes | `NeNeCommander.Domain` |
| use cases, ports, orchestration, reducers | `NeNeCommander.Application` |
| view models, deterministic input translation, intent mapping | `NeNeCommander.Presentation.WinUI` |
| local/UNC/WSL adapters, shell, settings persistence, OS integration | `NeNeCommander.Infrastructure.Windows` |
| executable startup, WinUI framework boundary, semantic XAML resources, dependency composition | `NeNeCommander.App` |
| automated behavioral proof | matching project under `tests/` |
| repository policy enforcement | `eng/` and `NeNeCommander.Architecture.Tests` |

## Folder law

- Folders are named for product concepts or adapter boundaries, not technical grab bags.
- `Helpers`, `Utils`, `Utilities`, `Common`, `Misc`, and equivalent catch-all folders are prohibited.
- A feature is placed by ownership. Mirrored copies across layers are prohibited.
- Generated files live under `obj/` or an explicitly declared generated directory and are never edited manually.
- Tests mirror the production namespace and file name using the suffix `Tests`.
- Each production file has one primary top-level type. Small private nested types are permitted when they cannot be reused independently.

## Adding a project

Adding, removing, renaming, or changing references for a project requires an accepted ADR and an atomic update to `eng/architecture.json`, this document, the solution, package allowlist, architecture tests, and lock files.
