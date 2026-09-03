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

### FS-002 — Capabilities are queried

- Status: **active**
- Enforcement: provider contract tests.

Recycle support, atomic move, replace behavior, case sensitivity, link support, timestamp precision, free-space reporting, and permission behavior are capabilities returned by the provider adapter. They are not fixed assumptions derived from a broad provider label.

### FS-003 — WSL discovery has one adapter

- Status: **active**
- Enforcement: dependency boundary and adapter tests.

`IWslDistributionCatalog` is implemented in Windows infrastructure and discovers registered distributions through one controlled `wsl.exe --list --quiet` invocation. Feature code never starts `wsl.exe`. Directory access and file mutations use the canonical `\\wsl.localhost` namespace through the WSL provider adapter; shell commands are not a second mutation path.

### FS-004 — Links are entries by default

- Status: **active**
- Enforcement: recursive-operation tests.

Recursive enumerate, copy, move fallback, and delete do not follow symbolic links, junctions, or reparse points by default. They operate on the link entry. A future follow-links feature requires an ADR, cycle detection, root containment proof, and new destructive tests.

### FS-005 — Cross-provider move is explicit

- Status: **active**
- Enforcement: gateway tests.

A same-provider move may use an atomic provider operation only when the capability says it is supported. A cross-provider move is the single composite algorithm: preflight, copy, verify the declared metadata and byte count, then delete the source. Partial completion is reported item by item and never described as rollback-safe.

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

A read returns the direct entries of one validated location through `IDirectoryReadPort` and never recurses or follows links. The adapter stops at the request's entry boundary and reports a bounded listing, observes cancellation before enumeration and before each entry, and reports denied, missing, or non-directory locations as typed failures instead of an empty listing. Hidden and system entries are reported; visibility is a later pane transition. An entry whose name the path model rejects is counted as unrepresentable, not shown and not silently dropped. Ordering is decided by `DirectoryListing`, never by provider enumeration order.

## Test safety

Live filesystem integration is split from deterministic provider contract tests. Live WSL mutation tests are opt-in and require `NENE_COMMANDER_WSL_TEST_ROOT` to identify a dedicated empty test-owned directory. The harness rejects a distribution root, `/home`, a user home, `/mnt`, the repository, or any ancestor of them. Cleanup verifies the resolved target before deleting it.
