# Language selection: startup and persistence design investigation

Issue #528 / 2026-11-22 / base main7ef9b96.
Status: proposed engineering design. No product implementation, accepted ADR,
Chinese translation or visual acceptance.

## Owners and invariant

SettingsSession retains one complete UserSettings and the ordered write queue.
SettingsDocumentValidator remains the only untrusted parser; WindowsLocalSettingsStore
retains whole-document validation, identity/byte revalidation, atomic publication
and preservation of rejected documents. CommanderSession routes the typed choice.
No custom dictionary loader, second settings file or new write path is proposed.

The App constructor currently initializes XamlControlsResources and DesignTokens.
StartAsync then reads settings before constructing CommanderWindow. The window creates
a ResourceLoader; LocalizedTextConverter caches another static ResourceLoader.
App.xaml contains neither localized text nor this converter. All views keep this
existing resource route.

## Proposed startup and selection behavior

1. Add a closed System/default choice plus explicit Japanese, English and the Chinese
   scope hide confirms. Schemas1–4 migrate in memory to System without a startup write.
   Previously system-selected behavior must not silently become a fixed locale.
2. Read settings through the existing asynchronous startup owner. Apply the closed
   explicit choice using Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride
   in the App composition boundary before resource initialization. Never pass arbitrary
   persisted language text to this API.
3. Move App InitializeComponent into that owned startup sequence, after language
   application and before theme/window construction. Do not block on the settings read.
   The fixture proves deferred initialization in its own App; actual product
   cancellation/defect/shutdown and asynchronous startup still need verification.
4. Construct existing loaders/window once. Before activation, set the existing root
   FrameworkElement.Language for an explicit choice; children inherit it. For System,
   retain the platform default. Do not change .NET culture, input language, keyboard
   layout or IME. Existing Windows regional formats and invariant columns remain.
5. Selection saves through the existing queue and changes UI language on next launch.
   Keep the current process in its launch language, including error messages. Explain
   this timing in the editor. Escape still closes without undoing selections.

Microsoft documents the override as process-global and non-persistent for unpackaged
apps, and directs callers to set it before loading resources. This product has
WindowsPackageType=None. A packaged distribution would require review of the
OS-persisted override before adoption; it must not become a second settings authority.
See [PrimaryLanguageOverride](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.globalization.applicationlanguages.primarylanguageoverride?view=windows-app-sdk-1.6)
and [MRT localization](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/localize-strings).

The diagnostic distinguishes resource lookup, element Language and regional culture.
Correct text alone does not establish the language used by assistive technology.
Explicit Language inheritance is still not Narrator speech proof. This feature
selects UI language and does not redesign regional numeric/date formats.

## Proposed schema and editor

Extend DisplayPreferences with one closed language choice and preserve every other
field in all selector calls. Proposed schema5 adds required uiLanguage to schema4,
with exact canonical identifiers for System, en-US, ja-JP and the confirmed Chinese
scope. Older shapes stay exact. Missing/duplicate/unexpected properties, wrong types,
unknown identifiers/versions and oversize input remain typed whole-document rejection.
Never partially apply or repair a rejected file. A normal accepted write emits the
complete current schema, preserving bookmarks/categories/slots, pane sorting, hidden
default and hint visibility. Old binaries may reject schema5; they must preserve it,
and downgrade compatibility is not claimed.

Use recognizable native radio names (日本語, English and the chosen Chinese script;
System localized). Extend the existing settings items and movement/activation route:
movement only focuses; Space/Enter selects and saves. Preserve repeat suppression,
Tab, modal precedence and close-focus return. Placement/tokens still need the design
handoff; this engineering document is not final visual approval.

SettingsWarningSaveFailed currently says the choice is active in this session.
That is inaccurate for a next-launch language choice. Use truthful shared save-failure
wording plus the explicit language timing explanation. A changed radio is not a saved
result. Preserve Pending/Failed/Cancelled states and latest-revision ownership.
Changing back must not produce a false restart-success indication.

## Open scope and required proof

hide was asked at19:47 JST whether Chinese means simplified, traditional or both.
No answer was received during this investigation. Do not ship guessed coverage.
Translate every required key, including accessible names, errors, help and search
labels, and prove exact key/placeholder agreement and permitted empty values.
Copied English text must not be hidden as complete translation.

#525 retains a synthetic Chinese320×300 counterexample: viewport32 versus row33.6.
Revisit this with actual chosen translations. Native IME/search, keyboard-only
selection, long text, tooltips, high contrast/DPI and accessible speech remain open.

Future focused evidence covers closed values, all selector preservation, queue
ordering/failure/cancellation/shutdown, exact schema1–5 migration/rejection/write
roundtrip/invalid-file preservation, presenter/mapper/focus, locale conformance,
App build and actual restart after saved/pending/failed choices. Select ADR-0061
scope from the final shared settings/startup impact. The full gate is needed only
when that wider impact cannot be established by focused proof. No product proof
is claimed by the present external diagnostic.
