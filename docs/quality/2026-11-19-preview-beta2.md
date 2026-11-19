# Preview beta.2 — 2026-11-19

Issue #459 follows hide's request to publish a Preview at a verified checkpoint.
This initial record describes preparation; publication is not yet established.

## Source and invariant

PR460 integrated footer Issue #454 as `c25dc30`. Its selected proof is retained in
[the footer record](2026-11-19-footer-key-hints.md); the tree matches the verified
candidate `f64c07b`. The release also contains the F6 progress resource correction
in PR449/`6b0d231`, which was absent from public beta.1. PR402 and PR458 remain
outside main pending their separate screen acceptance.

The App project's Version is the only product version, now `0.1.0-beta.2`.
`eng/publish-release.ps1` remains the only packaging entry point. No production
behavior, dependency version, gate, threshold or waiver changes in this release
preparation. The README describes schema 4, its read-only legacy migration and
older binaries' refusal to overwrite a rejected document.

## Selected release proof

QLT-013/SEC-009 require successful applicable deep review no older than 96 hours.
The footer's changed implementation invalidates beta.1's affected release proof.
Request one `security-deep-review.yml`; it includes the canonical gate, dependency
audit, adversarial repetition, mutation and CodeQL. Do not request a second quality
run. Verify successful analysis and zero open owned alerts separately through the
code-scanning API. A project-version change also matches dependency-review inputs.

`dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release`
passed with exit 0, including App and its references. No lock file changed.

The single publish entry will verify clean committed source, locked restore,
runtime/metadata, unchanged input hashes, notices, payload and archive hashes.
Independently extract the candidate and compare every payload entry. After
publication, anonymously download the three public assets and compare their
size, SHA-256 and service digest. These release checks are pending in this record.

## Environmental boundaries

Issue #94 is closed: hide reported the one-time display/keyboard/high-contrast
check complete on normal source `6b0d231`; the exact selected scale and independent
captures were not recorded. This is separate from PR402 acceptance. The footer's
Japanese native walk covers wrapping at 954×658 pixels, settings, focus, restart
and owned copy progress/results. It does not establish explicit scale, English,
high contrast or Narrator for the new footer. No launch of the shipping package or
all-provider live environment check is claimed by packaging alone.

Permanent evidence: `D:/NeNeCommander/evidence/release-beta2-20261119/`.
Task output: `D:/NeNeCommander/outputs/release-beta2-20261119/`.
Official candidates will be retained under `D:/NeNeCommander/releases/0.1.0-beta.2/`.
The running normal app, its fixture and unfinished Bookmark work remain preserved.
