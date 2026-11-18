# Bookmark input ownership correction

Issue: #430, following the independent review of Draft PR #402 / Issue #398.
Base: `db8fcc72ee342885185b789bd7ac2b72ed1b0f2e`.

## Contract and scope

The accepted ADR-0079 contract consumes a held single-shot key once. Previously the bookmark
table checked repeats only in its current context: opening search or a draft exposed the same
press to text input, and completed navigation exposed Enter to the file list. The correction
extends the canonical mapper's existing palette guard rather than adding another guard path.
The next initial event for the same identified key releases its guard. Other keys and raw events
do not, while ordinary typing and repeating movement remain unchanged. Composition remains
outside mapping so it neither arms nor consumes a command guard.

The browse view's low-priority focus restoration previously ran unconditionally. The view now
owns one replaceable request through `BookmarkFocusRestoration`, which contains no WinUI types,
session or task. A later search/region focus or exit from browsing cancels the pending request;
another request supersedes it. The queued callback rechecks that ownership before invoking the
native focus action. This is a framework focus lifetime change, not a new navigation route.

Rules: ARC-001 / ARC-004, CMD-009, KBD-001 / KBD-003 / KBD-005, QLT-005 / QLT-015,
TST-003 / TST-006, ADR-0061 / ADR-0079. Waivers: none. No schema, provider, persistence,
destructive-confirmation, dependency, gate or threshold changes.

## Selected verification

The affected boundary is Presentation input plus its WinUI event/focus adapter. Selected proof:

- Red/green mapper sequences for Enter after navigation, slash after search, and a/e after draft
  entry; fresh initial keys, overlapping held commands, raw Other events, normal typing and j/k.
- Deterministic queued focus sequences for cancellation, supersession, late old callbacks and
  reopening with the same native action. The focus red temporarily restores the old unconditional
  callback in the extracted component; it does not claim native WinUI reproduction.
- The affected keyboard/bookmark fixtures, including the unchanged palette repeat contract.
- App Release build for the changed native event and focus wiring, plus targeted formatting and
  complexity inspection. Local commit checks retain their normal hook.

No full gate, full-layer mutation, GUI input, app launch or real-user-data operation is selected.
PR #402 remains Draft and cannot merge until hide's
on-screen confirmation; these unit sequences and build do not replace that confirmation.

## Executed behavior proof

- Original mapper plus the new repeat fixture: 8 cases, 7 failed and 1 passed, exit 2.
  The first attempt had instead stopped at build time for IDE0039 in a test; that attempt is
  recorded separately and is not the behavioral red result. A local function and one retained
  delegate preserve the same-action identity scenario while satisfying the analyzer.
- Temporarily unconditional focus callback: 6 cases, 4 failed and 2 passed, exit 2. The guarded
  source was restored after this pure callback-order proof.
- The first green attempt ran a stale red DLL: 101 cases, 4 failed and 97 passed, exit 2.
  Restoring the source copy preserved an older modification time, so incremental compilation
  skipped it. Rewriting the identical source bytes refreshed that time; the next explicit build
  succeeded with zero warnings and errors.
- Corrected mapper and focus owner: `dotnet test --project
  tests/NeNeCommander.Presentation.WinUI.Tests -c Release --no-build --no-restore --filter
  'FullyQualifiedName~Keyboard|FullyQualifiedName~BookmarkFocusRestorationTests'` passed all 101
  cases, with zero failures and skips (exit 0). This includes the existing palette and other
  keyboard boundaries. Successful proof is reused while those inputs remain unchanged.

Presentation builds use `dotnet build
tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c
Release --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false`. App locked restore with
Release configuration succeeded. The host command `dotnet build
src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -m:1 -nodeReuse:false
-p:UseSharedCompilation=false` also succeeded with zero warnings and errors (exit 0).

Changed-file `dotnet format whitespace <project> --verify-no-changes --no-restore --include
<changed-files>` checks passed for Presentation, its tests and App. The initial Presentation
check reported 91 ENDOFLINE diagnostics in the two new production files (exit 2); the four new C#
files were normalized to the repository's CRLF form without changing their content. Existing
successful behavior/build proof is reused for this line-ending-only normalization. The App
formatter returned exit 0 with a workspace warning. A diagnostic rerun identified missing Debug
reference assemblies and duplicate generated XAML inputs; it still completed whitespace checking
with no edits. Release compilation is the separate host proof; no unnecessary Debug build was run.

`pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect` completed with 69 repository
findings (exit 1), not a repository-wide pass. In the touched files, the existing
`CommanderWindow.GetKeyboardContext` remains at cognitive complexity 11/10 with an unchanged
body. The host type measures 325/300 logical lines; the diff adds one shared composition property
to the already oversized type. Its changed event methods remain within every limit. All other
changed types and members meet the ceilings, including mapper 281 lines, focus owner 25 and
repeat guard 11. The host responsibility split remains separate; no threshold, baseline or
suppression was added. Raw and changed-file metrics are retained with the issue evidence.

## Evidence

The accepted source review is retained under `D:/NeNeCommander/evidence/issue-430/review.md`.
Execution output is retained under `D:/NeNeCommander/outputs/issue-430/` and durable verification
records under `D:/NeNeCommander/evidence/issue-430/`. The original issue-398 worktree is unchanged.
