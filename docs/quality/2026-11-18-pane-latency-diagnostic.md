# Pane latency diagnostic — 2026-11-18

Issue #451 records this bounded investigation at hide's stop request. It does not
change production code or establish the cause of hide's general latency report.
Related: #448, ADR-0061, QLT-015, GIT-001–004. Waivers: none.

## Question and measured boundary

After the F6 resource-key correction, the remaining question was whether ordinary
focus movement or Space selection performs costly work before WinUI rendering.
Source inspection shows that these intents use `PaneSession.HandleAsync` and the
canonical reducer without a directory read or settings write. The presenter owns
stable rows and replaces affected marks. `CommanderWindow.RenderAfterAsync`
renders before and after awaiting work; the diagnostic includes the presenter's
same-snapshot reuse, but does not execute the window or controls.

The probe references the normal Release Domain, Application and Presentation
assemblies, whose source is unchanged from public `v0.1.0-beta.1` through main
`2e4570a`. Their exact paths and before/after SHA-256 values are in `inputs.json`.
The initial fixture uses 100, 1,000 or the product maximum 10,000 in-memory file
entries, unknown metadata, UTC formatting, two selected entries and focus at the
end. Movement alternates between the last two entries; selection toggles the last
entry. The directory port returns an in-memory listing once per scenario. The
launcher throws if called. No real files, app window or keyboard are operated.

Each of the six scenarios has 50 warmups followed by 300 individual observations.
Each observation covers the existing session call, the existing incremental
presenter (bound once to a delegate by reflection), and its second call with the
same snapshot. Every observation verifies stable row-source identity and exact
same-snapshot presentation reuse. Each scenario verifies one directory-port call
and the expected row count. This diagnostic introduces no product hook or alternate
implementation path.

## Results

Milliseconds per measured observation, one run on the shared development host:

| Rows | Action | Median | p95 | Maximum |
| --- | --- | --- | --- | --- |
| 100 | Move near end | 0.0265 | 0.0380 | 0.0598 |
| 100 | Toggle selection | 0.0062 | 0.0132 | 0.0857 |
| 1,000 | Move near end | 0.2290 | 0.2491 | 0.4202 |
| 1,000 | Toggle selection | 0.0062 | 0.0124 | 0.0214 |
| 10,000 | Move near end | 1.2115 | 1.8561 | 31.9371 |
| 10,000 | Toggle selection | 0.0021 | 0.0044 | 0.0059 |

The first projection is separately timed once per scenario, outside the steady
observation series (0.1566–49.2986 ms). JIT/tiering, warm cache, scheduling and other host
activity are not separated; those values and the scenario-to-scenario differences
are not comparative performance proof. No threshold or latency guarantee is added.

The observed in-memory work did not reproduce a multi-second pause. This result
does not measure WinUI layout/painting, dispatcher/input delivery, resource-loader
work, filesystem/provider operations or antivirus, and does not identify hide's
exe, build or machine. The general latency cause and the on-screen outcome of
#448 remain unconfirmed. No speculative optimization follows this measurement.

## Commands, evidence and stop point

Permanent evidence: `D:/NeNeCommander/evidence/latency-20261118/`.

- `dotnet build LatencyProbe.csproj -c Release -m:2 -p:UseSharedCompilation=false`
  returned 0 with zero warnings/errors, using the copied repository SDK pin.
- `dotnet ./bin/Release/net10.0-windows10.0.26100.0/LatencyProbe.dll` returned 0.
  `results.json` retains all 1,800 raw observations, runtime metadata and checks;
  the two command JSON files retain exact invocations, times and exit codes.
- `Program.cs`, project and SDK files are preserved. `probe-output.zip` contains
  all 40 output files; every entry size/SHA-256 matches `archive-manifest.json`.
  The three referenced product assembly hashes were unchanged after execution.
- Cleanup preflight found no links, active process references or repository
  references. Automatic approval rejected deletion before process launch with
  `blocked by policy`. The output remains at
  `D:/NeNeCommander/outputs/latency-20261118`; no retry was attempted. Its prepared
  user cleanup script and the session master script were parsed, never executed.

hide requested stopping at a good checkpoint at 13:50 JST. This already-prepared
diagnostic and the final records are that checkpoint. Product tests, full/deep CI,
mutation, live UI, a new release and new implementation work are not started.
The next investigation requires the affected build and concrete slow interaction;
resume only on a new request.
