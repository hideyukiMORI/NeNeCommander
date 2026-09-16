# 引き継ぎ書 — 2026-09-17（ADR-0051 着手 checkpoint）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書、Opus 5 のバックグラウンド NeNeリナが実装・ゲート運転。
前日の引き継ぎ書（2026-09-16）の「体制」「確定した事実」「再開時の注意」はすべて有効。ここでは差分だけ書く。

## main の状態

- baseline: 本 docs 閉塞 PR #137 の squash commit（merge 後に確定）。その下に `634906f`（09-16 docs 閉塞）、`9c96f58`
  （#130 Stryker 5.0.0）、`0867971`（#129 CodeQL action）、`e7c89d2`（#93）。
- 真の score（main deep `35120031829`、Stryker 5.0.0 vstest）: Domain 95.52% / Application 95.87% /
  Infrastructure.Windows 90.86% / Presentation.WinUI 94.30%。Domain の余裕は 1 mutant。
- open CodeQL alert ゼロ。open PR は無し（#135 は PR 未作成）。

## hide の決定（2026-09-17）

1. ADR-0050 の挙動を承認: `Ctrl+W` で持続モード、上部中央ヘルパー、`h j k l` 移動、`+` / `-` 左上固定の
   拡大縮小、`m` / `r` 冪等 2 命令、`Esc` / `Ctrl+W` で抜ける、巻き戻し無し。
2. ADR-0051（`CommanderSession` 分割）への着手を承認。#100 の前提。
3. CS-013 超過の他 5 型（`WindowsLocalSettingsStore` 580、`CommanderWindow.xaml.cs` 509、
   `FileOperationGateway` 412、`WindowsLocalFileOperationAdapter` 327、`BookmarkManagerView` 312）と
   CS-013 のゲート化は別 Issue（未起票、候補のまま）。

## ADR-0051 の checkpoint

| 項目 | 値 |
|---|---|
| Issue | #135 `refactor(session): CommanderSession を scope owner に分割する` |
| branch | `refactor/135-commander-session-scope-owners`（未 push） |
| worktree | `C:\Users\info\WORKS\NeNeCommander-135`（clean） |
| head | `634906f`（= main、コード変更ゼロ） |
| 済んだ項目 | Issue 起票、branch / worktree 作成、残手順 7 項目の Issue コメント |
| 残手順 | owner 2 型 + record 2 型 + `InteractionOwnership`、ctor 3 引数化と生成箇所 5 つ + reflection 型配列、`CommanderWindow.xaml.cs:155` / `:245` の `snapshot.Scopes.…` 化、null-guard 追加、docs 6 ファイル、commit → push → Draft PR → dependency / exact-head deep / CodeQL 0 → Ready → canonical → merge → main deep |

設計側で決めた明確化（ADR-0051 本文に反映済み）: `CommandPaletteSession.Open` と
`AddressEditorSession.BeginEdit` は pane snapshot と `InteractionOwnership` の 2 引数（address は side を
加えて 3 引数）。`InteractionOwnership` は session が `PaneInteractionIsFrozen`、`AnyPaneExternalWorkIsRunning`
（address は `AnyPaneReadIsRunning`）、settings editor、他 scope の状態から導く。owner はこれらの述語を複製しない。

実装前の読み込みで確定した事実: scope 別の行番号は `634906f` でも依頼書どおり。`*.CloseFocusing` と scope
state の ctor は `internal` なので同 assembly 内の owner から可視性変更なしで使える。`NullGuardTests.cs:254-256`
の reflection は ctor 3 引数化で必ず書き換え。`GLOSSARY.md` は 2 カラム表で、`settings editor` の近くに入れる。

実装リナに渡した仕様と受入条件は Issue 本文と `docs/adr/0051-commander-session-scope-owners.md` が正本。
要点: owner は `SettingsSession` 形（自分の状態と lock だけ、判定値は引数、`InteractionOwnership` は closed
record、boolean 引数禁止、4 引数以内）、dispatch と pane 副作用は `CommanderSession` に残す、3 つの順序
不変条件（activate 後に editor open、`CloseFocusing` 後に navigate、`Closed` 後に dispatch）、record は
明示 ctor（positional 禁止）、既存 49 テストの本体は不変、論理行数はテスト化せず PR に記録、exact-head deep
必須、`CommanderSession` が 300 を超えたら merge しない。

## 再開手順

1. ADR-0051 の checkpoint を続きから: worktree で `git fetch origin` → main が進んでいれば `git merge
   origin/main` → 残手順を実装 → targeted tests → `eng/check.ps1 -Mode Commit` → push → dependency-review
   と exact-head deep → 4 層 score 読み戻し → CodeQL branch スコープ 0 → Ready → canonical → squash merge
   `refactor(session): CommanderSession を scope owner に分割する (#<n>) (#<PR>)` → main deep。
2. ADR-0051 merge 後、ADR-0050 を accepted にする docs 変更を #100 の PR に含め、#100 の実装に入る。
   **最初に runtime spike**（focus sink への `CharacterReceived` / `PreviewKeyDown` 到達、`MoveAndResize`
   32 px @100%、`PreferredMinimum*` が未宣言で 0）。spike はアプリを起動してキーを送るので、hide が席を外して
   いるか合図をもらってから行う（foreground 検証付き）。失敗したら ADR-0050 を改訂（fallback は事前承認なし）。
3. #100 本体: ADR-0050 の Migration をそのまま仕様に。exact-head deep は不要（native 境界なし）だが、runtime
   証拠（`AppWindow` bounds 前後、screenshot、helper の UIA name）を PR に記録する。設計 handoff は
   `docs/design/2026-09-16-window-adjustment-handoff.md`、キャンバスは claude.ai artifact
   `MBae6L9KmhyTSkXKVPbhMt`（参照用）。
4. その後 #94（専用環境と hide の在席が要る）。

## 候補（hide の承認後に起票）

`OpenBookmarks` の catalog 追加、`_defaultFileListFocusSuppressedState` のリセット条件、CS-013 の他 5 型と
ゲート化、`verify-coverage.ps1` の stale build 対策、WSL link entry 削除の capability ADR、Presentation
`AsyncWorkOwner.cs:139` survivor の behavior test。

## 後始末（hide）

統合済み worktree とローカル branch: 09-11 分に加え `-issue93` `-129` `-130` `-133`、および本 docs 閉塞の
worktree。ADR-0051 の worktree は作業中なので残す。
