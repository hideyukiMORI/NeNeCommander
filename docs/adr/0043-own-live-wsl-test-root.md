# ADR-0043: Own live WSL proof through one dedicated test root

Status: accepted

Date: 2026-09-07

## Context

ADR-0011 permits deterministic Windows integration tests to use `System.IO` only through
`TestOwnedTemporaryRoot`, and explicitly reserves live WSL roots for a later decision. FS-012,
QLT-009, TST-011, and Issue #93 require real same-distribution copy and composite-move proof,
but the repository has no executable harness that can safely own or clean a live WSL fixture.
Treating a configured path as sufficient ownership could recursively delete a replaced root,
foreign residue, a link target, a home, a mounted Windows tree, or repository content.

## Decision

Add one opt-in `LiveWslTestRoot` owner inside
`NeNeCommander.Infrastructure.Windows.Tests`. It accepts only an existing, empty, non-link
direct child of `/tmp` whose name begins with `NeNeCommander-Live-` in a distribution returned
by the canonical `WslDistributionCatalog`. The dedicated launcher reads
`NENE_COMMANDER_WSL_TEST_ROOT`, places its value in an ephemeral runsettings test parameter, and
removes that file on exit. The C# fixture reads the parameter through MSTest `TestContext` and
gains no direct environment exception. An unset value reports the live tier as unexecuted;
invalid, unsafe, unavailable, nonempty, or ambiguous input is a closed test failure.

The launcher reuses the exact Windows identity implementation by loading only the current Release
`NeNeCommander.Infrastructure.Windows.dll`, requesting the fixed
`NeNeCommander.Infrastructure.Windows.FileOperations.WindowsFileIdentifier` type, and invoking
its fixed nonpublic static `Describe(string)` member. It does not scan assemblies, search for a
fallback type or member, accept a user-selected member, add a second identity query, or broaden
reflection permission in production or test C#. A missing artifact, load failure, or absent fixed
member fails closed. Superseded within this ADR by the ADR-0049 integration below.

Before checking home, canonical path, and mount facts, the launcher captures the stable Windows
file identifier of `/tmp` and the configured root. It repeats both observations after those
queries and passes the original values plus closed accepted-fact tokens through the ephemeral
runsettings. The C# owner requires every value, captures the distribution root, `/tmp`, and the
configured root, and compares the latter two identities immediately before its first mutation.
It then captures a newly created direct run child and create-new ownership marker. Before each
product mutation and before cleanup it repeats provider-aware containment, non-link, stable-
identity, and exact-owned-entry checks. The existence of the run child alone never establishes
ownership. Superseded within this ADR by the ADR-0049 integration below.

Fixtures and product-created targets are registered with their stable identities. Cleanup first
proves that every observed entry belongs to the run, refuses any missing, replaced, or foreign
entry, then unlinks each intentionally created link entry without following it. It revalidates
the child after unlinking and only then recursively removes the run child. The configured root is
retained and must be empty afterward. A cleanup refusal leaves all remaining evidence in place and
reports a closed reason.

The `/tmp` shape and prefix narrow accidental input; they do not by themselves prove that a
nonstandard home, repository, or bind mount is absent. The operator must create a new dedicated
root after checking the selected distribution's actual home and mount facts. The release report
records those checks without exposing user, distribution, or root text. The harness then captures
and revalidates Windows-side identities, but retains the same path-reopen interval as the product
adapter; it does not claim handle-relative traversal or race freedom.

The first live attempt on 2026-09-07 failed closed before fixture or product mutation because the
existing `GetFileInformationByHandleEx(FileIdInfo)` identity query returned
`ERROR_NOT_SUPPORTED` for the actual WSL UNC directory. A read-only legacy
`GetFileInformationByHandle` probe distinguished the sampled entries but returned a zero volume
serial. Combining that result with a distribution name would not exclude inode collisions across
mounts or filesystems inside one distribution, so ADR-0033 and ADR-0036 are not weakened with a
legacy fallback. Issue #105 owns a future identity decision. Until that decision supplies an
equivalent provider/volume and entry identity, the live assertions remain unexecuted and this ADR
does not establish release proof.

The live transfer fixture constructs the production `ProviderFileOperationPort` and executes
copy and move through `FileOperationGateway`. Setup and evidence reads use the test-root owner;
they are not product mutation paths. A link fixture targets a sentinel inside the same run child.
The gateway must reject that transfer with zero effects and leave the source, target, link, and
sentinel unchanged.

`eng/run-live-wsl-tests.ps1` remains a thin launcher for the existing MSTest/Microsoft.Testing.Platform
project and exact `TestCategory=LiveWsl` filter. It does not join `eng/check.ps1`, change a threshold,
or create a second runner. Its process-scoped environment is restored on exit, and its report
redacts the configured root and distribution name. Any skipped or unexecuted required cell keeps
Issue #93 and release readiness open.

## Rejected alternatives

- Reuse `TestOwnedTemporaryRoot`: it maps WSL paths into NTFS and cannot prove a registered
  distribution, WSL namespace behavior, or live cleanup safety.
- Let each test create and delete arbitrary paths: this removes the sole ownership boundary and
  permits inconsistent cleanup.
- Accept any empty WSL directory: emptiness is not ownership and does not exclude homes,
  repositories, mount trees, or an attacker-controlled replacement.
- Invoke `cp`, `mv`, `rm`, or link cleanup through `wsl.exe`: this creates a second mutation engine
  and bypasses the product adapter and Windows-side link checks.
- Add the live filter to the canonical gate: a machine without an explicit root would turn an
  environmental skip into misleading merge evidence.

## Consequences

- Operators must explicitly prepare one retained `/tmp/NeNeCommander-Live-*` root and scope the
  environment value to the live invocation.
- The live harness is deliberately stricter than production path support. Other live root shapes
  require a new safety decision rather than a broader exception.
- A failed cleanup may leave the run child for diagnosis. The harness never deletes it merely to
  make a later run pass.
- C# direct environment access remains prohibited outside the existing settings-location adapter.
- A newly prepared root plus the recorded environment checks establishes the operator-owned test
  input for this release run. The prefix alone is never reported as ownership proof.
- Canonical, deep-review, and live evidence remain separate tiers.
- Identity and link checks reduce replacement risk but retain a path-based reopen race between the
  final check and each setup or cleanup operation.

## Migration and removal

There is no previous live harness to remove. ADR-0011 continues to own deterministic Windows
temporary roots; this decision adds the one separately named live WSL owner it reserved.

## Integration with ADR-0049

ADR-0049 replaced the `FileIdInfo` query for Windows-side WSL entries with the `wsl-v2` tuple read
by `WindowsWslFileSystem` through `ReadWslFacts`. This harness therefore obtains every identity,
for `/tmp`, the configured root, the run child, the marker, fixtures, and product-created entries,
from the production `WindowsWslFileSystem.Find` on the entry's own path; it never calls
`WindowsFileIdentifier` directly and never derives identity from enumeration. The launcher no
longer loads the Infrastructure assembly or reflects into a nonpublic identity member; it keeps
the root-shape, account-home, mount-fact, ephemeral-runsettings, and exact-TRX-count duties, and
the C# owner alone captures `/tmp` and configured-root identities at fixture start and compares
them immediately before its first mutation. Deterministic root-safety tests inject the unguarded
NTFS handle-facts reader through the existing `WindowsWslFileSystem` seam, exactly as
`WindowsWslFileSystemTests` do. Because a directory's `wsl-v2` token changes whenever its direct
children change, the owner re-captures an owned directory immediately after each mutation it
performs and compares against that capture; entry identities of files and links stay stable
across reads. The live tier gains the ADR-0049 read-only cells: on the configured distribution the
inode, link count, and change time of an owned fixture equal the values reported by a read-only
`stat`, a symlink fixture and its target produce different tokens, and an unchanged fixture yields
the same token after an intervening read. The launcher requires the exact declared number of
Passed live cases; any skip or absence fails the tier.

## Executable proof

`LiveWslTestRootTests` proves unset, missing admission facts, malformed, unregistered, unsafe-name,
nonempty, ancestor-link, root-link, run-child replacement, configured-root replacement,
ancestor-turned-link, ownership-marker replacement including a byte-identical one, owned-fixture
rewrite that restores length and last-write time, foreign residue during setup and cleanup,
owned-link cleanup, and repeated-identity behavior against `TestOwnedTemporaryRoot` through the
unguarded NTFS handle-facts seam. `LiveWslTransferTests` defines the required nested copy,
composite move, byte and declared kind/entry-set/length equality, source preservation or deletion
at the proper step, exact effects, link refusal with zero effects, and final cleanup assertions.
`LiveWslIdentityTests` defines the ADR-0049 read-only cells above. The existing CS-010 gate proof continues to reject direct environment access in
test code. A focused launcher proof verifies XML escaping, ephemeral runsettings cleanup, exact
filtering, and an unset unexecuted result without changing the canonical gate. Focused tests, the
affected Infrastructure project, Commit mode, deep review, the final canonical Ready gate, and the
separately recorded live command prove their respective tiers.
