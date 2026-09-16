# 引き継ぎ書 — 2026-09-16（#93 統合、dependabot 置換、ADR-0050 / ADR-0051 草案）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、Opus 5 のバックグラウンド NeNeリナが実装・
ゲート運転・診断・批判的レビューを担当する。今回は統合担当・調査/置換担当・レビュー担当の 3 名を並行で
使い、設計草案はレビュー担当に「問題だけを根拠付きで」出させて改稿した（ADR-0050 は 3 巡、ADR-0051 は
1 巡）。実装リナが規則外のブロッカーに当たったら停止して報告し、設計側が判断と文書を渡す運用は不変。

## main の状態

- baseline: `9c96f58`（#130 / PR #132 の squash）。その下に #129 `0867971`、#93 `e7c89d2`、docs `f7d06c1`。
- 真の score（#93 exact-head deep `35108628030` = main deep `35113714046`、Stryker 4.16.0 vstest）:
  Domain 95.52% / Application 95.87% / Infrastructure.Windows 90.86% / Presentation.WinUI 94.19%。
- #132 exact-head deep `35115416386`（Stryker 5.0.0 vstest、head `374c1ff`）と main deep `35120031829`
  （`9c96f58`）: いずれも Domain 95.52% / Application 95.87% / Infrastructure.Windows 90.86% /
  Presentation.WinUI 94.30%、conclusion passed。これが現在の真の score。
- open CodeQL alert はゼロ（main スコープ、branch スコープとも）。
- dotnet-stryker は 5.0.0 pin（TST-008、ADR-0048 Pin review 2026-09-16）。runner は vstest のまま。
- dependabot PR #127 / #128 は置換済みとして close 済み。

## 確定した事実

- main ruleset は `canonical-gate` のほかに review thread の解決も必須。CodeQL の note 級品質指摘は PR の
  review thread になり merge を止める。慣行はコード修正（alert が `fixed` になると thread は自動 resolve）。
  branch スコープの alert は `ref=refs/heads/<branch>` で読む。main スコープの 0 は branch の 0 を意味しない。
- Stryker 5.0.0: vstest 存続・default、config 互換、.NET 10 必須（既に pin 済み）、MTP の static 分離は未解決
  （upstream PR #3695 / Issue #3742 open）、coverage 帰属は解消。Timeout の再分類で Presentation に真の
  survivor 1 件（`AsyncWorkOwner.cs:139`）。Domain の余裕は引き続き 1 mutant。
- Presentation の Killed / Timeout 内訳は同一ツリーでも run ごとに数件揺れる（score は不変）。
- 実装リナへの手順書で main との差分を見るときは merge-base 起点（`git diff <base>..origin/main`）にする。
  二点間 diff は branch 側の追加が削除に見える。
- deep review は 30〜40 分。`gh run watch` は 10 分 timeout を分割して待つ。
- CS-013（300 論理行）超過は `CommanderSession`（324）のほか `WindowsLocalSettingsStore` 580、
  `CommanderWindow.xaml.cs` 509、`FileOperationGateway` 412、`WindowsLocalFileOperationAdapter` 327、
  `BookmarkManagerView` 312。ゲート化は未決（hide の承認待ち候補「CS-013 analyzer 強制」に合流）。

## #100 window shortcuts（未起票、ADR 草案 2 本）

草案は `docs/adr/0050-…`（proposed）、`docs/adr/0051-…`（proposed）、
`docs/design/2026-09-16-window-adjustment-handoff.md` として本 docs 閉塞 PR に同梱した。設計キャンバス
（idle / planned / refused / 2× 寸法図）は claude.ai artifact `MBae6L9KmhyTSkXKVPbhMt`（参照用）。

### ADR-0050 の要点

- `Ctrl+W` で持続的な window mode に入る。`h j k l` / 矢印で 32 DIP × scale 移動、`+` / `-` で左上固定の
  拡大縮小、`m` 最大化、`r` 復元（冪等 2 命令）、`Esc` / `Ctrl+W` で抜ける。巻き戻し無し。
- 境界規則は caption 規則のみ（top edge 行に min(step, width) の可視線分と下方向 1 step）。最小値は
  `OverlappedPresenter.PreferredMinimum*` が唯一の機構（未宣言なら 1 step 下限）。
- snap 状態は SDK に無く `IsWindowArranged` は SEC-014 で不可。maximized / minimized だけ拒否。
- Application は port を持たない。host が `AppWindowPlacementAdapter` で読み、`CommanderSession.AdjustWindow`
  （同期、`_paneWork` 不経由）が plan を返し、host が一度だけ適用する。leave は placement 不要で常に成功。
- 専用 key table（palette と同形）+ hint group 宣言、`Map` の専用分岐で未宣言キーは `KeyboardConsumed`、
  chord 判定は「context で宣言済みか」。helper は `CommanderWindow.xaml` に inline、wiring は
  `Views/WindowAdjustmentView`。coverage exclusion は追加しない。
- 実装の第一歩は runtime spike（focus sink への `CharacterReceived` 到達、32 DIP = 32 px @100%、
  `PreferredMinimum*` が未宣言で 0）。失敗したら ADR を改訂する（fallback 事前承認なし）。

### ADR-0051 の要点（ADR-0050 の前提）

- `CommandPaletteSession` と `AddressEditorSession` に状態・admission・検証を移す。判定に要る値は引数
  （`DualPaneSnapshot`、closed `InteractionOwnership`）で受け、他 owner を知らない。dispatch と pane
  副作用は `CommanderSession` に残す。3 つの順序不変条件（activate 後に editor 状態、close 後に navigate、
  close 後に dispatch）を保つ。
- ctor / snapshot は `TransientScopeOwners` / `TransientScopeSnapshot`（明示 ctor、positional 禁止）で束ねる。
- ADR-0047:48、ADR-0044:30、COMMAND_MODEL の palette 行を supersede / 置換、address 行を新設。
- 行数は PR に記録するだけ（1 型だけのテストは baseline と同じになるため）。

### hide に確認したいこと

1. `Ctrl+W` を mode の入口にすること（ブラウザ等の「閉じる」との期待値ギャップは ADR に明記済み）。
2. 拡大縮小を左上固定にしたこと、`m` / `r` を toggle ではなく冪等 2 命令にしたこと。
3. ADR-0051（`CommanderSession` 分割）に着手してよいか。09-11 引き継ぎ書で承認待ち候補だった項目。
4. CS-013 のゲート化と他 5 型の扱いを別 Issue にすること。

## 再開手順

1. hide の回答を受けて、ADR-0051 の Issue を起票 → `refactor/<n>-commander-session-scope-owners` →
   実装リナに ADR-0051 の Migration をそのまま仕様として渡す（既存 49 テストは生成箇所以外不変が受入条件、
   exact-head deep 必須）→ merge → ADR を accepted に。
2. 続けて #100: ADR-0050 を accepted にし、spike → 実装。exact-head deep は不要（native 境界なし）だが
   runtime 証拠（`AppWindow` bounds 前後、screenshot、UIA name）を PR に記録する。
3. #94 は hide の在席と専用環境が要る。入力自動化は foreground 検証付きでのみ行う。

## その後の焦点と候補

- follow-up 候補（hide の承認後に起票）: `OpenBookmarks` の catalog 追加、`_defaultFileListFocusSuppressedState`
  のリセット条件、CS-013 の analyzer 強制と他 5 型の分割、`verify-coverage.ps1` の stale build 対策、
  WSL link entry 削除の capability ADR、Presentation の `AsyncWorkOwner.cs:139` survivor の behavior test。
- 統合済み worktree の後始末は hide: 09-11 分に加え `-issue93` `-129` `-130`。

## 再開時の注意

- 実装リナの commit の Co-Authored-By は Fable 5.1 の 1 行に統一する（今回 1 件だけ Opus 併記あり）。
- bash から Windows ネイティブ実行ファイル（python 等）に MSYS パスを渡さない。
- `.gitattributes` の CRLF、`sed -i` 禁止、force push 禁止、`--admin` 禁止は不変。
