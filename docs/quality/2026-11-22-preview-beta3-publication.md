# Preview beta.3 publication — 2026-11-22

Status: completed publication; task cleanup follows the completion record.

## Accepted source and publication

hide's release-first instruction and acceptance of PR402 govern this work.
PR529 was Ready at15:30, dependency review44994027218 succeeded, and no review
thread remained. The GitHub connector's merge request returned403 without
mutation; the existing authenticated CLI then performed the normal, exact-head
squash. No admin bypass, force push or branch deletion was used.

- Accepted PR head: `6acdc2fdbd415bde87cebcc021867eefa8034eb3`.
- Squash/main source: `288a69575074e124b1f642e7e365b9cc8a05016d`,15:35:34 JST.
- Both trees: `fdce971f81020e5d46184b80ef61831a44a996c2`.
- No non-Markdown difference from locally proved264cffa.
- Annotated tag: `v0.1.0-beta.3`, targeting that accepted source.
- [Published Preview](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.3):
  release454258177,15:46:01 JST,prerelease=true,draft=false.
- Existing beta.2 assets and the user's running App/settings were not replaced.

The invariant is that the published bytes come from the accepted clean source
through the sole `eng/publish-release.ps1` entry. File-operation execution,
provider identities and keyboard command ownership are unchanged in this closure.

## Reused automatic proof and authorized CodeQL upload

[The correction record](2026-11-22-release-codeql-corrections.md) owns all build,
coverage, mutation, adversarial and semantic-analysis details, including failed
preparations and unchanged-file extraction limitations. Independent review found
no local proof blocker. Do not call the buildless extraction error-free.

The deep review44936588484/9ca919d completed its19 checks at2026-11-22 04:33:50 JST.
At15:43:03 it was11.153842hours old, inside96hours. All unaffected inputs and the
three files' supplementary proof are recorded under ADR-0061; commit/merge-only
changes did not trigger repeated tests.

hide's15:24「送信して」resolved the two earlier automatic approval rejections for
the original SARIF, including local diagnostic paths, to candidate and accepted
main. The payload remains3,548,275bytes, SHA256
`d255f59d3bc8ab1885deae032c446da04ac23551f5857a5a57b00ae23b9e9e64`.
Candidate analysis2223321214 and main analysis2223342717 both completed with164
rules,0 findings,empty error/warning and0 open ref alerts. No dismissal, deletion
or baseline was used. Authentication was piped to the official CLI's standard
input; no credential was saved.

## Packaging and independent verification

The accepted source was checked out detached in the existing D worktree. The
canonical publisher performed locked Release restore and official self-contained
publish; no application was launched.

| Check | Observed result |
|---|---|
| Payload and ZIP | All586files match size/SHA256 |
| Source inputs | All1,469tracked inputs unchanged;10lock files |
| Package originals | 21packages,51notice files match originals |
| SDK license | Pinned original SHA256 matches |
| Compiled resources | 11XBF bodies present in matching SDK-generated PRI |
| Localized strings | en-US387/387;ja-JP387/387;0mismatches |
| Independent extraction | 586files /298,924,352bytes match;0mismatches |
| Executable | AMD64;FileVersion0.1.0.0;ProductVersion0.1.0-beta.3+288a695… |
| Runtime and signature | .NET10.0.15 included;no external framework requirement;NotSigned |

The initial PRI verifier incorrectly converted the periods inside
`[using:Microsoft.UI.Xaml.Automation]` into path separators. The expected
AddressEditorInput attached property was present in the raw dump. The original
script and failure are preserved; the corrected verifier reconstructs resw names
from PRI separators, preserving namespace periods. It reused the unchanged dump
and checked every string. No product or payload modification, rebuild or new
analysis followed this verifier-only correction.

## Public assets

At15:49:06 the three public URLs were downloaded with no Authorization header or
default credentials. Every size and SHA256 matched both GitHub's service digest
and the local accepted candidate; mismatches0.

| Asset | Bytes | SHA256 |
|---|---:|---|
| NeNeCommander-0.1.0-beta.3-win-x64.zip | 113,826,188 | `8f62fcca810e5f47ef3f67bd430320f265a85ba83c317fb791a791c35e2e8f4c` |
| release-manifest.json | 377,139 | `4b5651772d466a24f23e5fcc5dc52b70ff8fc865992713811662d1f22bd93a2d` |
| SHA256SUMS.txt | 193 | `e5f61b1175ba408a76e80797cda1b6eec41d4e766dc582568735e06262d19258` |

Formal candidate:
`D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01`.
Permanent evidence:
`D:/NeNeCommander/evidence/release-beta3-20261122`.
Key records include merge-tree-proof.json,codeql-corrections/main-codeql-acceptance.json,
publication-freshness.json,candidate-01-verification.json,candidate-01-pri-locales.json,
candidate-01-extraction-proof.json,published-release.json and public-download-proof.json.

## Exact commands and outcomes

All following commands exited0 except the explicitly recorded first PRI attempt
and connector403. Paths below are the actual D evidence/source paths.

```powershell
gh pr merge 529 --repo hideyukiMORI/NeNeCommander --squash --match-head-commit 6acdc2fdbd415bde87cebcc021867eefa8034eb3 --subject 'chore(release): beta.3の配布準備と公開前検証の不足を補う (#522)'
git fetch origin
git merge --ff-only origin/main
git switch --detach 288a69575074e124b1f642e7e365b9cc8a05016d
codeql github upload-results --sarif=D:/NeNeCommander/evidence/release-beta3-20261122/codeql-corrections/final.sarif --repository=hideyukiMORI/NeNeCommander --ref=refs/heads/main --commit=288a69575074e124b1f642e7e365b9cc8a05016d --checkout-path=D:/NeNeCommander/worktrees/issue-522 --github-auth-stdin
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta3-20261122/verify-candidate.ps1 -Candidate D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01 -RepositoryRoot D:/NeNeCommander/worktrees/issue-522 -ResultPath D:/NeNeCommander/evidence/release-beta3-20261122/candidate-01-verification.json
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta3-20261122/verify-pri-locales.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta3-20261122/verify-extracted-candidate.ps1
git tag -a v0.1.0-beta.3 288a69575074e124b1f642e7e365b9cc8a05016d -m 'NeNe Commander 0.1.0-beta.3 Preview — 検証済み公開版'
git push origin refs/tags/v0.1.0-beta.3
gh release create v0.1.0-beta.3 D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01/NeNeCommander-0.1.0-beta.3-win-x64.zip D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01/release-manifest.json D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01/SHA256SUMS.txt --repo hideyukiMORI/NeNeCommander --verify-tag --prerelease --latest=false --title 'NeNe Commander Preview 0.1.0-beta.3' --notes-file D:/NeNeCommander/evidence/release-beta3-20261122/release-notes-final.md
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta3-20261122/verify-public-assets.ps1
```

Raw GitHub API readbacks preserve processing IDs,main/ref results and release
metadata. This documentation closure uses the normal Commit gate plus text/link/
diff review only; no product tests or CI are requested for these Markdown changes.

## Limits and remaining work

#94's one-time hide screen check and PR402 acceptance remain the human evidence.
This publication adds no new-PC launch,all-provider live proof,all-locale visual
proof or native speed claim. Known #523reactivation and #550post-Move focus reports
remain in the release notes; AppSDK PR517 and #453/#491 are separate.

After the completion documentation is integrated, preserve and verify all unique
task artifacts before removing only issue522 and its release-beta3 output.
Archive/cleanup manifests and completion.json in the permanent evidence directory
own the actual result; a future deletion is not claimed complete here.
Keep the formal candidate,public-download proof,branches,commits and evidence.
Retain the user's issue495 App and unfinished491/514/pr498 work.
