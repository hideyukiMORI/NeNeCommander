# ADR-0073: Close concrete record cases with inaccessible abstract members

Status: accepted

Date: 2026-10-18

## Context

Issue #305 reproduced a gap in CS-002/004/008 at baseline
`1abea9392ab9421fe47246e761d655c62b49d112`, using the pinned SDK 10.0.401 and C# 14.0.
A private ordinary constructor does not close an abstract record: its synthesized
protected copy constructor admits `External(KnownRoot original) : base(original)`.
The real `WindowAdjustmentAction` accepts this construction in a non-friend assembly;
the pure planner treats that unknown action as a zero-distance move.

A compilation census found 130 abstract records in the four non-host production
assemblies. Of 119 publicly visible roots, 118 admit a concrete external record.
`FileSystemPath` alone rejects it because its internal abstract identity methods
cannot be implemented externally. The remaining 11 roots are nonpublic and were
not compiled as externally visible APIs. App has no abstract record declaration.
This is a type-contract defect, not evidence of an input-to-managed-code exploit.

The current window-model tests inspect only the root assembly and omit record copy
constructors. Their comment that only ordinary constructors govern case creation
is disproved. They establish the expected in-assembly cases, not external closure.
The investigation [report](../reports/2026-10-18-record-closure-daily-report.md)
contains the census, exact commands, bounded consumer findings and retained evidence.

Microsoft documents synthesized copy behavior and its visibility in the
[record reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record).
Restricting a non-sealed record's explicit copy constructor to private or private
protected fails with CS8878, as described in the
[constructor diagnostics](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/constructor-errors).
Both failures were reproduced with the pinned compiler. Preview language features
shown in current documentation do not change the repository's C# 14 contract.

On 2026-10-18 hide selected the recommended implementation in Issue #306.
This accepts the closure definition and structural mechanism below. Issue #305's
investigation remains historical evidence, not the implementation acceptance.

## Decision

Retain record hierarchies, existing factories, immutable state and dispatch owners.
Define closure precisely: ordinary C# code in a non-friend external assembly cannot
construct an additional concrete case. Do not claim that it cannot declare an
abstract derived type, or that assembly visibility is a malicious-code sandbox.

Require a nonpublic abstract instance member whose implementation is inaccessible
to external non-friend derived records, and keep every concrete case sealed.
Reuse an existing meaningful member where it already provides that guarantee,
as `FileSystemPath` does. Otherwise use the single structural member below on the
root and an empty override on each sealed case:

```csharp
private protected abstract void RestrictDerivation();
```

```csharp
private protected override void RestrictDerivation()
{
}
```

The member has no runtime dispatch role and is not called to validate an action.
It introduces no state, runtime registry, extra construction route, enum or tag.
An empty implementation is intentional compile-time structure; do not manufacture
a runtime call solely for coverage or add an exclusion. Existing factory guards,
generated record equality and copy/with behavior remain in place.

Issue #306 migrates the exposed roots and sealed cases together with friend tests.
The compiler proof lives under `eng/record-closure/`, launched by
`eng/verify-record-closure.ps1`. It uses the pinned SDK's Roslyn compiler and bundled
framework reference pack directly, with no project, package, production dependency
or runtime registry. Generated binaries, sources and diagnostics stay under the
existing ignored `artifacts/record-closure/` boundary.

`eng/prove-gates.ps1 -Rule CS-002` runs the checker's self-proof before product build:
a valid closed hierarchy, reopened private-constructor hierarchy and externally
implementable abstract-member hierarchy distinguish closure from ordinary abstract
requirements. The full proof entry includes this same self-proof. After Release
build, `eng/check.ps1` invokes the product mode over all four non-host production
assemblies. The existing manual quality/deep workflows inherit that call.

The compiler imports all metadata and discovers visible abstract records, including
future additions. For every root it accepts an external abstract copy-derived
control and known-case `with` usage, implements externally implementable abstract
members in probes, and requires the specific compiler refusal of ordinary, copy
and inaccessible-override concrete derivation. It also requires existing concrete
cases to be sealed. Unexpected diagnostics fail rather than count as closure.
Nonpublic roots are inventoried but excluded from external accessibility claims.
Compiler sources/diagnostics and a machine-readable result are retained; the
checker never calls the structural member or constructs uninitialized product
objects. Existing factory/behavior tests plus focused copy/equality/payload tests
establish runtime compatibility.

## Alternatives not selected

- Tighten only the ordinary constructor: already disproved by the real product.
- Make the copy constructor private or private protected: rejected by C# 14 with CS8878.
- Guard an explicit protected copy constructor with an exact runtime-type comparison:
  the prototype preserves known-case copy/equality and rejects unknown construction,
  but still compiles the unknown concrete type, moves failure to runtime and requires
  manually preserving every root field in copy construction. Prefer compile-time closure.
- Replace records with classes or enums: changes the existing equality, copy and
  CS-002/004 contracts more broadly than the demonstrated defect requires.
- Add a public marker interface, public abstract member or runtime type registry:
  an external type can implement public members; a registry would be another runtime
  mechanism rather than a restriction in the existing hierarchy.
- Upgrade SDK/language or use preview closure syntax: outside ADR-0002 and this task.

## Consequences and limits

The prototype rejects a concrete external derived record with CS0534, and rejects
an attempted inaccessible override with CS0115 plus CS0534. An external abstract
record still compiles. An explicitly named friend assembly can implement the member;
that is deliberate trusted-code access, consistent with Microsoft's
[private protected reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/private-protected).
Existing adversarial friend-test records must implement the member while retaining
their behavioral rejection tests. Do not expand InternalsVisibleTo for this work.

Root-visible and concrete-visible `with`, value equality, hash equality, preserved
read-only payload and nested private cases need compatibility proof. The isolated
prototype proves these ordinary record properties, not compatibility for all 118
product hierarchies. Reflection, unsafe/uninitialized construction and hostile
in-process code are outside this closure guarantee. Same-assembly case changes
remain subject to the existing source review and known-case checks.

Adding an abstract member changes the inheritance contract and can break separately
compiled external derived types. Preserve the approved public factories and cases;
do not describe the migration as an API-compatible patch for arbitrary subclassers.

## Migration and removal

1. Confirm this definition and mechanism before product edits. Use the saved census
   to enumerate each affected root, sealed cases, fields, consumers and friend tests.
2. In one coherent implementation PR, migrate the 118 exposed roots lacking closure,
   retaining the meaningful `FileSystemPath` identity contract. Nonpublic roots are
   already outside this external visibility boundary; do not add members by rote.
3. Replace the incorrect copy-constructor exclusion claim and expand the existing
   architecture/conformance proof to cover external compilation. Keep useful
   in-assembly enumeration rather than replacing it with a constructor heuristic.
4. Update CS-002/004/008, the test strategy and gate-proof registry together with
   their executable enforcement. No parallel legacy model or temporary bypass remains.
5. Keep explicit unknown-variant consumer rejection where it already exists. Any
   change to the planner's current fallback is a separately stated behavior decision,
   not a hidden effect of the type-closure migration.

## Executable proof required for acceptance

The saved investigation matrix has 11 expected compiler outcomes and runtime checks
for the structural proposal and runtime-copy alternative; all pass. The structural
proposal covers both private nested and public sealed cases, payload, equality,
hashing, `with`, concrete rejection, inaccessible override, abstract derivation and
friend access. The real planner reproduction and 119-root census are separate proof.

The implementation must promote external-compilation proof into the existing gate
path, using the pinned compiler without a new production dependency. It must pass
the real allowed-case assemblies and fail an ordinary external constructor, a copy
constructor bypass and an attempted inaccessible override for each affected root.
Include a deliberately reopened minimal hierarchy as the negative proof of the
closure checker itself. Wire the proof through `eng/check.ps1` and the existing
manual CI path, with a focused entry and a registry entry under QLT-006/007. These
are enforced by the Issue #306 integration described above; the implementation PR
records the actual positive and negative results before completion is claimed.

Run affected factory/equality/copy and friend-dispatch tests during implementation.
Because the proposed migration touches shared models across four layers and adds
compiler-driven gate behavior, selected toy fixtures cannot establish its full
impact: request the canonical gate once for the completed candidate under ADR-0061.
Validate actual branch-coverage and mutation implications without weakening thresholds
or suppressing findings. Request deep review only if a concrete trust-boundary impact
requires it; do not duplicate a full gate already included in successful deep proof.
The original Issue #305 investigation changed no production, tests, dependencies
or gates; its historical verification remains isolated fixture builds/runs and
documentation review. Issue #306's implementation has the wider scope above.
