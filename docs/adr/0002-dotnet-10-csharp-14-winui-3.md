# ADR-0002: Use .NET 10, C# 14, and WinUI 3

Status: accepted

Date: 2026-09-02

## Context

The product targets Windows 11 and needs modern desktop UI, asynchronous APIs, analyzers, deterministic builds, and a supported long-term runtime.

## Decision

Use the exact .NET SDK in `global.json`, C# 14, WinUI 3 through the Windows App SDK, and Windows 11 as the supported operating-system family. Upgrade the SDK and packages only through a dedicated ADR update with a full gate pass.

## Rejected alternatives

- WPF: mature but not the selected Windows 11 UI stack.
- WinForms: insufficient fit for the intended adaptive, accessible presentation.
- Floating SDK selection: non-reproducible across developers and CI.
- Preview language/runtime features: unstable and unnecessary.

## Consequences

Build and UI integration require Windows tooling. Domain and application tests remain platform-independent by architecture. The exact SDK must be installed before any repository command.

## Migration and removal

None. This is the initial toolchain choice.

## Executable proof

`global.json`, `Directory.Build.props`, the CI setup step, and the SDK check in `eng/check.ps1`.
