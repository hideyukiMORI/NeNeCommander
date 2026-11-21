# AppSDK non-UI progress report — 2026-11-22

Issue514 repairs the public PR498 locked-restore failure by updating the existing
central AppSDK pin and both App/Architecture locks. App's graph matches PR498;
27 target-specific entries change, with no package names added/removed and no
product/test source changes. See the [quality record](../quality/2026-11-22-appsdk-lock-consistency.md).

Both locked restores and affected Release builds pass with zero warnings/errors.
Architecture25 and selected Presentation53 cases pass, zero skips. All1,017
source/test/build/tool input hashes remain unchanged. All11 logs/TRX files are
archived and fully verified; generated builds remain in the unfinished worktree.

This is completed non-UI proof, not native SDK acceptance. Integration and PR498
closure remain held for startup/shutdown, caption/resize, keyboard/IME/focus and
tooltip checks in an isolated environment or specifically supplied manual proof.
Readiness still requires dependency-review. No App launch, shared input, full CI,
mutation, release or cleanup deletion occurred. PR402/#491/#453 remain separate.
