# ADR-0088: Apply interface language through one live window resource context

Status: accepted

Date: 2026-11-23

Issue #528. hide explicitly requested immediate language selection at17:21 JST.
This supersedes the next-launch-only timing and loading mechanism in ADR-0086;
its closed choices, schema5, strict parser and sole ordered settings queue remain.
The requested timing authorizes this technical contract. Product/native acceptance
and publication are separate from that decision.

## Context and evidence

Existing ResourceLoader instances and x:Uid values retain launch-time text.
Microsoft documents explicit ResourceContext language qualifiers over the same PRI.
The pinned SDK fixture resolves all546 keys in each of six language transitions
without mismatches and restores the original System qualifier (ja;en-US;) without
setting PrimaryLanguageOverride. This is resource proof, not native UI proof.
The reported WindowsAppSDK#6801 interaction between global overrides and explicit
contexts is another reason to remove the former mechanism atomically.

- [MRT Core explicit context guidance](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/localize-strings#load-a-string-for-a-specific-language-or-other-context)
- [Upstream global override report](https://github.com/microsoft/WindowsAppSDK/issues/6801)

## Single mechanism and invariants

- SettingsSession retains ownership of the selected InterfaceLanguage and complete
  queued documents. The existing settings-only typed intent changes the selection
  synchronously before disk completion. The host observes the current snapshot;
  an older completion cannot roll the displayed language back. Save failure keeps
  the selected language for this process and exposes the existing save warning.
- One ViewTextResources instance belongs to the window XAML resource dictionary.
  Its single MRT resource context reads the existing shipped PRI. It captures the
  unmodified System language qualifier before any explicit choice, and restores
  that qualifier when System is selected. Only closed choices enter the host.
  No new translation data, settings file, global override, thread culture change,
  keyboard layout switch, process restart or window recreation is introduced.
- The same instance is explicitly supplied to all host renderers and localized
  XAML targets. LocalizedText attached keys replace x:Uid and the cached converter
  in one migration. Their framework adapter updates only the declared text,
  content, placeholder, accessible name or help text dependency property.
  Loaded targets subscribe to the window resource owner; Unloaded detaches them;
  a newly loaded or recycled target resolves its current key and current language.
  There is no reflection, window-tree traversal on keys, or ambient resource owner.
- The root element Language follows the actual resource candidate's language tag.
  This supplies language metadata for text and accessibility; built-in WinUI/OS
  menus and assistive speech require environmental verification. Application-owned
  text is the immediate-switch contract; do not claim control over cached OS text.
- Settings controls and their ItemsSource remain stable. Changes do not reopen the
  editor, move focus, reset radio choices, change text input, or recreate pane rows.
  An unchanged pane presentation refreshes localized status/counts without another
  ScrollIntoView. A changed presentation retains normal focus/scroll handling.
  The seven existing metadata formats are identical across shipped languages;
  the launch format and regional culture stay unchanged. A future divergent
  metadata format requires an explicit projection/scroll-preservation review.
- Settings is the only admitted language selection scope. A palette, bookmark
  draft, theme picker, address entry, help, Locations or active operation cannot
  simultaneously accept a settings language intent. Later opening uses the current
  resource owner. Their own queries, input, operation state and navigation are not
  mutated to accomplish localization. Cached constructor labels are migrated to
  declarative keys or resolved at the next ordinary render.
- Existing theme selection, save-on-change and shutdown drain semantics remain.
  Opening/rendering/applying resources never writes settings. Same-language renders
  do not broadcast another update. Missing declared resources are defects, not
  silent fallback text or a new per-language dictionary.

## Verification and migration

ADR-0061 selects the affected App build, settings admission/queue proof, relevant
presentation boundaries, literal-resource conformance and its positive/negative
fixtures. An isolated unshown copy of the real host verifies three languages and
System round trips, static/dynamic/virtualized text, UIA properties, later-opened
surfaces, stable controls/rows/scroll/selection and delayed or failed persistence.
Keep diagnostic substitutions and native limits explicit. No provider I/O,
filesystem operation, settings format or trust boundary changes in this work;
unrelated full mutation/provider suites add no relevant evidence.

Remove PrimaryLanguageOverride use, launch-only explanations and the old converter.
Update resource references atomically. Preserve .resw as the sole translation
source and retain all keys/placeholders. The conformance scan rejects missing
literal attached keys in every locale, absent explicit resource scope and the
removed cached mechanisms. Real keys/IME, speech, visible repaint and screen
acceptance are separate from hidden fixture geometry and dependency properties.

## Alternatives rejected

Late global override with cached loaders leaves stale text; clearing a global
setter is not needed. Rebuilding the window or replacing control item sources loses
interaction state. A second translated dictionary or embedded locale bundle would
duplicate the existing PRI. Generic reflection-based property assignment would hide
the supported dependency-property and localization-key boundaries.
