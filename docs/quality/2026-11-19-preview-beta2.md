# Preview beta.2 — 2026-11-19

Status: Preview published at 12:19:28 JST; all three public assets independently
downloaded and verified at 12:20:18 JST on 2026-11-19.

Issue #459 follows hide's request to publish a Preview at a verified checkpoint.
[v0.1.0-beta.2](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.2)
is an unsigned Windows 11 x64 prerelease. PR402 remains excluded.

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

PR461 integrated the version/distribution preparation as
`555704e5905f9c68db041e1566c79aba530b1692`. Its tree is identical to reviewed
`5197ca89c94c1ba98c404b25253d8bc03605a82c`; `git diff --exit-code` confirms this.
Dependency review 44441639885 passed. The normal commit hook passed 112 normative
rules and 24 registered adversarial cases. No gate was bypassed.

## Release deep review and CodeQL

The single [deep review 44441599491](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44441599491)
ran on `5197ca8` and completed successfully at 12:11:50 JST. The script report
finished at 12:08:09 JST; it was about 0.175 hours old at the publication preflight.
Every required internal check passed: canonical gate, tool restore, dependency
audit, three adversarial passes and mutation for all four protected layers.

- Canonical tests: 2,081 total, 2,065 passed, zero failed, 16 opt-in live skips.
- Branch coverage: Domain 100.00%, Application 100.00%, Infrastructure 94.48%,
  Presentation 94.47%; every protected threshold passed.
- Dependency audit reported no direct/transitive vulnerability entries.
- The separate CodeQL step passed. API analysis `2201989996` identifies the exact
  reviewed commit, has no error and zero results. A separate paginated open-alert
  API read returned zero alerts, therefore zero open owned alerts.

Artifact `14495201065` is 1,387,271 bytes and SHA-256
`5f2ace17f192e909a77506628279cca94920fb5501db90cf13cd77326f29461f`, matching the
service digest. Its report/mutation evidence and the decoded job log are retained.
No duplicate quality workflow was dispatched. The reviewed and published trees
are identical, so ADR-0061 permits reuse across the squash-merge commit identity.

## Official package and extraction

The clean accepted-source checkout produced candidate 01 through the sole entry:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/releases/0.1.0-beta.2/candidate-20261119-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

The pinned SDK evaluation, locked restore, official self-contained Release publish,
notice collection, unchanged-source check and archive creation completed with exit
0 and no build warnings/errors. The manifest records the exact argument arrays.

| Independent check | Observed result |
|---|---|
| `verify-candidate.ps1 -Candidate .../candidate-20261119-01 -RepositoryRoot D:/NeNeCommander/worktrees/issue-459 -ResultPath .../candidate-verification.json` | All 586 payload files and 586 ZIP entries match size/hash; no missing, duplicate or unexpected files |
| Tracked inputs and locks | All 1,323 input hashes and 10 lock hashes match; checkout clean |
| Notices | All 51 original package notice/manifest files across 21 package/pack records match; original SDK RTF matches its pinned hash |
| Executable | AMD64, ProductVersion `0.1.0-beta.2+555704e5905f9c68db041e1566c79aba530b1692`, FileVersion `0.1.0.0`, copyright retained, unsigned |
| Runtime | Self-contained .NET 10.0.15 and Windows App SDK payload; no external .NET framework requirement in runtimeconfig |
| PRI | SHA-256 `b42a49f850f21b3a1ea2cf776577727773152d08bd4a7cb5d77848aae89da585`; matches SDK output; all 11 complete application XBF byte sequences embedded |
| `verify-pri-locales.ps1`, using the pinned SDK `makepri dump /dt detailed` | All 350 en-US and 350 ja-JP strings equal the source catalogs |
| Separate `ZipFile.ExtractToDirectory` followed by every manifest file's size/hash comparison | All 586 extracted files match |

The ZIP is 113,745,349 bytes, SHA-256
`898fee78fae8a2b8361ebf7bdc6a735d5dff5287591c4959d487acae7da3beb4`.
This package was generated from accepted source; the running normal application's
bin directory was not used or overwritten.

## Extracted-package launch

The independently extracted package ran as owned process 31168/window 9244474.
At 12:07–12:08 JST it displayed both directory panes, F1 keyboard help and the new
key-hint setting with its explanation. Escape closed the settings and Alt+F4
closed the process. No setting was changed; the original 218-byte settings hash
remained `ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`.
Normal process 38360 and the unrelated Chrome window were preserved.
`native-package-result.json` and the AX/image evidence record this current-host
smoke check; it does not establish a fresh-machine deployment tier.

## Public distribution

Annotated tag `v0.1.0-beta.2` peels to accepted source `555704e`; the remote readback
matches. GitHub release `450836498` is `draft=false`, `prerelease=true`, published
at `2026-11-19T03:19:28Z`. It has exactly these three assets:

| Asset | Bytes | SHA-256 |
|---|---:|---|
| `NeNeCommander-0.1.0-beta.2-win-x64.zip` | 113745349 | `898fee78fae8a2b8361ebf7bdc6a735d5dff5287591c4959d487acae7da3beb4` |
| `release-manifest.json` | 350992 | `c592d512b5e618c51ea4f577c8225fdc012da89c481541bd004353ee221fc254` |
| `SHA256SUMS.txt` | 193 | `88bb55eda2232d1d214260d4d035321b719efc16a0349b63ee44ea9ff6c2f065` |

`pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/verify-public-assets.ps1`
downloads their public URLs without an Authorization header or default credentials.
Every downloaded size/hash matches the local candidate and GitHub's service digest;
zero mismatches. The public files, response metadata, final release notes and tag
readback remain in the evidence directory.

## Environmental boundaries

Issue #94 is closed: hide reported the one-time display/keyboard/high-contrast
check complete on normal source `6b0d231`; the exact selected scale and independent
captures were not recorded. This is separate from PR402 acceptance. The footer's
Japanese native walk covers wrapping at 954×658 pixels, settings, focus, restart
and owned copy progress/results. It does not establish explicit scale, English,
high contrast or Narrator for the new footer. The extracted-package launch above
is separate; no fresh-machine or all-provider live environment check is claimed.

Permanent evidence: `D:/NeNeCommander/evidence/release-beta2-20261119/`.
Task output: `D:/NeNeCommander/outputs/release-beta2-20261119/`.
The official candidate remains under `D:/NeNeCommander/releases/0.1.0-beta.2/`.
The running normal app, its fixture and unfinished Bookmark work remain preserved.
Completion documentation starts no product tests or CI. Cleanup is evaluated only
after its records are integrated, with exact paths, ownership, unique evidence,
ignored/untracked content, links and running references checked first.

## Publication records and cleanup

PR462 integrated these publication records as `31af3de` at 12:28:00 JST. The main
checkout is clean and synchronized; the documentation tree matches `44d38f9`.
No executable input or distributed asset changed, so no build/test/CI was repeated.

Cleanup preflight preserved and hash-matched all four task-output files, compared
all 586 extracted payload files with the retained official candidate, verified a
clean worktree and classified all 803 ignored files as bin/obj/artifacts. No links
or running target references were found; normal process 38360 remains preserved.
The 12:29 cleanup invocation was rejected before CreateProcess with `blocked by
policy`. Neither target was deleted and no alternative mechanism was attempted.
The daily report and handoff now carry all four pending #454/#459 targets and one
inspectable command. Permanent evidence, candidate, branches and commits remain.
