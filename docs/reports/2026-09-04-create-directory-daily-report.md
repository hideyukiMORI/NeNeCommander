# 日報 — 2026-09-04（F7 directory 作成）

Status: informational

## 本日の区切り

[Issue #34](https://github.com/hideyukiMORI/NeNeCommander/issues/34) / [PR #35](https://github.com/hideyukiMORI/NeNeCommander/pull/35) で、`F7` で active pane の location に directory を作る縦切りを完了し、squash merge した（main `b1fc92f`）。名前は Domain の `FileSystemPath.Child` で検証し、session が名前入力の modal state を持ち、gateway / port の作成経路を一つ足した（ADR-0020）。

本日はこの前に [Issue #24](https://github.com/hideyukiMORI/NeNeCommander/issues/24)（F5 copy）、[Issue #28](https://github.com/hideyukiMORI/NeNeCommander/issues/28)（Escape cancel）、[Issue #31](https://github.com/hideyukiMORI/NeNeCommander/issues/31)（進捗表示）も merge 済み。各日報は `docs/reports/2026-09-04-*.md` を参照。

## 完了したこと

- Domain: `FileSystemPath.Child(name)`（separator・`.`・`..`・空を拒否し、以後は `Parse` と同じ segment 規則）。
- Application: `CreateDirectoryRequest`（`Location` / `Target`、`InvalidName`）、`FileOperationEffectKind.DirectoryCreated`、`IFileOperationPort.CreateDirectoryAsync`、gateway の作成経路（inspect → cancel 観測 → 作成 → effect / progress `1 / 1`）、`OperationKind.CreateDirectory`、`OperationAwaitingName(location)`、`UserIntent.SubmitName` → `NameSubmission`（data を持つ唯一の intent）、`PaneSession.RefreshFocusingAsync`。`DualPaneSession` は awaiting name 中に `Escape` / `NameSubmission` 以外を凍結し、成功時は active pane を新しい directory に focus する。
- Infrastructure.Windows: adapter の `CreateDirectoryAsync`（identity revalidate、directory 以外 `NotFound`、reparse point `ProviderUnavailable`、containment 外 `Inspection`、既存名 `Conflict`）。
- Presentation: `NameEntryPresentation`（Hidden / Active）、create directory 系 `OperationStatus` 6 件、awaiting name も `Modal`。
- App: 名前入力 TextBox（`NameEntry`、既定 Collapsed）、`Modal` を text control の focus より優先する context、editor 表示中の `Confirm` を `SubmitName(text)` に置き換える転送、`RenderNameEntry`。resw 6 件。
- Docs: ADR-0020、ADR README、KEYBOARD_MODEL（KBD-002）、GLOSSARY。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- GitHub quality run（PR #35、`6805965`）: [`33810007684`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33810007684) 成功、3分22秒。
- - GitHub quality run（main、`b1fc92f`）: [`33810498280`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33810498280) 成功、3分8秒。
- ローカル deep review（`6805965`）: passed。
- tests: 288 passed、0 failed、0 skipped。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 95.77%、Presentation.WinUI 97.74%。
- mutation score: Domain 96.15%、Application 97.73%、Infrastructure.Windows 95.81%、Presentation.WinUI 100.00%。
- 実機: Release の exe を起動し、UI Automation で status 行の control と両ペインの一覧を確認。`NameEntry` は Collapsed のため UIA tree に出ない（想定どおり）。`F7` の実操作は hide の実機確認待ち。

## 気付き

- pre-commit で 3 回止まった。(1) CS-010: テストで `DateTime.UtcNow` を使った → directory の identity は子の作成で変わるので不要だった。(2) sed が CRLF を LF にした → `crlf.ps1` で正規化してから commit。(3) Application 99.29%: 1 source の request では gateway の「inspection が cancelled」分岐に到達できない → `InspectAsync` を直接呼ぶ形にし、`PaneSession` の loading 中 refresh のテストを足した。
- adapter テストで「子を作った直後の親」の identity 変化を timestamp に頼ると不安定。identity の revalidation は既存テストで証明済みなので、そのケースは外した。

## 残した注意点

- 名前入力の TextBox は placeholder の見た目。design handoff 待ち。
- editor の text は framework state で、window が `Confirm` に添える一点だけが window の判断（ADR-0020 に記録）。
- 前回からの注意点は継続（`docs/reports/2026-09-04-operation-progress-daily-report.md`）。

## 次の推奨縦切り

`F2` rename（同じ名前入力 modal と `NameSubmission` を再利用し、gateway に `RenameRequest` と port の rename step を足す）。詳細は [`docs/handoffs/2026-09-04-create-directory-handoff.md`](../handoffs/2026-09-04-create-directory-handoff.md)。
