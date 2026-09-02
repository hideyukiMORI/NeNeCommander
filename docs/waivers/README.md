# Waivers

Status: normative

A waiver is a temporary exception for a narrow rule whose invariant remains protected by compensating proof. It is not an alternative implementation path and not a way to make a failing gate green.

## Allowed scope

A waiver may cover one exact rule ID, owner, reason, file/member scope, compensating control, removal condition, and expiry of at most 30 days. Copy `0000-template.md`, allocate the next identifier, and keep the active file in this directory. Expired waivers fail conformance.

## Never waivable

- compiler or nullable errors;
- analyzer or formatting suppressions;
- warnings-as-errors;
- the canonical gate or negative gate proofs;
- dependency direction or undeclared dependencies;
- the single filesystem mutation gateway;
- destructive-operation confirmation and root containment;
- secret handling;
- production code during the policy-foundation stage.

When the removal condition is met, delete the waiver in the same change that removes the exception. Long-lived policy belongs in an ADR and the normative rule itself, never in a renewed waiver chain.
