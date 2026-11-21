# AppSDK non-UI handoff — 2026-11-22

Issue514 / branch build/514-appsdk-lock-consistency starts from main e2f675b.
The [quality record](../quality/2026-11-22-appsdk-lock-consistency.md) owns the exact
commands, graph review and native limitations. Only central AppSDK pin and both
App/Architecture locks change. No product/test source, API, gate or settings change.

Non-UI proof is complete: locked restores, zero-warning/error builds,
Architecture25 and Presentation53 tests;1,017 pre/post input hashes match. Reuse
successful results for unchanged relevant inputs. D:/NeNeCommander/evidence/appsdk-270-20261122
holds graph rows, commands, input hashes and11 fully archived/verified logs/TRX.
The earlier PR498 failure is retained separately under appsdk-270-20261121.

Do not merge or close PR498 until native startup/shutdown, caption/resize,
keyboard/IME/focus and tooltip acceptance is established. The current shared
desktop is not a test target; no isolated VM/test PC is available. No native or F6
sequence may be resumed based on this record. At Ready, dependency-review must
pass for the final dependency inputs. Published beta.2 and PR402 remain untouched.

Retain these unfinished paths: D:/NeNeCommander/worktrees/issue-514,
D:/NeNeCommander/outputs/appsdk-270-20261122, D:/NeNeCommander/worktrees/pr-498-review,
and D:/NeNeCommander/outputs/pr-498-review. Keep source/build artifacts until
acceptance and archive verification, then use the normal exact-target cleanup
checks. No cleanup rejection or deletion occurred in Issue514.
