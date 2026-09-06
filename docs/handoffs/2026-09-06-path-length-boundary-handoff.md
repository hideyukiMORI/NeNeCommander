# Handoff — path-length boundary proof — 2026-09-06

Status: informational

## Scope

Issue #89 is a test-only correction based on verified main `4b01d286cea2a823dddddd87aca0c611bbb2d0ff`. It adds no parser path, dependency, threshold change, suppression, exclusion, or baseline. The sole protected contract is acceptance of a valid 32767-character input and `TooLong` rejection at 32768 characters.

## Evidence

Before the test change, focused complete mutation left `FileSystemPath.cs` equality mutant 30 alive: replacing `input.Length > MaximumPathLength` with `>=` had no killing test. After the change, Domain tests pass 66/66, Domain line and branch coverage are 100.00%, the focused report marks mutant 30 killed by one test, and whole-Domain complete mutation passes at 96.48%.

The observed whole-layer score includes non-deterministic classification of unrelated hash-code mutants already seen between Issue #81 runs. The new boundary mutant is deterministic, and even the earlier repeatable 189/199 result becomes 190/199 (95.48%) when this single proof is applied. No hash-value assertion was added.

## Integration order

Keep the PR Draft through review and exact-head security evidence. After its final canonical Ready gate succeeds, integrate Issue #89 before rebasing Issue #85. The rebased WSL transfer candidate then requires a fresh deep review and Ready canonical gate.
