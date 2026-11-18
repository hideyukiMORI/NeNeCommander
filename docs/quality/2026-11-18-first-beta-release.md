# First beta packaging — Issue #434

Status: implementation; release not yet approved or published.

hide requested a release at a usable checkpoint on 2026-11-18. The preparation
starts from main `97d8a12` after #428/PR #432. Root owns the separate #433 CodeQL
correction, integration, final release evidence, #94 confirmation and publication.
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

The heavy restore/build/publish slot has not yet been granted. Packaging validation
is still pending. The following tool-specific checks ran before that slot:

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

## Remaining release work

The resulting package is not evidence of launch, #94 screen checks, all provider
environments, CodeQL alert closure, fresh deep review, signing or public hosting.
Those remain separate recorded conditions. Microsoft package license/notice
files are retained from the exact resolved inputs. The Windows SDK's URL-only
license now has a verified original document and fixed retrieval metadata; the
distribution guide requires acceptance of applicable component terms before use.
