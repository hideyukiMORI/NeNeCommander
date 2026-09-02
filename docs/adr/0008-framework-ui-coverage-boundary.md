# ADR-0008: Isolate the framework UI coverage boundary

- Status: Accepted
- Date: 2026-09-02

## Context

`CommanderWindow.xaml.cs` is a WinUI-created window boundary. Its constructor, routed-event arguments, focus manager, and XAML root require a live Windows UI thread and framework-created state. Treating that file as deterministic unit-testable code would require a second UI abstraction or unsafe construction of framework objects. Keyboard translation and intent mapping remain product logic and must stay covered. The framework boundary belongs to the App host so Presentation remains independently analyzable by the mandatory mutation runner.

## Decision

Exclude only generated `obj` sources, `GeneratedCodeAttribute` members, and `Views/CommanderWindow.xaml.cs` from the branch-coverage denominator. The exclusion is centralized in `eng/coverage.settings` and is protected by conformance checks. Keep the App-hosted window code-behind restricted to framework initialization, context observation, event forwarding, and event publication. Move every deterministic translation or decision into separately tested presentation classes. The Presentation project does not reference the WinUI runtime package; App translates live routed events into its deterministic API.

## Consequences

- Presentation branch coverage and mutation analysis measure all deterministic input translation and intent logic.
- Product decisions cannot be moved into the excluded window boundary because architecture and conformance rules reject them.
- Any additional source exclusion requires another accepted ADR and a corresponding gate change.

## Rejected alternatives

- Constructing WinUI routed-event objects without a live UI runtime: framework state is not a stable unit-test seam.
- Excluding the complete presentation assembly: this would hide keyboard product logic.
- Lowering the coverage threshold: this weakens the ratchet instead of defining the boundary.

## Enforcement

`eng/coverage.settings`, `eng/conformance.ps1`, `eng/verify-coverage.ps1`, and `eng/check.ps1`.
