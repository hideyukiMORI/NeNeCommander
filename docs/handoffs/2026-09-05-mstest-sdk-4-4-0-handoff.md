# MSTest.Sdk 4.4.0 handoff — 2026-09-05

Status: informational

## Work item

- Issue: #68
- Branch: `build/68-mstest-sdk-4-4-0`
- Replaces: stale Dependabot PR #27
- Decision record: ADR-0006 remains authoritative because the test runner mechanism is unchanged.
- Invariant: every test project uses the single central SDK pin and preserves discovery, filters, coverage, and locked restore.
- Canonical mechanism: `global.json` plus CFG-002 and the five test-project lock files.

## Completed verification

Locked restore and Release build passed with zero warnings/errors. All 400 tests passed with zero skips. The four adversarial filters selected 38, 48, 32, and 5 tests and all passed. Coverage remained at 100%, 100%, 96.27%, and 96.84%. Negative gate proof passed with the new SDK pin-drift case.

## Integration steps

1. Commit through the repository Commit-mode hook, push, and open a Draft PR closing #68.
2. Confirm the remote PR head equals local HEAD and its base is current.
3. Mark Ready and require the canonical CI gate on that exact candidate.
4. Squash merge, synchronize clean `main`, and close stale PR #27 with a link to the replacement.

## Remaining environmental proof

No desktop or device proof applies to this test-toolchain update. The only outstanding proof is GitHub's canonical Ready CI on the final integration candidate.
