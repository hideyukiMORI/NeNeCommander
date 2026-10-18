# CS-013 優先順の整理・日報 — 2026-10-19

hide の残件優先順に従い、2026-10-18 夜から翌19日にかけて、①残りの
Presentation、②ファイル操作の共通契約、③プロバイダー境界までを整理した。
19日00:08の「区切りのいいところで日報と引き継ぎ書を書いて」に従い、
WSL側の Issue #340 を今回の区切りとする。④設定保存と⑤残りのテスト・
制御フロー整理／全体適用は未着手の次回作業として残す。

## 完了範囲

| 優先順・作業 | Issue / PR | 対象検証 | CS-013 残件 |
|---|---|---|---:|
| 開始点：App画面構成まで整理済み | #330 / #331 | 既存の日報を参照 | 153 |
| ① 表示モデルとキー入力 | [#332](https://github.com/hideyukiMORI/NeNeCommander/issues/332) / [#333](https://github.com/hideyukiMORI/NeNeCommander/pull/333) | 114件成功 | 141 |
| ② 転送契約・Gateway実行責務 | [#334](https://github.com/hideyukiMORI/NeNeCommander/issues/334) / [#335](https://github.com/hideyukiMORI/NeNeCommander/pull/335) | 275件成功 | 118 |
| ③ 管理側のごみ箱コールバック | [#336](https://github.com/hideyukiMORI/NeNeCommander/issues/336) / [#337](https://github.com/hideyukiMORI/NeNeCommander/pull/337) | 82件成功 | 110 |
| ③ Windowsローカル操作・事前確認 | [#338](https://github.com/hideyukiMORI/NeNeCommander/issues/338) / [#339](https://github.com/hideyukiMORI/NeNeCommander/pull/339) | 190件成功 | 104 |
| ③ WSL観測値・事前確認・直接影響するテスト | [#340](https://github.com/hideyukiMORI/NeNeCommander/issues/340) / 対応PR | 273件成功、Live WSL 3件未実行 | 80 |

各行のテストは重複するため、合算して固有テスト数とはしない。今回の優先順の
開始点153件から73件を解消した。#324の初回180件からの累計では100件の解消。
現在は製品20件、テスト60件、eng 0件。CS-013 は引き続き単独の計測ツールで、
全体ゲートを有効化したという意味ではない。

先行変更の詳しい設計・コマンド・検証根拠は、
[Presentation日報](2026-10-18-presentation-input-refactor-daily-report.md)、
[共通契約日報](2026-10-18-transfer-contract-refactor-daily-report.md)、
[ごみ箱日報](2026-10-18-recycle-callback-refactor-daily-report.md)、
[Windows操作日報](2026-10-18-windows-operation-refactor-daily-report.md)に残す。
共通契約の `ResumeAsync` は `TransferConflictResponse` を受け取る4引数へ
全呼出元を同時移行済み。古いシグネチャを次回作業へ持ち戻さない。

## Issue #340 の変更と不変条件

実行経路は既存の `FileOperationGateway` → provider port → I/O execution
boundary のまま。WSLの識別トークン、観測順序、リンク・ドライブ・包含判定、
衝突予約、コピー後検証、検証済みソース削除の条件を維持した。

- `WslRewriteFacts` に同一ハンドル由来の長さ・リンク数・更新時刻・変更時刻を
  集約し、`WslHandleFacts` を7引数から4引数へ整理。元の7プロパティを維持。
- `WslEntryMetadata` に identity・kind・属性を集約し、`WslFileSystemEntry`
  を5引数から3引数へ整理。観測名はパスから再生成せず、kindも属性から再解釈
  しない。元の5プロパティを維持。両グループは不変の内部record。
- WSL内とWSL→Windowsのバッチ事前確認から、1ソースの確認処理を抽出。
  Windows→WSLの検証では、移行先確認と元データ再検証を分離した。Find、
  ドライブ再確認、リンク確認、予約追加、plan追加の順序は変えていない。
- 作成・改名・削除の既存分岐を平坦化。検証済み削除を先に判別し、通常削除の
  再検証は既存の唯一の経路を通す。native ABI、公開port、依存関係は変更なし。
- 既存53テストメソッドと属性を13の責務別クラスへ移し、既存の共有処理を
  3つのコンテキストへ分離。長いテストは元の操作順を維持したまま段階ごとの
  検証へ分けた。2つの競合注入は元の観測回数を維持する専用状態へ移した。
- 観測値の全プロパティ、等価性・hash・record copy、null境界を確認する
  `WslSnapshotTests` 3ケースを追加した。

対象の製品9件・テスト15件の超過を解消。今回の変更・新規C#には超過がない。
`WslFileOperationAdapter` は288 logical linesで上限300以内。

## 検証と実行コマンド

ADR-0061 により、内部責務の移動と直接利用側の境界を対象にした。
固定SDK 10.0.401、作業場所 `D:/NeNeCommander/worktrees/issue-340`、
TEMP/TMP は `D:/NeNeCommander/outputs/issue-340/tmp`。

```powershell
$env:PATH = 'D:/NeNeCommander/tools/dotnet-10.0.401;' + $env:PATH
$env:TEMP = 'D:/NeNeCommander/outputs/issue-340/tmp'
$env:TMP = $env:TEMP
$project = 'tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj'
dotnet restore $project --locked-mode -p:Configuration=Release
$changedCSharp = @((git diff --name-only --diff-filter=AM) + (git ls-files --others --exclude-standard) | Where-Object { $_ -match '\.cs$' })
dotnet format style $project --no-restore --include $changedCSharp --diagnostics IDE0005 --verbosity minimal
dotnet format whitespace $project --no-restore --include $changedCSharp --verbosity minimal
dotnet build $project --configuration Release --no-restore --disable-build-servers
dotnet test --project $project --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~WslInspectionContractTests|FullyQualifiedName~WslMutationTests|FullyQualifiedName~WslTransferPreflightTests|FullyQualifiedName~WslTransferStepTests|FullyQualifiedName~WslTransferExecutionTests|FullyQualifiedName~WslDeletionAdmissionTests|FullyQualifiedName~WslSnapshotTests|FullyQualifiedName~WindowsFileIdentifierTests|FullyQualifiedName~WindowsWslFileSystemTests|FullyQualifiedName~WindowsToWslTransfer|FullyQualifiedName~WslToWindows|FullyQualifiedName~WslTrailing|FullyQualifiedName~Verified|FullyQualifiedName~ProviderFileOperationPortTests|FullyQualifiedName~MappedDriveLocalMutationBoundaryTests|FullyQualifiedName~WindowsLocalCopyTests|FullyQualifiedName~WindowsLocalMoveTests|FullyQualifiedName~LiveWslRootAdmissionTests|FullyQualifiedName~LiveWslRootOwnershipTests|FullyQualifiedName~LiveWslRootLinkCleanupTests|FullyQualifiedName~LiveWslRootSetupTests|FullyQualifiedName~LiveWslRootIdentityTests'
dotnet build $project --configuration Release --no-restore --disable-build-servers
dotnet test --project $project --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~ExecuteAsyncWhenWindowsSourceDeletionStopsAfterFirstLeafReportsPartialSource'
dotnet format whitespace $project --no-restore --include $changedCSharp --verify-no-changes --verbosity minimal
python -X utf8 D:/NeNeCommander/evidence/wsl-provider-refactor-20261019/Verify-WslMigration.py
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
git diff --check
```

最終Releaseビルドは警告・エラー0。最初の選択実行は276件中273成功、失敗0、
3件未実行。最後にテスト構成処理を抽出した1メソッドの3 DataRowだけを再実行し、
3件とも成功した。製品コードはその間変更しておらず、他の成功結果を再利用した。
273件に再実行3件を加えて276件成功とは数えない。最終書式確認も成功。

`FullyQualifiedName~Verified` が実環境テストの名前にも一致したため、以下の
3ケースは `LiveWsl:Unexecuted:RootParameterAbsent` として実行前に止まった。
実ディストリビューションを使った成功証拠とは扱わない。

- `ExecuteAsyncWhenLiveWslTreeMovesToWindowsDeletesVerifiedSourceAsync`
- `ExecuteAsyncWhenLiveCompositeMoveCompletesDeletesSourceAfterVerifiedTargetAsync`
- `ExecuteAsyncWhenLiveWindowsTreeMovesIntoDistributionDeletesSourceAfterVerifiedTargetAsync`

`LiveWslRoot*Tests` は既存の隔離NTFSルートを使うハーネス境界の決定的テストで、
実ディストリビューションを使うLive WSLテストとは異なる。今回の操作・競合検証は
既存の隔離ルートとWSL観測seamを使用した。

照合スクリプトは、53メソッドの属性、元のassertionと実行順、共有helper、
helperへの引数対応、2つの事前確認、Windows側再検証、2つの競合注入本体を照合。
段階化したテストの一部はassertionを各操作の直後へ移したが、assertionの集合・
回数と操作順は維持した。`WindowsFileIdentifier` / `WindowsWslFileSystem`
全体と2つの既存fakeは、観測値グループの構築を展開すると元のトークン列と一致。
作成・改名・削除の平坦化は元の判定優先順と対象テストを照らして確認した。

開発途中のビルドではIDE0046、移動後の不要using、IDE0090を検出し、最終ビルド
前に修正した。警告抑制は追加していない。照合スクリプトの初回実行で見つかった
抽出位置・JSONキー・assertion分類の問題も修正し、最終照合は成功している。

最終計測は821ソース、1,260型、7,993 callable、未解決呼出7,347。
全ソースhashを実ファイルと照合した。80件の内訳は method-lines 32、cognitive
11、nesting 5、parameters 13、type-lines 19。exit 1 は残件の存在を正しく報告
した結果であり、全件準拠とは扱わない。計測ツール自体は変更しておらず、#324の
fixture・launcher検証を再利用した。

通常のcommit hookは維持。内部移動の構造照合と、識別・リンク・ドライブ・衝突・
部分効果・検証済み削除の直接影響するテストで今回の範囲を確認した。新しい信頼
条件や公開契約の変更はなく、未解決の広域影響もないためfull/deep CI、全層mutation、
無関係な回帰実行は選択していない。手動リリース確認 #94 は別件のまま。

## 成果物・統合・次回

恒久証拠は `D:/NeNeCommander/evidence/wsl-provider-refactor-20261019/`。
最終ビルド・テスト・計測ログ、JSON、移行対応表、照合スクリプトと結果、移行用
5スクリプトを保持する。先行4作業の証拠はそれぞれの日報記載先に保持済み。

Issue #340 の対応PRと同証拠領域の `completion.json` を、squash merge、main同期、
後片付けの確定記録とする。作業ブランチとcommitは保持し、取込状態・未保存物・
未追跡／無視ファイル・唯一の成果物・稼働参照・リンクを確認したうえで、
`D:/NeNeCommander/worktrees/issue-340` と `D:/NeNeCommander/outputs/issue-340`
を整理する。削除拒否が出た場合は完了扱いにせず、規約どおり実行待ちを引き継ぐ。

次回は[引き継ぎ書](../handoffs/2026-10-19-cs013-priority-handoff.md)の順序で進める。
現在の残件一覧は[計測記録](../quality/cs013-measurement-2026-10-18.md)と恒久JSONが正本。
