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
| operation activity | The closed `OperationActivity` of the dual-pane session: idle, running, awaiting confirmation, completed with a gateway outcome, or request rejected. |
| design token | A semantic resource such as surface, spacing, typography, or state color. |
| gate | An executable check called by `eng/check.ps1` locally and in CI. |
| waiver | A narrow, owned, expiring exception that does not weaken a protected invariant. |
