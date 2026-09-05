# Commit and Repository Conventions

Status: normative

NeNe Commander follows the NENE2 Issue-driven lifecycle. Repository changes have one traceable path: Issue, focused branch, coherent commits, pull request, required checks, squash merge, and synchronized clean `main`.

### GIT-001 — Every change starts from one Issue

- Status: **active**
- Enforcement: agent protocol, pull-request template, and review.

An Issue states the problem and evidence, intended outcome, affected rules and modules, acceptance criteria, verification plan, and excluded work. Read-only exploration may occur without an Issue. The repository bootstrap and Issue #1 initial vertical slice are the sole initialization exception.

### GIT-002 — Branch names have one form

- Status: **active**
- Enforcement: local review and repository rules.

Branches use `<type>/<issue-number>-<short-kebab-summary>`, for example `feat/12-wsl-directory-listing`. Direct development on `main`, force pushes, and default-branch deletion are prohibited after the initialization exception.

### GIT-003 — Commit messages have one form

- Status: **active**
- Enforcement: repository `commit-msg` hook and pull-request review.

Commits use Conventional Commits in this exact form:

```text
<type>(<optional-scope>): <Japanese description> (#<issue-number>)
```

Allowed types are `feat`, `fix`, `docs`, `refactor`, `test`, `build`, `ci`, and `chore`. Type, optional lower-case scope, `BREAKING CHANGE`, and other Conventional Commit keywords remain English. The description and explanatory body are Japanese. Use `!` before the colon and a `BREAKING CHANGE:` footer for an incompatible public contract change. The subject is at most 100 characters.

### GIT-004 — Pull requests are the integration boundary

- Status: **active**
- Enforcement: pull-request template, required checks, and repository settings.

Each pull request contains purpose, change summary, canonical path, rule IDs, verification results, waivers, remaining risks, and `Closes #<issue-number>`. It contains one focused work unit. Squash is the only merge method. After merge, return local `main` to the clean synchronized remote state.

Use draft PRs during implementation and review. Under QLT-015, Draft to Ready requests the single full integration gate. Commits use lightweight checks; do not combine unrelated work or suppress commits to avoid full-suite cost. Changed head/base inputs require renewed validation before merge, as specified in `docs/DEVELOPMENT_WORKFLOW.md`.

The initial direct `main` publication exists only because no remote default branch was available. It must use the Issue #1 commit form and pass the complete canonical gate before push.
