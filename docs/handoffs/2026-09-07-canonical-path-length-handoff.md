# Handoff — canonical path-length boundary — 2026-09-07

Status: informational

## Scope

Issue #103 is an independent correction based on main `a4532b38231a4ec543116d412143436f2b92f882`. ADR-0042 changes the earlier raw-input-only contract so both raw input and successful canonical text are at most 32767 UTF-16 code units. It intentionally rejects previously accepted legacy WSL aliases and UNC roots when canonicalization expands them past that boundary.

The sole production change remains inside `FileSystemPath.Parse`. There is no bookmark-local check, alternate parser, dependency, filesystem access, provider-identity change, suppression, exclusion, baseline, or threshold change.

## Proof

Failure-first tests independently cover UNC root-separator expansion, legacy WSL alias expansion, and the raw-length-32767 alias that previously produced canonical length 32776. Exact canonical length 32767 remains accepted for UNC and WSL, and each accepted canonical value reparses to the same text and identity. The focused five-test filter changed from three failures to 5/5 passing.

The whole Domain suite passes 71/71 and Domain line and branch coverage are 100.00%. Commit mode passes 112-rule conformance, the 18-case adversarial registry, security and supply-chain checks, and whitespace inspection.

Whole Domain mutation, independent review, exact-head security deep review, and canonical Ready CI are pending and must be recorded before integration.

## Integration

Keep the pull request Draft through independent review and exact-head deep evidence. Integrate Issue #103 before rebasing Issue #99 so bookmark paths consume the corrected sole parser boundary without a local compatibility check. Any final head or base change requires fresh evidence under the repository workflow.
