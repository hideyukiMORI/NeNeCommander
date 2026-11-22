# Preview beta.3 CodeQL corrections — 2026-11-22

Issue #553 is a release correction within #522 / PR529. The November 22 resumed
milestone instruction keeps release first, then the recorded next-version Issues.
GitHub's all-state milestone query returned no records. Historical fixed model
assignments do not apply: investigation, implementation and verification are by
the current agent; design consultation is requested only when design is needed.

## Discovery and retained failures

The existing candidate is chore/522-preview-beta3 at 8dc51e5, with only four
Markdown files changed from deep-reviewed 9ca919d. Main is 08c2d9c; both were
clean. PR402 is merged and hide's screen acceptance is complete. The current
release remains unpublished. The existing input-isolation rule is retained.

Second deep review 44936588484 / job136944433359 completed successfully at
19:33:50 UTC on November 21. All19 script checks passed; CodeQL is separately
reported as an external step. The artifact14721856768 is1,511,214bytes and its
SHA256 is6bcdefd64ac2f977c4c2432c6f749bfedaaf976c9c70c162a6a8bc8b4acd6e67.
The complete archive, extracted reports, job log and API results are retained in
D:/NeNeCommander/evidence/release-beta3-20261122/second-deep-review.

CodeQL analysis2221789209 has3 results, although the default-branch open-alert
query returned0. The explicit candidate-branch query correctly returned open
alerts154,155,156. Workflow success is not zero-alert acceptance. The source
SARIF and both API responses are retained without dismissing any alert.

Read-only preparation encountered Git dubious ownership and sandbox denial of
the user's GitHub CLI configuration. Per-command safe.directory admitted only
the known checkout; the existing CLI authentication was used through reviewed
escalation. Initial connected issue-search arguments were invalid and were
corrected after reading the tool schema. Connected Issue creation returned403
Resource not accessible by integration; the existing gh CLI created#553.
One Windows rg path wildcard was invalid and was corrected with a glob filter;
one guessed ADR filename was absent and the actual indexed file was then read.
A sandbox Python command was unavailable; the configured user Python is reached
through the same reviewed execution boundary. None of these attempts changed
product behavior or security settings. No automatic approval rejection occurred.

The first App build failed with CA1861 after a constant array was passed directly
to Select. The failure remains in codeql-corrections/app-build.log. Its replacement
extracts the existing field-registration body into RegisterField; no suppression,
new shared mutable array, dependency or contract exception was introduced.

## Change and invariant

- KeyboardRepeatGuard.Observe uses a typed outcome switch for the existing
  palette execution, nonrepeating Bookmark action and native draft Enter cases.
  The same key enters the same existing set; release on a fresh initial event,
  native IME/popup handling and mapper ownership are unchanged.
- BookmarkInputOwnership calls RegisterField once for each of the same four
  names in the same order. Lookup, field ownership, composition subscriptions
  and each captured-field LostFocus callback retain their order and meaning.
- PaneSelectionSessionTests projects the same initial/cleared selections before
  executing the same copy/enumeration/containment assertions, in the same order.

The only execution paths remain KeyboardIntentMapper's existing repeat guard,
BookmarkInputOwnership's native control registration and PaneReducer's existing
selection contract. This changes no file-operation trust boundary, XAML, resource,
key mapping, setting, dependency, gate, threshold or waiver. #523 and#550 remain
separate next-version behavior fixes.

## Focused verification

From D:/NeNeCommander/worktrees/issue-522, exact command arrays and complete logs
are in codeql-corrections/commands.txt and run-focused*.ps1. Every selected restore
uses --locked-mode -p:Configuration=Release. Every build uses -c Release
--no-restore -m:2 -p:UseSharedCompilation=false.

```powershell
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode -p:Configuration=Release
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~BookmarkKeyboard|FullyQualifiedName~BookmarkDraftKeyboard|FullyQualifiedName~KeyboardPalette|FullyQualifiedName~KeyboardModal|FullyQualifiedName~KeyboardIntentMapper|FullyQualifiedName~KeyboardChord'
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter FullyQualifiedName~PaneSelectionSessionTests
dotnet format whitespace NeNeCommander.slnx --no-restore --include src/NeNeCommander.Presentation.WinUI/Input/KeyboardRepeatGuard.cs src/NeNeCommander.App/Views/BookmarkInputOwnership.cs tests/NeNeCommander.Application.Tests/PaneSelectionSessionTests.cs --verify-no-changes
```

Final App/Presentation.Tests/Application.Tests builds have0 warnings/errors;
42 keyboard cases and3 selection cases pass,0 failed/skipped. Format exits0
with its workspace-load warning retained. Successful App restore was reused
after the CA1861 source correction. No new native/desktop observation is claimed.

## Release proof still pending at this checkpoint

ADR-0061 reuses the successful baseline's unchanged Domain/Infrastructure source,
tests, tooling, dependency and threat evidence. Refresh the affected Application
and Presentation coverage/mutation evidence and use the same official CodeQL
2.27.5 security-and-quality suite, build-mode none and canonical configuration
to analyze the complete final source. Retain tool digest, commands, complete
SARIF, source-input equality and final branch alert readback. This is supplemental
evidence, not a claim that a new all-layer deep workflow ran. The accepted
baseline must still be inside96 hours when the release is published.

PR acceptance, accepted-source packaging, payload/ZIP/PRI/notice verification,
public tag/assets readback and exact task cleanup follow their existing entry
points. See [release record](2026-11-22-preview-beta3.md) and
[handoff](../handoffs/2026-11-22-preview-beta3-handoff.md).

The existing complexity command `pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect`
completed with exit1 and5 findings (method-lines1, nesting1, parameters2,
type-lines1), not a clean-complexity claim. Its full result and log are retained.
No global gate wiring or baseline was changed. The final verification input
manifest contains1,075 non-Markdown tracked files.
