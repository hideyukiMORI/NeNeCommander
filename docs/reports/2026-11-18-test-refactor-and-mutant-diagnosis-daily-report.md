# 2026-11-18 テスト整理と単一変異の診断

hideの「未完了から。サブエージェントたくさん使っていい」を受け、Commanderの未完了を進めた。
主担当が#417の受理・統合を行い、独立レビュー2席の確認後、#421/#422の分割と#424の局所診断を分担した。
build/test/Inspectは一つずつ実行した。PR #402の実機確認待ちの領域には触れなかった。

## 完了した変更

| Issue / PR | 結果 | main |
| --- | --- | --- |
| #417 / [PR #423](https://github.com/hideyukiMORI/NeNeCommander/pull/423) | Infrastructure.Testsの26 findingsを解消。設定保存・Provider・Live WSLの34ファイルを責務分割 | `b22b0b1` |
| #422 / [PR #425](https://github.com/hideyukiMORI/NeNeCommander/pull/425) | AsyncWorkOwnerの25ケースを通常・障害/終了・割込みへ分割、2 findingsを解消 | `ca3bfd1` |
| #421 / [PR #426](https://github.com/hideyukiMORI/NeNeCommander/pull/426) | Application.Testsの18ファイルを整理し9 findingsを解消。186ケースを保持 | `1dcba46` |
| #424 | PaneReducerの単一source mutantを固定入力で比較。元版/変異版とも2ケースと31ケース成功、CI原因は未確定 | 文書PRで収載 |

製品コード・schema・依存・閾値・除外・toolingの恒久変更なし。
統合後`1dcba46`の`pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect`は
**67→30 findings、37件消失・追加0**。exit 1は残る30件による。
製品8件、テスト22件。型の長さ5、メソッドの長さ13、引数8、認知的複雑度2、入れ子2。

## 証拠と試行

- #417は前回の850件（834成功・16 live opt-in skip）、Architecture21件、live13/13を、入力hash一致で再利用した。901 C#入力と旧証拠32ファイルを照合し、2席の読み取りレビューで必須修正なし。今回の対象formatと通常hookは成功。旧通常試験の外側の引数/exitが別記録されていない限界も明記した。
- #422はRelease build警告/エラー0、対象25/25、4file format成功。25ケースの属性・意味、同期contextを比較した。projectなしの`dotnet test --help`がsolution評価を始めた試行は停止し、成功には数えない。
- #421はRelease build警告/エラー0、対象186/186、18file format成功。158宣言・44 DataRow・41helperと所有状態を照合。初期buildの不要using（IDE0005）26件、続く8件を直した。最初のformatterで修正されなかった原因は未確定で、project cwd/basename指定の最終確認を採用した。失敗ログも保存した。
- #424は424入力を固定し、既存Stryker reportと同じ1箇所の置換を注入。元版2→変異版2→変異版31→元版31がすべて成功し、exact test ID集合も一致した。Release build3回も成功。自作変異の復帰後はsourceとassemblyのhashが元版に一致した。
- PRの選択検証・文書・diffを確認し、通常hookでcommit、Draft→Ready→squashで統合した。GitHub連携の更新権限不足（403）は認証済みCLIで処理した。full/deep CI、全層mutation、無関係な試験、GUIは今回起動していない。

各変更の詳細は[Infrastructure](../quality/2026-11-18-infrastructure-test-refactor.md)、
[AsyncWorkOwner](../quality/2026-11-18-async-work-owner-test-refactor.md)、
[Application](../quality/2026-11-18-application-test-refactor.md)、
[単一変異](../quality/2026-11-18-pane-mutant-diagnosis.md)へ。
正確なコマンド・raw log・TRX・比較結果は`D:/NeNeCommander/evidence/issue-417/`、
`issue-421/`、`issue-422/`、`issue-424/`。統合後の静的計測は#424の`integrated-inspect*`。

## 残件と整理

CIのKilled原因は未確定。正常なPaneStateならFirstとFirstOrDefaultは同じ値になるという
コード上の推論と実測は整合するが、Stryker注入・実行制御を再現していない。
次の最小条件は[品質記録](../quality/2026-11-18-pane-mutant-diagnosis.md#解釈と限界)に残した。

PR #402はhideのブックマーク画面の実機確認待ち。#386、#397、ADR-0051の5引数はhide判断待ち。
残るCS-013のうち26件はPR #402の変更ファイル、2件は5引数、独立候補はKeyboardHelpViewの2件。
PR #420は現行Test SDK pinに関わる依存更新で今回採用していない。#94の配布前確認は別枠。

#417/#421/#422のworktreeとoutputsは、取込・未保存物・ignored出力・証拠・稼働参照・リンクを
確認して削除済み。ブランチ、commit、恒久証拠を保持した。
#424の文書統合後の整理は`D:/NeNeCommander/evidence/issue-424/completion.json`を確定記録とする。
#398のworktreeは未統合の実機待ちのため保持する。追加waiverなし。
