# Bookmark ADR identifier correction — Issue #445

The published main (`126352d`, Preview source `46f1a82`) assigns ADR-0079 to
first-Preview timing. Draft PR402's feature `275b464` independently assigned that
same number to the bookmark manager. One identifier therefore referred to two
different decisions when the two branches were considered together.

## Change and invariant

Keep the published ADR-0079 and tag unchanged. Rename the feature's bookmark ADR
to ADR-0080 and update current links/references in the decision index, design
handoff, glossary, keyboard model, project state and #430 quality record. Its
acceptance date (2026-11-16), decision content and manual screen requirement are
unchanged. The two historical 11/16 reports keep their original wording and gain
an explicit identifier-correction note. The new ADR records the old identifier
so commit-specific historical citations remain explainable.

Issue398's obsolete "direction undecided, implementation not started" state and
Issue430/PR402's current ADR references are updated after feature integration.
PR402 remains Draft and cannot merge into main until hide's screen acceptance.
XML documentation comments in source and tests receive the same number correction.
No executable product/test text, build, dependency, schema, threshold, waiver or gate changes.

Rules: ARC-001, QLT-001/015, GIT-001–004, ADR-0061 and the ADR index's number
allocation rule. Waivers: none.

## Selected verification

Only text, links, Git diff and ordinary commit hooks apply under ADR-0061.
The external `renumber.py` compares the complete ADR text before/after: removing
the correction note and restoring the title number reproduces the original
exactly. The combined current-main/feature ADR filenames have no number mapping
to two decisions after renumbering. All local links in changed documents resolve;
every non-Markdown change replaces the identifier only on a line beginning `///`.
Reversing those comment identifiers reproduces each original C# file exactly.

Successful quality run44217730539 at `275b464` is reused because executable product
and test text, dependencies and tooling are unchanged. No build, product test, CI,
mutation, GUI input or user-data operation was requested. Main is not merged
into the feature by this correction, so no unrelated integration is implied.

## Attempts and evidence

The live open-Issue audit found six remaining issues; comparing PR402's existing
ADR to published main revealed this collision. An initial connector search with
unsupported `per_page` was rejected; the supported search succeeded. A guessed
Preview ADR filename and a literal rg wildcard path failed to resolve; the
tracked-file inventory supplied the exact paths before editing. Neither failed
read changed files. The document correction ran once from a clean feature-based
worktree. The first document-only proof passed 241 local links; a subsequent
whole-repository reference scan found old identifiers in XML documentation
comments. Those were then corrected with exact reversible-text proof before
opening the PR. The first normal commit hook passed; the completed change keeps
the same ordinary hook and does not broaden product verification.

Original ADR, precise correction script, identifier/link/diff proof and ordinary
hook logs are retained under `D:/NeNeCommander/evidence/issue-445/`.
The dedicated worktree is `D:/NeNeCommander/worktrees/issue-445`; it contains no
build output. Branch/commit and evidence remain retained. The completion record
identifies the feature integration and worktree disposition. No previously
rejected cleanup target is retried as part of this task.
