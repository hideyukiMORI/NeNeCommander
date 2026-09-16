# Live WSL harness daily report — 2026-09-07

Status: Draft; environmental proof incomplete

## Scope and invariant

Issue #93 keeps product transfer on the single `FileOperationGateway -> ProviderFileOperationPort -> WslFileOperationAdapter -> WindowsWslFileSystem` path. ADR-0043 adds an opt-in test owner and launcher around that path. The launcher accepts only a retained empty `/tmp/NeNeCommander-Live-*` root, binds operator home/mount facts to observed root identities, requires exactly three Passed TRX cells, and never counts skips as live proof.

## Implemented Draft

- `LiveWslTestRoot` owns one run child and create-new marker, captures every owned entry, checks non-link containment and identity before setup/effects/cleanup, refuses foreign residue, and retains the configured root.
- `LiveWslTestRootTests` cover missing or malformed admission, unsafe/unregistered/nonempty roots, another-root identity, replacement after admission, root/run/marker replacement, same-entry marker rewrite, foreign residue, and owned-link cleanup.
- `LiveWslTransferTests` define nested copy, composite move, and source-link refusal. They independently compare exact bytes and ADR-0037's declared kind, entry set, and byte-count metadata. TestContext output is limited to provider, redacted root/identity, typed completion/failure/effects, and setup/cleanup status.
- `eng/run-live-wsl-tests.ps1` builds the focused Release project, uses only the fixed current Infrastructure assembly/type/member for identity, checks all registered account homes and the native mount, rechecks identities, writes ephemeral runsettings, and validates exactly three Passed TRX results. A `/` account home is handled by the existing forbidden-root shape; every other home is checked for equality and ancestor/descendant overlap.

## Actual environment and result

The diagnostic snapshot was dirty at base `0dab3f67c07fceafc77827e78f068a36c36e7003`; it is evidence for the described working tree, not for a completed commit. Windows was `10.0.26200.9278`, WSL was `2.7.13.0`, and its reported kernel was `6.18.33.2-2`. One registered WSL2 distribution supplied an existing, non-link, empty dedicated root on native `ext4`. Distribution, user, root text, and identifier values remain out of this public report.

The first configured launcher attempt exposed and then corrected a StrictMode-only empty-pipeline count bug before C# ran. The next attempt loaded the exact existing `WindowsFileIdentifier.Describe` member but `GetFileInformationByHandleEx(FileIdInfo)` returned `ERROR_NOT_SUPPORTED` for `/tmp` through the actual WSL UNC provider. It stopped before home/mount completion, run-child creation, fixture setup, gateway execution, or cleanup. A final read-only check confirmed that the dedicated configured root remained empty and non-link.

A bounded read-only diagnostic applied legacy `GetFileInformationByHandle` to the same no-follow handles. It was stable across repeated observations and distinguished the sampled distribution root, `/tmp`, configured root, and an existing link from its target. Its WSL volume serial was zero. Microsoft documents that the legacy identity relies on the volume serial plus 64-bit file index, that file IDs can be reused or change, and that network providers may return partial information or fail. Distribution text plus a zero volume serial cannot exclude inode collisions across mounts or filesystems inside one distribution. The fallback was rejected; production code did not change. Issue #105 now owns an equivalent-strength identity decision.

The official WSL Plan 9 server source confirms that `StatToQid` assigns `qid.Path` from `st_ino`, while `st_dev` is retained separately as the file object's device value and is not part of that QID. Directory enumeration likewise publishes `d_ino`. No official Windows `p9rdr` source was found that guarantees an additional mount/device component in the legacy Windows FileIndex. This rejects the hypothesis that the sampled FileIndex alone is a provider-wide device-plus-inode identity.

References: [GetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle), [BY_HANDLE_FILE_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/ns-fileapi-by_handle_file_information), [FILE_ID_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info), [CreateFileW no-follow behavior](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew), and [commit-fixed official WSL Plan 9 source](https://github.com/microsoft/WSL/blob/556440f392aef150dc2e8d39152b20a4d7d5b83c/src/linux/plan9/p9file.cpp#L68-L72).

## Focused verification

| Command | Result |
| --- | --- |
| `dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore` | Exit 0; zero warnings/errors. |
| `dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~LiveWslTestRootTests'` | Exit 0; 15/15 passed. |
| `pwsh -NoProfile -File ./eng/conformance.ps1 -RepositoryRoot .` | Exit 0; 112 rules passed. |
| `pwsh -NoProfile -File ./eng/security-check.ps1 -RepositoryRoot . -SkipProof` | Exit 0; 18 adversarial cases registered; script/secret/supply-chain checks passed. |
| `git diff --check` | Exit 0. |
| unset `NENE_COMMANDER_WSL_TEST_ROOT`; `pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1` | Runner exit 1 as required; 0 executed, 3 skipped with `LiveWsl:Unexecuted:RootParameterAbsent`; exact WSL version and dirty snapshot recorded. |
| configured dedicated root; `pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1` | Failed closed at the identity query with `ERROR_NOT_SUPPORTED`; live test process did not start. |
| fixed read-only WSL empty-root check | Passed after all diagnostics; configured root retained empty and non-link. |

The three required live cells are unexecuted, and actual product transfer mutations are zero. The full canonical gate and deep review were not run because this Draft is blocked and cannot become Ready or merge. No skipped or focused result substitutes for those tiers.

## Remaining work

1. Resolve Issue #105 without weakening ADR-0033/0036 or adding a shell identity engine.
2. Rebase or update this Draft if needed, rerun focused safety checks, and run the three configured live cells on the retained empty root.
3. Record exact Passed TRX, typed outcomes/effects, source/target assertions, and successful owned cleanup.
4. Only after the live acceptance is complete, perform the security-sensitive deep review and Draft-to-Ready canonical CI gate required for merge.
