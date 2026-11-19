# Bookmark keyboard usability review

Status: implementation specification for Issue #457; hide's PR402 screen acceptance remains pending

Date: 2026-11-19

hide requested sustained milestone work, with keyboard-only Bookmark usability first, multiple
GUI/interface expert personas and background design consultation with NeNe Commanderリナ.
This review amends the behavior details of [ADR-0080](../adr/0080-bookmark-manager-keyboard.md)
and preserves the approved Direction A and existing semantic tokens.

## Evidence and provenance

- Normal main `189bb61` and the unaccepted PR402 feature `8c1c551` were compared separately.
  Main still has the older button-centered manager. PR402 already implements browse movement,
  region switching, add/edit/delete keys, explicit deletion confirmation and F1 help.
- The packet contains both complete keyboard-model documents and references the sole key map,
  translator, bookmark decisions and App views. The approved Direction A HTML is a mock.
- Two actual captures from 2026-11-16 at 125% show the feature's **closed** modal and shell at
  normal/narrow widths. A current normal-main shell capture was also preserved. These images
  demonstrate shell/footer clipping; none demonstrates modal layout, focus, IME or native popup
  behavior. Initial Computer Use discovery returned no windows; a later app selection allowed
  the current shell capture. No successful modal check is inferred from that recovery.
- Raw consultation outputs and input copies are retained under
  `D:/NeNeCommander/evidence/bookmark-ux-20261119/`. The packet records each image's provenance.
  UI proof, when obtained, is recorded separately from these source-based findings.
- A subsequent actual added-bookmark draft capture, `native-draft.png`, shows the complete
  form and Ctrl+Enter button label at the current desktop size. The interaction reviewer
  identified ambiguous `Saved` wording beside an unsaved draft; the footer now labels this
  separate channel `Settings:`. Native input/focus findings, fixes and the later Computer Use
  failure are recorded in the [quality record](../quality/2026-11-19-bookmark-keyboard-usability.md).

## Consultation and discussion

Three read-only specialist reviews covered keyboard productivity, interaction/information
design, and accessibility. They exchanged the conflicting recommendations before the final
implementation specification. They changed no code and ran no app, build or test.

The requested Claude `/design` invocation actually ran. In the installed CLI it is an access
management command offering consent/revoke, not a design consultation skill; the first result
stopped without reviewing the packet. A follow-up consultation used the installed design
critique, accessibility review and handoff skills over local inputs, with no cloud design
project access or permission change. NeNe Commanderリナ reported those skill invocations and
reviewed the provided shell images/mock/source. Her second-round result accepted the combined
specification and withdrew her initial ordinary-Enter-save recommendation.

| Finding | Decision after discussion | Evidence still needed |
|---|---|---|
| Drafts require moving through controls to save; ordinary Enter also belongs to selectors/buttons | Draft-only Ctrl+Enter in the canonical bookmark table. Enter remains native. Save buttons and F1 read the same key declaration. No binding added to deletion confirmation. | Native button/selector behavior and keyboard walk |
| Only search monitored composition; Escape could cancel a draft during IME | Monitor search, name, path and category name at the input boundary. Open ComboBoxes own input. Retain native Enter/Escape presses in the existing repeat guard so release, not owner change, permits the next command. | MS-IME composition and popup routing |
| Search Enter returns to list while hints still advertise opening/editing | Keep the two-step search contract: Enter/Down/Escape returns to results with query retained; a fresh Enter opens. Show only search-specific return hints. | Search/native Tab/hint changes and held Enter |
| First entry, filter changes and saved drafts can have no selected row | On list entry, retain the visible selection or select the first row through Select; zero rows remain empty. Defer selection with focus so the previous command has released the work owner. Save selects the accepted replacement and explains clearing the search. Cancel preserves return context. | Native selection/focus and no unintended persistence |
| Both columns use active selection styling | Use the existing active/passive pane markers, with an outline on the focused region including empty lists. Search makes both lists passive. | Modal image and high-contrast interpretation |
| Form/confirmation Tab can escape to background controls | Cycle Tab/Shift+Tab in the panel. Keep browse Tab's existing two-region contract. Scroll form/confirmation bodies while retaining the action footer. | Both drafts, both confirmations, navigation failure |
| Input labels and duplicate row names lack explicit accessible association | Associate labels; include category in the short row name and put complete path/slot in HelpText. Preserve existing AutomationIds. Notify only changed, nonempty status text. | UIA readback and Narrator, including repeated status |
| Slot conflicts are discovered only at Save | Show existing assignment names in slot choices; preserve collision rejection and never reassign another bookmark implicitly. | Selector content and rejected draft retention |

No new numeric assignment keys, nested F1 scope, dirty-draft confirmation or alternate
persistence/navigation route is introduced. Category editing still returns to the bookmark
list as ADR-0080 already specifies. A drive root's registration name defaults to its canonical
path; initial name text is selected for replacement. Missing pane locations remain empty.

## Related footer issue

Issue #454 is common to normal main and PR402. Its proposed separate change reuses
`KeyHintWrapPanel` for the shell hints, keeps each key and label together, and adds a persisted
key-help visibility setting defaulting to on. Progress, result and warning content remain
independent. The initial suggestion to cap at two rows and hide lower-priority keys was not
accepted for implementation without width/height evidence; first measure wrapping with all
keys visible. Keep F1 discoverability when hints are hidden. No footer implementation or
measurement is claimed by this design document.

## Focused proof and limits

Use affected Application bookmark state/persistence tests, Presentation bookmark/keyboard/help
tests, App Release build and view ownership proof. Preserve failing-before evidence for the
save-return defects. The input/model/schema/provider/dependency boundaries outside this feature
are unchanged, so this review does not request full regression or all-layer mutation.

Source inspection cannot establish native Popup routing, real IME, focus cycling, Narrator,
narrow layout or scaling. Check these on the owned app and record unavailable cases explicitly.
No consultant or automated check substitutes for hide's pending acceptance; PR402 stays Draft.

The actual check found that empty lists and state transitions need explicit focus handling.
ListViews are tab stops, and the existing deferred-focus owner distinguishes requested state
transitions from passive row restoration. Layout-driven focus does not cancel an explicit
request. Later pointer input or initial native Tab does cancel it; a pending form/confirmation
initial focus consumes Enter until dispatch, while IME and open selectors retain native input.
The independent keyboard review checked the later-input and native-owner boundaries. These
final hooks still require the interrupted native walk; unit proof is not a substitute.

Framework references: [focus navigation and Cycle](https://learn.microsoft.com/en-us/windows/apps/develop/input/focus-navigation),
[TextBox IME composition lifecycle](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.textbox.textcompositionstarted),
and [keyboard interactions](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-interactions).
