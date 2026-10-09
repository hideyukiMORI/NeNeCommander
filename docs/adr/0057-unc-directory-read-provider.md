# ADR-0057: Read UNC share directories through the shared enumeration operation

Status: accepted

Date: 2026-10-09

## Context

The charter's mission names UNC shares beside local paths and WSL distributions. The Domain parses
`\\server\share\...` as `WindowsUncPath` (ADR-0042 bounds it, FS-001 gives it case-insensitive
identity), addresses, bookmarks, and history already accept it, and the pane reaches
`ProviderDirectoryReadPort`, which returns `ProviderUnavailable` for anything that is not
`WindowsLocalPath` or `WslPath`. ADR-0010 and ADR-0035 reserved the UNC adapter as a third
participant in that one router, with no second read path and no second listing type. The two
existing readers differ only in their type guard and their visibility rule; everything else is the
shared `WindowsDirectoryReadOperation`, `WindowsDirectoryEnumerator`, and the ADR-0027 execution
boundary. A 2026-10-09 read-only survey (Issue #183) confirmed this and found three facts the
decision must name: Windows sends the current user's credentials to any server a path names; an
unreachable host blocks the first enumeration call until the SMB client times out, during which the
pane and the other scopes stay frozen as they do for any read; and the failure normalizer already
maps `ERROR_BAD_NETPATH` and `ERROR_BAD_NET_NAME` to `ProviderUnavailable` but has no rows for the
other network and logon errors.

## Decision

- **One more reader in the same router.** `WindowsUncDirectoryReader` in Infrastructure.Windows
  guards on `WindowsUncPath`, classifies visibility by the Hidden and System attributes exactly as
  the local reader does, and runs the shared read operation and enumerator through the existing
  execution boundary. That attribute rule moves from the local reader into the shared operation as
  `ClassifyByAttributes`, so the local and UNC readers pass the same function and no second copy
  exists. `ProviderDirectoryReadPort` gains the third branch, and its public constructor composes
  the UNC reader beside the other two, so the App composition root is unchanged. No listing type,
  read port, enumerator, or normalizer is duplicated; the WSL reader is unchanged and the local
  reader changes only by referring to the shared attribute rule.
- **Reading only.** Copy, move, delete, rename, directory creation, launch, entry identity,
  recycle, and share enumeration in the Locations picker stay `ProviderUnavailable` for UNC
  locations. Each later capability needs its own ADR because SMB servers differ in file identifiers,
  timestamps, and ACL semantics (FS-002, ADR-0049).
- **Closed failures, no fallback.** `WindowsFileFailureNormalizer` maps `ERROR_LOGON_FAILURE`
  (1326) and `ERROR_SESSION_CREDENTIAL_CONFLICT` (1219) to `AccessDenied`, and
  `ERROR_NETWORK_UNREACHABLE` (1231), `ERROR_HOST_UNREACHABLE` (1232), `ERROR_SEM_TIMEOUT` (121),
  `ERROR_NETNAME_DELETED` (64), and `ERROR_UNEXP_NET_ERR` (59) to `ProviderUnavailable`, beside the
  existing 53 and 67. A failed read is the pane's existing typed failure state, refreshable under
  FS-010; the reader never retries, never rewrites the path to another provider, and never prompts.
- **Blocking is declared, not hidden.** The first enumeration call on an unreachable host blocks
  until the operating system's SMB timeout. Cancellation is observed only before enumeration and
  between entries, as ADR-0027 already states, so the pane and the scopes that freeze during a
  read stay frozen for that long. This decision accepts that for the read provider and assigns the
  user-visible remedy, cancelling a loading pane, to Issue #184 as a separate Application change.
- **Implicit credentials are a named asset.** Navigating to a typed or bookmarked UNC path makes
  Windows authenticate to that server with the current user's identity, the same as Explorer. The
  security model records this under its existing UNC-credential asset; the reader adds no
  credential handling, prompt, or storage, and no path, server, or share text enters diagnostics
  (SEC-010). A new adversarial case, ADV-021, states that unreachable and unauthenticated UNC reads
  fail closed with no fallback, and ADV-011 and ADV-017 name UNC beside local and WSL.
- **Deterministic proof in the gate, live proof later.** Gate tests inject the enumerator seam, as
  the local and WSL readers do, and prove the guard, the router branch, visibility, the entry
  boundary, cancellation points, unrepresentable names, and the HRESULT table. No gate test touches
  the network (TEST_STRATEGY). A live UNC tier in the ADR-0043 form, with an operator-provided
  dedicated share root, is a separate Issue; until it runs, the quality documents say that live UNC
  proof has not run. Parsing is unchanged: administrative shares such as `C$` remain accepted by
  the existing component rules, and server aliases (`localhost`, `127.0.0.1`, FQDN) remain distinct
  identities, which this ADR records rather than resolves.

## Rejected alternatives

- Widening the local reader's guard to accept UNC: it would hide the provider difference that FS-002
  and the trust boundaries require to stay visible.
- A UNC-specific listing type or read path: ADR-0010 forbids a second one.
- New failure kinds such as `CredentialsRequired` or `Unreachable`: the pane cannot act on them
  differently yet; `AccessDenied` and `ProviderUnavailable` already carry the distinction the user
  needs, and the vocabulary can grow when a credential flow exists.
- Timeouts or `LongRunning` workers inside the execution boundary: that is an ADR-0027 revision with
  its own abandonment semantics and belongs with Issue #184.
- Shipping mutation or launch for UNC in the same change: each depends on server identity and
  deletion semantics that are not known in general.

## Consequences

- Address, bookmark, and history navigation to a share now lists it; every other operation on it
  stays refused with a typed reason.
- A typo in a server name can freeze the pane for the SMB timeout; Issue #184 owns the remedy.
- The security deep review runs at integration readiness because a new external I/O boundary opens
  and an adversarial case changes.
- FS-011 is reworded to "UNC reads are supported; UNC mutation, launch, and identity remain
  `ProviderUnavailable`", and the `WindowsLocalPath` definition drops "supported Windows device
  path", which the parser already rejects.

## Migration and removal

No stored data changes. Removing the reader removes the router branch, the normalizer rows, the
tests, ADV-021, and the document sentences together.

## Executable proof

Infrastructure tests with the injected enumerator prove the guard, the router's three-way split,
attribute-based visibility, bounded listings, cancellation before and between entries, unrepresentable
name counting, and every HRESULT row with its `ThreatId`. The existing router test that expected
`ProviderUnavailable` for UNC is rewritten to distinguish UNC from WSL; the local reader's own guard
test stays. Domain table tests fix the accepted administrative share names and the distinct
identity of server aliases. Focused suites,
coverage, Infrastructure.Windows mutation, the commit gate, the canonical Ready gate, and the
security deep review at integration readiness prove their tiers; live UNC proof is recorded as not
run.
