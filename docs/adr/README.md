# Architecture Decision Records

Status: normative

ADRs record a durable choice where more than one plausible mechanism exists. Accepted ADRs are constitutional input; they do not override a later contradictory ADR unless that ADR explicitly supersedes them.

States are `proposed`, `accepted`, `superseded`, or `rejected`. An accepted ADR contains context, the single chosen mechanism, alternatives rejected, consequences, migration/removal work, and executable proof. Copy `0000-template.md` and allocate the next number.

An ADR is required for project-graph changes, a new dependency, a canonical-mechanism change, a provider-policy change, a protected API exception, a coverage exclusion, or a gate change. An ADR cannot authorize suppressions, silent destructive behavior, or skipping the canonical gate.

## Accepted decisions

- [ADR-0001: Strictness is mechanically enforced](0001-strictness-is-mechanically-enforced.md)
- [ADR-0002: Use .NET 10, C# 14, and WinUI 3](0002-dotnet-10-csharp-14-winui-3.md)
- [ADR-0003: Use CommunityToolkit.Mvvm generators](0003-communitytoolkit-mvvm-generators.md)
- [ADR-0004: Model WSL as a provider boundary](0004-wsl-provider-boundary.md)
- [ADR-0005: Use the five-project layered graph](0005-layered-project-graph.md)
- [ADR-0006: Use MSTest on Microsoft.Testing.Platform](0006-microsoft-testing-platform.md)
- [ADR-0007: Use the NENE2 Issue-driven public repository lifecycle](0007-issue-driven-public-repository.md)
- [ADR-0008: Isolate the framework UI coverage boundary](0008-framework-ui-coverage-boundary.md)
- [ADR-0009: Restore dependencies under the build configuration](0009-release-configuration-restore.md)
