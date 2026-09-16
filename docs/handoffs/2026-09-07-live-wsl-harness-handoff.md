# Handoff — live WSL harness — 2026-09-07

Status: blocked Draft; not merge ready

## Resume contract

Issue #93 is implemented as a reviewable Draft on `test/93-live-wsl-harness`, but its required live proof did not execute. Issue #105 is now the first dependency: the actual WSL UNC provider rejects the existing `GetFileInformationByHandleEx(FileIdInfo)` query with `ERROR_NOT_SUPPORTED`. Do not mark #93 complete, mark its PR Ready, run merge gates as if live passed, or add the rejected legacy fallback.

The dedicated root remains retained, empty, and non-link. Its exact local location and selected distribution are recorded only in the ignored local diagnostic report. Public logs and committed reports redact distribution, user, root, and identifier values. Do not remove the configured root or change its parent/distro as cleanup; resume with a read-only emptiness and identity preflight after #105 supplies a compatible mechanism.

## Invariant and implementation

Product transfer remains `FileOperationGateway -> ProviderFileOperationPort -> WslFileOperationAdapter -> WindowsWslFileSystem`. ADR-0043 adds one launcher and one test-owned root boundary. The runner checks root shape, all account homes, native mount facts, pre/post identities, an ephemeral runsettings file, and exactly three Passed TRX cases. The C# owner independently rechecks admitted `/tmp` and configured-root identities before its first mutation, then tracks the run child, marker, setup entries, gateway-created entries, and cleanup.

The three live tests define required copy, composite-move, and link-refusal assertions. They compare exact bytes and the provider-declared kind/entry-set/byte-count contract and emit only redacted typed evidence. They are definitions, not successful environmental proof.

## Evidence and blocker

The matching [daily report](../reports/2026-09-07-live-wsl-harness-daily-report.md) records exact commands and results. The final focused checkpoint built with zero warnings/errors, passed 15/15 root-safety tests, passed 112-rule conformance, passed the focused security scan, and passed `git diff --check`. The unset launcher reported 0 executed and 3 skipped, then returned exit 1. The configured launcher stopped at the existing identity query before fixture/product mutation. A final read-only root check passed.

The read-only legacy query supplied a stable 64-bit file index for sampled WSL entries but a zero volume serial. That cannot preserve the existing provider/volume collision guarantee across mounts inside one distribution. Issue #105 requires an equivalent-strength provider/mount epoch plus entry identity, no-follow same-handle semantics, restored-metadata replacement refusal, same-entry rewrite refusal, and closed incomplete-information behavior. It does not require eliminating the already documented path-reopen TOCTOU interval.

Full canonical and deep review were deliberately not run for this blocked Draft. The live transfer count remains 0/3, and product transfer mutation count remains zero.

## Resume order

1. Read AGENTS, PROJECT_STATE, ADR-0033, ADR-0036, ADR-0043, Issue #105, this handoff, and the Draft PR. Confirm the dedicated root is still empty without following links.
2. Resolve #105 through one accepted identity mechanism; do not use shell mutation/identity or metadata-only/legacy volume-zero fallback.
3. Update #93 onto the resulting identity change, rerun only affected focused proof, then run `eng/run-live-wsl-tests.ps1` with the explicit retained root. Require all three exact TRX cases to be Passed and cleanup to retain an empty configured root.
4. Update report/state with the final commit and live evidence. Run security deep review only when the security-sensitive candidate is integration ready, then mark Draft Ready to request the canonical CI gate. Merge only after all required evidence passes.
5. After #93 completes, continue Issue #94 as the next dedicated release-environment tier. Keep parallel #99/PR #102 isolated.
