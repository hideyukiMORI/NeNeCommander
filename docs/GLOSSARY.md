# Glossary

Status: normative

Use these terms in code, documentation, tests, telemetry, and UI resources. Do not invent synonyms for the same concept.

| Term | Exact meaning |
|---|---|
| pane | One of the two file-list surfaces. |
| active pane | The sole pane that receives navigation and file-operation intents. |
| passive pane | The other pane; the default destination for cross-pane operations. |
| focus item | The single item addressed by movement and open commands. |
| selection | The explicit set of items marked for a batch operation. |
| filesystem path | A validated `FileSystemPath` value with a known provider boundary. |
| provider boundary | `WindowsLocal`, `WindowsUnc`, or `Wsl`, including capabilities and semantics. |
| intent | A UI-independent request for behavior. |
| command | Application-layer orchestration of one intent. |
| operation | A filesystem mutation executed only by `FileOperationGateway`. |
| outcome | A closed typed success, cancellation, conflict, or failure result. |
| entry | One direct child of a read location with its validated path, provider-reported name, and closed kind. |
| listing | An immutable `DirectoryListing`: the deterministically ordered entries of one location plus its completeness and unrepresentable-entry count. |
| entry boundary | The positive number of provider entries after which a read stops and reports a bounded listing. |
| pane snapshot | An immutable `PaneSnapshot`: the pane's closed content (absent or listed) and closed read activity (idle, loading, failed, cancelled). |
| pane session | The sole `PaneSession` coordinator that owns one pane snapshot and advances it through intents and reads. |
| pane side | `PaneSide.Left` or `PaneSide.Right`; the closed identity of one pane surface. |
| operation activity | The closed `OperationActivity` of the dual-pane session: idle, running with progress, awaiting confirmation, awaiting a name, completed with a gateway outcome, or request rejected. |
| operation progress | The closed `FileOperationProgress` the gateway reports once per source whose every step completed: completed and total source counts. |
| design token | A semantic resource such as surface, spacing, typography, or state color. |
| key binding | One declared entry of the canonical key map: a keyboard context, a layout-translated key, its explicit modifier state, and the single intent it emits. |
| key hint | One displayed shortcut: the localization resource naming a key cap and the one naming what the key does. Hints are generated from key bindings, never written into a view (KBD-005). |
| operation bar | The single full-width surface at the bottom of the shell. It shows the operation status with its closed `OperationBarTone`, the closed `OperationDetail`, the name entry, and the key hints of the current keyboard context. |
| color scheme | One of the eight approved `ColorScheme` members. Each has a kebab-case identifier, a closed `Dark` or `Light` appearance, and exactly one resource dictionary `Themes/Schemes/<identifier>.xaml` that defines every color key. |
| settings document | The sole persisted preferences file `%LOCALAPPDATA%\NeNeCommander\settings.json`, shaped `{ "schemaVersion": 1, "showHiddenItems": <boolean>, "colorScheme": "<identifier>" }`. It is read through `ISettingsStore`, never written by the application, and an absent or rejected document keeps `UserSettings.Default`. |
| gate | An executable check called by `eng/check.ps1` locally and in CI. |
| waiver | A narrow, owned, expiring exception that does not weaken a protected invariant. |
