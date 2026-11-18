# First beta packaging — Issue #434

Status: Preview published and all three public assets verified on 2026-11-18; manual #94 remains open.

At 11:03 JST on 2026-11-18 hide requested publication as a Preview before returning
home for the manual check. ADR-0079 records this scoped permission. #94 remains open
and unperformed. Shipping verification, deep review and CodeQL readback completed
before publication. PR #402 remains excluded. Final evidence is recorded below.

hide requested a release at a usable checkpoint on 2026-11-18. The preparation
starts from main `97d8a12` after #428/PR #432 and includes the accepted #433 CodeQL
correction. Root owns integration, final release evidence, #94 confirmation and publication.
This change does not include the pending bookmark PR #402.

## Invariant and change

The App project's `Version` is the single product-version source. The existing
application project and official .NET publish remain the only build path. One
`eng/publish-release.ps1` entry creates an unsigned Windows 11 x64 self-contained
folder ZIP, original component notices, source/file hashes and a release manifest.
The existing Win32 assembly identity is retained independently of ProductVersion.
No product behavior, settings schema, dependency pin, gate or threshold changes.

Rules: ARC-001, QLT-009/012/013/014/015, SEC-007/008/009/011, CS-024/025,
GIT-001–004, ADR-0031/0054/0061. Waivers: none.

## Selected verification

Select script parsing and preflight refusal checks, actual locked restore/publish,
source/lock comparison, binary version/runtime composition and ZIP content/hash
verification. App version metadata may affect the Architecture test project's
project-dependency lock; inspect the actual restore result rather than assuming
no lock difference. No unrelated product tests or mutation run is selected for
packaging tooling. Root coordinates the final release deep review separately.

The following tool-specific checks ran before the shared restore/build/publish
slot was granted:

| Check | Result |
| --- | --- |
| PowerShell parser on both scripts | No parse errors |
| `publish-release.ps1` with relative output, checkout output, existing output and dirty checkout | All four reject with exit 1 and the intended reason, without creating output |
| `Copy-ReleaseNotices` on a fixed existing assets snapshot | 21 package/pack records; all 51 copied package files have the source SHA-256 |
| Notice fixtures with an escaping license path or package cache path | Both reject before creating their output |
| Windows SDK license with explicit original cache and fixed-URL download | Both reproduce SHA-256 `dd07eb178e00c6bba4148457fc00ff77cd4887eb521d504186fe59c9ec8bbe62` |
| Windows SDK license with an incorrect explicit cache | Rejects before copying, without falling back to network |

The assets snapshot in the notice test predates this candidate; this is collection
tool proof, not a claim that the release restore or current dependency audit ran.
Raw results are `preflight-results.json`, `notices-proof-2-result.json`,
`notices-negative-results.json` and `license-final-proof.json` under the task's
permanent evidence directory.

An attempt to add the original RTF directly failed `git diff --cached --check`
(upstream trailing whitespace, exit 2). A temporary ZIP-storage approach was
rejected during review and removed before commit. The final repository contains
only its fixed source/length/hash metadata; the original remains in external
evidence. Publishing retrieves or reuses that exact original and fails on a
missing or changed source. No whitespace rule, exclusion or binary attribute changed.

The independent review found that the final `git status` exit code was not checked:
a failed command with empty stdout could be mistaken for a clean checkout. The
publisher now rejects that failure before file inventory, ZIP or manifest creation.
`prove-publish-status.ps1` executes the unmodified status-check block extracted
from the old and corrected scripts with controlled Git exit/stdout. The old block
proceeds on exit 128/empty stdout; the correction rejects it with its specific
verification error. Dirty status and changed input hashes also reject, while a
successful empty status with unchanged inputs proceeds (4/4 corrected cases).
The changed publisher script parses with zero errors. This proof runs no restore,
build or publish. The proof script and `publish-status-proof.json` are preserved
in the task's evidence directory.

## Candidate attempts

The accepted main `860e61d` (#433 included) was merged through the normal commit
hook as `c93492f`. Candidate 01 on that source failed during locked restore with
four NU1004 errors: a command-line `RuntimeIdentifier=win-x64` also reached the
four RID-independent library projects and disagreed with their existing locks.
No build or ZIP ran; all source and lock files remained unchanged. The failed
candidate directory and `candidate-01-command.log` are retained.

The publisher now evaluates the App project's existing runtime declaration and
requires `win-x64`; it passes no global RID override. Under the corrected argument
set, property evaluation reports App RID `win-x64` / `IsRidAgnostic=false` and all
four libraries an empty RID / `IsRidAgnostic=true` (`rid-evaluation.json`). No
RID declaration or lock policy changed to accommodate the publisher.

Candidate 02 on `7143a76` passed locked restore for all five production projects
and official publish, including Release ReadyToRun. The publisher's required-file
check then stopped on `resources.pri`, so it created no ZIP or success manifest.
The SDK's actual unpackaged filename is `NeNeCommander.App.pri`; that file was
also absent from publish although present in the build output. The build/publish
inventory difference was that PRI plus eleven loose XBF files. `makepri dump`
confirmed the eleven application XBF resources are already `EmbeddedData` in the
PRI, and independent review matched all eleven complete XBF byte sequences inside
it. Loose XBF files therefore are not missing runtime content once the PRI ships.

The existing `EnableMsixTooling=false` selected MRT Core generation, which does not
add its PRI to .NET publish. The pinned SDK already supplies
`AddProjectPriToResolvedFileToPublish` in `Microsoft.Windows.SDK.BuildTools.MSIX.Pri.targets`.
The App now enables that tooling and keeps `WindowsPackageType=None`; independent
property evaluation confirms `AppxPackage`, `GenerateAppxPackageOnBuild`,
`PublishAppxPackage` and `_GenerateMsixPackage` remain false. The required-file
check uses the actual `NeNeCommander.App.pri` name. No custom target, bin-directory
copy, loose-XBF duplication, new package or alternate resource generation is added.
Candidate 02 and its log, inventory difference, PRI dump and embedded-resource
summary remain in external evidence. The changed project configuration requires
one corrected publish; ordinary product tests are not repeated for this metadata.

## Verified candidate 03

Source `00ae7205c6548397e491e6e73be56d0c567b0fbb` produced candidate 03 through:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-03 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

The runtime evaluation, locked restore, official publish with four MSBuild workers,
post-publish checks, notice collection and ZIP creation all return success. No build
warning or error was emitted. The manifest retains the exact argument arrays and
all input/file hashes. The standard SDK publishes `NeNeCommander.App.pri`; no MSIX
or loose application XBF files are produced in the candidate.

| Verification | Result |
| --- | --- |
| External `verify-candidate.ps1` | All 586 payload files and 586 ZIP entries match the manifest's size/SHA-256; no missing, unexpected or duplicate ZIP files |
| Source and dependency inputs | All 1,294 tracked input hashes match after publish, including all 10 lock files; checkout clean |
| Component notices | 21 package/pack records; all 51 package notice/manifest files match the restored originals; SDK RTF matches its fixed hash |
| Executable metadata | AMD64; ProductVersion `0.1.0-beta.1+00ae7205c6548397e491e6e73be56d0c567b0fbb`; FileVersion `0.1.0.0`; copyright `© 2026 HideyukiMORI`; unsigned |
| Runtime composition | Self-contained `.NET 10.0.15`, Windows App SDK native files present; no external framework requirement in runtimeconfig |
| PRI | SHA-256 `1c4db89ea63222b40e17af99071ba3c71fbef27ef1284200c0ef93b257eadb6f` matches the SDK build original and the ZIP; all 11 application XBF byte sequences are embedded |
| External `verify-pri-locales.ps1` with pinned `makepri dump /dt detailed` | Every `en-US` string (348/348) and `ja-JP` string (348/348) matches the source `.resw` value |
| `dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release` | Exit 0; all 10 lock hashes remain unchanged, checkout clean |

The candidate ZIP is 113,730,670 bytes with SHA-256
`1a3911183835cabdeb10b037f48ab84b86003fa955c00dd7a71c34913750adef`.
The stable Win32 side-by-side identity remains `1.0.0.0`, separately recorded from
the executable's product/file versions. The evidence directory retains
`candidate-03-command.log`, `candidate-03-proof.json`, `candidate-03-pri-locales.json`,
`architecture-lock-proof.json`, the proof scripts and the PRI dump. The independent
resource-path review is `pri-review.md`. Every source commit used the normal
conformance/security/commit-message hooks; no hook or gate was bypassed.

This final record update changes documentation only. Candidate 03 identifies its
own source and is not relabeled as a later commit. Root creates the shipping
package from accepted source after integration and reuses the unchanged applicable
proof under ADR-0061. No application was launched by this task.

## Limits of candidate proof

The candidate alone is not evidence of launch, #94 screen checks, all provider
environments, CodeQL alert closure, fresh deep review, signing or public hosting.
The final automated/publication evidence follows below; launch, #94, environmental
checks and signing remain unperformed. Microsoft package license/notice
files are retained from the exact resolved inputs. The Windows SDK's URL-only
license now has a verified original document and fixed retrieval metadata; the
distribution guide requires acceptance of applicable component terms before use.

## Accepted-source shipping package

PR #440 integrated the preparation as `46f1a82db62e2e28accf6bec3eb81a67f1d85a5a`.
Its dependency review run 44221630000 succeeded. The final Preview-policy review
found no remaining required correction at `9aa473d`; the old release-note wording
was corrected before acceptance. #94 remains open under ADR-0079.

The accepted source produced the shipping package with exit 0 and no warnings/errors:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/releases/0.1.0-beta.1/shipping-20261118 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

`verify-candidate.ps1` independently matched all 586 payload and ZIP files, all
1,295 source inputs, 10 locks and 51 original package notice files. The binary is
AMD64, FileVersion `0.1.0.0`, ProductVersion `0.1.0-beta.1+46f1a82db62e2e28accf6bec3eb81a67f1d85a5a`.
The ZIP is 113,730,832 bytes, SHA-256
`c4dad8d8007948ba5455950b9f653853e5971512dcb093e83c73622961910e69`.
The PRI and both source locale blobs equal candidate03 exactly; the 348/348
Japanese and 348/348 English string proof is reused, with all 11 embedded XBF
byte sequences checked in the shipping PRI. The source checkout remained clean.

Raw command/result, shipping proof and locale reuse records are under the task's
permanent evidence directory. The application was not launched. Release deep
review run 44221900660 was requested once on this exact source. Its successful
result, CodeQL API readback and publication are recorded below. The later
#439 / PR441 main commit `43e857e` is not part of the shipping source and does not
change its product inputs. No duplicate quality run was requested.

## Failed-candidate preservation

Candidate01's one file and candidate02's 531 files were archived under
`D:/NeNeCommander/evidence/issue-434/` as separate ZIPs. Every archive entry's bytes
and SHA-256 match the source inventory; per-file sizes, hashes and modification
times are retained in their `candidate-20261118-0N-archive.json` records. The final
console projection of ordered dictionaries printed null columns; the persisted
records were read back and summarized separately. No source directory was deleted
by that archival operation. After release acceptance and preflight, their
directory-removal request was rejected before launch (`blocked by policy`).
The directories remain in the explicit user cleanup list; no retry was made. Candidate03 remains referenced by the earlier #94
manual-check request, so retain it until that request is resolved; the final
shipping folder is the authoritative public-package source.

## Final automated acceptance and publication

The single [deep review 44221900660](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44221900660)
at shipping source `46f1a82db62e2e28accf6bec3eb81a67f1d85a5a` completed successfully
at 12:17:20 JST, within 96 hours of publication. It ran `pwsh -NoProfile -File
./eng/deep-review.ps1`, including the canonical `eng/check.ps1`: all 19 internal
checks passed, with 2,032 tests passed, zero failures and 16 live opt-in skips.
The three adversarial rounds passed and the dependency audit found no vulnerabilities.
Coverage was Domain/Application 100%, Infrastructure 94.48%, Presentation 94.45%.
Mutation was Domain 97.47%, Application 96.59%, Infrastructure 92.30%, Presentation 94.86%.
No additional quality run or repeated successful suite was requested.

The separate actual CodeQL workflow step also succeeded. Analysis 2192418870
identifies the same source, reports zero results, empty error/warning fields and
914/914 C# files scanned. The 12:19:04 JST API readback returned zero open alerts,
including zero owned alerts. Analysis success and alert closure were checked
separately; no dismissal, suppression, exclusion or baseline was added.

The raw artifact ZIP (14394166939) matches service digest
`a1524cf21232f1affd8e86e8efbbf14ba840d9aa5515d8df7e32c5c02cf06f19`.
The run JSON, complete raw log, deep-review JSON, four mutation reports, CodeQL
analyses and open-alert response are preserved under `D:/NeNeCommander/evidence/issue-434/`.
A log-read request while the job was running returned BlobNotFound/404; the
completed log was subsequently retrieved. No gate retry resulted from that read failure.

At 12:24:04 JST the annotated tag `v0.1.0-beta.1` and
[NeNe Commander 0.1.0-beta.1 Preview](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.1)
were published (release 449483714, prerelease true, draft false).
The remote tag peels to the shipping source. At 12:25:30 JST all three assets
were downloaded through anonymous HTTPS, without an authentication header.
Each size and SHA-256 matched both its local shipping original and service digest:

| Asset | Bytes | SHA-256 |
| --- | ---: | --- |
| NeNeCommander-0.1.0-beta.1-win-x64.zip | 113730832 | `c4dad8d8007948ba5455950b9f653853e5971512dcb093e83c73622961910e69` |
| release-manifest.json | 346162 | `42fe0f937e1604ea2d2c99f501252c5c1975c75e1b6a26d36e46f63206b8c7d7` |
| SHA256SUMS.txt | 193 | `036158fe680cba6083560774eb1326f50192822f6d4de1144d63d335b8051d80` |

The exact publication result, release-note body, remote tag, public-release JSON
and `public-download-proof.json` are preserved. This verification does not claim
the downloaded package was launched. #94 remains open/unperformed, including
DPI/high contrast and manual keyboard/modal confirmation; the unsigned status
and limitations are explicit in the published description. #402 remains Draft
and excluded. Later tooling commits #439/#443 are also outside the release source.

## Completion and retained output

Documentation-only finalization reuses these successful unchanged inputs under
ADR-0061 and runs text/diff/link review plus normal commit hooks only.
The shipping folder is permanent. Candidate03 and the manual UI fixture are
retained because the outstanding #94 request references them. The 112 temporary
tool-output files were copied and hash-verified under `issue-434/tool-outputs`.
Deletion of that output directory and failed candidates 01/02 was rejected
before launch; their exact targets and the prepared user-only command are in the
daily report/handoff. No rejected deletion was retried. The publication-record
worktree is eligible for native removal only after this PR is integrated;
`issue-434/final-completion.json` records that separate outcome. Branches, commits,
permanent evidence and the public shipping artifacts remain retained.
