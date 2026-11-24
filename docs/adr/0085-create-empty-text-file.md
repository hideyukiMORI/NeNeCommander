# ADR-0085: Create one empty text file through the existing mutation gateway

Status: accepted

Date: 2026-11-22

Issue #544. The design owner accepts this technical contract under hide's current
continuation instruction after independent read-only review and the two clarified
scope/cancellation points. This is not implementation, shipment or native UI acceptance.
ADR-0084 is already allocated to Issue #547 in its retained worktree.

## Context

The existing F7 and F2 flow owns one name-entry state, one request factory, one
FileOperationGateway, and routed provider steps. A new empty file needs a distinct
request/effect and an exclusive creation primitive. It must not use an editor,
Shell, temporary template copy, overwrite, or a second mutation service.

A November22 isolated Windows probe disproves ordinary File.OpenHandle(CreateNew)
as the complete primitive: a dangling final symbolic link caused the absent link
target to be created. CREATE_NEW combined with FILE_FLAG_OPEN_REPARSE_POINT refused
that entry with error80. Both still follow a linked ancestor. These are OS primitive
observations, not provider or product proof; see the linked quality record.

## Decision

- Add a closed UserIntent.CreateTextFile and OperationKind.CreateTextFile. Use the
  existing OperationAwaitingName, NameSubmission, DualPaneSession.StartAsync and
  FileOperationGateway serialization. Capture the currently listed active location
  when opening the editor; modal freeze retains active side and location. No listed
  directory means no editor. Running, confirmation, conflict and other modal/scope
  ownership keep their current precedence. Admission is re-evaluated on dispatch.
- CreateTextFileRequest.Create(location, name) derives exactly one direct child
  through FileSystemPath.Child. It freezes one location/source and one target using
  the existing request-owned snapshot. Do not trim, translate, append an extension,
  invent a suffix after collision, or normalize the user's accepted filename again.
- Create exactly zero bytes, without BOM, newline, metadata template or implicit
  encoding selection. Empty bytes are compatible with multiple encodings; no claim
  that an encoding has been detected or stored is made. Do not launch after creation.
- Suggest a localized filename ending in .txt when the editor first appears:
  English New text file.txt; Japanese 新しいテキスト.txt. The existing editor selects
  the complete proposed name. The user may replace it, including the extension.
  A closed presentation-owned literal/resource text variant supplies initial text;
  Application never reads localization. F7's empty seed and F2's literal existing
  name migrate atomically through that same variant. The host resolves text only.
- Propose Ctrl+F7 in FileList and the existing normal navigation-surface route.
  F7 remains directory creation. Reuse the existing key/modifier translator and
  repeat guard, canonical binding table, F1 help and command labels. Shift is not
  currently represented by the modifier model, so Shift+F7 is not silently added.
  Palette execution uses the same intent. The New > Text file pointer entry is
  coordinated with #530/#531; keyboard/palette work alone cannot close that acceptance.
- Add IFileOperationPort.CreateEmptyFileAsync(locationSnapshot, target, token).
  FileOperationGateway inspects IdentityOnly, observes cancellation before admitting
  the single provider step, and publishes FileCreated(target) and progress1/1 only
  after that step succeeds. No ordinary-deletion capability is requested. Failed
  preflight or failed exclusive creation has no completed effect or progress.
- Preserve ADR-0027's admitted atomic-step semantics: scheduler-level cancellation
  does not discard a queued/running mutation. Cancellation before admission stops
  creation. Once admitted, the step may complete; a successful effect is reported
  even if cancellation arrives during it: the gateway returns Succeeded with one
  FileCreated effect and progress1/1, not Cancelled with effects. This preserves
  the existing successful-target focus policy. There is no delete-on-cancel rollback.
  Refresh/listing failure cannot erase the completed creation receipt. Successful
  creation uses the existing RefreshFocusingAsync target policy and passive refresh;
  no new selection or focus owner is added.
- ProviderFileOperationPort routes this single-parent operation. Proposed admitted
  providers are Windows local under current WindowsDriveBoundary rules and WSL
  through its current 9P identity/extended-length boundary. UNC and mapped/unknown
  drives retain closed refusal. Windows local AND WSL are the completion scope of
  this feature. Until the selected live WSL cells pass, the entire feature remains
  unaccepted and unshipped; no runtime evidence flag or Windows-only completion is
  introduced. Windows success or a fake alone never grants WSL creation permission.
- Each provider revalidates the original location identity and its current non-link
  ancestor chain, requires a non-link directory, the same provider, and equality of
  target.Parent with that exact location using FileSystemPathIdentityComparer.
  Generic descendant containment is insufficient for this direct-child operation.
  Repeat existing drive/provider admission at the effect boundary. Use the existing
  WSL resolver unchanged, including Linux names and distribution identity.
- One narrowly owned Infrastructure creation primitive calls source-generated
  CreateFileW with GENERIC_WRITE, no sharing, noninheritable null security attributes,
  CREATE_NEW, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OPEN_REPARSE_POINT, and no template.
  It receives only provider-resolved text after admission. The valid SafeFileHandle
  is closed in the owning step without writing, reopening, deleting or retrying.
  Keep identity imports/9P facts in WindowsFileIdentifier; do not move identity or
  arbitrary filesystem utilities into the creation primitive.
- Known existing entries, including directories, regular files, hardlinks and final
  reparse points, are conflicts. Native80/183 are conflicts. The local probe returns
  error5 for a directory: classify it as Conflict only when an existing target is
  actually observed; otherwise preserve AccessDenied. Unknown/ambiguous failures
  remain closed. A failure cannot invoke an overwrite or suffix retry.
- OPEN_REPARSE_POINT protects the final component, not its ancestors. Revalidation
  does not lock the parent chain or a drive mapping. Retain and state the existing
  final-query-to-path-reopen race; do not claim handle-relative creation or complete
  resistance to a parent replacement after the last observation. No new broad
  handle-relative mutation subsystem is proposed by this feature.

## Rejected alternatives

File.Create, WriteAllText and Create/OpenOrCreate can overwrite. BCL CreateNew alone
follows a dangling final link in the observed environment. Exists-then-create is not
exclusive and cannot close late collisions. Copying a bundled blank template adds
source ownership and another policy for a zero-byte operation. Shell new-file verbs,
an embedded editor, automatic opening, suffix retries and delete-on-cancel change
the requested semantics. A second name modal or private key map violates existing
owners. Holding arbitrary ancestor handles is a separate provider-policy change.

## Consequences and migration

A new filesystem mutation crosses a material trust boundary. Before code acceptance,
update ARC-005/CMD-001/FILESYSTEM_BOUNDARIES, the operation glossary and ADV-003/004/
005/014/015/016/017/024 mappings; add no new dependency, project, settings schema or
gate. Every production provider and test fake implements the one new port member
in the same unit; cross-transfer-only adapters fail closed. All request/effect/kind
projections must be exhaustive, with existing directory/rename behavior preserved.
The initial-name presentation migration must not change native IME or modal freeze.

This proposal is local. The pending #542 public source push rejection and #547
Claude-brief transmission rejection are not retried or bypassed. There is no new
visual layout here; a future context-menu layout follows its own design handoff.

## Executable proof required

1. Request: direct-child derivation, immutable source list, invalid separators,
   traversal, Windows device names, provider-specific name/case/length and nulls.
2. Gateway: one inspection/step/progress/effect; failure before effect; cancellation
   before/after inspection and during an admitted step; concurrent refusal; no
   destructive capability, delete, copy, verify or Shell call.
3. Session/Presentation: both sides, frozen location, Escape, duplicate submission,
   all transient scopes, absent listing, invalid name, conflict, provider failure,
   new-target focus, passive refresh, refresh failure after effect, translated seed,
   Ctrl+F7 initial/repeat/context behavior, unchanged F7/F2/text/IME, palette and help.
4. Providers in the existing owned-root harness: empty bytes, file/directory/hidden/
   readonly/hardlink/live-link/dangling-link conflicts and preserved sentinels;
   controlled late collision; invalid parent/provider/direct-child; replaced or
   linked ancestor; drive remap; boundary-length names; no cleanup after refusal.
   Use deterministic manual scheduler barriers for provider timing. The exploratory
   16-contender observation is not a replacement for controlled product race tests.
5. WSL fake contracts plus selected opt-in live cells on the existing dedicated
   root owner must prove the exact exclusive/no-follow flags, case sensitivity,
   extended names and link refusal on 9P. No shell command fallback or user path.
6. Baseline failures and focused builds/tests first. This new shared port/effect
   and native mutation boundary requires one canonical deep-review candidate after
   the complete implementation is prepared; do not also run duplicate quality.
   Its execution/transmission follows existing approval restrictions. No product
   build/test/CI is justified by this proposal-only commit. Native input/screen proof
   remains separate and requires an isolated desktop/session.

Sources: [FileMode](https://learn.microsoft.com/en-us/dotnet/api/system.io.filemode?view=net-10.0),
[CreateFileW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew),
[symbolic-link effects](https://learn.microsoft.com/en-us/windows/win32/fileio/symbolic-link-effects-on-file-systems-functions).
Local observations and their limits are in [quality](../quality/2026-11-22-text-file-creation-design.md).
