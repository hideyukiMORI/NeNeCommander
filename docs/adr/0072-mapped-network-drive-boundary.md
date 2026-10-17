# ADR-0072: Keep mapped network drives outside local mutation admission

Status: accepted

Date: 2026-10-17

Issue: #181

## Context

`WindowsLocalPath` describes drive-rooted syntax, not an observed storage capability. A mapped
network drive therefore reaches the local mutation adapter and local-to-WSL transfer today.
Locations already displays `DriveKind.Network`. Recycle already requires a fixed drive under
ADR-0063, and atomic move already queries mounted-volume identity under ADR-0032. Those defenses
must not be described as missing. The remaining gap is explicit admission to local identity and
effects, including a drive remapped after preflight, and the missing network fact in the pane.

The [Windows drive-type API](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getdrivetypew)
distinguishes remote, unknown and missing roots from local media. The existing
[DriveInfo.DriveType property](https://learn.microsoft.com/en-us/dotnet/api/system.io.driveinfo.drivetype?view=net-10.0)
exposes those OS facts. The
[FILE_ID_INFO contract](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info)
describes file/volume identity on one computer; it does not establish a mapped server's identity
across reassignment. The
[volume GUID query](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumenameforvolumemountpointw)
does not support SMB volume management. A successful local-shaped query is therefore insufficient
evidence to admit a network mutation.

hide resumed work on 2026-10-17 and delegated design decisions to NeNe Commanderサナ. This decision
supersedes the original Issue #181 proposal to permit confirmed permanent network deletion in
this change. UNC mutation and identity remain unsupported under ADR-0057. This change closes the
drive-letter bypass to that restriction; it does not implement a second network mutation provider.

## Decision

- **One OS fact boundary.** An internal `WindowsDriveBoundary` in Infrastructure.Windows owns the
  current drive-type query and local admission. Its default reads `DriveInfo.DriveType` for the
  validated drive root. A constructor-injected `Func<WindowsLocalPath, DriveType>` is the deterministic
  test seam; there is no mutable static hook, cache, interface solely for tests, new scheduler, or
  dependency. Queries execute inside the existing Windows I/O worker. The parser, canonical text,
  bookmarks and history keep the original drive letter; no UNC translation or alias inference occurs.
- **A local classification is necessary, not sufficient.** Network, Unknown, NoRootDirectory and
  unrecognized drive types cannot enter local file identity or effects. Fixed, Removable, CDRom and
  Ram establish only a non-network drive observation; all existing per-operation capabilities,
  read-only media failures, link checks, identities and containment checks still apply. In particular,
  recycle remains Fixed-only and the WSL-to-Windows destination retains its narrower Fixed/Removable
  admission. A drive type never grants NTFS semantics or atomicity by itself.
- **Every existing owner enforces the same admission.** Provider composition shares the boundary
  with the local adapter, both cross-provider transfers, Windows-side WSL filesystem and verified
  tree access. Local inspection, source/destination preflight, capability queries and effects check
  the relevant typed paths. Recursive copy checks local source and destination paths at each child;
  verification and evidence-bearing source deletion repeat the check through `VerifiedTreeAccess`.
  Direct adapter calls cannot bypass admission by avoiding the router. Existing callback and
  revalidation points remain the effect boundaries. Expected denial is `ProviderUnavailable`, with
  already completed effects preserved by the existing partial-effect outcomes. No retry or fallback
  to another provider, permanent deletion or source cleanup is added.
  `VerifiedTreeAccess` keeps its existing raw filesystem traversal contract inside Infrastructure;
  a dedicated internal `IOException` subtype stops a refused reopen. The verification and verified
  deletion owners catch it before their general I/O handler and return the canonical
  `ProviderUnavailable` outcome, retaining any recorded partial deletion. This is not an exception
  result across a port or a throw-to-probe classifier; classification uses the drive query directly.
  No new feature-local result abstraction or outward exception contract is introduced.
- **Settings retain their separate owner.** `WindowsLocalSettingsStore` uses the same drive
  boundary while capturing and matching its location and at its existing pre-effect and cleanup
  revalidations. A network or unknown location is refused through the existing settings failure
  vocabulary. Unsafe cleanup leaves the existing residue report; it never deletes through a newly
  mapped root. No settings schema, candidate naming, identifier generator or publication mechanism
  changes. The Win32 file-identifier implementation itself is unchanged.
- **Read and Shell paths stay usable.** The local reader queries the current drive type inside
  its worker and continues through the existing bounded Windows enumeration. This fact is not
  obtained from a cached Locations row, and a disconnected drive does not become local merely
  because its label/readiness is unavailable. Network failures still use the existing normalizer,
  cancellation and Escape-abandon contracts. The existing one-shot Shell handoff retains the
  original path and makes no target-identity promise. No live share is contacted by the tests.
- **The pane shows an observed fact.** `DirectoryReadSucceeded` carries an optional `DriveKind`
  beside its existing listing. Presence is valid only for a drive-rooted listing; absence means
  no drive-kind report, as for UNC and WSL. It is display evidence, never mutation authorization.
  `PaneContentListed` retains it across focus, selection, sorting and failed navigation, and a new
  successful read replaces it. `PanePresentation` projects a closed network location indication.
  `PaneStatusLine` joins that indication with the existing status and sort text using the existing
  separator and localized words. Bounded/omitted warnings and activity status remain visible.
  The App only forwards the extra presentation value. No layout, styling or new input route changes.
- **Observed races fail closed; the last reopen race remains.** Classification is fresh at each
  admission and effect boundary, so an observed local-to-network or local-to-unknown reassignment
  stops the operation. It is not a stable binding to a drive mapping. Reassignment after the last
  query but before a path-based OS reopen remains the existing residual race; this ADR does not
  claim handle-relative operations or protection against privileged mount manipulation. Read facts
  describe the observation associated with the read, not future mapping identity. An OS-owned
  recursive effect, such as permanent directory deletion or Shell recycling, has a final admission
  before that call; its internal child operations are not independently controlled by this adapter.

## Alternatives rejected

- Enabling network permanent deletion using the local `windows-v2` identity: server identity and
  mapping lifetime have no accepted capability contract or live proof.
- Checking only the router or only the Locations cache: direct adapters, destinations, later
  callbacks and remapping would bypass the observation.
- Classifying a path as local from its drive-letter text, an absent network label or a failed
  remote query: unknown is not local and must not create a permissive fallback.
- Converting a mapped path into a UNC path during navigation: it would change visible/persisted
  identity and would require a separate mapping-resolution and race contract.
- Using a new listing type, mutation gateway, scheduler or network credential mechanism: the
  existing owners already provide the canonical paths and failure vocabulary.

## Consequences and migration

Mapped network browsing and file activation remain available, and the pane identifies the drive
as network when the reader reports it. File mutation/inspection on that drive is now explicitly
unavailable. Supporting network mutation later requires a separate accepted identity/capability
ADR and environmental proof, including mapping changes; permanent confirmation alone is insufficient.

The read-success factory gains the explicit optional-kind argument, and every production and test
caller migrates atomically. No compatibility overload or second construction path remains. Existing
Locations labels are reused conceptually but remain separate from current per-read facts. No settings
or persistence migration is required. Removing this boundary would require replacing all consumers
and proof together, not bypassing one callsite.

## Executable proof and verification scope

Deterministic tests inject OS facts, preserve failing behavioral evidence before each fix, and prove
network/unknown rejection without effects, each source/destination direction, directly invoked adapters,
recursive children, remapping between preflight and effect, evidence-bearing deletion, settings
publication/cleanup safety, cancellation and partial effects. Read/pane tests prove current network
facts, state preservation/replacement, existing bounded/omitted status, both panes and localization.
If a new seam is needed to make an old gap executable without a live share, the red record identifies
that non-behavioral seam and retains the exact red source, patch, command, hashes and result.

ADR-0061 selects affected local tests/build/format while implementing, followed by one explicitly
requested security deep review at the stable candidate. The boundary affects all local mutations,
cross-provider transfer, settings and shared read outcomes; isolated classifier tests cannot establish
that wider security impact. Deep review includes the canonical gate, coverage, adversarial repetitions,
mutation and CodeQL; a second quality run is unnecessary. Successful evidence is reused while its
relevant inputs remain unchanged. A new adversarial mapping records observed drive reassignment and
network admission. Live mapped-share, removable-media and #94 release UI proof remain unexecuted
environmental tiers; no drive mapping, credentials or machine settings are changed for this task.
