# 2026-11-19 Bookmark操作性改善・中間記録

Status: ongoing; Issue #457 implementation candidate, native acceptance pending

hideの「止めるまで進める」という依頼で再開した。最優先のBookmarkを、既存PR402の
キーボード操作から改善している。通常mainと公開Previewには反映していない。

## 変更と検証

- キー操作・画面構成・アクセシビリティの3専門家が、全キーモデル、実装、利用可能な画像を
  読み取り専用で検討し、相反する案を調整した。実際のNeNe Commanderリナにも相談した。
  この環境の`/design`はアクセス管理のコマンドだったため、同じClaude環境のデザイン批評・
  アクセシビリティ・引き継ぎスキルで続けた。クラウド権限は変更していない。
- 保存は編集画面限定のCtrl+Enter。通常Enterはコントロールへ渡す。検索中は
  Enter/Down/Escapeが「一覧へ」であることを案内し、次のEnterで開く契約を維持する。
- 一覧に入ると可視選択または先頭を選び、保存後は検索を解除して保存した項目を選ぶ。
  取消では元の検索・カテゴリを維持する。閲覧による設定書込みは発生しない。
- 全Bookmark文字入力のIMEと、開いたカテゴリ／枠の選択肢を保護する。
  独立レビューで見つかった通常Enter長押しの回帰は、失敗する試験を保存して修正した。
- アクティブ領域を既存トークンで区別し、入力ラベルと割当済みの枠名を示す。
  実画像レビューの指摘により、設定の保存状態には「設定:」を添えた。
- 実起動で保存ボタンのリソース取得エラーを検出して修正。さらに、空一覧と画面切替の
  フォーカス不良を再現した。既存の遅延処理を拡張し、後続Tab／pointerで予約を取消し、
  初期focus予約中のEnterを消費する。IME／選択肢のnative入力は維持する。

Applicationの95件と追加1件、Presentationの135件に対する変更後の案内8件・focus8件、
App Release build、view所有権20件、変更ファイルの書式確認が成功した。
ビルド失敗、停滞したMSBuildの停止、restore不足、CRLF修正、静的検査の追加指摘と解消も
[品質記録](../quality/2026-11-19-bookmark-keyboard-usability.md)へ残した。
無関係な全件回帰、coverage、mutation、hosted CIは実行していない。

## 現在の制限

最後の実画面操作は`foreground window did not report a process id`で失敗し、
対象の再発見・再接続後も継続できなかった。修正後の初期focus、取消、保存、IME、
native popup、削除確認の安全な初期focus、狭幅、Narratorは未確認のまま。
試験用アプリだけを終了し、通常mainの画面は残した。設定ファイルのhashは試験前と一致し、
ブックマークを保存していないため復元操作も不要だった。

Issue #457のPRとPR402はDraftを維持する。hideのPR402画面受理を代替しない。
次は残る実機確認を可能な時点で再開し、独立した#454のフッター改善、#453のF6計測へ進む。
作業は継続中であり、この記録を停止依頼として扱わない。

`D:/NeNeCommander/worktrees/issue-457`は未受理の候補として保持する。
`D:/NeNeCommander/outputs/bookmark-ux-20261119/`の入力原稿・進捗・空の試験fixtureも未完了のため保持。
恒久証拠は`D:/NeNeCommander/evidence/bookmark-ux-20261119/`。
既存の拒否済み削除対象は以下へ引き継ぎ、再試行していない。


## ! hide実行待ち：後片付け

以下15対象の削除は自動承認審査が起動前に拒否したため保持する。理由は `blocked by policy` のみ。
Folio6対象、11:28のCommander #439の2対象、11:44の#443出力、12:26の#433出力、
公開後の#434出力と失敗候補01/02が対象。別手段で再試行しない。
main取込、未保存物、ignored出力、唯一の成果物、稼働参照、リンクを確認済み。
Commanderは#439の399file、#443の5file、#433のTRXと95 tool artifacts、#434の112fileと
失敗候補01/02を恒久証拠へ保全しhash照合済み。#433/#443の作業木は削除済み。

- `D:/NeNeFolio/worktrees/935-window-width`
- `D:/NeNeFolio/outputs/935-window-width`
- `D:/NeNeFolio/agents/935-window-width`
- `D:/NeNeFolio/agents/window-width-20261118`
- `D:/NeNeFolio/design/20261118-search-explanation`
- `D:/NeNeFolio/agents/session-20261118-sana`
- `D:/NeNeCommander/worktrees/issue-439`
- `D:/NeNeCommander/outputs/issue-439`
- `D:/NeNeCommander/outputs/issue-443`
- `D:/NeNeCommander/outputs/issue-448`
- `D:/NeNeCommander/outputs/latency-20261118`
- `D:/NeNeCommander/outputs/issue-433`
- `D:/NeNeCommander/outputs/issue-434`
- `D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-01`
- `D:/NeNeCommander/releases/0.1.0-beta.1/candidate-20261118-02`

hide実行用の準備済みコマンド（既に無い対象は許容。サナは実行していない）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/session-20261118/cleanup-requested.ps1
```

対象一覧は`cleanup-targets.json`、保全/拒否の原本は`D:/NeNeCommander/evidence/`内の
各Issueとsessionのフォルダへ保存。Commanderのscriptは固定対象のinventory、Git状態、
参照/リンクを照合し、保存済みhash証拠を再利用してdirectory単位のnative削除だけを行う。
既存scriptの成功記録を再利用し、終了時に更新したmasterと診断用helperの2scriptも構文解析成功。
いずれも実行はしていない。Folioの検査付きscriptを呼ぶ経路は維持した。
ブランチ/commit/恒久証拠/公開shipping/候補03/実機用fixture/未完了PR402の作業木は保持する。
hideの実行報告と不在確認まで後片付けは未完了。
