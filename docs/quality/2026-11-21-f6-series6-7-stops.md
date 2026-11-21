# F6 Series6/7 stopped warmups — 2026-11-21

Issue #491 / PR #492 remains Draft and its public update awaits destination
approval. This record changes documentation only. The historical A/B diagnostic
inputs remain frozen; no product, dependency or release change is made.

The invariant remains F6 through the existing command/session/gateway, provider
and rendering path. The sole TrialLedger admission requires saved preparation,
fresh observer/App identity and pristine fixture proof. Any unexpected caller or
input failure stops that series permanently, without replacement or pooling.

## Series6

The separately fixed plan used 16 fresh fixture pairs in A1/B1/B2/A2 order, one
excluded warmup and three measured samples per block. A1 App32420 completed the
excluded warmup through the current-call admission. Exact READY0/DONE0 and
observer exit0 were recorded. The completion checkpoint was saved before a caller
assignment to undeclared nativeDone threw ReferenceError. The guard stopped the
series; it was not resumed. Normal close produced Count1/Io.Count1, Errors0 and
ExtraReceipts0. One completed pair and 15 pristine pairs retain 32 matching hashes.

The completion-record-20261121 helper keeps the returned result in a local const,
then captures and records the UI after DONE. Its synthetic proof reproduces the
old post-DONE ReferenceError, passes the fixed complete callback path and verifies
that a post-DONE capture failure stops without retry. It issues zero native keys.

## Series7

Series7 fixed a new plan and 16 fresh pairs before launch, adopting that completion
helper with otherwise unchanged ABBA order, counts, metrics and stop conditions.
A1 App41320 completed exactly one excluded warmup. Saved preparation, sole F6,
matching DONE0, saved checkpoint, after-DONE UI and source0/target2 with both
hashes passed. The observer exited0 with empty stderr. Preparation of measured
A1/1 reached Space for the first item and Down. A later current-state refresh
confirmed that state.

The assistant then incorrectly called undefined nativeAction. JavaScript rejected
the identifier before invoking an action callback; no key or second F6 ran in
that cell. The error was explicitly routed through the existing guard, which
saved a stopped ledger with one key, one acknowledgment, no pending sample and
one completed warmup. This is a caller error, not a product failure. The series
was not resumed, and no measured sample or accepted comparison sample exists.

App41320 was normally closed. Independent OS lookup then found both owned App
and observer absent. Raw Count1/Io.Count1, Errors0/ExtraReceipts0, nine callbacks
and three closed render spans confirm the single warmup structure. All 15 unused
pairs remain pristine and all 32 fixture hashes match. Settings remain428bytes
with the original hash. Each A/B variant's1366 source and355 binary inputs,
34 helper inputs and194 historical fixture files remain unchanged. All45 transfer
files were copied to the D evidence archive with path/size/SHA256 verification;
the raw trace was also copied and hash-matched. The stopped series retains its
unmodified original outputs, fixtures, receipts, UI and controller evidence.

## Verification and disposition

Evidence is under
D:/NeNeCommander/evidence/f6-continuation-comparison-20261119:
series6-20261121, completion-record-20261121 and series7-20261121.
The matching D output root retains raw32420/raw41320 and both fixture sets.

Executed successful commands include:

    & 'C:/Program Files/nodejs/node.exe' 'D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/completion-record-20261121/check.mjs' 'D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/completion-record-20261121/proof-attempt1'
    python D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/series6-20261121/verify-freeze.py after-stop
    python D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/series7-20261121/verify-freeze.py after-stop
    pwsh -NoProfile -File C:/Users/info/WORKS/NeNeCommander/artifacts/issue491-prepared-bridge-20261121/close-series7.ps1

These are completed historical attempts, not instructions to overwrite artifacts.
The final command checks the stopped ledger, raw counts, exact observer handshake/
exit, all fixture hashes, unchanged frozen inputs and owned process absence before
archival. The full-block analyzer still requires four samples; it was neither
relaxed nor used to claim acceptance of these incomplete blocks.

The earlier independent review covers the current-call admission helper. It does
not constitute independent acceptance of these stopped series or the later caller
correction. No product build/test/full CI/mutation or extra native trial ran for
this record. No latency improvement, current-main performance, #453 closure,
PR402 screen acceptance or release result is claimed. The worktree, unfinished
outputs and transfer artifacts remain pending technical acceptance/integration;
no cleanup deletion was attempted.
