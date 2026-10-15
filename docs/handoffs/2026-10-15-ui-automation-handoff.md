# 引き継ぎ書 2026年10月15日 UI Automation識別子

Status: informational

[Issue #240](https://github.com/hideyukiMORI/NeNeCommander/issues/240)は、既存のファイル行・設定shortcut・削除確認に識別子を宣言する変更。
この文書の基点はmain `e0fe13b77b4acbbdc6d1445fd6082b7e5e970e46`で、最終main SHA、PR merge、整理の結果はIssue PRを確認する。
詳細は[日報](../reports/2026-10-15-ui-automation-daily-report.md)と
`D:/NeNeCommander/evidence/issue-240/`に保存している。

## 再開手順

1. `AGENTS.md`と`PROJECT_STATE.md`、現在のhideの指示を読む。
2. `git status --short --branch`、`git worktree list --porcelain`、Issue #240とそのPRの現状を照合する。
3. 通常配置には固定SDKがなくなったため、必要なdotnet実行には`D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe`を使う。
   git hookを実行するプロセスでは、そのディレクトリだけをPATH先頭に加える。global PATHやrepo pinは変更しない。
4. 次の一件を選び、既に完了したソート保存・palette scope追加・今回の識別子宣言を再実装しない。

今回のAstra設計とSOL背景作業の分担はhideがこの作業について明示したもの。
次のセッションで無条件に固定せず、現在の指示と適用範囲を確認する。
追加worktreeは`D:/NeNeCommander/worktrees/<task-id>`、一時物は`D:/NeNeCommander/outputs/<task-id>`。
必要証拠は`D:/NeNeCommander/evidence/`へ収載する。

## 設計と注意点

正本は[Automation identification](../DESIGN_HANDOFF.md#automation-identification)。唯一の機構は既存Presentation投影とAppのattached properties。

- 行IDは`FileListRow_`と正確な名前。必ず`LeftFileList`または`RightFileList`内で完全一致検索する。
  左右同名、WSLの大小文字、Unicode、記号を加工しない。indexやactive状態を使わない。未実体化の行や永続ファイルidentityの取得には使わない。
- `SettingsEntryHint`は既存hintのラベルTextBlock。共用templateに渡るpalette hintも空の`AutomationId`を持つ。
- `DeletionConfirmation`は既存操作バー内のGridに確認待ちの間だけ付く。
  `LabeledBy`が局所化済み`OperationStatus`を参照し、WinUI既存のnamed-container peerを使う。
  終了後はattached値を空にするが、frameworkは`OperationContent`へfallbackし得る。判定は`DeletionConfirmation`の完全一致。
- `cells.json`は宣言を記録しただけ。F8はharnessで引き続き拒否され、Input cellもskip/not-implementedのまま。
  独立modal、custom peer、新しい確認・入力・provider経路はない。

新しいtestは300論理行未満の専用fixtureへ置き、既存の巨大fixtureの分割をこの変更へ混ぜていない。
独立レビューの2指摘は解消済み。一次資料、固定revision、確認範囲は`review/review.md`にある。

## 再利用できる証拠

- 最終AppとPresentation.TestsのRelease build成功、警告・エラー0。
- 対象23件成功、失敗・skip 0。不変21件を再利用し、移動した確認testとpalette hintの2件が最終実行。実行総数24との区別を保つ。
- 変更C#11ファイルのformat verify成功。harness selftestは103/103成功。
- 全コマンド・filter・入力hash・TRX・初期失敗は`verification/verification.md`以下に保存。
- 最終の文書・binding・改行・リンク確認、Commit mode、PR状態はIssue PRと恒久証拠へ追記する。

初回のSDK不足と途中のCA1822は失敗として保存している。レビュー前のApp buildを最終成功へ読み替えない。
成功した証拠は関係する入力が変わらなければpush・merge・文書更新で再実行しない。
full/deep、coverage、mutation、実UIA、読み上げ、実focus、#94を成功と扱わない。

## 残件と整理

`outputs/issue-236`は2026-10-15に不在確認済みで、前回の削除待ちは完了。
今回の統合後のworktree/output整理結果はIssue PRに記録する。固定SDKと恒久証拠は削除対象ではない。
拒否された削除があれば開発手順に従い、正確な残存先と一行を日報末尾へ追記して引き継ぐ。

#94はhideがいる日の配布前手動確認、#181は低優先度の割当ネットワークドライブ対応。
実UIA hierarchy、provider実環境、未実装Input cellsも別の作業として残る。
次の製品機能は未選定。`CommanderSession`の289/300論理行も今回変更していない。
