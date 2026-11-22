# Theme catalog source comparison

Issue #527 / 2026-11-22 / base main7ef9b96.
Documentation-only repository change. No product build/test/CI or theme implementation.
See the [integration investigation](../design/2026-11-22-theme-catalog-investigation.md).

Evidence: D:/NeNeCommander/evidence/theme-catalog-20261122.
Worktree: D:/NeNeCommander/worktrees/issue-527.
Reserved disposable output: D:/NeNeCommander/outputs/theme-catalog-20261122 (empty).
No other repository is modified. Nib's unrelated untracked bash.exe.stackdump is retained.

## Method and results

audit-catalog.py reads fixed git blobs, not a moving working copy. It stores24
source inputs with byte length/SHA256 and their commit/path. All selected live inputs
also matched their fixed commits at collection. It extracts all125 Nib names,
appearance,16 body roles and author/license/URL; reads127 Folio records; and compares
all eight Commander dictionaries, each with18 colors/18 brushes.

- Names/order:125 shared, exactly equal. Folio-only light/dark; Commander-only
  nene-dark/ubuntu/nene-black/nene-light. Nib91 dark/34 light.
- All116 expanded Nib metadata entries agree with actual code values/appearance/
  attribution. Folio's125 attribution entries agree with the actual Nib table.
-124 shared body foreground/background pairs match. vscode-high-contrast-light
  differs: Nib background#FFFFFF/foreground#292929; Folio#1E1E1E/#D4D4D4.
- Commander monokai/dracula core pairs match; solarized-dark differs in both roles
  and solarized-light differs in foreground. This is not a full18-token comparison
  across different products' semantic roles.

audit-import-version.py preserves3 additional files from Folio's fixed Nib import.
audit-source-boundaries.py compares all125 old/current Nib names/order and16 roles/
appearance. Only vscode-high-contrast-light differs, including dark-to-light.
Nib commit3e1e0970 identifies the2026-10-20 correction. No correction is applied here.

The first whole-suffix notice comparison was false because Folio appends Mermaid/
Pele notices. notice-difference.txt retains that raw result; the later bounded
comparison proves the palette block is exactly equal to old/current Nib. It does
not mean those unrelated notices are palette requirements. The retained decoded
block is71,389 characters, SHA25631629c34013fc98b7778528cf2bbb9c1d32ecd5967b29321c82f2b723450851a.

audit-base-contrast.py computes source body foreground/background relative-luminance
ratios for125 entries. All exceed4.5, minimum solarized-dark4.747877839876171.
The diagnostic explicitly uses the sRGB linearization threshold0.04045 and
weights0.2126/0.7152/0.0722, then (lighter+0.05)/(darker+0.05).
This establishes only those125 source pairs, not Commander selections/disabled text,
framework controls, accessibility certification or visual acceptance.
It also rechecks the24 original snapshot hashes without rerunning another product's tests.

## Commands and trial retention

All four commands exit0, with separate scripts/logs/results retained:

    python -X utf8 D:/NeNeCommander/evidence/theme-catalog-20261122/audit-catalog.py
    python -X utf8 D:/NeNeCommander/evidence/theme-catalog-20261122/audit-import-version.py
    python -X utf8 D:/NeNeCommander/evidence/theme-catalog-20261122/audit-source-boundaries.py
    python -X utf8 D:/NeNeCommander/evidence/theme-catalog-20261122/audit-base-contrast.py

The first snapshot script refuses overwrite; these commands are records of completed
collection, not instructions to rerun unchanged inputs. Source snapshots and raw
mismatch results remain. Initial read-only searches used unexpanded filename globs/
absent paths; exact file discovery corrected those lookups without mutation.
No native window, keyboard/pointer, user settings or external review service is used.
Normal eng/check.ps1 -Mode Commit remains active for the documentation commit.

Count/legacy visibility awaits hide's20:11 response. Final role mapping, selection UI,
ADR/conformance migration, settings/persistence/native proof and release-notice
integration are incomplete. Existing #542 push/#547 external-design approval holds
remain and are not circumvented through this branch. Unintegrated worktree and
source evidence stay retained; actual merge/cleanup belongs to a closing record.
