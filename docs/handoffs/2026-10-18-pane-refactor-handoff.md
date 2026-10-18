# ペイン表示整理の引き継ぎ — 2026-10-18

Issue #328はhide指定順の②。①#326 / PR #327は統合と整理まで完了した。
今回の実装・検証範囲は[日報](../reports/2026-10-18-pane-refactor-daily-report.md)が正本。
ペイン表示の5責務をimmutableな内部部品に整理し、公開binding、row cache、focus、
modal継続、Application状態所有を維持した。旧constructor overloadは残していない。

旧59 test methodsを移行し6メソッド追加、直接影響する74ケースが全成功。
Presentation.Tests/App Release buildは警告・エラー0、変更C#のformat確認はexit 0
（workspace読込み警告あり）。計測は159指摘、今回変更したC#の指摘0。
full/deep・全件回帰・coverage/mutation・live UI/provider・#94は実行していない。
検証入力不変なら成功証拠を再利用する。global gateへの接続は未実装。

恒久証拠は`D:/NeNeCommander/evidence/pane-refactor-20261018/`。
Issue PRと`completion.json`で最終統合・main同期・追加worktree/outputの整理を確認する。
次は③`CommanderWindow`と`BookmarkManagerView`の表示部品・イベント配線の責務分離。
既存owner、入力経路、focus、async lifecycleを変えず、影響する境界だけを検証する。
Application/provider/settingsの残件と全体gate化は後続の別範囲。
