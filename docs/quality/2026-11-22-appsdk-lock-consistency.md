# AppSDK lock consistency and non-UI proof — 2026-11-22

Issue #514. Candidate base: `e2f675b63f492cc49b8e190112adcc7944a56da0`.
Status: local non-UI proof complete; native environmental acceptance outstanding.

## Problem and canonical mechanism

Dependabot PR498 at3da9729 updates the central Microsoft.WindowsAppSDK pin and
App lock from2.6.5 to2.7.0, leaving the Architecture.Tests lock at2.6.5. Its
unchanged head failed this exact command with NU1004 on November21:

```powershell
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --locked-mode -p:Configuration=Release
```

That causal failure, public package graph and command metadata remain at
D:/NeNeCommander/evidence/appsdk-270-20261121. They were not rerun. Issue514
updates the existing central pin and regenerates both dependent locks from main.
There are no product C#/XAML, test, project, workflow, schema or gate changes.
App is the sole direct AppSDK consumer; Architecture.Tests references App.

The App lock equals the public PR graph structurally. App has14 changed entries
and Architecture has13, retaining framework-specific duplicate entries; no
package name or framework target is added or removed. All changed rows belong to
the AppSDK package family, its Windows.AI.MachineLearning dependency or the App
project-reference requirement. The SDK family moves2.6.5 to2.7.0 and MachineLearning
moves2.4.89 to2.5.96. Other rows and pins are unchanged.

[Microsoft's stable release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0?pivots=stable#version-270)
record the November17 release, including input, text control, tooltip/style and
DispatcherQueue-after-shutdown fixes. The package also updates AI capabilities;
NeNe Commander adds no API use. Source search found no explicit System Composition
Engine, LanguageModel, ContentDialog, MenuFlyout, ItemsView or WebView2 use.
Existing DispatcherQueue and text/focus use still make native validation relevant.

Rules: ARC-001/004, CS-024, QLT-004/005/009/011/014/015, SEC-009,
ADR-0061/0078. No waiver or suppressed advisory.

## Exact non-UI proof

Commands ran in D:/NeNeCommander/worktrees/issue-514. The permanent evidence
folder D:/NeNeCommander/evidence/appsdk-270-20261122 contains prepare.ps1,
commands.json and test-commands.json with full arguments, timestamps and exits.
App/Architecture lock regeneration used the following, once per project:

```powershell
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --force-evaluate
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --force-evaluate
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj -p:Configuration=Release --locked-mode
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-restore
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -c Release --no-build --no-restore --report-trx --results-directory D:/NeNeCommander/outputs/appsdk-270-20261122/architecture-results
$filter = 'FullyQualifiedName~CaptionBarPresenterTests|FullyQualifiedName~KeyboardTranslationTests|FullyQualifiedName~KeyboardModalTests|FullyQualifiedName~KeyboardPaletteTests|FullyQualifiedName~WindowAdjustmentKeyboardTests|FullyQualifiedName~WindowShutdownOwnerTests|FullyQualifiedName~AsyncWorkOwnerFaultAndShutdownTests'
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore --filter $filter --report-trx --results-directory D:/NeNeCommander/outputs/appsdk-270-20261122/presentation-results
git diff --check
```

Every command exited0. The Architecture build also built App and its production
references; both build summaries report zero warnings/errors. NuGet's existing
all-dependency audit and warnings-as-errors remained enabled. App assets contain
two targets/20 libraries and Architecture one target/35 libraries, both with no
restore log entries. XML parsing and JSON structural comparison pass; source
formatting/complexity tools are unnecessary because no C#/XAML source changed.

All25 Architecture cases and53 selected Presentation cases pass with zero skips.
The five Architecture classes inspect compiled App project references, window/view
composition, scheme mappings and async ownership. The seven Presentation classes
exercise the pure caption/input/modal/window/shutdown consumer contracts. They do
not instantiate the native App or establish actual SDK focus, IME, resize, visual,
input or DispatcherQueue-shutdown behavior. TEMP/TMP for tests point to the owned
D output temp directory. No user settings or native App was invoked by this work.
All1,017 tracked source/test/build/tool inputs match their pre-test hashes.

This focused scope covers changed dependency resolution and its direct App loading
and consumer boundaries. No unrelated Domain/Application/provider suite, full
quality/deep CI, coverage, mutation or performance series was run. Readiness must
still execute dependency-review for the final dependency inputs. Local audit does
not substitute for that workflow.

## Acceptance and retained work

Integration and PR498 closure are held pending native environmental proof of
startup/normal shutdown, caption/resize, keyboard/IME/focus and tooltip behavior.
hide requires non-UI work and has no available isolated VM/test PC. Do not launch
the previously prepared native sequence or send shared input through another tool.
PR402 hide screen acceptance, stopped #491 series, parent453 and release94 remain
separate. No released package or ordinary App is replaced.

All11 logs/TRX files,123,836bytes, are archived with full path/size/SHA256 verification
in nonui-logs.zip. Build products stay in the unfinished D worktree; source, locks,
input hashes and proof scripts remain available. Retain both issue514 worktree and
appsdk-270-20261122 output, plus the earlier pr-498-review reproduction, until
technical acceptance and normal integration. No cleanup deletion was attempted.

## Main synchronization after publication

PR517 was published as Draft at7097717. After PR516 integrated Issue512 as
7f0e6d3, that accepted main was merged into the candidate. Only PROJECT_STATE's
adjacent inserted records conflicted; both records were retained. Of the1,017
previous proof inputs,1,016 remain exact and the sole changed source is the
volume GUID buffer file, exactly equal to accepted Issue512. The dependency graph,
App, Presentation and Architecture sources/tests are unchanged.

Reuse the25/53 AppSDK cases for these unchanged boundaries and Issue512's ten
independent provider tests, whose project does not reference AppSDK. No product
test or build was repeated merely for branch synchronization. Existing binaries
remain the original e2f675b-based proof outputs; rebuild the final frozen candidate
before future native acceptance. This does not claim a newly executed combined
binary or remove the native/dependency-review hold.
