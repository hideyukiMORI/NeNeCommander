# ADR-0006: Use MSTest on Microsoft.Testing.Platform

Status: accepted

Date: 2026-09-02

## Context

The repository requires one test runner, native .NET 10 orchestration, WinUI compatibility, code coverage, deterministic filtering, and no overlapping adapters.

## Decision

Use `MSTest.Sdk` 4.5.1 with the .NET 10 `Microsoft.Testing.Platform` runner selected in `global.json`. Use its Default extension profile for Microsoft code coverage and TRX support. VSTest mode and other test frameworks are prohibited. The pin was upgraded from 4.3.3 to 4.4.0 on 2026-09-05, from 4.4.0 to 4.4.1 on 2026-09-17, and from 4.4.1 to 4.5.1 on 2026-10-08, each time after the official stable release review; the runner mechanism and extension profile did not change. ADR-0048 defines the sole exception: the mutation tier drives the same test assemblies through an isolating VSTest host because the pinned Stryker.NET MTP runner cannot isolate static mutants; canonical test execution stays on MTP and is unchanged.

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

## Pin review 2026-10-08 (MSTest.Sdk 4.5.1)

Accepted by the NeNe Commander design owner under hide's delegated authority for Issue #165, replacing Dependabot PR #164. This dedicated ADR update also accepts the coverage extension's additional transitive dependencies below. It extends the existing test-platform choice; no direct package reference, production dependency, test runner, or coverage mechanism is added.

The [official stable release](https://github.com/microsoft/testfx/releases/tag/v4.5.1) was published on 2026-10-07. The [official changelog](https://github.com/microsoft/testfx/blob/main/docs/Changelog.md#4.5.1) explains that 4.5.0 could not ship because of release infrastructure problems. The published 4.5.1 SDK, rather than the unreleased 4.5.0 label or the tag's stale `Unreleased` heading, is the reviewed input. Changes include new analyzer diagnostics and optional testing capabilities; the existing projects keep the Default extension profile and do not opt into UIAutomation, Hosting, or packaged-app test controllers.

The published [MSTest.Sdk package](https://www.nuget.org/packages/MSTest.Sdk/4.5.1) pins MTP 2.5.1, Microsoft code coverage extension 18.12.0, and `Microsoft.NET.Test.Sdk` 18.10.1. The adapter also requires `Microsoft.TestPlatform.ObjectModel` at least 18.10.1. Therefore `Directory.Packages.props` advances the existing test SDK pin from 18.9.0 to 18.10.1 in the same change, preserving ADR-0048's single package graph and isolating VSTest host. Keeping 18.9.0 is rejected because it no longer matches the pinned MSTest SDK. Changing `UseVSTest`, the canonical runner, or the coverage profile to avoid this update is rejected because it would replace established mechanisms without solving a product need.

The [coverage extension package](https://www.nuget.org/packages/Microsoft.Testing.Extensions.CodeCoverage/18.12.0) declares four additional implementation dependencies: `Mono.Cecil` 0.11.6, `System.IO.Pipelines` 10.0.10, `System.Text.Encodings.Web` 10.0.10, and `System.Text.Json` 10.0.10. On the pinned .NET 10 targets, restore prunes the three framework-provided `System.*` packages; the actual addition to each of the five test-project lock files is only `Mono.Cecil` 0.11.6. This graph is accepted only through the existing Microsoft coverage extension, whose branch-coverage output is required by TST-009; no production lock file changes. The exact resolved graph and content hashes belong in the lock files and remain subject to SEC-007 audit. Adding direct references or separate utilities around these packages is outside this decision. Removing the coverage extension or excluding its dependencies would break the selected Default profile; remaining on 4.4.1 is a valid rollback of this entire pin change if the required proof fails, not permission to mix incompatible pins.

Proof for this update consists of a locked Release restore, a warning-free Release build of every test project, CFG-002 success and pin-drift rejection, MTP execution producing both Cobertura and TRX, and the final canonical Ready CI gate over all tests and the four coverage thresholds. Mutation compatibility is checked through Domain and the Presentation `OperationStatus` static-resource mutants using the unchanged Stryker VSTest configuration. Those limited checks do not claim a new all-layer deep review; the remaining layers stay in the scheduled tier. Integration results are recorded in the PR closing Issue #165.

At the next MSTest or coverage pin change, review the added dependency graph, its test-only scope, Default-profile behavior, and compatibility with ADR-0048 again. If the upstream extension stops requiring any of these packages, remove that transitive entry through lock regeneration in the same pin update. Removing the coverage mechanism itself requires a new architectural decision. ADR-0048's separate Stryker migration condition remains unchanged.
