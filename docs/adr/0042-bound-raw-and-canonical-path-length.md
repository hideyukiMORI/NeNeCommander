# ADR-0042: Bound both raw and canonical filesystem path text

Status: accepted

Date: 2026-09-07

Accepted under hide's delegated implementation authority and adopted by the NeNe Commander design owner for Issue #103.

## Context

`FileSystemPath.Parse` is the sole boundary from path text to a provider-qualified value. Before this decision it rejected raw input above 32767 UTF-16 code units but did not apply that limit to the canonical result. Canonicalization can add text: a UNC provider root gains its trailing separator, and legacy `\\wsl$` is persisted as `\\wsl.localhost`. A raw WSL alias at the prior exact input boundary could therefore produce `CanonicalText` of length 32776.

Issue #89 truthfully protected the former contract: valid input of length 32767 was accepted and raw input of length 32768 was rejected. It did not decide a canonical-result limit. A caller-specific guard would leave other parser consumers exposed and create a second path policy.

## Decision

- Both raw input and every successful `CanonicalText` are limited to 32767 UTF-16 code units.
- `FileSystemPath.Parse` keeps its raw preflight and applies one result check after provider normalization. A successful provider parse whose canonical text is longer than the limit becomes `PathParseFailureKind.TooLong`.
- An accepted path remains closed under parsing: parsing its `CanonicalText` succeeds with the same canonical representation and provider identity.
- The parser remains pure. Provider selection, normalization, identity rules, segment validation, and the no-filesystem-I/O contract do not change.
- Callers, including bookmark validation, rely on the parsed result and do not add their own path-length checks.

## Rejected alternatives

- Keeping only the raw-input guard allows canonical display and persistence values to exceed the parser's fixed boundary.
- Reserving enough raw capacity for the longest known prefix expansion couples the public bound to one alias and unnecessarily rejects shorter canonical results.
- Adding guards to bookmarks, settings, or provider adapters duplicates the sole parser policy and leaves other consumers inconsistent.

## Consequences

Valid raw inputs of length at most 32767 that expand beyond the canonical limit are now rejected. In particular, some legacy WSL alias inputs and UNC roots without a trailing separator change from success to `TooLong`. The existing Issue #89 guarantee still applies when valid input at the raw boundary also remains inside the canonical boundary.

No dependency, filesystem access, provider capability, identity rule, suppression, exclusion, baseline, or threshold changes.

## Migration and removal

The parser, FS-001 and SEC-002 text, ADV-002 and ADV-015 defenses, and boundary tests change together. There is no compatibility path or caller-local fallback to remove.

## Executable proof

`FileSystemPathTests` separately proves exact canonical length 32767 acceptance and 32768 rejection for a UNC root-separator expansion and legacy WSL alias expansion. It proves the formerly accepted raw-length-32767 WSL alias is rejected when its canonical value would be 32776, and reparses each accepted exact-boundary canonical value to the same text and provider identity. Existing raw 32767/32768 tests continue to protect the input boundary. The adversarial tests remain mapped to ADV-002 and ADV-015, and the final candidate requires exact-head security deep review and the canonical Ready gate.
