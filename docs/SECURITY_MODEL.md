# Security Model

Status: normative

## Assets

The protected assets are user files, file metadata, credentials implicit in UNC access, WSL distribution data, settings, operation history, diagnostic logs, signing material, CI credentials, and the integrity of binaries and update inputs.

## Trust boundaries

- All path text, filenames, metadata, settings files, clipboard content, drag/drop data, and persisted history are untrusted.
- Windows local, UNC, and each WSL distribution are distinct providers and distinct failure domains.
- Shell, Win32, Windows App SDK, NuGet, GitHub Actions, and design/review services are external boundaries.
- Presentation events and cancellation can arrive in hostile orders.
- Diagnostic output may leave the process and must not contain credentials, secret values, or unnecessary full paths.

### SEC-001 — Threats have executable mappings

- Status: **active**
- Enforcement: `eng/adversarial-cases.json`, security scan, and adversarial tests.

Every material threat has a stable ID, owner, expected defense, and test mapping. A security-relevant behavior change updates the threat model and its test in the same change.

### SEC-002 — Untrusted input is bounded before use

- Status: **active**
- Enforcement: parser, size-limit, containment, and malformed-input tests.

Inputs are length-bounded, parsed once, normalized without identity loss, and rejected when ambiguous. Paths, distribution names, resource keys, settings values, and operation batches never enter shell or filesystem APIs as unchecked text. WSL discovery uses fixed process argument tokens, bounds both redirected streams before publishing output, and validates every reported distribution through the canonical path parser; one malformed line rejects the whole snapshot. WSL directory reads route only from a validated `WslPath`, and each untrusted entry name passes through `FileSystemPath.Child` before publication.

### SEC-003 — Filesystem operations resist races

- Status: **active**
- Enforcement: failure-injection and race tests.

Preflight does not grant permanent trust. Adapters revalidate identity and containment at the side-effect boundary, treat links explicitly, use handles or provider identity where available, and report time-of-check/time-of-use changes without widening the target. Windows local identity combines the Win32 volume/file identifier obtained without following a reparse point with rewrite-sensitive metadata; query ambiguity or failure is closed.

### SEC-004 — CI and runtime use least privilege

- Status: **active**
- Enforcement: workflow scan and adapter design.

Workflows default to `contents: read`, checkout does not persist credentials, and only CodeQL analysis receives `security-events: write`. Runtime code does not request elevation or broaden ACLs. Privilege escalation requires a new threat model and ADR.

### SEC-005 — Secrets are prohibited in repository content

- Status: **active**
- Enforcement: local secret-pattern scan, negative proof, and GitHub push protection when hosted.

Credentials, private keys, connection strings containing secrets, signing keys, and access tokens are never committed, even as test data. Tests construct unmistakably synthetic values at runtime.

### SEC-006 — Workflow dependencies are immutable

- Status: **active**
- Enforcement: workflow conformance scan and Dependabot.

Every external GitHub Action is allowlisted and pinned to a full 40-character commit SHA with its release tag in a comment. Mutable tags and branches are prohibited. Dependabot proposes updates; the security gate verifies them.

### SEC-007 — Every package advisory blocks

- Status: **active**
- Enforcement: repository-wide NuGet Audit and dependency review.

NuGet audit is enabled for direct and transitive dependencies at `low` severity. `NU1901` through `NU1904` are errors. Advisory suppressions are prohibited. Pull requests adding any known vulnerable dependency fail dependency review.

### SEC-008 — Static security analysis is periodic

- Status: **active**
- Enforcement: CodeQL scheduled workflow at implementation stage.

CodeQL uses the extended security-and-quality query suite on every three-day deep review and can also be manually dispatched. C# is analyzed without a build, and the canonical CodeQL configuration excludes only generated `obj` trees; it does not define a positive `paths` allowlist that could omit modified, untracked, or newly added owned source. Repository conformance protects the query suite, build mode, configuration reference, and exact generated-path exclusion with negative proofs. Results are uploaded as code-scanning findings. CodeQL findings are not converted into a baseline, and workflow success is not evidence that the open-alert count is zero; alert state is read back from the code-scanning API after analysis.

### SEC-009 — Hostile behavior is reviewed every three days

- Status: **active**
- Enforcement: scheduled deep-review workflow and report.

The default branch runs the canonical gate, secret and workflow scan, adversarial case suite, dependency audit, and mutation analysis every third UTC calendar day. Schedule delay by the hosting platform is recorded; a release requires a successful deep review no older than 96 hours.

### SEC-010 — Logs minimize sensitive data

- Status: **active**
- Enforcement: diagnostic contract tests.

Logs use typed event IDs and provider kinds. Credentials, environment values, file contents, command lines, UNC credentials, tokens, and full paths are prohibited. Path evidence is reduced to a stable salted diagnostic fingerprint and non-sensitive basename only when required.

### SEC-011 — Security failures fail closed

- Status: **active**
- Enforcement: command and provider tests.

Unknown capability, ambiguous identity, verification failure, stale state, malformed settings, unavailable security scanner, or incomplete destructive confirmation stops the affected operation. It never selects a permissive fallback.

### SEC-012 — External AI review requires explicit approval

- Status: **active**
- Enforcement: workflow secret and network review.

Repository content is not sent automatically to Claude, ChatGPT, or another external model. An AI red-team integration requires an ADR naming the provider, exact data sent, retention policy, credential boundary, cost ceiling, prompt-injection defense, and human owner. Until then, the scheduled adversarial review is deterministic and model-independent.

### SEC-013 — Security debt has no silent grace period

- Status: **active**
- Enforcement: issue and release policy.

Critical and high findings stop development of unrelated release work until contained. Any leaked credential is revoked before code cleanup. Lower-severity findings still fail the gate and require a fix or constitutional decision; waivers cannot suppress advisories or destructive-operation risks.

### SEC-014 — Native interop keeps a closed safe surface

- Status: **active**
- Enforcement: repository conformance and negative proof.

Native Windows imports live only in Infrastructure.Windows and use source-generated marshalling. `AllowUnsafeBlocks` is enabled only for that project because the interop generator requires it; handwritten `unsafe` code and enabling unsafe blocks in any other project are prohibited. Interop failures are normalized at the provider boundary and never weaken identity, containment, or collision revalidation.

## Hosting controls required before first push

Enable branch rulesets requiring `quality`, `dependency-review`, and scheduled-security health; require review; block force pushes and deletion; enable dependency graph, Dependabot alerts and updates, CodeQL code scanning, secret scanning, and push protection. These server settings cannot be established until a remote repository and visibility/license tier exist.
