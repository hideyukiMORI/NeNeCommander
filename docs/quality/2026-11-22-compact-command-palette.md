# Compact command palette — Issue #525

Date: 2026-11-22
Status: local XAML and selected geometry/API-scroll proof; one synthetic short-height case, native acceptance and publication remain unresolved.
Base: main7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2.

## Change and scope

Only CommanderWindow.xaml and DesignTokens.xaml change product behavior.
The existing command-palette surface is capped at640 effective pixels with24
outer inset, retaining its near-top placement. Marker/shortcut columns remain
Auto; the title/target and reason become2-star/1-star columns so a long reason
cannot force the row beyond its slot. Hints reuse the existing KeyHintWrapPanel
and canonical key-hint template/source. No C#, resources, input/session/IME,
settings, provider, command execution, package or saved schema changes.

The [design handoff](../design/2026-11-22-compact-command-palette-handoff.md)
records external advice and the exact engineering adaptations. This is one
existing pane-overlay/command mechanism (ARC-001/012, CS-023, KBD-001..005).
#526 target-pane consolidation and #548 Locations geometry are independent.

## Trials and native layout evidence

Evidence: D:/NeNeCommander/evidence/compact-modal-design-20261122.
Task: D:/NeNeCommander/worktrees/issue-525.
Fixture/output: D:/NeNeCommander/outputs/compact-modal-design-20261122/layout-probe.

A task-owned WinUI fixture uses the same pinned WindowsAppSDK package, reads the
actual XAML/resources and links the unchanged production KeyHintWrapPanel source.
It replaces template binding values with explicit fixture texts and removes the
row Loaded callback, then calls Measure/Arrange on detached elements. It creates
no Window, does not activate/focus anything, sends no input and reads no settings
or user directory. Each result records its process ID, exact view SHA256 and limits.
This does not establish native keyboard/IME behavior or complete product layout.

- Baseline matches the accepted main view byte hash. At width320, English/Japanese
  rows measure376/357 and last hints end at417/378, demonstrating horizontal excess.
  Other baseline widths1280/640/480 do not show that excess for these texts.
- A built-in WrapPanel was tried after finding its registration in the pinned
  package and documentation. Runtime XamlReader rejects the type as unavailable.
  The error is retained in changed-layout.json.error.txt. Registration alone was
  insufficient; no suppression/dependency update was introduced. The final XAML
  uses the already-existing project KeyHintWrapPanel instead.
- With the final product inputs, eight short-title日英 cases at1280/640/480/320
  pass horizontal bounds. At320, rows measure320 and hints wrap onto two rows.
- Six additional longest-real-label cases at592/272/128 fit, retaining title
  ellipsis and complete hint units. Four actual surface cases prove client1280
  gives centered640; clients640/320/200 give592/272/152 with24 side insets.
- The first expanded Chinese fixture accidentally chose a longer English title.
  Its three cases are retained but superseded. Only the corrected three Chinese
  cases rerun, at592/272/128; the long Chinese title measures273 at592, truncates
  at narrower widths, and all five hints remain inside via1/3/5 rows. No unchanged
  English/Japanese or surface case is rerun for this fixture correction.
- Accepted distinct proof is17 row/hint cases plus4 surface cases; all relevant
  bounds are checked in layout-comparison.json. Synthetic Chinese is no claim
  that #528's locale is shipped. Native title IsTextTrimmed is observed, but the
  production Loaded tooltip binding is not executed by this detached fixture.
- App locked Release restore/build succeeds with zero warnings/errors. All C#,
  session/mapper tests and dependencies equal the accepted base; their existing
  behavior proof is reused. No unrelated product suite, full CI, coverage,
  mutation or CS-013 inspection is repeated for two XAML changes.

## Attached composition follow-up at19:43 JST

A separate `attached-probe` uses the #551/#548 finding that an owned unshown Window
can attach XamlRoot after dispatcher progress. It measures actual native client
sizes640/320 by800/300 at125%, using the complete palette surface, search box,
12 literal long-title/unavailable rows, detail and five canonical hints. English
and Japanese use shipped resources; Chinese remains synthetic future stress text.
The actual CommandPaletteRowContainerStyle and KeyHintWrapPanel are loaded. The
row Loaded tooltip handler is removed, so tooltip binding is not tested.

All attempts are retained, and only the final lifecycle variant collected results:

- Initial restore/build succeeded0 warnings/errors. Parsing failed because the
  fixture had omitted the window-scoped CommandPaletteRowContainerStyle. The
  launcher stopped its owned process at20 seconds. PID was not captured by that
  first launcher, and no successful layout is claimed. `attached-missing-style*`
  and `attached-probe-missing-style.cs` preserve the error/source/timeout.
- The exact product style was added to the fixture, and diagnostic failure cleanup
  was changed to exit only its own process after writing an error. Build again
  succeeded0 warnings/errors. Process8496 exited2 because the first130 ms tick did
  not yet have XamlRoot. `attached-style-corrected*`, `attached-single-tick.error.txt`
  and `attached-probe-single-tick.cs` preserve this invalid one-tick assumption.
- The final fixture explicitly sizes its own native client, waits for actual
  XamlRoot within the same20-second overall bound, then applies the requested
  effective client size. No longer delay is treated as success. Lifecycle build
  succeeded0 warnings/errors; process21264 exited0 without reaching the bound.
  `attached-lifecycle-*` and `full-attached-lifecycle-layout.json` record this run.

All12 final conditions acquire XamlRoot, match the requested client size, reach
and realize the last row, return the first row to top0/offset0, and keep the modal
and hints inside24 inset. All8 shipped-language conditions contain the entire last
row. Of the4 synthetic-Chinese conditions,3 contain it entirely; at320×300 the
viewport is32 while the row is33.6 effective pixels high, so about1.6 is outside.
This stress case remains unresolved, not rounded into a pass. Complete-row proof
is11/12; arrival/return and outer geometry are12/12. It needs follow-up in the
Chinese locale/short-height design before that combination can be accepted.

`attached-scroll-comparison.json` records these distinct assertions with0.02-pixel
float tolerance and confirms all1079 original product inputs unchanged. Final
activation count is0, ever-visible is false, and all sampled foreground values
match the initial handle. No Show/Activate, injected input, product/session,
settings or user data is used. Final source and the exact product panel source are
in `attached-fixture-source/` with hashes. Prior successful App/geometry proof is
reused; no product build/test/CI repeats. The fixture's timer is diagnostic sampling,
not new product code or a deterministic product-test claim.

From attached-probe, commands were `dotnet restore LayoutProbe.csproj --locked-mode
-p:Configuration=Release` then `dotnet build LayoutProbe.csproj -c Release --no-restore
-maxcpucount:4`; subsequent variants only used the latter. Each exact net10/win-x64
LayoutProbe.exe launch used `Start-Process -WindowStyle Hidden -PassThru` and
`WaitForExit(20000)`, with fixed task-owned JSON output and no command-line argument.
The initial timeout, second exit2 and final exit0 remain separate outcomes.
## Exact commands

From the task worktree:

```powershell
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -maxcpucount:4
```

From the dedicated layout-probe output, initial setup used:

```powershell
dotnet restore LayoutProbe.csproj -p:Configuration=Release
dotnet build LayoutProbe.csproj -c Release --no-restore -maxcpucount:4
```

The generated lock and fixed direct package are retained. Each subsequent fixture
change uses the same no-restore build. Start-Process launches the exact built
LayoutProbe.exe with -WindowStyle Hidden, a single JSON-output filename and a
bounded20-second wait. Output arguments, in order, are baseline-layout.json,
changed-layout.json (type-not-found trial), existing-panel-layout.json,
expanded-layout.json and chinese-layout-corrected.json. Build logs and original
fixture sources preserve every material attempt. The normal Commit hook remains
`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`; XML/diff review is scoped.

## Limits and lifecycle

ADR-0061 scope is a view-layout correction with affected App build, direct native
geometry fixture and canonical Commit conformance. No application behavior or
trust boundary changes. Attached composition now adds the bounded125% API proof
above; the synthetic320×300 complete-row assertion remains unresolved. Visible
full-product behavior, other DPI/text scales, screen-reader output, native search/
IME/key routing, scrim hit testing, focus return and performance latency remain
unproved and separate (#94/#523/#453/#491). Helper geometry is not screen acceptance.

#542 is committed locally at7724d88 but a prior automatic review rejected its
push for lack of destination/payload-specific approval. Its question is pending;
this task does not bypass that rejection or send another branch as a workaround.
The completed local changes and evidence stay available. PR publication,
integration and cleanup have not occurred for #525; do not delete either worktree
while unintegrated. The user App/issue495, settings and unfinished491/514 remain.
A closing PR/completion record will own eventual integration and exact cleanup.
