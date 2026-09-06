# C# Coding Rules

Status: normative

These rules define the only approved C# subset for production and test code. Compiler acceptance alone is insufficient. Rule IDs are stable; reuse them in diagnostics, reviews, ADRs, and gate proofs.

## Type and state design

### CS-001 — Strong types at every boundary

- Status: **active**
- Enforcement: API conformance scan and tests.

Raw strings, integers, tuples, and dictionaries may not represent paths, item identities, sizes, provider kinds, operation requests, or outcomes after boundary parsing. Introduce a precisely named immutable type with one construction path.

### CS-002 — Closed states, not flags

- Status: **active**
- Enforcement: public-API scan and review.

Domain and application states use sealed record-class hierarchies. Boolean mode parameters, magic strings, nullable state switches, bit fields, and combinations of flags that permit impossible states are prohibited.

### CS-003 — Project-owned value types are reference types

- Status: **active**
- Enforcement: syntax scan.

Project-owned `struct`, `record struct`, and mutable tuple models are prohibited because `default(T)` bypasses invariants. Use a sealed record class with a private constructor and the canonical factory. Framework value types may be consumed normally.

### CS-004 — Domain choices are not enums

- Status: **active**
- Enforcement: syntax scan and review.

Domain and application choices use closed sealed record-class hierarchies so arbitrary integer casts cannot create invalid values. Enums are permitted only at framework interop boundaries and must be translated immediately through an exhaustive switch.

### CS-005 — Null has one meaning

- Status: **active**
- Enforcement: nullable compiler analysis and suppression scan.

Null means absence only. It never means invalid, failed, not loaded, cancelled, or unknown. The null-forgiving operator, nullable-disable directives, and warning suppression are prohibited. Boundary data is validated before entering non-nullable APIs.

### CS-006 — Expected outcomes are typed

- Status: **active**
- Enforcement: API scan and command tests.

Expected failures return the canonical closed result type. Do not return success booleans, error strings, null, default objects, or framework exceptions as business outcomes. Do not create feature-local result abstractions.

### CS-007 — Public state is immutable

- Status: **active**
- Enforcement: API scan.

Public and internal models expose constructor-complete immutable state. Public setters, mutable collection types, mutable fields, and mutation methods on state snapshots are prohibited.

### CS-008 — One construction path

- Status: **active**
- Enforcement: constructor scan and tests.

Invariant-bearing types have private constructors and one canonical named factory, normally `Parse` for boundary text or `Create` for validated components. Parallel constructors, `TryCreate`, builders, object initializers, and partially initialized states are prohibited.

## Surface and organization

### CS-009 — Minimal visibility

- Status: **active**
- Enforcement: analyzer and review.

Default to `private`, then `internal`. A type or member is public only when another project must consume it. Interfaces exist only at a dependency inversion boundary or where multiple real implementations are required.

### CS-010 — No ambient access

- Status: **active**
- Enforcement: forbidden-API scan.

Mutable statics, service locators, global containers, ambient contexts, reflection-based discovery, runtime assembly scanning, and direct reads of time, randomness, environment, registry, or current directory are prohibited outside named adapters. The CS-010 forbidden-API table rejects aliases for `System.DateTime`, `System.DateTimeOffset`, `System.TimeProvider`, `System.Environment`, `System.Diagnostics`, and `System.Diagnostics.Stopwatch`. It separately rejects `using static` directives for the clock-bearing types `System.DateTime`, `System.DateTimeOffset`, `System.TimeProvider`, `System.Environment`, and `System.Diagnostics.Stopwatch`, plus `TimeProvider.System` and `Environment.TickCount` / `TickCount64`. Every `Stopwatch` type-token reference is rejected outside the exact named `IClock` adapter path, which also closes qualified, target-typed, and initializer construction. The settings-location adapter exception permits only its direct `Environment` location access; it does not permit an environment clock. This deliberately rejects safe-looking date arithmetic and non-ambient `Stopwatch` type use; the repository uses named clock adapters instead. The check is a constrained raw-text scan, not C# semantic analysis: it retains interpolation coverage and may reject matching text in comments or strings. It does not resolve identifier shadowing, Unicode-escaped identifiers, or comments inserted between tokens; those forms remain outside the permitted repository language rather than creating a second lexer.

### CS-011 — Names identify one responsibility

- Status: **active**
- Enforcement: name scan.

Use vocabulary from `docs/GLOSSARY.md`. `Manager`, `Helper`, `Helpers`, `Util`, `Utils`, `Utility`, `Utilities`, `Common`, `Misc`, `Base`, and `General` are prohibited in project-owned type, member, and folder names. Avoid `Data`, `Info`, and `Item` unless the glossary defines the exact concept.

### CS-012 — One primary type per file

- Status: **active**
- Enforcement: syntax scan.

The file name matches its single primary top-level type. Small private nested types are allowed only when meaningless outside their owner. Multiple public or internal top-level types in one file are prohibited.

### CS-013 — Complexity has hard ceilings

- Status: **active**
- Enforcement: analyzer at implementation stage.

A method has at most 40 logical lines, cognitive complexity 10, nesting depth 3, and 4 parameters. A type has at most 300 logical lines. Split behavior by named responsibility before reaching a limit; do not extract arbitrary fragments into generic helper modules.

## Language form

### CS-014 — Explicit, uniform syntax

- Status: **active**
- Enforcement: `.editorconfig`, formatter, and syntax scan.

Use explicit local types, file-scoped namespaces, braces for every control block, using directives outside namespaces, and block bodies for methods and constructors. Primary constructors are prohibited. Properties and accessors may be expression-bodied only on one line. One statement per line.

### CS-015 — Collections do not leak mutation

- Status: **active**
- Enforcement: API scan.

Public APIs expose `IReadOnlyList<T>` or a purpose-specific immutable abstraction. Do not expose arrays, `List<T>`, mutable dictionaries, or mutable enumerable sources. Materialize an owned snapshot at the boundary.

### CS-016 — Async is end to end

- Status: **active**
- Enforcement: analyzer, forbidden-API scan, and tests.

Awaitable methods end in `Async`. I/O methods accept `CancellationToken` as the last required parameter. `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `async void` except framework event forwarding, unowned tasks, and `Task.Run` around naturally asynchronous I/O are prohibited.

### CS-017 — Exceptions stop at adapters

- Status: **active**
- Enforcement: adapter tests and review.

Catch only exceptions that can be normalized or enriched. Platform adapters translate expected platform failures to the canonical outcome model. Empty catches, broad `catch (Exception)` for control flow, throw-to-probe behavior, exception swallowing, and rethrowing a new exception without the original cause are prohibited.

### CS-018 — Platform APIs stay in infrastructure

- Status: **active**
- Enforcement: namespace and forbidden-API scan.

`System.IO`, Win32, Windows Storage, shell execution, registry, process APIs, WSL invocation, and OS capability probing belong only to `NeNeCommander.Infrastructure.Windows` or the executable composition root when startup itself requires them. Domain, application, and presentation code consume ports. `NeNeCommander.Infrastructure.Windows.Tests` may use `System.IO` only through the test-owned temporary root harness defined by ADR-0011.

### CS-019 — Generated code is deterministic

- Status: **active**
- Enforcement: clean-tree build proof and path scan.

Generated output is reproducible from committed inputs, written only to declared generated locations, and not edited manually. Generated files are not committed unless an ADR records why deterministic regeneration cannot be used.

### CS-020 — Suppressions are prohibited

- Status: **active**
- Enforcement: repository scan.

`#pragma warning disable`, `SuppressMessage`, `NoWarn`, `WarningsNotAsErrors`, nullable disable, analyzer baselines, and editor-only severity reductions are prohibited. Fix the cause. A rule change requires an ADR and an executable negative proof; a waiver cannot suppress a compiler or analyzer diagnostic.

## Presentation

### CS-021 — One MVVM mechanism

- Status: **active**
- Enforcement: package allowlist and syntax scan.

CommunityToolkit.Mvvm source generators are the sole view-model notification and command mechanism. View models derive from no project-owned base class. Manual `INotifyPropertyChanged`, custom `ICommand`, custom relay commands, and a second MVVM framework are prohibited.

### CS-022 — Code-behind only forwards framework events

- Status: **active**
- Enforcement: presentation scan and tests.

Code-behind may initialize components, maintain framework-required view references, and forward events as typed intents. It contains no file operation, navigation decision, validation, state mutation, provider selection, settings policy, or user-facing string construction.

### CS-023 — UI text and styling are resources

- Status: **active**
- Enforcement: XAML and C# scans.

User-facing text is referenced from localization resources. Colors, brushes, spacing, typography, corner radii, control density, and motion are referenced through semantic design resources. Hard-coded visual constants in views are prohibited except values explicitly documented as framework geometry.

## Dependencies and documentation

### CS-024 — Dependencies are allowlisted and pinned

- Status: **active**
- Enforcement: central package management, lock files, and manifest scan.

Every package must solve a documented requirement that the platform and existing dependencies cannot. Versions live only in `Directory.Packages.props`; restores use lock files. Floating versions, per-project versions, transitive reliance without pinning, and overlapping libraries are prohibited.

### CS-025 — Technical language is English

- Status: **active**
- Enforcement: review and resource scan.

Identifiers, XML documentation, diagnostics, logs, tests, ADRs, and technical documentation are English. Commit type, scope, and Conventional Commit keywords are English; commit descriptions and bodies are Japanese under `docs/COMMIT_CONVENTIONS.md`. User-facing text is never assembled in production code and must be localizable.

### CS-026 — Public APIs explain invariants

- Status: **active**
- Enforcement: XML documentation compiler diagnostics.

Every public type and member has XML documentation describing its contract, invariant, ownership, and relevant cancellation or failure behavior. Comments explain why a constraint exists; they do not narrate obvious syntax.

## Testing form

### CS-027 — Tests use one style

- Status: **active**
- Enforcement: test scan and review.

Use MSTest through `MSTest.Sdk`. Test methods are named `MethodWhenConditionExpectedOutcome`; asynchronous tests add `Async` to `Method`. Tests follow arrange, act, assert in that order and prove one behavior. Hand-written fakes are used at declared ports; dynamic mocking frameworks, sleeps, wall-clock dependence, random unseeded data, and tests that depend on execution order are prohibited.

### CS-028 — Destructive tests are isolated

- Status: **active**
- Enforcement: integration harness and test scan.

Filesystem mutation tests operate only inside a unique test-owned temporary root that is resolved and asserted before use. WSL integration tests require a dedicated opt-in root and never use a home directory, repository root, mounted Windows root, or arbitrary user path.
