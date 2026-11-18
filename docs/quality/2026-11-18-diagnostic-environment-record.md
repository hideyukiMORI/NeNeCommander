# Diagnostic environment record — Issue #443

The accepted #439 comparison exposed a filename collision: the wrapper first
wrote its environment allowlist to `environment.json`; `Invoke-PaneCommand` then
used the same path for the command named `environment` (`dotnet --info`). The
command journal replaced the CPU/culture/timezone details. The actual CI artifact
and its recorded limitation remain unchanged.

The one-line correction writes initial details to `environment-details.json`.
The existing command journal remains `environment.json`, with output in
`environment.log`. Fields, commands, fixed diagnostic inputs, dispatch guards,
thresholds, restoration and artifact collection are unchanged. No product or
settings schema changes. This tool change is outside Preview source `46f1a82`.

## Selected proof

Only the changed logging boundary is exercised. The permanent proof script parses
the original `43e857e` wrapper and the corrected wrapper, extracts each actual
environment assignment/save statement, executes those statements, then invokes
the unchanged `Invoke-PaneCommand` under its real `environment` name with a
harmless child returning 0. It does not run the guarded diagnostic entry point.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-443/prove-environment-record.ps1
```

Exit 0: the baseline metadata hash changes after the journal write; the corrected
metadata hash remains identical. Both journals retain their name, completed
status and exit 0. Both wrapper ASTs parse without errors. The persisted JSON
contains the initial field inventory and before/after hashes for both cases.
The proof's console projection printed null columns for ordered dictionaries;
the persisted JSON was read directly and confirms the values above. No proof was
repeated to change that display.

Evidence: `D:/NeNeCommander/evidence/issue-443/` contains the exact proof script,
raw output and `environment-record-proof.json`; the controlled records are in
`D:/NeNeCommander/outputs/issue-443/record-proof/` until preservation and cleanup.
Normal commit hooks and the focused diff are the remaining integration checks.
No Stryker, product restore/build/tests, CI replay or release rebuild is selected.
The old hosted environment values are not reconstructed or declared recovered.

Rules: QLT-009/012/015, SEC-011, GIT-001–004, ADR-0061. Waivers: none.

Integration: normal conformance/security/commit-message hooks passed at `307899c`,
and PR444 merged as `43f3f26`. The five proof output files were archived with
matching hashes. Git removed the worktree; output deletion was rejected before
launch (`blocked by policy`) and is left for hide through the prepared command.
