# Immediate interface language — Issue528 / ADR-0088

Date: 2026-11-23
Status: local implementation and focused verification; no publication/integration.

hide requested immediate switching at17:21 JST after reporting that the previous
save/reopen/restart language selection, translation and English-mode Japanese input
worked. This new timing is not yet native screen acceptance. Worktree is
D:/NeNeCommander/worktrees/issue-526, branch feat/528-live-interface-language,
starting at510d53de (the retained525/526 palette change). The live issue527 App and
current user settings remain untouched. Evidence is
D:/NeNeCommander/evidence/live-language-20261123.

## Mechanism and selected impact

ADR-0088 replaces launch-only x:Uid/cached ResourceLoader conversion with one
explicit window-owned MRT ResourceContext over the same PRI. LocalizedText declares
exact text/content/placeholder/UIA properties and its explicit owner. Loaded
subscribes and Unloaded detaches; key/owner removal clears only previously owned
properties. Every renderer receives that owner. SettingsSession, its closed
InterfaceLanguage, schema5 and complete-value queue retain their behavior.

Selection applies before save completion, including failure. System restores the
original platform qualifier without global override/region/keyboard changes.
Cached captions/help/theme instructions/bookmark save labels refresh through the
same path. An unchanged pane presentation does not repeat ScrollIntoView, retaining
row sources and offsets. All seven metadata formats are equal across locales;
existing metadata formatting stays unchanged. This feature changes App adapters,
markup and localized text, not navigation, file operations or a trust boundary.

## Executed proof

Commands run from the worktree unless a fixture path is specified. Complete logs,
source scripts and failed attempts are retained under the evidence directory.

```powershell
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
pwsh -NoProfile -File eng/conformance.ps1
pwsh -NoProfile -File eng/prove-gates.ps1 -Rule CS-023
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
dotnet format whitespace src/NeNeCommander.App/NeNeCommander.App.csproj --no-restore --include <changed C# paths> --verify-no-changes
git diff --check
```

- App build1 rejected IDE0031/unused using. The corrected build2 exits0 with0
  warnings/errors; no suppression. Later changes in product are documentation only.
- Conformance passes112 rules. CS-023 has one positive and eight negative fixtures,
  including unknown attached key, absent explicit owner, cached loader and x:Uid.
  All pass. TEMP/TMP point only to the dedicated D-drive gate-temp; the existing
  verifier validates/removes its own generated roots. Catalogs are parsed once.
- Complexity exits1 with exactly the existing five findings (same paths, positions,
  metrics and values), recorded in complexity-comparison.json. No clean claim.
- Changed C# whitespace verification exits0 with the existing workspace-load warning.
  Diff whitespace passes. Three547-key catalogs have identical key sets and
  placeholder multisets; metadata-format equality is independently checked.
- Existing schema/queue/presentation tests from the language-selection and theme
  records are reused: their behavior inputs are unchanged. No unrelated provider,
  full/deep, coverage, mutation or CI run is claimed. Native uncertainty requires
  its environment, not unrelated automatic tests.

## Explicit context experiment

The isolated resource-probe uses the pinned SDK and the baseline product PRI.
`dotnet restore LanguageProbe.csproj -p:Configuration=Release` and
`dotnet build LanguageProbe.csproj -c Release --no-restore` exit0/build0 warnings.
Its sole unshown process exits0. Six English/Japanese/Chinese transitions each
compare all546 baseline keys with the corresponding .resw and have zero mismatches.
Restoring the captured `ja;en-US;` qualifier restores System text; global override
stays empty. The launcher shell's exit1 came only from attempting to read the
nonexistent error file after success; the process and result are retained separately.
This experiment is resource proof, not native display proof.

## Actual controls, baseline and corrected candidate

The preparation script creates two isolated App-source copies on D. Baseline comes
from git archive510d53de; candidate comes from current source. Both use original
XAML/renderers and real settings/startup/session/store routes. Diagnostic-only
substitutions redirect startup to dedicated settings and two owned150-file folders,
remove Activate, and replace native focus calls. No shared input or foreground
ownership is requested. Fixture analyzers are disabled for diagnostic code; the
real App build above retains its normal analyzers. Substitutions are inventoried.

Each fixture uses locked Release restore and Release --no-restore build of its
App/NeNeCommander.App.csproj. All fixture builds exit0 with0 warnings/errors. Only
its exact executable is launched using Start-Process -WindowStyle Hidden, passing
baseline or candidate. Product source, the live user App and user settings are not
patched or launched for this diagnostic.

Baseline exits2 at the intended assertion: selecting English while the write is
pending leaves the Settings Close label in the old language. Its first five
preconditions pass. Candidate ultimately exits0 with94 checks, stage20, six writes,
125% measured scale, visible=false and no activation events. Realized localized
property audits contain zero mismatches (up to456 properties as more surfaces load).
This is one sequence, not94 separate processes or repeated unique test suites.

It proves immediate en-US/zh-Hans/ja-JP/System text and UIA names; same radio/source,
pane rows/focus and scroll offsets; delayed/failed complete-value writes without
language rollback; later palette/theme/bookmark/help text; illegal language input
in palette/picker preserving query/preview; ordinary delivered bookmark query;
unchanged cultures/global override/theme; and loaded/unloaded/reloaded resource
subscriptions, key removal and owner removal without clearing unrelated properties.

Three diagnostic-only attempts are retained: attempt1 selected a newly reopened
radio before realization; attempt2 rendered a bookmark before native TextChanged
was delivered; attempt3 sent Help Escape to the generic route instead of the
existing KeyboardHelpView route and timed out atstage17. The fixture was corrected
without product changes. Final diagnostics and all prior versions/errors remain.
Do not claim those failed attempts passed or hide their substitutions.

The18:25:54 independent read-only review found no concrete required correction and
confirmed the final94-check result. It changed nothing and ran no additional tests.
Physical keys, native IME composition, speech, visible repaint and screen acceptance
remain outside this evidence. Built-in WinUI/OS text has no immediate-switch claim.
The selected scope is sufficient for the changed managed/UI resource mechanism;
remaining native acceptance is explicitly retained before integration.

## Next

Final source/artifact parity verifies 171 App source files after only the
inventoried diagnostic substitutions, 132 identical XBF files and four identical
dependency DLLs. 1234 relevant inputs are frozen in final-input-hashes.json.
The local commit and its normal hook are recorded in local-completion.json. New public payload and native
acceptance remain separate. Continue requested window persistence/off-screen
correction as Issue567; its exact approved body was created/read back successfully.
Keep incomplete fixtures/worktree and the live527 execution source. No cleanup
rejection or pending deletion request was introduced.
