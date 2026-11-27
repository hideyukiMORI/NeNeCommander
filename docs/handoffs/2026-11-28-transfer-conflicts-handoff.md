# 2026-11-28 引き継ぎ — Issue #560 上書き処理

状態: 停止。最終更新: 2026-11-28 03:33 JST。hideの「途中で止めよう」に従い、全Infrastructure mutation208を中断した。新たな修正・再実行は始めていない。再開はhideの指示を待つ。

## 正本と許可範囲

- 作業ツリー: `D:/NeNeCommander/worktrees/issue-560`
- ブランチ: `feat/560-transfer-resumption`
- 最終製品commit: `18510316da3108c5796b108419dfdd4c68c9e04d`。通常Commit/message hook成功。
- 固定入力: `D:/NeNeCommander/evidence/transfer-conflicts-20261127/final-infra-inputs-206.json`、1,620入力。
- 継続記録: 同じ証拠rootの `stopped-checkpoint-224.json`。その後の最終結果は本書と日報を優先する。
- hideの15:41承認は未公開ブランチへの接続と専用ファイルの完全な検証。公開、main統合、リリース、フォルダー内容マージの許可はない。

実行していた `run-infra-acceptance-208.ps1` / session50947は終了済み。全mutationは中断しており、完了レポート・最終スコアはない。通常DLLと異なるmutation consumerが残るため、通常テストにそのまま使わない。再開前に下の停止結果を読む。

## 守る不変条件と変更箇所

唯一の実行経路は `FileOperationGateway`。凍結したsource、全体の事前検査、表示した正確な競合への同意、コピー・検証・条件付きソース削除を維持する。[ADR-0095](../adr/0095-qualified-transfer-replacement.md)を先に読む。

上書きは確認したファイル実体の内容更新であり、パスの原子的な置換ではない。未知・不完全な観測は拒否する。現在のハンドルと新しい照合だけが副作用や片付けを許可し、履歴receiptや保存された復旧対応表はその許可にならない。途中の不確かさと識別済み素材は、同じreceiptに保持する。自動復旧はない。

直近の製品変更はInfrastructureの以下6ファイル。各既存ownerの中で、保護要求の長さの保持、重複判定の整理、上限内の完了追記1回化、factoryで保証される重複同意照合の整理を行った。

- `FileOperations/WindowsTransferSecurityDescriptor.cs`
- `FileOperations/WindowsTransferArtifactOwner.cs`
- `FileOperations/WindowsTransferRecoveryFrame.cs`
- `FileOperations/WindowsTransferRecoveryJson.cs`
- `FileOperations/WindowsTransferRecoveryWriter.cs`
- `FileOperations/WindowsLocalOperationSteps.cs`

ソース・対象・親・祖先のネイティブ照合、バイト全体の検証、stream/shapeの再確認、書込み前の不確かさ記録、所有素材だけの片付けを維持する。Application/Domain/Presentation/Appの実装入力は直近の成功済み証拠から変わっていない。public recordの閉包形状も変えていない。

## 確認済み証拠

証拠rootは `D:/NeNeCommander/evidence/transfer-conflicts-20261127`。

| 記録・command | 結果と適用範囲 |
| --- | --- |
| `pwsh -NoProfile -File .../run-infra-acceptance-208.ps1` | App/通常consumer build、1,684成功・環境依存20skips、分岐93.7292%。全Complete mutationは指示により中断、最終スコアなし |
| `pwsh -NoProfile -File .../run-codeql-209.ps1` | exit0。通常ビルド警告/エラー0、両方式164規則 |
| `reconcile-reuse-212.ps1` | Domain101、Application726、Presentation787関連入力のpath集合・hash一致。前の211のDomainテストpath選択誤りは訂正し元記録を保持 |
| `reconcile-codeql-manual-213.ps1` | exit0。169件=既存168件+typed receipt assertionの追加1件。42owned/127generated |
| `reconcile-codeql-sources-216.ps1` | exit0。C#1,382ファイルが両archiveで全件一致、live/snapshot全1,620入力一致 |
| `reconcile-codeql-buildless-217.ps1`、`reconcile-codeql-diagnostics-217.ps1` | exit0。34件=既存33件+同じ追加1件。未解決型8件等の限界を明記 |
| `pwsh -NoProfile -File .../run-product-crash-214.ps1` | exit0。最新通常208App DLLと同一の3依存DLLを使い、8所有子プロセス停止+正常Move1条件、124項目成功 |
| `pwsh -NoProfile -File .../run-final-boundaries-218.ps1` | exit0。変更ownerのadversarial298件×3回成功、依存方向1件成功。独立した同一ソースsnapshotの通常DLLを使用 |

上の `.../` はすべて証拠root。各scriptの正確な引数、開始・終了・exitは対応するJSON/logにある。製品の全テストをもう一度回す指示ではない。

Application104の1,382件/分岐100%/mutation95.11%、Presentation89/117の643件/分岐97.3753%/mutation92.34%、Domain83/95の146件/分岐100%と既存mutation/adversarial成功は212の入力照合で再利用する。全canonical75の改行失敗は保持し、88以降の成功したformat/build/closure/architectureや93の依存audit等を入力に対応づける。新しい単一の全canonical/deep-review commandが丸ごと成功したとは主張しない。

CodeQL buildlessの115compilation-error、18extraction-error、322extraction-messageは前回とdescriptor/path/正規化message/件数が一致。全通知6,330件、未解決型8件を保持する。追加テスト6ファイルのnullable注釈14件、各1,000件観測のCS0436/CS1701の分布差を記録した。コンパイラー追跡の通常solution buildは警告/エラー0。解析の完全性や指摘0件を主張しない。

298件×3回は別の通常コンパイル済みsnapshotで実行し、通常source/consumer/test DLLのhash不変を確認した。これはgit revisionを含む208App DLLとのバイト一致ではない。プロセス停止214は208App DLLの完全一致を確認している。物理キー・IME・音声・画面受入れ・電源断を代用しない。

## 停止時の確定結果

全Infrastructure mutation208は02:30:19から03:31:55まで実行し、hideの明示指示で途中停止した。対象のPID・起動時刻・親子関係・コマンドを照合して実行元と子プロセスだけを停止し、14プロセスの残存0と、その後の子プロセス残存0を確認した。固定1,620入力は全件不変。

完了したmutationレポートは生成されていない。プロセスのexitは-1、wrapperは完了レポートがないことを検知してexit1で終了した。これは途中停止であり、合格や特定の最終スコアを示さない。直前の完了した全検証178は88.15331%で90%未満のまま。今回の通常テスト・分岐網羅・CodeQL・境界検証の成功は、それぞれの証拠として保持する。

停止前の最初の本人確認は、時刻を文字列に戻す際の秒未満の精度低下を検出して拒否し、何も停止しなかった。保存時と現在のUTC tickを直接照合して正確な対象を確認した後に停止した。この観測側の訂正も `mutation-interrupted-223.json` に記録した。

通常Infrastructure DLLと通常App内DLLは同一のまま。テストconsumer DLLにはmutationの状態が残っており、通常DLLとhashが異なる。停止後のビルドや再テストは行っていない。次回の通常検証前に、対象buildでconsumerを戻して一致を確認する。

ログ・専用テストroot・元の失敗・途中出力は保持した。停止証拠は `mutation-stop-owned-processes-223.json`、`mutation-interrupted-223.json`、`stopped-consumer-224.json`。実行中の自分の検証は残っていない。

## 再開するとき

1. hideの再開指示を確認し、本書の最終判定と実際のbranch・dirty・実行中processを照合する。古い「実行中」の記載を現在の状態とみなさない。
2. `mutation-interrupted-223.json`、`infra-acceptance-stages-208.json` と元ログを読む。208の完了レポートと `infra-acceptance-complete-208.json` は存在しない。最後の完了した178の失敗88.15331%と、今回の中断を混同しない。次の全検証が必要だが、再開指示後に固定入力・通常consumerと実行範囲を確認して準備する。既存出力を上書きする再実行や閾値変更・除外・限定率の全体合格扱いをしない。
3. 合格なら、必要なstage証拠と関連入力の対応を最終確認し、公開・main統合の具体的な内容を準備する。現在のローカル承認だけで公開しない。
4. 変更していない成功済みの証拠を再利用する。通常テストを実行する場合は、instrumented consumerを使わず通常DLLとの一致を先に確認する。
5. フォルダー内容のマージは別の未承認範囲。Issue #560全体や新リリースを完了としない。

## 保持する作業物

未統合の `D:/NeNeCommander/worktrees/issue-560` と、`D:/NeNeCommander/outputs/transfer-conflicts-20261125`・`transfer-conflicts-20261126`・`transfer-conflicts-20261127` を保持する。最後のrootにはCodeQLの固定source/database、通常DLLを固定したprocess-crash214、各診断用成果物がある。現在の検証や未公開成果物を含むため整理対象にしない。

恒久証拠は各日付の `D:/NeNeCommander/evidence/transfer-conflicts-*`。他の未完了worktree、試遊版、通常App・設定も変更しない。今回新しい削除拒否はなく、hide実行待ちの後片付けはない。

詳細な時系列は[従来の引き継ぎ](2026-11-25-transfer-conflicts-handoff.md)、[品質記録](../quality/2026-11-25-transfer-conflicts.md)、終了概要は[日報](../reports/2026-11-28-transfer-conflicts-daily-report.md)。
