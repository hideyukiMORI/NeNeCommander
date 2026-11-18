# Owned CodeQL findings — Issue #433

The release-readiness read found 37 open owned-source quality notes: 32
`Path.Combine` recommendations, three Select recommendations, one Where
recommendation and one catch-all. The ten original files still matched the
recorded main analysis and base `97d8a12`. These are quality notes without a
security severity; the path query does not establish 32 exploitable paths.

## Change and invariant

Fixed relative components use `Path.Join`. The source catalog explicitly rejects
rooted manifest paths before retaining its repository-containment check. The
record-closure compiler admits a probe name through one direct-filename guard
before deleting or writing artifacts. Its program rejects an unknown mode before
reading references or creating output. Complexity reporting catches eight known
input, I/O and compiler-related exception families; unexpected exceptions escape
as failures. The four LINQ rewrites retain lazy enumeration, evaluation order and
the existing target brush instances.

There is no new product feature, dependency, pin, schema, suppression, dismissal,
baseline, exclusion, threshold or CodeQL configuration change. Rules:
CS-013/014/017/020, SEC-002/008/009/011/013, QLT-012/013/015,
GIT-001–004, ADR-0031/0061/0073/0075. Waivers: none.

## Selected and executed proof

Tool self-proofs exercise the changed path and analysis boundaries; the App and
Architecture project build checks the only production-source rewrite. Under
ADR-0061 unrelated product suites and a separate full gate are not selected.
One release deep review will provide the full candidate proof and CodeQL analysis.

| Command | Result |
| --- | --- |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode SelfTest` | Exit 0; all 25 groups passed, including rooted and escaping project paths |
| `pwsh -NoProfile -File eng/verify-record-closure.ps1 -Mode SelfTest` | Exit 0; existing closure/compiler proofs and seven invalid probe names passed |
| `pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect` | Exit 1; 28 existing repository findings, matching the prior count; not a repository-wide complexity pass |
| `dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --locked-mode -m:1 -nodeReuse:false` | Exit 0 |
| `dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false` | Exit 0; App included, zero warnings/errors |
| `dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~ColorSchemeResourceTests --minimum-expected-tests 3` | Exit 0; 3 passed, zero failed/skipped; TRX saved |
| `dotnet format whitespace NeNeCommander.App.csproj --no-restore --verify-no-changes --verbosity diagnostic --include Themes/ColorSchemeResources.cs` from the App directory | Exit 0; no edits; Debug/WinUI workspace-load warnings are preserved in the diagnostic log |

The three existing theme tests cover dictionary paths, theme selection and null
guards. They do not execute `CopySchemeInPlace`; preservation of its brush
identity and update order is a source-review conclusion, not a live UI result.
The source catalog's existing src/tests check uses the raw relative prefix;
this change proves repository escape and rooted-path rejection, not normalized
src/tests-only containment for an internal `..` path.

An independent read-only review found no required code correction. Its record
contains the original and reviewed blob IDs. Exact arguments, timing, exits, raw
logs, self-proof JSON, Inspect output, original inputs and review are retained
under `D:/NeNeCommander/evidence/issue-433/`; TRX is under the corresponding task
output until archived. No GUI, user-data operation or unrelated test ran.

CodeQL workflow success and closure of alerts are separate conditions. The
post-analysis branch/main alert API result and release evidence remain pending;
this local verification does not claim zero open alerts or a completed release.
