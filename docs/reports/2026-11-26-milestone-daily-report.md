# 2026年11月26日 開発日報

Status: closing record; resume only on hide's instruction.

hideの09:11の「区切りのいいところで止めて日報と引き継ぎ書を書いて」に従い、
#560の検証要件・削除直前の再確認を区切りに停止する。対象は本日07:13頃の再開から
今回の記録・通常hookでのローカル保存まで。マイルストーン全体は未完了。

## 本日の成果

| 対象 | 到達点 | 公開・統合の状態 |
| --- | --- | --- |
| #562 選択操作 | mainとの差を整合し、全選択・反転・解除・Shift範囲選択の直接境界を再確認 | PR585でfb4f2dbへ統合、Issue閉鎖。完了記録PR587でf44f6f0 |
| #545 名前フィルター | 既存の証拠を照合し、変化した選択テスト59件だけ再確認 | PR588でad85de1へ統合、Issue閉鎖。完了記録PR590でf7c24b6 |
| #560 転送競合 | 保存済み21commitを受理mainへ整合。検証receiptに必須要件を追加し、削除直前のnative属性・stream・linkを再確認 | feat/560-transfer-resumptionにローカル保存。機能未公開・未統合、上書き選択は未有効化 |

新しいリリースは作成していない。公開済みbeta.5は従来どおりで、今回統合した
selection/filterや未完了conflictが配布済みパッケージに加わったとは扱わない。
本体は`C:/Users/info/WORKS/NeNeCommander`のmain `f7c24b6`、終了記録は#560の作業ブランチ。

## 選択操作と名前フィルター

#562はApplication 88件、Presentation 110件、通常App build警告・エラー0。
隔離した非入力デスクトップで3言語それぞれ169確認が成功した。locale表記の大文字差と
JSON項目順による観測側の誤りは保存・修正し、実効localeとnative ListViewの項目・focusを
照合した。App 182入力のうち180は完全一致、2つは宣言済み診断置換、4依存DLLも一致。
物理キー、実IME、読み上げ、pixel・人による見た目の受理とは区別する。

#545は1,398の旧証拠入力のうち1,397が一致し、差分は受理済み選択テストだけ。
その59件が成功し、App 185入力・4 DLLが一致したため、旧製品・native・coverageの
成功証拠を再利用した。独立レビューはいずれも追加修正なし。受理mainと各候補の
tree一致、Issue閉鎖、本文・SHAの読戻しを確認した。

GitHub connectorのIssue/PR作成403は接続権限の失敗で、自動承認拒否ではない。
同じ保存済み本文を認証済みCLIから公開し、内容を読戻した。保護規則は迂回していない。
詳細と正確なコマンドは[選択操作](../quality/2026-11-25-selection-controls.md)と
[名前フィルター](../quality/2026-11-25-pane-name-filter.md)の品質記録に保持する。

## #560で区切った範囲

唯一のFileOperationGateway、VerifiedCopyReceipt、VerifiedSourceDeletionを維持。
全てのVerifiedCopyEntry.FileがFileVerificationRequirementsを明示して受け取る。
通常コピーは既存のDefaultStream契約を維持し、厳格なOverwriteContents要件では、
同じ保持ハンドル・親階層・native admissionを通じて、実local NTFS・通常ファイル・
単一link・Normal/Archive属性・無名streamのみであることを確認する。
source/targetの完全snapshotとsourceの再観測、同一object拒否を含む。
削除直前の最後のidentity取得後にも同じ要件を再確認する。

これにより、既存identityと既定stream内容が一致していても、隠し属性、空を含む追加ADS、
追加hardlinkなどがあればsourceを残す。新しい削除経路、追加streamの除去、snapshotの
基準更新はない。ADR0069の最終確認からpath削除までの隙間と、metadata/mapped-writerの
制限は残る。実providerから厳格receiptを生成する接続とgatewayのdowngrade拒否は未実装。

検証の最終状態は次のとおり。重複する実行の件数は合算していない。

- 最初のInfrastructure選択52件、Application 33件が成功。
- 遅い変更を入れる4件と同一objectの1件は、防御を一時的に外したときの失敗を保存し、
  製品ソースをhash一致で復元。修正後の境界12件、alias関連5件が成功。
- App/Presentation buildは警告・エラー0。Presentation 62件、record closureは170/154/失敗0。
- 規約検査が試験のboolean mode parameterを3ファイルで検出。明示入力・型付きpathへ修正し、
  最終23件とbuild、format/verify、conformance112が成功。
- complexity Inspectはexit1で、既存KeyboardHelpPresenterのnesting 4/limit3の1件のみ。
  既存問題を修正対象へ広げず、閾値を変えていない。
- 09:12:30の最終独立読取レビューで必須修正なし。1,548入力を固定し、改行のみの1差も個別証明。

失敗したbuild/style、対照試験、観測修正、読取だけのsetup missも
[転送競合の品質記録](../quality/2026-11-25-transfer-conflicts.md)と恒久ログに保存した。
最後の実行入口は以下。各scriptには実際の引数・filter・exit code・時間を保持する。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-green-06.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-causal-08.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-alias-causal-10.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-consumers-12.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/transfer-conflicts-20261126/run-shape-final-13.ps1
```

変更範囲は内部要件と直接利用側。無関係な全件試験、coverage/mutation、full CIは回していない。
全ADR0095の破壊的操作としての受理、実case-sensitive環境120/124、provider/UI接続、
別設計のfolder mergeは未完了。停止指示後はこれらの新工程へ進んでいない。

## 保存と後片付け

選択操作の5,817ファイル/581,927,878 bytes、フィルターの9,187ファイル/1,146,169,557 bytesを
恒久ZIPへ保存し、全ファイルのhash・サイズを照合した。取込・clean状態・唯一の成果物・
link・稼働参照を確認した後、完了作業コピーと対応一時出力を除去。元branch/commit・生の
証拠は保持した。完了時刻はselection 07:59:36、filter 08:50:51 JST。

#560の未完了作業は`D:/NeNeCommander/worktrees/issue-560`に保持する。
証拠は`D:/NeNeCommander/evidence/transfer-conflicts-20261125`および`transfer-conflicts-20261126`、
一時出力は対応する`D:/NeNeCommander/outputs/transfer-conflicts-20261125`と`transfer-conflicts-20261126`。
終了commit・通常hook結果・clean状態は`shape-commit-16.json/log`で確認する。
他の未完了作業とinteractive03、通常設定、共有入力デスクトップは維持する。

最初の通常commit15は新規試験ファイルの末尾空行で停止。規約112・security24は成功していた。
末尾改行だけを直し、本文一致を確認して通常hook付きで16を再実行。失敗15は保持し、
最終入力hashはmanifest16へ更新した。製品試験は再実行していない。

後片付けの実行待ち・承認待ちはない。次回は[終了時引き継ぎ](../handoffs/2026-11-26-milestone-handoff.md)
から、hideの再開指示を確認して続ける。
