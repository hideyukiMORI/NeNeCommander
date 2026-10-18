# ADR-0075: Define CS-013 metrics through one standalone SDK-based checker

Status: accepted

Date: 2026-10-18

Accepted within hide's explicit request to define and implement the next CS-013
measurement step (Issue #324). This accepts measurement semantics and a standalone
tool, not global gate activation or the subsequent product refactors.

## Context

ADR-0051 reserves global CS-013 gating for a separate decision. Issue #322 found
five product types above 300 logical lines and 42 parameter lists above four, but
cognitive complexity remained unmeasured and nesting was provisional. Thirteen
parameter lists are fixed native signatures; eight more are managed mirrors.
Enabling a gate now would either fail existing code or require an invalid baseline.

## Decision

`eng/measure-complexity.ps1` and `eng/complexity/` are the sole maintained CS-013
measurement implementation. Like the ADR-0073 compiler tool, the launcher builds
only its own sources with the exact SDK's compiler, BCL references and bundled
Roslyn assemblies. It adds no solution project, package, restored analyzer or
product build. The output is ignored `artifacts/complexity/`.

The named metric version is **cs013-v1**. Limits remain method logical lines 40,
cognitive complexity 10, nesting 3, owned parameters 4 and type logical lines 300.
The following definitions supersede the provisional #322 audit definitions.

### Source and member scope

Inspect every working-tree C# source under `src`, `tests` and `eng`, including
untracked and ignored handwritten sources. Only the manifest projects' root
`bin`/`obj` directories are generated exclusions. Generated-looking comments do not
exclude source. A source outside its declared project is an error; `eng` is one
engineering analysis unit. Missing/empty roots or projects, links, invalid C# and
conditional compilation directives fail inspection. Conditional variants cannot
silently disappear under a default symbol set. Supporting them requires a future
versioned metric decision and complete variant proof.
One source is limited to one MiB; exceeding the bound is an input failure.

All declared types, including nested types, contribute logical lines. Partial
declarations are combined by project and fully qualified metadata identity,
including generic arity and file-local identity. A nested type also contributes to
its containing type. Declarations/signatures/attributes count; surrounding using
directives and namespaces do not. A logical line is a nonblank physical line
containing a syntax token other than a structural brace. Comments and brace-only
lines are ignored; nonblank lines inside multiline string tokens count.

Methods, constructors, destructors, operators/conversions, explicit accessors,
expression-bodied properties, all indexers, local functions and anonymous functions are
measured. A nested callable is independently measured and remains part of the
enclosing callable's line/flow load. Auto-accessors without a body contribute zero
flow complexity. Parameters include optional, extension-receiver, `params` and
ref/out/in declarations; implicit setter `value` and synthesized members are not
additional explicit parameters. Delegate declarations also expose their declared
parameter count. Primary constructors and top-level statements are refused under
the existing explicit-construction/source-form rules, not skipped.

### Cognitive complexity and nesting

The flow weights are informed by [SonarSource's Cognitive Complexity white paper,
version 1.7](https://www.sonarsource.com/docs/CognitiveComplexity.pdf), with the
following repository-specific C# and static-call-graph definition. This independent
implementation does not incorporate Sonar analyzer code or claim product parity.

Start at zero. An if, loop, catch, switch statement/expression or conditional
expression adds one plus its enclosing flow depth. Else-if and else add one without
that surcharge; their bodies have the same depth as the first if's body. Switch
arms/cases do not add individually. Each goto adds one. Each consecutive run of
like short-circuit operators (`&&`, `||`) or pattern operators (`and`, `or`) adds
one; parentheses preserve runs, while negation starts a separate expression.
Return, throw, ordinary break/continue, null-coalescing, try/finally, using and lock
add nothing by themselves. Nested callables add one context level to the enclosing
analysis, but start at zero for their own result. Maximum reached context depth is
the nesting measure.

Add one per callable participating in a cycle of the source-call graph. Nodes are
declared callable symbols, and invocation edges use Roslyn-resolved source targets
or all source candidates when overload resolution is ambiguous. Normalize generic
and partial symbols; the implementation of a partial method owns its cycle penalty,
while its bodyless declaration has none. Graph cycles include indirect recursion; repeated recursive
calls still add only one. Calls through delegates, dynamic dispatch or unknown
external targets do not invent source edges. Semantic models use framework and
SDK references plus the transitive declared project graph, not a product build; unresolved
invocations are counted in the report. This is a specified static metric, not a
complete runtime call graph or proof that the program compiles.

### Native signature boundary

Always report the raw parameter count. The four-parameter design ceiling applies
to signatures controlled by the project, including test code and managed callback
mirrors. It does not require rewriting an externally fixed ABI signature. Classify
an ABI signature only through framework metadata symbols and structural proof:

- A static bodyless extern `DllImport` or static bodyless partial `LibraryImport`
  method with the actual framework attribute.
- A bodyless interface method on a real `GeneratedComInterface` carrying a Guid.
- The exact implementing method mapped by Roslyn to such an interface, on a class
  carrying the actual framework `GeneratedComClass` attribute.

Source-defined lookalike attributes, matching names, ordinary interfaces, unrelated
overloads and managed forwarding mirrors are insufficient. This narrow ownership
definition applies only to the parameter metric. Native bodies/types still receive
all other metrics. The report identifies each native classification and reason;
there is no path/type baseline, wildcard exception or suppression.

### Execution and adoption

`Inspect` reports all measurements and every over-limit metric in deterministic
order, returning 1 for findings, 0 only for complete conformity and 2 for tool/input
failure. `SelfTest` returns 0 only after positive, negative and boundary fixtures
pass. The launcher invalidates any previous result before compiling or inspecting,
so a failed run cannot expose stale success. Reports contain relative paths,
source hashes (UTF-8 encoding of decoded source text), metric/parser versions and
unresolved-call counts. A nonexistent/unwritable output root can prevent writing
a report; exit 2 is authoritative and a prior file never overrides a failed exit.

Neither mode is connected to `eng/check.ps1`, hooks or CI by this ADR. Existing
findings remain visible and must be resolved before the separate global adoption
step. Existing compiler/analyzer enforcement, CI cadence and protected thresholds
remain unchanged. The #318 composition test remains until the later adoption
change reconciles it with the common checker.

## Rejected alternatives

- A new analyzer package: the pinned SDK already supplies the required parser and
  symbol model; a second dependency and policy engine is unnecessary.
- Regex-only metrics or parameter-name matching: neither defines nested syntax,
  partial identity, recursion or the native boundary adequately.
- Treating control depth as cognitive complexity, omitting test code, or silently
  ignoring inactive/generated-looking source: each would misstate compliance.
- Altering native argument lists, exempting their entire files, or exempting managed
  mirrors: only the fixed signature lacks project design freedom.
- Wiring a changed-files-only checker or raising limits to get a green gate: these
  would grandfather existing violations rather than implement the rule.

## Consequences

Metrics are reproducible without executing product code. Flow/call-graph semantics
are explicit and versioned; they do not claim equivalence to a commercial analyzer.
The syntax subset fails closed on unsupported conditional/primary/top-level forms.
Native recognition uses semantic metadata rather than a maintained name list.
Current findings can be inspected immediately while responsibility refactors remain
separate work. Any later metric/input/native-policy change requires a reviewed ADR
update and corresponding fixtures.

## Migration and removal

Replace the #322 one-off audit as the maintained/recommended execution path. Keep
its dated evidence for provenance, mark its counts historical and link the new
report. Update coding rules, layout, quality documentation and project state in
this change. No production API, file-operation mechanism or CI stage is migrated.

## Executable proof

Tool-only fixtures cover every threshold at and beyond its limit, comment/string
and multiline handling, nested/partial/generic/file-local identities, overloads and
direct/indirect recursion, short-circuit/pattern sequences, branch/catch/switch and
nested callable weights, valid/spoofed native declarations and managed mirrors,
untracked input, parse/conditional/empty/missing/link refusal, deterministic results
and stale-result invalidation. Inspection of the current source must retain its
nonzero finding result. ADR-0061 selects these tool proofs plus normal Commit
checks; no unrelated product suites or full/deep CI run for this tooling change.
