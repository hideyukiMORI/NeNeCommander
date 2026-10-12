# ADR-0060: Address WSL entries through extended-length Win32 paths

Status: accepted

Date: 2026-10-12

Accepted for Issue #190 on 2026-10-12 by the NeNe Commander design owner under the cadence hide set
on 2026-10-09; the defect was confirmed by a read-only live diagnosis on hide's machine with writes
only under the dedicated test root of ADR-0043.

## Context

Every WSL provider component reaches a distribution through the Windows file API with the UNC text
of a `WslPath` (`\\wsl.localhost\<distribution>\...`): `WindowsWslFileSystem` for existence,
inspection, creation, and tree copy targets; `WslDirectoryReader` through
`WindowsDirectoryEnumerator` for listings; `WindowsFileIdentifier.ReadWslFacts` for the 9P identity
of ADR-0049; and `WindowsToWslTransfer` for the targets of ADR-0059. The Win32 path normalizer
rewrites that ordinary text before the request reaches the 9P server: a segment that ends with `.`
or a space loses that ending, and `Path.GetFullPath` applies the same rule to the text that
`WindowsLocalTreeCopy.ResolveDirectChild` derives for every child inside a copied tree.

The WSL segment rules of the path model accept every name a Linux filesystem accepts (ADR-0004,
ADR-0015), so `foo.` is a valid `WslPath` segment and a valid entry name in a listing. The live
diagnosis of Issue #190 showed what happens next on this machine (.NET 10, WSL 3.0.1): the entry
`foo.` is enumerated with its dot, `File.Exists` on the ordinary text answers false, a copy to the
ordinary text `...\written.` silently creates `written`, and verification through the same
normalizer compares the renamed target with itself and reports success. Collision detection asks
`TargetExists` with the same text and never sees an existing `foo.`. The same text with the
extended-length prefix (`\\?\UNC\wsl.localhost\...`) preserves the dot for existence, reading, and
writing, because Win32 performs no normalization on that form. In the diagnostic's `a:b` case,
9P exposed the colon as a private-use substitute (`U+F03A`) that round-tripped through the
ordinary API. That observation does not establish a policy for every substitute name or for
transferring such names to Windows.

This is a silent rename of user data on a path the gateway has already verified, which violates
the charter's first priority and the unrepresentable-segment defense of ADV-015. One mechanism
must decide how WSL path text reaches Win32.

## Decision

- **One resolver produces every Win32 path string for a WSL path, and it produces the
  extended-length form.** `WslPath` gains no new text property for Win32 use; the Infrastructure
  layer owns one internal resolution (`WslWin32PathText` or an equivalent single function) that
  maps a `WslPath` to `\\?\UNC\wsl.localhost\<distribution>\<segments>` and nothing else. The
  existing seam `WindowsWslFileSystem(Func<WslPath, string> resolvePath)` adopts that resolver as
  its default; the WSL directory read, the 9P identity query, and the cross transfer of ADR-0059
  obtain their strings from the same resolver. No WSL path text reaches a Win32 API, `FileInfo`,
  `DirectoryInfo`, `File`, `Directory`, or `CreateFileW` in its ordinary UNC form.
- **Tree children keep their exact names.** `WindowsLocalTreeCopy.ResolveDirectChild` is unchanged:
  `Path.GetFullPath` leaves an extended-length path unnormalized, so a child named `foo.` under a
  prefixed parent resolves to `...\foo.`. The rejection of path syntax inside a child name (empty,
  rooted, separators, `.` and `..`) remains the function's responsibility for both providers.
- **The path model and the user-facing text are unchanged.** `FileSystemPath.Parse` keeps rejecting
  `\\?\` and `\\.\` input (`IsDeviceNamespace`), `CanonicalText` keeps the ordinary
  `\\wsl.localhost\...` form for display, settings, bookmarks, address input, history, identity
  comparison, and containment. The prefix is an Infrastructure fact that never leaves the layer:
  entries produced from a prefixed enumeration are projected back onto their `WslPath` by name, and
  every snapshot, outcome, and effect carries `WslPath` values, not strings.
- **Windows local and UNC transport text is unchanged.** Their typed path segment rules reject a
  trailing dot or space (`IsValidSegment`). This does not prove that every descendant of a local
  tree is representable by that path model: NTFS can contain such names through extended-length
  APIs. The future WSL-to-Windows stage must validate all destination children before and during
  copy and verification, and refuse unsupported names with a typed failure; this resolver does
  not supply that reverse-transfer admission.
- **Reads and writes agree.** Because the listing, the existence check, the collision check, the
  copy, and the verification all use the resolver, a name that a listing shows is the name a copy
  creates and a verification compares; there is no second resolution path that could disagree.

## Rejected alternatives

- Refusing WSL target segments that end with `.` or a space before any effect: it would be fail
  closed, but it rejects names the destination filesystem supports, and it leaves the read side
  unable to open or identify entries that its own listing shows. The defect is in the transport
  text, not in the name.
- Adding the prefix only inside `ResolveDirectChild` or only inside `WindowsWslFileSystem`: two
  resolutions would exist, and the component left on the ordinary form (listing, identity, or
  collision check) would still disagree with the others on the same entry.
- Giving `WslPath` a public extended-length text: Domain would carry a Win32 transport detail, and
  the ordinary form would remain reachable by every caller, so the single mechanism could not be
  enforced by the dependency law.
- Translating `U+F03A` and the other 9P substitutes back to their Linux characters: a separate
  name-mapping decision. This repair preserves the API-visible names; reverse-transfer policy
  for those names requires its own design and proof.

## Consequences

- A WSL entry named `foo.`, `bar. `, or `sub.` is listed, opened, identified, copied, moved, and
  verified under its exact name; the silent rename of Issue #190 cannot occur on this provider.
- ADR-0059 obtains WSL target text from the same resolver. This repair makes no broader promise
  that arbitrary Windows local trees contain only names representable by the local path model.
- Error normalization is unchanged: the Win32 status codes behind `IOException` are the same for
  the extended-length form, and the existing normalizer rows of ADR-0035 and ADR-0057 keep their
  meaning. Any status observed to differ in the live tier is added as a row, not special-cased.
- The 9P substitute characters remain visible in the pane exactly as the Windows API reports them;
  whether to render `U+F03A` as `:` is a follow-up for hide.
- The live WSL tier declares one new cell that creates a trailing-dot tree inside the owned root
  through the resolver, copies it within the distribution, and proves that the listing, the
  target, and the verification keep the names. The live tier on this machine needs the
  distribution kept alive for the whole run (handoff of 2026-10-12).
- The security deep review runs at integration readiness because the change touches every WSL
  read and write path and the identity query.

## Migration and removal

`WindowsWslFileSystem`, `WslDirectoryReader` (through the enumerator it is given),
`WindowsFileIdentifier.ReadWslFacts` call sites, and `WindowsToWslTransfer` switch to the resolver
in one change; the parameterless constructor of `WindowsWslFileSystem` stops passing
`CanonicalText`. Each provider reader passes one deferred enumeration to the shared
`WindowsDirectoryReadOperation.Read` (the canonical text for Windows local and UNC, the resolver's
text for WSL). The shared operation invokes it only after observing cancellation and within its
failure-normalization boundary; its four parameters remain within CS-013. The identity query receives
the full name of the entry opened through the resolver's text, which keeps the prefix, and `WindowsToWslTransfer` reaches
every target through the `WindowsWslFileSystem` it is composed with, so neither needs a second
call to the resolver. Tests that build expected Win32 strings from `CanonicalText` are rewritten to
expect the resolver's output. ADV-015 gains the sentence that Infrastructure addresses WSL entries
through extended-length text so that no segment is renamed by Win32 normalization; FS-001 and FS-012
state the same fact for WSL transport. This decision is removed
only by an ADR that gives the WSL provider a transport other than the Windows file API.

## Executable proof

`WindowsWslFileSystem` tests on the owned NTFS root create entries through `\\?\`-prefixed paths
(NTFS accepts a trailing dot through that form) and prove that `Find`, `TargetExists`, inspection,
creation, and the tree copy keep `foo.`, `sub.`, and `sub.\inner` exactly, and that the ordinary
form of the same names is never passed to the file API (a resolver test asserts the exact string
for a representative `WslPath`, including a distribution name and nested segments). Directory read
tests prove that a listing containing `foo.` projects the entry name unchanged onto its `WslPath`.
Gateway tests on the owned root prove a same-distribution copy and composite move of a tree that
contains trailing-dot names with exact-name verification and with the source left intact on
failure, and that a collision with an existing `foo.` is reported as `Conflict` with zero effects.
Identity tests prove `ReadWslFacts` receives the prefixed text. ADV-015 cases cover the silent
rename. The live WSL tier declares the new cell; focused suites, coverage, Infrastructure.Windows
mutation, the commit gate, the canonical Ready gate, and the security deep review at integration
readiness prove their tiers.
