# Gate Proof Registry

Status: normative

| Gate | Positive proof | Negative proof | Current stage |
|---|---|---|---|
| required policy files | repository foundation passes | missing required document is rejected | active |
| merge-time full validation | readiness-only full workflow and lightweight hook/bootstrap are accepted | push-triggered full workflow, full pre-commit, skipped full job, and lightweight default are rejected | active |
| proof fixture materialization | current-tree inspection inputs, including an untracked-style source, are copied | nested generated output and reparse-point content are not traversed or copied | active |
| unique normative rule IDs | all declarations are unique and active | duplicate rule declaration is rejected | active |
| protected build settings | root settings match pinned values | warnings-as-errors removal is rejected | active |
| restore/build configuration coherence | locked restore evaluates `Configuration=Release` | default-configuration restore is rejected | active |
| suppression prohibition | clean source is accepted | pragma suppression is rejected | active |
| production stage interlock | foundation contains no `src` implementation | production source during foundation is rejected | active |
| platform API boundary | `System.IO` inside Windows infrastructure and its integration tests is accepted | `System.IO` in any other test or production project is rejected | active |
| environment boundary | direct location access in `WindowsLocalSettingsLocation` is accepted | direct `Environment` access in any other production or test file is rejected | active |
| CS-010 ambient clock boundary | the exact named `StopwatchClock` adapter and settings-location API concern pass | ambient aliases, static imports, `TimeProvider.System`, environment clocks, and every `Stopwatch` type reference outside the adapter are rejected by separate source-form fixtures | active |
| color scheme dictionary parity | eight scheme dictionaries with one identical color and brush key set are accepted | a renamed key in one scheme dictionary, and a color declared in `DesignTokens.xaml`, are rejected | active |
| presentation semantic resource keys | every colour resource key a presentation record names exists in the scheme dictionaries | a presentation record that names an undeclared brush is rejected | active |
| project graph | manifest parses and has unique projects | undeclared/missing reference is rejected | activates with implementation |
| dependency allowlist | declared packages match the manifest | unlisted/versioned project package is rejected | activates with implementation |
| C# restricted subset | compliant syntax is accepted | forbidden struct, enum, async blocking, and ambient API fixtures are rejected | activates with implementation |
| XAML design tokens | semantic-resource markup is accepted | hard-coded color or layout token is rejected | activates with implementation |
| workflow supply chain | allowlisted actions use immutable SHAs | mutable action tag is rejected | active |
| secret detection | repository patterns are clean | synthetic token is rejected | active |
| script safety | approved PowerShell subset passes | dynamic execution is rejected | active |
| NuGet audit | all transitive advisories at low severity fail | disabled audit is rejected | active |
| workflow privilege | least-privilege PR flow passes | `pull_request_target` is rejected | active |
| mutation strength | complete mutation config and protected thresholds pass | threshold reduction is rejected | active |

`eng/prove-gates.ps1` creates its fixtures only under an OS-owned temporary directory, materializes the current working tree through the shared generated-directory pruner, runs the real conformance script against each fixture, asserts the expected exit codes, and removes only the resolved test-owned directory. The materialization proof does not consult Git, so modified and untracked inspection inputs remain in scope.
