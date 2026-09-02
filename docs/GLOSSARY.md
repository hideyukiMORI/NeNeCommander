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
| design token | A semantic resource such as surface, spacing, typography, or state color. |
| gate | An executable check called by `eng/check.ps1` locally and in CI. |
| waiver | A narrow, owned, expiring exception that does not weaken a protected invariant. |
