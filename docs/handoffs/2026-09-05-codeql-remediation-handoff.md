# CodeQL remediation handoff — 2026-09-05

Status: informational

## Work item

- Issue: #2
- Branch: `chore/2-codeql-alerts`
- Decision: ADR-0031
- Invariant: the `security-and-quality` suite analyzes every owned source and test path; only generated `obj` trees are omitted.
- Canonical mechanisms: `.github/codeql/codeql-config.yml` for analysis paths; `WindowsLocalTreeCopy.ResolveDirectChild` for copy-target child resolution.

## Verification checkpoint

- Analysis `1723374634` read back as 61 open findings: 47 generated `obj`, 14 owned/test.
- Security conformance/proof: PASS with all previous six cases plus two SEC-008 CodeQL weakening cases.
- Locked Release restore and solution build: PASS, zero warnings/errors.
- Affected Domain, Application, Infrastructure.Windows, and Presentation.WinUI tests: PASS, 393 total and zero skipped.
- ADV-009 now explicitly rejects empty, special, traversal, multi-segment, and rooted target-child names.

## Integration steps

1. Commit through the repository Commit-mode hook, push, and open a Draft PR closing #2.
2. Manually dispatch `security-deep-review` on the branch. Require deep review and CodeQL success on the exact remote head, then read branch alerts through the API and require zero.
3. Confirm the PR head equals local HEAD and the base is latest, then mark Ready and require fresh canonical CI.
4. Squash merge, synchronize clean `main`, dispatch the default-branch deep review, and require both success and zero open default-branch alerts by API read-back.

## Remaining environmental proof

Code-scanning analysis and alert state exist only on GitHub. Local build, tests, security conformance, and negative fixtures are complete; no desktop, keyboard, or destructive filesystem interaction is required.
