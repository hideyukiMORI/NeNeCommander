# Security Model

Status: normative

## Assets

The protected assets are user files, file metadata, credentials implicit in UNC access, WSL distribution data, settings, operation history, diagnostic logs, signing material, CI credentials, and the integrity of binaries and update inputs.

Navigating to a UNC location, whether typed, bookmarked, or taken from history, makes Windows authenticate to the named server with the current user's logon session (NTLM or Kerberos), as Explorer does. Explicitly opening a UNC file through the Shell (ADR-0065) uses that same implicit credential boundary; NeNe Commander adds no credential input, storage or retry. The Shell or associated application may show its own authentication or security UI. The UNC directory reader (ADR-0057) does not handle, prompt for, store, or log credentials; it passes the validated canonical path to the shared enumeration, and a logon or credential-conflict failure is a closed `AccessDenied` outcome with no retry or fallback (ADV-021). No path, server, or share text enters diagnostics (SEC-010).

## Trust boundaries

- All path text, filenames, metadata, settings files, clipboard content, drag/drop data, and persisted history are untrusted.
- Windows local, UNC, and each WSL distribution are distinct providers and distinct failure domains.
- Shell, Win32, Windows App SDK, NuGet, GitHub Actions, and design/review services are external boundaries.
- Presentation events and cancellation can arrive in hostile orders.
- Diagnostic output may leave the process and must not contain credentials, secret values, or unnecessary full paths.

Drive-letter syntax is not a local-storage trust boundary. Under ADR-0072, one Infrastructure
drive query supplies current OS facts to local mutation, transfer, verified-deletion and settings
owners. Network/unknown admission is refused, including an observed remap after preflight
(ADV-024). Directory reads and the one-shot Shell handoff keep the original drive-rooted path and
Windows logon-session authentication behavior; no credentials, mapping changes or UNC rewriting
are added. The pane's drive-kind report is descriptive, never permission to mutate.

### SEC-001 — Threats have executable mappings

- Status: **active**
- Enforcement: `eng/adversarial-cases.json`, security scan, and adversarial tests.

Every material threat has a stable ID, owner, expected defense, and test mapping. A security-relevant behavior change updates the threat model and its test in the same change.

### SEC-002 — Untrusted input is bounded before use

- Status: **active**
- Enforcement: parser, size-limit, containment, and malformed-input tests.

Inputs are length-bounded, parsed once, normalized without identity loss, and rejected when ambiguous. `FileSystemPath.Parse` bounds both raw input and the provider-normalized canonical result, so root separators and WSL alias expansion cannot produce an accepted path beyond the fixed boundary. Paths, distribution names, resource keys, settings values, and operation batches never enter shell or filesystem APIs as unchecked text. The Windows file launcher receives a typed `FileSystemPath` and admits non-root Windows-local, UNC and WSL paths (ADR-0065). UNC and WSL canonical paths must be shorter than 260 UTF-16 code units, and WSL names must additionally satisfy the canonical Windows segment rules (ADR-0064). It passes the original canonical text as one `ProcessStartInfo.FileName`, derives the working directory from its canonical parent, enables Shell association, and supplies no verb, arguments, or command interpreter. It rejects unsupported variants, roots, excessive UNC/WSL lengths and incompatible WSL names before scheduling, without translation or fallback. Launch-time network/session failures normalize to `ProviderUnavailable` and access/logon/credential conflicts to `AccessDenied`, with one attempt and no new authentication mechanism (ADV-019). WSL discovery uses fixed process argument tokens, bounds both redirected streams before publishing output, and validates every reported distribution through the canonical path parser; one malformed line rejects the whole snapshot. WSL directory reads and mutations route only from validated `WslPath` values, and UNC directory reads only from validated `WindowsUncPath` values. Each untrusted enumerated name passes through `FileSystemPath.Child` before publication, and mutation targets are canonical children produced by the existing request model.

The existing Windows device-name defense (ADV-015) includes the six `COM`/`LPT` superscript
forms documented by Windows, with the same case and extension handling as ASCII device names
(Issue #245). WSL parsing preserves those Linux names; the shared Windows-name predicate blocks
their Shell handoff (ADV-019), and Windows-destination child derivation rejects their transfer
before effects (ADV-022). This is a lexical admission rule, not filesystem identity or an OS probe.

### SEC-003 — Filesystem operations resist races

- Status: **active**
- Enforcement: failure-injection and race tests.

Preflight does not grant permanent trust. Adapters revalidate identity and containment at the side-effect boundary, treat links explicitly, use handles or provider identity where available, and report time-of-check/time-of-use changes without widening the target. Windows local identity combines the Win32 volume/file identifier obtained without following a reparse point with rewrite-sensitive metadata. Windows-side WSL identity (ADR-0049) combines the distribution name, the Linux inode read from a no-follow handle whose file system reports `9P`, kind, reparse tag, link count, length, last-write time, and kernel-assigned change time; the 9P namespace exposes no volume serial or 128-bit identifier, so the identity form is selected by the validated provider and never by a failed query. Entry identity defends against concurrent unprivileged replacement or rewrite. An actor who can mount inside a distribution (distribution root or the same user through FUSE) can place a different entry with an equal inode at the same path and is outside this defense, as is a clock rollback inside the distribution; hide accepted that scope on 2026-09-10. Query ambiguity or failure is closed. WSL mutations additionally revalidate the distribution and parent or destination boundary, refuse reparse points before tree copy and source deletion, preserve a failed copy's partial-target effect, and never fall back to a shell command. WSL-to-WSL transfers may cross distributions through that same adapter (ADR-0066), retain one source distribution per batch, reserve exact destination names before effects, and leave permanent source deletion solely to the gateway after successful verification (ADV-022). A user-requested Windows association handoff follows the separate path-activation contract in ADR-0046; it does not relax the filesystem snapshot, query, or mutation identity and containment requirements above. The launcher performs no existence, attribute, reparse, or identity preflight because such a probe cannot bind the later Shell resolution. It hands off the exact validated path once, reports only acceptance or a normalized rejection at that moment, and never widens, retries, or claims stable target identity.

Windows recycle (ADR-0063, ADV-023) also revalidates source identity, non-link ancestors and the source tree in the Shell pre-delete callback. Mandatory recycle-only flags and a callback requiring the recycle flag prohibit permanent fallback. Completed receipts survive later cancellation or failure in the canonical outcome. Shell still reopens the path after the last check; this is an explicit residual race, not handle-relative deletion. The live proof records source/root identities before effects and restores only its own returned receipt. Unknown or unlocated residue is retained; the user's recycle bin is never enumerated or purged by the proof.

ADR-0072 adds fresh drive admission at existing source/destination and recursive-child boundaries,
including settings capture/publication/cleanup and verified source deletion. It closes observed
network or unknown mapping changes without inventing SMB identity. A final query-to-path-reopen
race remains; this is not a handle-relative mapping lock. Prior effects retain their existing partial
outcomes, and unsafe cleanup retains residue instead of widening deletion.

Composite-move source deletion additionally consumes the exact descendant evidence returned by copy verification (ADR-0069). The shared verifier compares bytes and records provider identities, directory membership and SHA-256 content evidence. Source-provider deletion first validates the full evidence, then revalidates each known entry, its ancestors and its matching target at the effect boundary, and removes directories nonrecursively. A detected source or target change stops further deletion; already removed source descendants are explicitly reported. Every surviving directory still requires its expected full identity, including WSL change time. Immediately after a successful direct-child deletion, only that parent may be recaptured between two equal full observations after validating the original object component and exact remaining membership. No unrelated or pre-effect baseline refresh is permitted. A reused inode after this recapture therefore fails the full identity check. ADR-0069 explicitly accepts the narrow owned-mutation-to-immediate-recapture interval as well as retaining the old final-query-to-path-mutation interval and existing 9P mount/clock scope; an object component alone is not a lifetime identity. This is not a frozen tree or transactional guarantee.

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

### SEC-008 — Static security analysis is explicitly requested

- Status: **active**
- Enforcement: CodeQL manual workflow at implementation stage.

CodeQL uses the extended security-and-quality query suite in explicitly requested deep review for applicable security-integration or release proof (ADR-0061); it has no periodic or PR trigger. C# is analyzed without a build, and the canonical CodeQL configuration excludes only generated `obj` trees; it does not define a positive `paths` allowlist that could omit modified, untracked, or newly added owned source. Repository conformance protects the query suite, build mode, configuration reference, and exact generated-path exclusion with negative proofs. Results are uploaded as code-scanning findings. CodeQL findings are not converted into a baseline, and workflow success is not evidence that the open-alert count is zero; alert state is read back from the code-scanning API after analysis.

### SEC-009 — Deep review follows security and release impact

- Status: **active**
- Enforcement: explicitly dispatched deep-review workflow and report.

When justified by security-sensitive integration or release, explicitly request the canonical gate, secret/workflow scan, adversarial cases, dependency audit and mutation analysis through the single deep-review workflow. Its full canonical result is reusable; do not duplicate quality for unchanged inputs. There is no schedule. A release retains the requirement for a successful deep review no older than 96 hours.

### SEC-010 — Logs minimize sensitive data

- Status: **active**
- Enforcement: diagnostic contract tests.

Logs use typed event IDs and provider kinds. Credentials, environment values, file contents, command lines, UNC credentials, tokens, and full paths are prohibited. Path evidence is reduced to a stable salted diagnostic fingerprint and non-sensitive basename only when required.

### SEC-011 — Security failures fail closed

- Status: **active**
- Enforcement: command and provider tests.

Unknown capability, ambiguous identity, verification failure, stale state, malformed settings, unavailable security scanner, or incomplete destructive confirmation stops the affected operation. It never selects a permissive fallback. Settings writes preserve malformed input; reject reparse points observed in the Windows local ancestor chain and at the document itself; freeze the temporary file's Win32 identity while its owned handle is open; and revalidate the approved ancestor anchor, destination identity and exact content, plus temporary identity and exact bytes immediately before atomic publish. A later observation cannot replace the approved ancestor anchor. Each write selects one sibling temporary candidate through the injected identifier source (ADR-0070). Every observed entry kind at that candidate is a collision, without retry; its generated name does not establish ownership. Legacy fixed-name and other-attempt residue is neither read nor removed. Closed failures report directory-creation state and temporary residue separately. Distinct candidates no longer serialize different stores: a detected destination change rejects the stale writer, but two writers can pass their final comparisons before either publishes. ADR-0040 and ADR-0070 retain the residual reopen race of the path-based BCL directory, document, publish, and cleanup operations, without cross-process compare-and-swap or merge guarantees.

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

Native Windows imports live only in Infrastructure.Windows and use source-generated marshalling. `AllowUnsafeBlocks` is enabled only for that project because the interop generator requires it; handwritten `unsafe` code and enabling unsafe blocks in any other project are prohibited. Interop failures are normalized at the provider boundary and never weaken identity, containment, or collision revalidation. Each identity form keeps exactly one owner: the ADR-0049 `9P` file-system literal and both WSL handle-facts entry points exist only in `WindowsFileIdentifier`, only `WindowsWslFileSystem` reaches the 9P-guarded reader, and no other production file reaches the unguarded reader.

The ADR-0063 recycle boundary uses source-generated COM interfaces and unique wrappers. One awaited Shell operation owns initialization, callbacks, receipt lifetime, final release and COM uninitialization on one STA. Application sees only the existing typed port outcomes. No Shell object escapes that lifetime, and no elevation, global Shell worker or second delete command is added.

## Hosting controls required before first push

Under ADR-0061, retain required PRs, squash-only merging, resolved review threads, no bypass actors, and force-push/deletion protection. Remove unconditional required CI statuses so document-only PRs can merge without starting CI; the reviewer must inspect the evidence selected for the actual change. Retain dependency graph, Dependabot alerts/updates, CodeQL findings, secret scanning and push protection. Scheduled-security health is not a merge requirement because periodic execution is disabled.
