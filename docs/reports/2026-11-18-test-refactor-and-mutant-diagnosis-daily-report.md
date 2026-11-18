# 2026-11-18 テスト整理と単一変異の診断

hideの「未完了から。サブエージェントたくさん使っていい」を受け、Commanderの未完了を進めた。
主担当が#417の受理・統合を行い、独立レビュー2席の確認後、#421/#422の分割と#424の局所診断を分担した。
build/test/Inspectは一つずつ実行した。PR #402の実機確認待ちの領域には触れなかった。

この冒頭から「残件と整理」までは09時台前半の確定記録。以後の継続結果は末尾へ追記する。

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

## 09時台の継続と公開準備

hideは「おしまいと言うまでCommanderを続ける」「サブエージェントもいっぱい使う」と再指定し、
公開できる区切りが来たらタグを作ってリリースすることも依頼した。Folioは作業対象外。

- #428 / PR432はmain `97d8a12`へ統合。ヘルプのスクロール条件式だけを平坦にし、比較順・値・所有を保持。
  Appと対象test buildは警告/エラー0、ヘルプ20ケース成功。Inspectは30→28、追加0。
  formatのworkspace警告は詳細ログに保存。通常hook成功。作業木と一時出力は証拠/hash保全後に削除した。
- #429は#424と同じ424入力で、pin済みStryker/VSTestの対象1mutantを2 exact ID、続いて31 exact IDで実行。
  両方Survived、初期/coverage/activeの各phaseは全件Passed、ID集合も一致。子exit2は95%閾値保持による不足。
  最初のwrapper exit1も区別して記録。注入source、active ID1813、全結果、trace警告を保存し、元source/2DLLを復帰照合。
  原因は未確定、次は同条件をCIで1回比較する範囲。[品質記録](../quality/2026-11-18-single-stryker-diagnosis.md)。
- PR402の独立レビューでキー反復の遷移後漏れと遅延focusを確認し、#430の別枝で修正中。
  #431は同feature枝のApplication3fixtureを分割中。実機確認前のDraft/未統合条件は維持する。
- #386はNuGet専用経路のignore処理がsecurity updateにも適用され得るため、設定とpinを据え置いた。
  hosted serviceでの事前除去の証拠がなく、Ruby側だけの説明をNuGetへ流用しない。
- 初回公開の#434を作成。既存のrelease/tagは0件。現main＋#428＋#433をWindows x64 betaの区切りとする。
  CodeQL APIはowned品質指摘37件がopenで、最新main解析の10fileは現sourceと同一。#433で修正し再解析する。
  fixed literalにも出るPath.Combine品質推奨を、危険入力成立や重大な脆弱性と混同しない。
  #94、候補への96時間以内のdeep review、版とpublish成果物の照合は未完了。

恒久証拠は`D:/NeNeCommander/evidence/issue-428/`、`issue-429/`、
`issue-386/`、`release-readiness-20261118/`。#430/#431/#433/#434は進行中で、成功未確認を完了に数えない。
この時点で公開・タグ作成・display設定変更はしていない。追加waiverなし。

## 11時台の統合とPreviewの指定

- #429 / PR435はmain `c938274`へ統合し、証拠保全後に作業木と一時出力を整理済み。
- #433 / PR437はmain `860e61d`へ統合。CodeQLの37件に対応する局所変形、対象build・試験・format・通常hookと独立レビューが成功。Inspectの28 findingsは前後同一。CodeQL APIゼロは未確認なので#433は再openして保持する。
- #430 / PR436は`d31248c`、#431 / PR438は`e3c8f4f`としてブックマークfeatureへ統合。101ケースと分割した110ケース、独立レビューが成功。mainを取り込んだfeature `275b464`のquality run 44217730539は成功（全2087、成功2071、live opt-in skip16、失敗0）。coverageはDomain/Application100%、Infrastructure94.48%、Presentation94.49%。PR402は引き続きDraftでhideの画面確認前にmergeしない。
- #434の候補1はglobal RIDがlibrary lockと不一致でNU1004、候補2はpublishにApp PRIが欠けて停止。SDK既存のPRI publish経路を有効にした候補3は586ファイルのZIP/原本hash、1294入力、10lock、51原文notice、11 embedded XBF、日英各348文字列を照合して成功。全失敗ログと試行を保存。版は0.1.0-beta.1、未署名Win11 x64 self-contained。最終配布はmain統合後の正典publishで作る。
- 11:03、hideは実機確認が帰宅後になるためPreviewとして先に公開するよう依頼した。ADR0079にその範囲を記録し、#94は未実施のまま公開説明へ明記する。公開前の自動deep review、CodeQL API照合、配布物照合は維持する。この時点では公開/タグ作成は未実施。
- #439の固定424入力・1mutant・31 IDをCIで比較するmanual workflowは実装・tool38チェック・保存済み実ログ照合・独立レビュー済み。実CIは未実行。今回のPreview sourceから分離し、main受理後1回だけ比較する。元のKilled原因を断定せず、全層mutationへ広げない。

恒久証拠: `D:/NeNeCommander/evidence/issue-430/`、`issue-431/`、`issue-433/`、`issue-434/`、`issue-439/`。
#398/#430/#431はPR402がmain未統合のため、#433/#434/#439は最終受理/証拠収載前のため作業木を保持。
配布候補3つと実機用fixtureも保持する。追加waiverなし、製品設定schema変更なし。

## 公開用sourceの確定とCI比較結果

#434 / PR440はmain `46f1a82`へ統合、依存review44221630000は成功。
最終shippingはこのsourceで正典publishを実行してexit0、警告/エラーなし。
586 payload/ZIP・1295入力・10lock・51原文noticeを照合し、PRIと日英source blobの一致で既存の各348文字列の証拠を再利用。
ZIPは113730832 bytes、SHA256 `c4dad8d8007948ba5455950b9f653853e5971512dcb093e83c73622961910e69`。
場所は `D:/NeNeCommander/releases/0.1.0-beta.1/shipping-20261118/`。
アプリは起動していない。公開用deep review44221900660は同sourceで1回実行中、CodeQL照合と公開は未完了。
#94のIssue本文もADR0079へ合わせ、未実施/openを維持した。

#439 / PR441はその後のmain `43e857e`へ統合、依存review44221977999は成功。
固定source `b5d9af8`のmanual比較44222206602を1回実行し、対象1813はローカル同様Survived。
初期/coverage/mutantの各31 exact IDが全Passed、15注入は正規化一致、424入力と2DLLの復帰が一致。
raw子exit2・wrapper記録final2、外側shell表示exit1/workflow failureを区別する。成功gateとは呼ばない。
artifact14391551154のraw ZIPはservice digest `ad7bf1d6a86a6220cbcc0ae1da5f67dad74a188b5cb0ff7311c3cc4690caec21` と一致し、35内容hash（隠しfile含む36files）も全一致。
環境allowlist JSONは同名のcommand journalで上書きされたためCPU/culture/timezoneは未保存。OSも異なり、全環境一致を主張しない。
固定条件比較は成立し、過去full batchのKilled原因は未確定。再実行・全層展開・score訂正はしない。
最終比較の独立レビューと原本は `D:/NeNeCommander/evidence/issue-439/`、公開用証拠は `issue-434/`。
PR402は引き続きDraftの実機確認待ちで今回のPreviewに含まない。

## 環境記録の保存名修正

#443 / PR444はmain `43f3f26`へ統合。初期環境をenvironment-details.jsonへ保存する1行を変更し、
command journalのenvironment.json/logと分離した。実際のwrapperの記録blockとcommand writerを使った局所proofは、
旧版でmetadata消失・新版でhash保持、両方のjournal完了/exit0を確認。parser・通常hookも成功。
過去CIの欠損値は復元せず、Stryker/製品試験/CIを再実行していない。公開source46f1a82には含まれない。
作業木は整理済み、証拠5fileは恒久保存/照合済み、一時出力の削除だけは後述の拒否で保持。

## ! hide実行待ち：後片付け

同じ日のFolio作業整理で、サナ所有物の削除は自動承認審査に起動前に拒否された。
返された理由は `blocked by policy` のみ。別手段で再試行しない。Folioの6対象は10:25に残存確認。
11:28にはCommander #439の2対象も、取込7file・保存399file照合・未保存/稼働参照/リンクなしを確認した後、
同じ理由で起動前に拒否された。11:44に#443の作業木はgitで削除できたが、保存済み5fileの一時出力削除は同じ理由で起動前拒否。以下9対象を保持する。
FolioとCommander #439は別々の拒否記録。Folioのmain・ブランチ・Issue935・リナの作業・実data・通常exeを対象にしない。

- `D:/NeNeFolio/worktrees/935-window-width`
- `D:/NeNeFolio/outputs/935-window-width`
- `D:/NeNeFolio/agents/935-window-width`
- `D:/NeNeFolio/agents/window-width-20261118`
- `D:/NeNeFolio/design/20261118-search-explanation`
- `D:/NeNeFolio/agents/session-20261118-sana`
- `D:/NeNeCommander/worktrees/issue-439`
- `D:/NeNeCommander/outputs/issue-439`
- `D:/NeNeCommander/outputs/issue-443`

準備済みの検査付きコマンド（対象が既に無い場合も扱う。サナは実行していない）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/session-20261118/cleanup-requested.ps1
```

保全と拒否の原本は `D:/NeNeCommander/evidence/session-20261118-sana/` と `D:/NeNeCommander/evidence/issue-439/`、`issue-443/`。
統合コマンドと#439/#443の個別scriptは構文解析のみ成功、拒否された削除は未実行。
後片付けはhideの実行報告と不在確認まで未完了とする。
