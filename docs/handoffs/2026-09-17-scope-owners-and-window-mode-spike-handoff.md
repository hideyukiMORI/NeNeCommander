# 引き継ぎ書 — 2026-09-17 第 2 セッション（ADR-0051 統合済み、#100 実装 checkpoint）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書、Opus 5 のバックグラウンド NeNeリナが実装・調査・ゲート運転。
2026-09-16 の引き継ぎ書の「体制」「確定した事実」「再開時の注意」は引き続き有効。ここでは差分だけ書く。

## main の状態

- baseline: 本 docs 閉塞 PR（Issue #142）の squash commit（merge 後に確定）。その下に `5046947`（#135
  ADR-0051）、`c536978`（#139 MSTest.Sdk 4.4.1）、`d742f02`（09-17 第 1 セッションの docs 閉塞）。
- 真の score（main deep `35215425806`、`5046947`）: Domain 95.52% / Application 95.98% /
  Infrastructure.Windows 90.86% / Presentation.WinUI 94.30%。Domain の余裕は 1 mutant のまま。
- open CodeQL alert ゼロ。open PR は無し。open Issue は #100、#94（と merge まで #142）。
- `CommanderSession` は 261 論理行。scope owner は `TransientScopeOwners` / `TransientScopeSnapshot` に足す。

## ADR-0050 の状態

`docs/adr/0050-window-adjustment-mode.md` は main では `proposed` のまま。第 4 次改訂（runtime spike の
結果の反映）と `accepted` 化は #100 の branch に入っている。改訂の内容と根拠は同日の日報
「Issue #100 の runtime spike」。要点: 未宣言の `PreferredMinimum*` は `null` で adapter が 0 に翻訳、
100% セルは #94 へ、DPI 跨ぎ move の後の OS リサイズは打ち消さない、leave は「file list に focus →
overlay を collapse」の順、`GetKeyboardContext` が session 状態から先に `WindowAdjustment` を返す、
`Ctrl+W` の character は `w` と U+0017、`+` / `-` は Shift を見ない。

runtime spike は完了しており再実行しない。証拠の log と送出ドライバはセッションの scratchpad にあり、
リポジトリには入れていない。送出ドライバの要件は同じ: `SendInput`、1 打ごとに
`GetForegroundWindow() == アプリの hwnd` を確認、他アプリへの `SetForegroundWindow` 禁止、ファイル操作キー
を送らない。

## Issue #100 の checkpoint

| 項目 | 値 |
|---|---|
| Issue | #100 `feat(window): ウィンドウ移動とサイズ変更shortcutを定義する` |
| branch | `feat/100-window-adjustment-mode`（push 済み、PR 未作成） |
| worktree | `C:\Users\info\WORKS\NeNeCommander-100`（clean） |
| head | `ef6e7e2`（未検証の checkpoint。main `5046947` の上） |
| 済んだ項目 | ADR-0050 第 4 次改訂と accepted 化、design handoff の 1 文、Application の window 型群・planner・`WindowAdjustmentSession`・scope record の 3 つ目の member・`CommanderSession` の admission / freeze / 同期 2 member、Presentation の key・translator・context・binding table・`Map` 分岐・presenter 2 つ |
| 検証 | Release build 0 warning、既存 test 全 pass（Application 379、Presentation 136）、Commit mode PASS。新規 test 0 件。canonical・runtime 証拠・mutation は未実施 |
| `CommanderSession` | 281 論理行（PR #141 の数え方で 283 相当）、上限 300 |

残手順:

1. **設計リナが先に ADR-0050 を直す**（#100 の branch 上）: Decision 4 の「every other key, including `Enter`,
   `Tab`, printable characters, editing chords, and `Other`, yields `KeyboardConsumed`」を、virtual-key route の
   `Other` は pass through（handled な `PreviewKeyDown` が `CharacterReceived` を抑止するため、consume すると
   文字キーが mode に届かない）、識別済みの未宣言キーと未宣言の文字は consume、に書き換える。Executable proof
   の「`KeyboardConsumed` for every undeclared key including `Other`」も同じく直す。`WindowPlacement` の
   2 record 化（`WindowSizeConstraint`、`WindowWorkAreas`）を Decision 3 に一言、multi-cap の hint template を
   design handoff の Layout に一言足す。
2. App: `Windowing/AppWindowPlacementAdapter`、`CommanderWindow.xaml` の inline overlay、
   `Views/WindowAdjustmentView`、`GetKeyboardContext` の導出、leave の focus 順序、scrim tap、UIA、apply
   例外の defect observer 連絡。`CommanderWindow.xaml.cs`（509 行で既に超過）の増分は最小に。
3. localized resource（en-US / ja-JP、24 キー程度）。
4. 新規 test: ADR-0050 Executable proof の Application / planner / adversarial / Presentation の各項目。
5. docs: `KEYBOARD_MODEL.md`、`COMMAND_MODEL.md` の 3 行、`GLOSSARY.md` の 5 語、`docs/adr/README.md`。
6. Commit mode → push → Draft PR（`Closes #100`）→ runtime 証拠 → mutation のローカル実測 → 設計レビュー。

Issue #100 への checkpoint コメントは未投稿（実装リナのセッションで外部書き込みの権限が拒否された。設計リナは
拒否された操作を代行しない）。本表と残手順が正本。hide が Issue にも残したい場合は本節を貼る。

未解決: 区切りの直前に main worktree の 5 ファイルへ先頭 BOM だけの差分が同一時刻で付いた（復元済み、書いた
主は不明）。次回、作業開始時に main worktree の `git status` を確認し、再発したら原因を特定する。

## 再開手順

1. #100 の checkpoint を続きから: worktree で `git fetch origin` → `git merge origin/main`（rebase /
   force push 禁止）→ Issue #100 の checkpoint コメントの残手順 → targeted tests →
   `eng/check.ps1 -Mode Commit` → push → Draft PR（`Closes #100`）→ runtime 証拠 → Application と
   Presentation.WinUI の mutation をローカルで `eng/deep-review.ps1` と同形に実測（exact-head deep は
   ADR-0050 の判断どおり不要、native 境界なし）→ 設計リナのレビュー → Ready → canonical → squash merge
   `feat(window): ウィンドウ調整モードを追加する (#100) (#<PR>)` → main deep。
2. runtime 証拠はアプリを起動してキーを送るので、hide が席を外しているか合図をもらってから行う。記録する
   もの: 各 action 前後の `AppWindow` bounds、idle / planned / refused の screenshot、helper の UIA Name と
   HelpText、leave 直後の focused element（file list）と address editor が closed のままであること、mode
   open 中に deactivate → reactivate した後の focused element（sink）、`h` 押しっぱなしの連続移動。
3. #100 の merge 後、Issue #94 に 100% セルと helper の 100–300% / high-contrast / Narrator / 8 scheme の
   セルが加わった旨をコメントする。その後 #94（専用環境と hide の在席が要る）。

## hide への未決の問い（blocking ではない）

hide のキーボードは US-101。拡大は `Shift+=` かテンキー `+`、縮小は `-`。`=` alias は JIS の理由で却下のまま
（JIS では `=` が `-` キーの Shift 面）。設計リナの推奨は決定どおり出荷して、使ってから再考。

## 候補（hide の承認後に起票）

前回からの継続: `OpenBookmarks` の catalog 追加、`_defaultFileListFocusSuppressedState` のリセット条件、
CS-013 の他 5 型（`WindowsLocalSettingsStore` 580、`CommanderWindow.xaml.cs` 509、`FileOperationGateway`
412、`WindowsLocalFileOperationAdapter` 327、`BookmarkManagerView` 312）とゲート化、`verify-coverage.ps1`
の stale build 対策、WSL link entry 削除の capability ADR、Presentation `AsyncWorkOwner.cs:139` survivor の
behavior test。今回の追加: ADR-0048 の review 契機に test platform（MSTest.Sdk / MTP）の pin 変更を足す。

## 後始末（hide）

統合済み worktree とローカル branch: これまでの分に加え `-135`、`-136`、`-139`、および本 docs 閉塞の
`-142`。`NeNeCommander-100` は作業中なので残す。
