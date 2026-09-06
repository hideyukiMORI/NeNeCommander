# Daily report — canonical path-length boundary — 2026-09-07

Status: informational

## Goal and invariant

Issue #103 refines the single `FileSystemPath.Parse` contract through accepted ADR-0042. Both raw input and every successful `CanonicalText` are limited to 32767 UTF-16 code units. Provider normalization, identity, and pure no-I/O parsing remain unchanged, and callers do not duplicate the policy.

## Failure-first evidence

Against base `a4532b38231a4ec543116d412143436f2b92f882`, five focused fixtures produced two expected passes and three failures. Exact canonical-length-32767 UNC and WSL paths already parsed and reparsed successfully. The old parser incorrectly accepted canonical length 32768 after a UNC trailing-root-separator expansion and after a legacy WSL alias expansion, and it accepted a raw-length-32767 WSL alias whose canonical form has length 32776.

The exact failure-first command was:

```powershell
dotnet test tests/NeNeCommander.Domain.Tests/NeNeCommander.Domain.Tests.csproj -c Release --filter "FullyQualifiedName~ParseWhenUncCanonicalText|FullyQualifiedName~ParseWhenWslAliasCanonicalText|FullyQualifiedName~ParseWhenMaximumRawWslAlias"
```

It exited 2 with 2/5 passed and the three overlong-canonical fixtures failed because each received `PathParseSuccess` instead of `PathParseFailure`.

## Change and focused proof

`FileSystemPath.Parse` now inspects its one provider-normalized outcome and converts only an overlong successful canonical result to `TooLong`. The same five-test filter passes 5/5 after the fix. FS-001, SEC-002, ADV-002, and ADV-015 now state and map the refined boundary.

## Remaining integration proof

The whole Domain suite passes 71/71. A test-owned Release coverage run reports 100.00% Domain line and branch coverage. Commit mode passes conformance for 112 unique normative rules, validates all 18 registered adversarial cases, and finds no secret, supply-chain, or whitespace failure.

Whole Domain mutation, independent review, exact-head security deep review, and canonical Ready CI remain pending. A skipped or pending tier is not a passing result.
