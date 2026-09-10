# ADR-0049: Identify Windows-side WSL entries through one provider-scoped 9P identity

Status: accepted

Date: 2026-09-10

Accepted for Issue #105 on 2026-09-10 by the NeNe Commander design owner after hide accepted the
threat scope recorded under Consequences.

## Context

ADR-0033 identifies a Windows local entry with the 64-bit volume serial and 128-bit file identifier
from `GetFileInformationByHandleEx(FileIdInfo)` plus kind, length, creation time, and last-write
time. ADR-0036 reuses the same `WindowsFileIdentifier` for `\\wsl.localhost` entries under the
`wsl-v1` token. Issue #105 found that the WSL namespace cannot satisfy that contract. A read-only
diagnostic on 2026-09-10 (WSL 2.7.13.0, kernel 6.18.33.2, Ubuntu-22.04, Windows 11 26200.9445)
established the facts below; every one of them was measured, none is inferred.

- The 9P redirector rejects `FileIdInfo`, `FileStorageInfo`, `FileCaseSensitiveInfo`,
  `FileNormalizedNameInfo`, `FileIdInformation`, `FileStatInformation`, `FileStatLxInformation`,
  `FileFsObjectIdInformation`, and `FileIdExtdDirectoryInfo` with `ERROR_NOT_SUPPORTED` or
  `ERROR_INVALID_PARAMETER` regardless of the requested access. No 128-bit identifier, Linux mode,
  or Linux device number is available from the Windows namespace.
- Every volume query returns serial 0, an empty label, creation time 0, and the file system name
  `9P`. `FileRemoteProtocolInfo` is byte-identical for every path (`WNNC_NET_9P`, version 1.0,
  flags 0, reserved areas 0). The only distribution discriminator in the namespace is the share
  name.
- `NtQueryInformationFile(FileInternalInformation)` returns the Linux inode number (21/21 samples
  equal to `stat %i`), and `NumberOfLinks` equals `stat %h` (21/21).
- Inode numbers collide across mounts inside one distribution: twelve mount roots with distinct
  `st_dev` (`/proc`, `/dev`, `/run`, `/sys`, `/mnt/wsl`, `/dev/shm`, `/dev/pts`, `/run/lock`,
  `/sys/fs/cgroup`, two snap roots, `binfmt_misc`) all return inode 1; `/` and `/tmp/.X11-unix`
  both return 2; `/bin` and `/run/user` both return 15.
- Directory enumeration (`FileIdBothDirectoryInfo`) reports the ext4 mount-point stub inode while
  opening the same path reports the mounted root inode. Enumeration identity and handle identity
  disagree at every mount point.
- `FILE_FLAG_OPEN_REPARSE_POINT` is honored: a Linux symlink opens as attribute `0x400` with
  reparse tag `0xA000001D` (`IO_REPARSE_TAG_LX_SYMLINK`), its own inode, and `nlink` 1, distinct
  from its target (`/bin` 15 versus `/usr/bin` 46).
- `CreationTime` is not a birth time; it is `min(atime, mtime, ctime)` and it changed during the
  read-only diagnostic because `relatime` advanced `atime`.
- `FileBasicInfo` is always available on 9P, and its `ChangeTime`, `LastWriteTime`, and
  `LastAccessTime` equal Linux `ctime`, `mtime`, and `atime` in 29/29 samples, including every
  colliding mount root. Read-only access moved `LastAccessTime` and `CreationTime` while
  `ChangeTime`, `LastWriteTime`, attributes, `EndOfFile`, and `NumberOfLinks` stayed unchanged
  (9/9 files). A directory's `ctime` and `mtime` change when a process inside the distribution
  writes into it, exactly as NTFS last-write time does.
- `GetVolumeInformationByHandleW` reports `9P` for 43/43 WSL handles on every mount and `NTFS`
  for every local volume. An SMB loopback share also reports `NTFS` and supports `FileIdInfo`, so
  the name identifies the query capability rather than locality.
- drvfs mounts (`/mnt/c`, `/mnt/c/Windows`) cannot be opened through the share with any access
  mask (`ERROR_ACCESS_DENIED`); they appear in enumeration only as stub entries.
- `\\wsl$` and `\\wsl.localhost` return byte-identical results.

Consequently production `WindowsWslFileSystem` fails closed on every real WSL entry today. The
same-distribution create, rename, delete, copy, and composite move accepted by ADR-0036 and
ADR-0037 have never been executable against a real distribution, and the Issue #93 harness cannot
fix its root identity.

## Decision

- **`WindowsFileIdentifier` stays the single identity owner and gains one second, explicitly
  requested query form.** `Describe` and `DescribeHandle` keep the ADR-0033 `FileIdInfo` contract
  for Windows local entries and the settings store, with no fallback. A new `ReadWslFacts(path)`
  entry point is called only by `WindowsWslFileSystem`. The form is selected by the calling adapter
  from the validated provider (`WslPath`), never by catching a failed `FileIdInfo` query; a Windows
  local or UNC path whose `FileIdInfo` query fails still fails closed.
- **`ReadWslFacts` reads every field from one no-follow handle and verifies the file system
  name.** It opens the path with `FILE_READ_ATTRIBUTES`, share read/write/delete,
  `OPEN_EXISTING`, `FILE_FLAG_BACKUP_SEMANTICS`, and `FILE_FLAG_OPEN_REPARSE_POINT`; requires
  `GetVolumeInformationByHandleW` to report exactly `9P` and otherwise throws the normalized
  `IOException`; then reads `FileInternalInformation` (inode), `FileAttributeTagInfo` (attributes
  and reparse tag), `FileStandardInfo` (directory flag, `EndOfFile`, `NumberOfLinks`), and
  `FileBasicInfo` (`LastWriteTime`, `ChangeTime`). Any failed query closes the identity. Length and
  times come from the handle, not from a second `System.IO` path lookup. The native read is split
  into an internal handle-facts reader without the file-system check and the `9P`-guarded wrapper;
  only the wrapper is reachable from `WindowsWslFileSystem`, and the unguarded reader exists so
  that deterministic tests can obtain real handle facts from NTFS test roots.
- **The `wsl-v2` token is
  `wsl-v2|<distribution>|<inode>|<kind>|<reparse tag>|<nlink>|<length>|<last-write ticks>|<change ticks>`.**
  `<distribution>` is the canonical parsed distribution name of the `WslPath`. `<kind>` is
  `directory`, `file`, or `link`, derived from the attributes and reparse tag of the link entry
  itself. Creation time and last-access time are excluded because 9P derives creation time from
  access time. `ChangeTime` (Linux `ctime`) is included because the kernel assigns it on every
  content, metadata, link-count, or rename change and no unprivileged caller can set it through 9P
  or through Linux APIs, so a replacement entry that restores inode, kind, length, and `mtime`
  still differs. `nlink` is included because it distinguishes hard-link additions and most mount
  roots from ordinary directories. Token composition is a pure internal function over the facts
  record.
- **Enumeration identifiers are never identity.** Directory listing continues to use `System.IO`
  enumeration for names, attributes, and sizes only; every identity for preflight, revalidation,
  copy verification, deletion, and the Issue #93 harness comes from `ReadWslFacts` on the entry's
  own handle. Mixing the two forms is prohibited because they disagree at mount points.
- **Unopenable mounts close the operation.** An entry that cannot be opened through the share
  (drvfs `/mnt/<drive>` today, which answers `ERROR_ACCESS_DENIED`) reports the failure that the
  shared Windows failure normalizer already assigns to that error, `AccessDenied`; the adapter
  does not add a WSL-only normalization and does not fall back to enumeration data, to `wsl.exe`,
  or to the Windows local provider. (Corrected on 2026-09-11 from `ProviderUnavailable`: the
  ADV-017 contract maps the observed error to `AccessDenied`, and both are closed failures.)
- **No distribution epoch is claimed.** The namespace exposes no serial, object identifier, or
  creation time for the share, so a distribution is identified only by its canonical name. A
  recreated distribution with the same name is the same provider; its entries are then different
  entries by inode and `ctime`, which is the only recreation signal this decision relies on.
- **The residual reopen race remains as recorded.** Identity is captured and revalidated by path
  reopen before every `System.IO` side effect, exactly as ADR-0033 and ADR-0036 record for the
  Windows local adapter; handle-relative Linux mutation is not claimed.
- **Issue #93 reuses the same mechanism.** The live harness fixes root and child identity through
  `WindowsWslFileSystem` inspect and revalidate and adds no runner-owned identity or shell query.

## Rejected alternatives

- Legacy `GetFileInformationByHandle` file index alone (the Issue #105 exclusion): the serial is
  always 0, and the index collides across mounts and is reused after deletion within one file
  system.
- Metadata only (kind, length, timestamps): rejected by ADR-0033 for Windows local and weaker
  still on 9P, where creation time is derived from access time.
- `FileStatLxInformation` for `LxMode` and device numbers: `STATUS_NOT_SUPPORTED` on 9P with every
  access mask; on NTFS the Lx fields are zero.
- Parsing `/proc/self/mountinfo` through the share to derive `st_dev` per path: a second identity
  source made of untrusted text, racy against mount changes, and describing the 9P server's mount
  namespace rather than the opened handle.
- `wsl.exe -- stat` as identity engine: prohibited by FS-003 and ADR-0004, and racy against the
  Windows-side handle.
- Keeping creation time in the tuple: it changes under read access, so every revalidation after a
  listing would report `IdentityChanged` and the provider would be unusable.
- Falling back to the 9P form whenever `FileIdInfo` fails on any provider: it would silently weaken
  NTFS/ReFS identity on an unexpected error; the form is selected by provider, not by failure.
- Rejecting WSL mutation permanently: keeps the current fail-closed state but abandons the accepted
  ADR-0036/ADR-0037 product behavior without a stronger safety argument than the one below.

## Consequences

The Windows-side WSL identity is weaker than the NTFS 128-bit identifier in exactly one respect:
two distinct entries on different mounts inside one distribution can share an inode number. Making
such a pair appear at the same path between inspect and mutation requires mounting inside the
distribution, which needs distribution root or a user-initiated FUSE mount by the same user. This
decision treats that actor as outside the concurrent-unprivileged-replacement threat that entry
identity defends against, states it in `docs/SECURITY_MODEL.md`, and relies on `ctime` and
`nlink` to separate the ordinary cases. Clock rollback inside the distribution is likewise outside
the scope. hide accepted this scope on 2026-09-10.

`docs/FILESYSTEM_BOUNDARIES.md` FS-001 changes from one shared tuple to a provider-specific
snapshot: Windows local keeps ADR-0033; Windows-side WSL uses the `wsl-v2` tuple above. FS-012
gains the enumeration and unopenable-mount rules. A new adversarial case (ADV-020) covers a WSL
replacement that restores inode, kind, length, and `mtime` but not `ctime` or `nlink`, and the
link-versus-target distinction on 9P.

## Migration and removal

`wsl-v1` tokens exist only inside one operation's snapshot and are never persisted; no migration
is needed. If a future Windows release exposes `FileIdInfo` or `FileStatLxInformation` over 9P, a
successor ADR may replace the tuple; until then `ReadWslFacts` must not opportunistically use
them.

## Executable proof

- Deterministic: token composition is proven over a facts record (shape, `link` kind from
  attribute `0x400` plus tag `0xA000001D`, directory with nonzero length rejected); the guarded
  reader fails closed on a non-`9P` file system name and on any failed information class;
  `WindowsWslFileSystem` inspect and revalidate on an NTFS test root obtain real handle facts
  through the unguarded reader and prove that a content rewrite, a same-length replacement with
  restored `mtime`, a hard-link addition, and a link swapped for its target each yield
  `IdentityChanged` with zero effects, while an unchanged entry revalidates after a read. The
  Windows local adapter and settings store never reach the WSL form; a security rule proves the
  `9P` literal and both native entry points live only in `WindowsFileIdentifier` and that only
  `WindowsWslFileSystem` references `ReadWslFacts`. ADV-020 maps to those tests.
- Live read-only (Issue #93 environmental tier, opt-in): on a real distribution, inode equals
  `stat %i`, `ctime` equals `stat %Z`, `nlink` equals `stat %h`, a symlink and its target produce
  different tokens, and two consecutive reads of the same unchanged entry produce the same token
  after an intervening read access. Mount-root distinction is not claimed by the live proof.
- Live mutation (Issue #93): create, rename, copy, composite move, and confirmed delete each pass
  through `ReadWslFacts` before the side effect; a replaced source between inspect and mutation is
  rejected with zero effects.
