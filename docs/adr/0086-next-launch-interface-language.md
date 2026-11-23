# ADR-0086: Select the next-launch interface language through existing settings

Status: accepted

Date: 2026-11-22

Issue #528. hide requested Japanese, English and Chinese selection and confirmed
Simplified Chinese at21:34 JST. The existing unpackaged distribution and single
settings/resource owners are retained. Accepted for the technical contract under hide's current continuation authority after independent read-only review at22:24 JST
and correction of the System setter path. Implementation/translation/startup
and native feature acceptance remain separate.

## Evidence and constraints

The bounded thirteen-process investigation found that existing ResourceLoader and
LocalizedTextConverter instances can keep their original language after a late
override. Deferred App resource initialization plus explicit root Language worked
in the isolated, unshown fixture; actual product startup is not established by it.
See the [investigation](../quality/2026-11-22-language-selection-investigation.md).

Microsoft's [PrimaryLanguageOverride documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.globalization.applicationlanguages.primarylanguageoverride?view=windows-app-sdk-1.8)
describes a process-global override, non-persistent for unpackaged apps, and calls
for setting it before resource loading. Existing resources may require a restart.
The [resource guidance](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/localize-strings)
uses one .resw per language and the existing ResourceLoader/x:Uid route. Both
sources were checked on November22; the pinned local SDK remains authoritative
for compilation and runtime evidence.

## Decision

- Add one closed InterfaceLanguage choice: System, Japanese, English and
  SimplifiedChinese, with exact persisted identifiers system, ja-JP, en-US and
  zh-Hans. System is the default and preserves the current platform-selected
  behavior. Only these shipped choices can reach the platform override. Unknown
  tags, aliases, casing, whitespace and null are not accepted as another choice.
- The ordered public language catalog owns its array behind a private read-only
  view. It exposes neither a mutable collection nor a non-generic ICollection
  synchronization root that could disclose storage. Consumers may index and
  enumerate the four closed choices; their order and identities are stable.
- Add the choice to the existing DisplayPreferences complete value. Its one
  factory takes all four display choices; all callers migrate atomically, with
  no optional language argument or compatibility overload. All existing selectors,
  bookmarks and pane-sort updates preserve the other fields.
- One typed selection intent goes through CommanderSession's existing settings
  precedence into SettingsSession, which changes the selected next-launch value
  and queues the complete document through its existing ordered writer. The
  existing revision, failure, cancellation, no-rollback and shutdown semantics
  remain. Merely opening, focusing, rendering, reading or launching never writes.
- The current process retains its launch language. Selection takes effect at the
  next launch after a successful save. Do not restart or rebuild the window,
  reload converters, mutate thread cultures, change keyboard layout/IME, or add a
  live-switch event. The editor always states the next-launch timing. Choosing
  the original value again has no special restart-success or pending-language flag.
- Write schema5 in the existing property order, appending required uiLanguage to
  schema4 after showKeyHints. Read exact schemas1-4 with InterfaceLanguage.System
  in memory, without startup rewrite. Schema5 rejects a missing/duplicate/unknown
  or wrongly cased property, wrong JSON type, unknown identifier, excessive UTF-8
  length and unknown version as a whole. Unknown language text has one canonical
  SettingsReadFailureKind.UnknownInterfaceLanguage reason. Existing JSON-shape
  precedence and the65536-byte limit stay unchanged.
- SettingsDocumentValidator is the sole untrusted document boundary. Its existing
  SettingsDisplayCodec maps the closed language or a typed rejection. The one
  serializer always emits schema5. WindowsLocalSettingsStore keeps its current
  location, validation, identity/byte checks, temporary ownership and atomic
  publish. Rejected existing documents remain untouched. Older binaries reject
  schema5 rather than dropping it; no downgrade writer is provided.
- CommanderApplication's existing AsyncWorkOwner reads settings asynchronously.
  Its constructor initializes only lifecycle owners; move InitializeComponent
  into StartAsync after the read and language application, before theme or view
  construction. On the owning UI context, set
  Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride
  once only for an explicit closed language. For System, do not call the setter:
  retain the new unpackaged process's unmodified default. An already-cancelled startup
  must not initialize/show the window. Keep exception observation and resource
  lifetime under the existing startup/shutdown owners; no second coordinator.
  This orders explicit host calls; it does not promise to suppress all WinUI-internal
  dictionary evaluation or evaluation caused by inspecting Application.Resources.
- After constructing CommanderWindow and before activation, set the existing
  root FrameworkElement.Language for explicit choices; System leaves native
  inheritance intact. The existing app-owned ResourceLoader, LocalizedTextConverter
  and view x:Uid lookups are constructed after startup language selection. No second
  loader, copied application translation map or settings file is introduced.
- The choice selects interface text only. Existing explicit/invariant metadata
  formatting and Windows regional culture stay unchanged. Framework-owned strings
  and assistive speech still require separate environmental evidence.

## Settings editor integration

Reuse the current modal, ordinary radio-option template/tokens and keyboard owner.
After the two existing checkboxes and before scheme choices, show a localized
language heading, a wrapped next-launch explanation and four options: localized
System, 日本語, English, 简体中文. Keep separate native radio GroupName values for
language and theme; selecting one group must not deselect the other. Stable
language option identifiers and their native names expose the selected state.
The close button remains initial focus. Extend the existing SettingsKeyboardView
ordered item construction to include the two option groups. Movement remains
focus-only; Space/Enter selects through the existing native Checked forwarder;
repeats/Tab/Escape retain ADR-0077. Do not reset ItemsSource for progress-only
renders or replace focused radio controls. The existing render guard prevents
programmatic assignment from enqueueing saves.

The shared save-failure sentence must say that changes could not be saved,
without claiming every selection is active in this process. Keep Pending,
Succeeded, Failed and Cancelled projections truthful. Save failure is distinct
from next-launch timing. This extends the accepted settings layout with existing
components; no new colors, dimensions, typography or competing design direction.
Screen acceptance and fitting four additional rows are still required.

## Locale completeness and verification

Add zh-Hans/Resources.resw beside the two existing catalogs. Translate every key,
including status/errors, safety confirmations, accessible properties, help and
settings; preserve placeholder sets and intentionally empty values. Keep proper
names and key legends as such, explicitly inventory any identical non-language
values instead of calling copied English a translation. Key equality, casing and
format parity are machine checked; semantic translation review remains separate.
When another feature joins main, translate its keys before accepting the combined
candidate. The unmerged #544 and #547 resources are not silently claimed shipped.

ADR-0061 selects affected construction/selection and settings queue consumers;
strict schema1-5 migration, ADV-012 rejection and owned-root round trips/preservation;
settings focus/presentation/key resource proof; App build; language catalog/PRI
verification and an isolated unshown startup fixture using the real startup code
with explicit owned settings paths/ports where possible. Record exactly which
parts are real product code and which are fixture substitutions. Reuse unchanged
successes. The serializer/parser and startup changes require direct boundary
proof, not unrelated provider suites. Review the final impact before requesting
any full/deep gate; no claim that limited diagnostic evidence proves production
startup, save/restart, native focus/IME or screen-reader speech.

An isolated native session is required for actual restart after saved/pending/
failed selections, keyboard-only focus/selection, Japanese/Chinese IME and
accessible names/speech. Shared-desktop input remains prohibited. No normal App,
user settings or beta.3 installation is changed for this work. Feature acceptance
stays incomplete until required functional/translation/startup evidence exists;
remaining screen observations must be named rather than inferred from build success.

## Rejected alternatives

A live override with existing loaders leaves mixed-language caches. A second
loader/translation dictionary duplicates resources. A separate language setting
file or OS-persisted authority duplicates settings. Defaulting old documents to a
fixed language changes existing behavior. A permissive optional field in schema4
weakens its exact-shape contract. Auto-restart adds lifetime behavior and can
interrupt operations. Changing regional culture or IME expands the request.

The System branch must not attempt to clear the override with an empty string.
Microsoft.Windows setter handling has a reported empty/null rejection
([WindowsAppSDK issue5335](https://github.com/microsoft/WindowsAppSDK/issues/5335));
the initial diagnostic observed an unset System state and did not prove an empty
setter. Avoid that API path entirely in this next-launch-only design.

A future packaged distribution must review the OS-persisted override before
adoption. The unpackaged non-persistence assumption cannot silently migrate.
