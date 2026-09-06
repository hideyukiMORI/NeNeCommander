# Filesystem Boundaries

Status: normative

Filesystem behavior is selected from a parsed provider boundary and an explicit capability snapshot. A raw path string never decides business policy.

## Canonical path model

`FileSystemPath.Parse` is the only entry from text. It returns one of these closed variants:

- `WindowsLocalPath`: drive-rooted or supported Windows device path.
- `WindowsUncPath`: server and share plus validated segments.
- `WslPath`: distribution identity plus an absolute Linux path.

`\\wsl.localhost\<Distro>\...` and legacy `\\wsl$\<Distro>\...` inputs both parse as `WslPath`. Display and persistence render the canonical `\\wsl.localhost\<Distro>\...` form. Internally, provider kind and segments are stored separately; string prefix checks outside the parser are prohibited.

`FileSystemPath.Parent` is the only way to derive the containing location. Each variant derives it from its own root and segments without re-parsing, preserves provider identity rules, and returns absence at the drive, share, or distribution root.

### FS-001 — Normalize without changing identity

- Status: **active**
- Enforcement: path property tests.

Parsing removes redundant separators and `.` segments and resolves `..` without crossing a root. It does not trim legal filename characters, invent a drive, case-fold Linux names, resolve links, touch the filesystem, or silently reinterpret a relative path.

All sets, duplicate checks, selection checks, and confirmation comparisons use `FileSystemPathIdentityComparer`. Windows local and UNC identity is case-insensitive. A WSL distribution name is case-insensitive, while its Linux path is case-sensitive. Direct record equality is not a filesystem identity decision.

For operation snapshots, Windows local and Windows-side WSL entries use the operating system's volume/file identifier plus kind, byte length, creation time, and last-write time. The identifier query opens the entry itself without following a reparse point. This snapshot detects both replacement and rewrite; it does not replace the mandatory effect-boundary revalidation.

### FS-002 — Capabilities are queried

- Status: **active**
- Enforcement: provider contract tests.

Recycle support, atomic move, replace behavior, case sensitivity, link support, timestamp precision, free-space reporting, and permission behavior are capabilities returned by the provider adapter. They are not fixed assumptions derived from a broad provider label.

### FS-003 — WSL discovery has one adapter

- Status: **active**
- Enforcement: dependency boundary and adapter tests.

`IWslDistributionCatalog` is implemented in Windows infrastructure and discovers registered distributions through one controlled `wsl.exe --list --quiet` invocation. Its argument tokens are fixed, shell execution is disabled, stdout and stderr are drained within a 64 KiB boundary each, cancellation terminates and awaits the owned process, and every non-empty line is parsed as a WSL root before the complete immutable snapshot is published. At most 256 reported lines are accepted and distribution duplicates use case-insensitive identity. Feature code never starts `wsl.exe`. Directory access and file mutations use the canonical `\\wsl.localhost` namespace through the WSL provider adapter; shell commands are not a second mutation path.

### FS-004 — Links are entries by default

- Status: **active**
- Enforcement: recursive-operation tests.

Recursive enumerate, copy, move fallback, and delete do not follow symbolic links, junctions, or reparse points by default. They operate on the link entry. A future follow-links feature requires an ADR, cycle detection, root containment proof, and new destructive tests.

### FS-005 — Cross-provider move is explicit

- Status: **active**
- Enforcement: gateway tests.

A same-provider move may use an atomic provider operation only when the capability says it is supported. Windows local capability uses the operating system's mounted-volume identity, never a drive-letter or path-prefix guess, and is revalidated at the side-effect boundary. An unsupported same-provider move and a cross-provider move use the single composite algorithm: preflight, copy, verify the declared metadata and byte count, then delete the source. Partial completion is reported item by item and never described as rollback-safe.

### FS-006 — Delete policy is capability-bound

- Status: **active**
- Enforcement: policy tests and confirmation tests.

When a provider explicitly supports recycle semantics, ordinary delete requests use recycle. Otherwise deletion is permanent and requires a confirmation that names the provider, permanence, and item count. UNC and WSL paths are never assumed to support a recycle bin. Bypassing confirmation is prohibited.

### FS-007 — Conflicts use one resolver

- Status: **active**
- Enforcement: conflict matrix tests.

Collision decisions are `Replace`, `Skip`, `KeepBoth`, or `Cancel`, restricted by provider capability. The conflict resolver produces a typed decision; adapters do not show UI or choose a default. Batch-wide choices are explicit and auditable.

### FS-008 — Paths cannot escape an operation root

- Status: **active**
- Enforcement: containment tests.

Temporary, test, and staged-operation roots are resolved before mutation. Every target is checked against the same provider-aware root. String-prefix containment is prohibited. A failed or ambiguous containment check stops the operation.

The settings document is accepted only as a `WindowsLocalPath`. Its store runs reads and writes through the existing Windows local I/O scheduler, walks from the document parent to the drive root with the existing Win32 volume/file identifier, rejects every reparse-point or non-directory ancestor it observes, and rejects the document itself when it is a reparse entry. It revalidates the captured chain before directory or sibling-temporary creation, before publishing, after publishing, and before cleanup. Only startup capture and a verified chain after owned directory creation may establish the ancestor baseline; later checks must match it. A rejected result reports directory creation as not attempted, observed, or unconfirmed separately from temporary residue. A detected identity or exact temporary-byte change stops the write. `Directory.CreateDirectory`, temporary `CreateNew`, `Move`, `File.Replace`, and cleanup `File.Delete` remain path-based BCL operations with a final reopen race after the preceding check; ADR-0040 does not claim handle-relative traversal or reject ordinary hard links as reparse traversal.

### FS-009 — Metadata preservation is truthful

- Status: **active**
- Enforcement: provider matrix tests.

Operations preserve only metadata promised by the source/destination capability intersection. Unsupported ownership, mode, ACL, alternate stream, link, or timestamp fidelity is reported; it is not silently claimed or emulated with a second path.

### FS-010 — Availability failure is a normal outcome

- Status: **active**
- Enforcement: adapter and command tests.

Disconnected shares, stopped WSL distributions, removed drives, permission changes, and vanished files produce typed outcomes and a refreshable pane state. They do not crash the UI or cause automatic fallback to another location.

### FS-011 — Directory reads are bounded and fail closed

- Status: **active**
- Enforcement: `IDirectoryReadPort` adapter contract tests and listing tests.

A read returns the direct entries of one validated location through `IDirectoryReadPort` and never recurses or follows links. One Infrastructure.Windows provider router delegates validated `WindowsLocalPath` and `WslPath` requests to their adapters; both use one shared direct-enumeration operation and the existing I/O execution boundary. Unsupported `WindowsUncPath` remains `ProviderUnavailable`. The adapter stops at the request's entry boundary and reports a bounded listing, observes cancellation before enumeration and before each entry, and reports denied, missing, or non-directory locations as typed failures instead of an empty listing. Windows hidden/system attributes and WSL dot-prefixed names are reported as provider facts; all entries remain in the listing and visibility is a later pane transition. Every name is derived with `FileSystemPath.Child`; a rejected name is counted as unrepresentable, not shown and not silently dropped. Ordering is decided by `DirectoryListing`, never by provider enumeration order.

### FS-012 — WSL mutations are provider-local and fail closed

- Status: **active**
- Enforcement: provider-router, adapter, gateway, identity-race, link, and collision tests.

One `ProviderFileOperationPort` routes validated paths to provider-owned adapters behind `FileOperationGateway`. The WSL adapter permits directory creation only as a direct child of the revalidated location, rename only within the same parent and distribution, and deletion only in confirmed permanent mode. Within one distribution it also implements complete-batch transfer preflight, copy, and verification; atomic move remains unsupported, so the gateway alone composes move as copy, verify, then permanent source deletion. It revalidates the Windows file identifier and rewrite-sensitive metadata immediately before every side effect, refuses every source tree or destination containing a reparse point, and treats an existing target as `Conflict`. Cross-distribution and cross-provider transfer, overwrite, atomic WSL move, and recycle remain unavailable until their respective capability and product policies are accepted; the adapter does not invent a fallback or remove a partial target.

## Test safety

Live filesystem integration is split from deterministic provider contract tests. Live WSL mutation tests are opt-in and require `NENE_COMMANDER_WSL_TEST_ROOT` to identify a dedicated empty test-owned directory. The harness rejects a distribution root, `/home`, a user home, `/mnt`, the repository, or any ancestor of them. Cleanup verifies the resolved target before deleting it.
