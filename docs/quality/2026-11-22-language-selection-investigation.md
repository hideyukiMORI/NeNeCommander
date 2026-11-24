# Language selection: bounded resource-lifecycle investigation

Issue #528 / 2026-11-22 / main7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2.
Scope: external diagnostic and documentation only. Product code, settings, packages,
resource catalogs and beta.3 are unchanged. See the
[proposed design](../design/2026-11-22-language-selection-investigation.md).

## Isolation and provenance

Evidence: D:/NeNeCommander/evidence/language-selection-20261122.
Worktree: D:/NeNeCommander/worktrees/issue-528.
Fixture/output: D:/NeNeCommander/outputs/language-selection-20261122/probe.

The diagnostic uses the pinned SDK/dependency with a copied unchanged lock/manifest,
the exact shipped English/Japanese catalogs/DesignTokens and the actual LocalizedTextConverter.
Copied App.xaml changes only the diagnostic class name. A small compiled UserControl
uses four actual x:Uid properties: settings title, close content, bookmark search
placeholder and address accessible name. It is not CommanderWindow.

Every case owns one independent process and one unshown Window. No Show/Activate,
injected input, product/session invocation, user-directory read or user settings
read/write occurs. All13 cases attach XamlRoot and exit0 within20 seconds, with
0 activation events, false visibility and unchanged sampled foreground handles.
These observations do not establish general focus/timing guarantees. A100ms
dispatcher timer observes attachment; it is not a product timer or deterministic test.

## Observations and retained attempts

| Timing | Cases | Observation |
|---|---:|---|
| System default | 1 | Empty override, Japanese on this Japanese environment. |
| Before App XAML | 2 | Selected English/Japanese text matches in loader/converter/view. |
| After App XAML, before loaders | 2 | Same text result; not permission to ignore the documented before-resources recommendation. |
| After existing loader/converter | 2 | English case: new loader English, existing loader/static converter still Japanese. |
| After existing view | 2 | English case also keeps the old title Japanese; new view is English. |
| Deferred App XAML after override | 2 | Text matches; element Language remains ja. |
| Deferred initialization plus root Language | 2 | Text matches; root/title/address/search Language inherits the selected en-US/ja-JP. |

Each of13 cases is compared against320 plain resource keys and4 selected x:Uid
properties, immediately and after attachment. All observations match their stated
expectations. The2 English late-switch counterexamples remain failures of that
design, not successful switching. The remaining63 property keys, framework-owned
control strings, complete product startup and Narrator speech are not proved.

Every process starts with an empty override, consistent with unpackaged non-persistence.
CurrentCulture/CurrentUICulture remain ja-JP and sample number/date formatting stays
unchanged in all13. An explicit root Language is separate from resource selection.

Locked restore and both diagnostic builds succeed with0 warnings/errors. The first
launcher never started a process: its guard expected '0 個のエラー', while the
successful log prints '0 エラー'. launch-guard-attempt.txt retains this error.
Correcting the guard did not rebuild. No runtime case timed out or failed.
initial-source/ preserves the9-case fixture; final-source/ adds4 deferred/root-language
cases and child-language observations. Earlier cases were not repeated.
Original JSON/run records retain values/PIDs. verification-summary.json records10
relevant product input hashes and confirms no source/test/eng changes.
A later document-generation tool call failed JavaScript parsing before execution;
the corrected call writes the documents once and does not rerun diagnostics.

## Exact commands

From the dedicated probe directory:

    dotnet restore LanguageProbe.csproj --locked-mode --disable-parallel -p:Configuration=Release
    dotnet build LanguageProbe.csproj -c Release --no-restore -maxcpucount:4

The build ran once per fixture version. Each exact net10/win-x64 LanguageProbe.exe
launch used Start-Process -WindowStyle Hidden -PassThru, explicit mode/locale, and
WaitForExit(20000). Only its own timed-out process could be stopped; none was.

    pwsh -NoProfile -File D:/NeNeCommander/evidence/language-selection-20261122/verify-probe.ps1

The verifier exits0; verification.log retains per-case results. This documentation
change runs no product build/test, coverage, mutation, full CI or release check.
The normal eng/check.ps1 -Mode Commit hook remains active.

Chinese script scope awaits hide's answer. No schema5, selected-language UI, new
translation, accepted ADR, actual save/restart/native input/IME/DPI/high-contrast or
speech result is claimed. The feature is not merge-ready. Unintegrated worktree/output
remain. Existing #542 push and #547 Claude-brief approval holds are not retried or
circumvented; this investigation sends no branch/content externally.
