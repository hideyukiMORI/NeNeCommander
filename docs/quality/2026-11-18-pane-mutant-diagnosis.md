# PaneReducerの単一変異の局所診断（#424）

## 結果

`GetFocusIndex`の`First`を`FirstOrDefault`へ置き換えた単一ソース変異では、
CIが記録した失敗を再現しなかった。元版と変異版で、記録されたkilledByの2ケースと
coveredByの31ケースがすべて成功した。CI側でKilledになった原因は未確定である。

| 入力 | killedByの2ケース | coveredByの31ケース |
| --- | --- | --- |
| 元版 | 2成功、失敗/skip 0 | 31成功、失敗/skip 0 |
| FirstOrDefault版 | 2成功、失敗/skip 0 | 31成功、失敗/skip 0 |

TRXの実行ID集合は各回とも既存レポートの指定集合と一致した。
31ケースをmethod名だけで選ぶと同じDataRowメソッドの33ケースになるため、
31個のexact IDで絞った。対象外のケースを結果に混ぜていない。

## 入力と実行

#410に残った、CI run 43780626571 / 43787538393 / 43793338605のKilledと
ローカル5ファイル限定mutationのSurvivedとの差を、#424の受入条件どおり小さく切り分けた。
元入力は`b5d9af8dcf0c04622be517aaa3ce6bf0390e8b3e`。
source・test・依存/tool設定の424ファイルをSHA256で固定した。
既存Stryker reportのlocation/replacementを正確に適用し、変えたのは1箇所の
`First`から`FirstOrDefault`だけ。生成ソース、exact diff、入力hashを保存した。

2ケースは以下のとおり。

- `DualPaneMoveSessionTests.HandleAsyncWhenMoveHasSelectionMovesSelectionAndKeepsFocus`
- `DualPaneOperationLifetimeTests.HandleAsyncWhenSourcesCompleteObserverSeesRunningProgress`

| コマンドと順序 | 結果 |
| --- | --- |
| 対象Application.Testsのlocked restore | exit 0 |
| `dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-restore`（元版・変異版・元版復帰） | 3回ともexit 0、警告0・エラー0 |
| `dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-build --no-restore --filter <指定集合> --results-directory <専用出力> -- --report-trx --report-trx-filename <名前>`（元版2→変異版2→変異版31→元版31） | 4回ともexit 0。上表の件数とexact ID集合を照合 |

完全なargv/cwd/exitは`D:/NeNeCommander/evidence/issue-424/commands.json`と各stepのJSON。
全試行・raw log・TRX・生成コード・assembly hashも同じ場所に保存した。
自作変異を戻した後、424入力のSHA256と元版assemblyのSHA256が一致し、worktreeはcleanだった。
永続のsource・テスト・依存・gate変更はない。

## 解釈と限界

コードを読んだ推論では、空の表示一覧は`GetFocusIndex`を呼ぶ前にreturnし、通常の
`PaneState`ではフォーカスが表示項目に含まれるため、この2つのLINQメソッドは同じ値を返す。
上の実測はこの推論と整合する。ただし、全入力の同値性やCI失敗の原因を実証したものではない。

今回の実行は正典のRelease buildとMicrosoft.Testing.Platform経路であり、Strykerの
注入・batching・scheduler・VSTest実行制御を再現していない。既存CI証拠には
対象mutantの個別失敗メッセージ・実行履歴・注入後コードがない。
環境差、競合、テストの不安定性を原因として断定しない。

原因調査を続ける場合の最小条件は、pin済みStryker/VSTestでこのmutantだけを、同じID集合で
実行し、注入コード・active mutant ID・選択/実行されたtest ID・失敗とstackを保存すること。
全層mutationへ広げず、比較する入力を一つずつ変える。元のmutation scoreは訂正しない。

今回の受入条件は局所診断と未確定事項・次条件の記録までで、達成した。
規則: TST-008、QLT-015、GIT-001〜004、ADR-0061。waiverなし。
full gate、Stryker再実行、GUI、WSL、実データ操作は行っていない。
