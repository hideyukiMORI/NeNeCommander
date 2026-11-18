# ADR-0079: Publish the first Preview before the screen confirmation

Status: accepted

Date: 2026-11-18

## Context

hide requested an initial public release, then explicitly asked to publish it as
a Preview because the manual screen check cannot run until hide returns home.
This is the first public GitHub prerelease for people choosing to try the product.
ADR-0054's ordinary release confirmation remains unperformed in Issue #94.

## Decision

- The initial `0.1.0-beta.1` may be published as a GitHub **Preview prerelease**
  before #94. Its release notes and distribution README state that the screen
  confirmation has not run; they do not imply stable or visually verified status.
- #94 stays open for hide's later check. No automation changes display or
  accessibility settings. The scoped permission does not approve PR #402's
  separate bookmark-screen change or waive its manual acceptance.
- Accepted source, verified shipping artifacts, dependency audit, applicable
  successful deep review within 96 hours, successful CodeQL analysis and zero
  open owned alerts remain required. No executable gate or threshold changes.
- This supersedes only ADR-0054's timing for this first Preview. Later releases
  retain its confirmation requirement unless hide makes a new explicit decision.

## Consequences and proof

The Preview discloses the unperformed screen and environmental checks. A unit
test, packaging success or prerelease publication never counts as #94 evidence.
QLT-009 and the publishing guide reference this scoped decision. There is no
production-code or executable-proof change in this decision.

## Alternatives and migration

Waiting for #94 would contradict hide's current Preview request. Declaring a
stable release or marking #94 passed would claim evidence that does not exist.
The existing beta version and single publishing path are retained; no alternative
build or permanent manual-check waiver is introduced. The quality policy,
publishing guide, package README and current state carry this scope together.
