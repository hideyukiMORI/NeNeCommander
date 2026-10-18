# CS-013 standalone tool handoff — 2026-10-18

Issue [#324](https://github.com/hideyukiMORI/NeNeCommander/issues/324) implements
accepted ADR-0075, `cs013-v1`, and the sole `eng/measure-complexity.ps1` entry.
It supersedes the temporary #322 audit as the maintained measurement. Product
refactors and global gate wiring remain subsequent work; no threshold, package,
product API, suppression or gate/CI stage is changed here.

The [measurement](../quality/cs013-measurement-2026-10-18.md) records 722 sources,
1,162 types and 7,804 callables: 77 product and 103 test findings, zero engineering
findings. All five metrics are measured. Thirteen fixed ABI parameter lists retain
raw counts/reasons; eight managed mirrors remain owned findings. The 7,222
unresolved invocations expose the source-only model's limits, not compiler success.

Final tool build, 24 fixture groups, seven launcher success/failure cases and
changed-folder formatting pass. Inspection intentionally returns 1 for 180
findings. Reuse these results while tool and source inputs remain unchanged.
The [daily report](../reports/2026-10-18-cs013-tool-daily-report.md) records exact
commands, development corrections, failed attempts and selected ADR-0061 scope.
Normal Commit checks are required; product build/tests, full/deep CI, coverage,
all-layer mutation and live environment tiers are not selected. #94 stays separate.

Continue with the responsibility-specific rows in the #322 adoption plan, beginning
with pure Presentation. Resolve all retained findings before proposing one common
gate through the existing mechanism. Never grandfather current findings or
change native signatures to satisfy project-controlled parameter design.

Permanent reports, raw metrics, tool/input hashes and launcher proof are under
`D:/NeNeCommander/evidence/cs013-tool-20261018/`. The Issue PR and `completion.json`
own final squash/main/cleanup facts. After integration, remove only the task's
`D:/NeNeCommander/worktrees/issue-324` and `D:/NeNeCommander/outputs/issue-324` after
the prescribed checks. Branch/commits, permanent evidence and shared SDK remain.
If automatic approval rejects cleanup, do not retry; carry exact targets and the
prepared hide command under the mandated cleanup marker. No rejection is recorded
at this implementation checkpoint. Prior #322 is integrated through PR #323 with
no additional worktree/output or pending cleanup.
