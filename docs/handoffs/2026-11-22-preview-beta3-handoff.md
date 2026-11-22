# 2026-11-22 引き継ぎ — Preview beta.3公開前

記録時点: 2026-11-22 03:46 JST。
[日報](../reports/2026-11-22-preview-beta3-daily-report.md)と
[公開前検証記録](../quality/2026-11-22-preview-beta3.md)を併読する。
この文書は公開前の途中記録であり、古い「PR402受理待ち」を引き継がない。

## 最新のhide指示

PR402の現状画面は受理済み。「まずリリース、その後に改修」が優先。
追加28件は日報のIssue一覧に収載した。最新2件は#551モニター間移動、
#552グローバルhotkeyによる表示/非表示と表示時の前面化。
常時topmost固定や自動起動の要否は未確定。
今回、日報と引き継ぎ書の作成を依頼された。
この依頼をrelease済み・新機能実装済み・停止指示とは扱わない。

通常は単体実行。共有デスクトップの入力を使わない。
既存の画面受理を、新しいnative試験や稼働App終了の許可へ広げない。
サナの各対話末尾にはJST日時を付け、hideと呼ぶ。
過去のモデル固定分担や一律広範囲テストは引き継がない。

## 現在地

- main: 08c2d9ce3e79d9ed482b7485c641d90dcbe14e54、記録時点でremote同期・clean。
- PR402はmerge済み。受理3764d7eとmergeのtree一致、関連71件成功。
- #518/#519はPR520/521でd1f5b18/800844cへmerge・専用整理済み。
- release: #522 / [PR529](https://github.com/hideyukiMORI/NeNeCommander/pull/529)、Draft。
- branch: chore/522-preview-beta3。
- worktree: D:/NeNeCommander/worktrees/issue-522。
- 実行中review対象: 9ca919d9c7d51388bfc79718c145b363560edb03。
- 本文書追加後の差分はdocsだけ。実装/test/dependency/tool入力が同じなら
  review成功証拠は再利用し、commit番号だけを理由にやり直さない。
- 恒久証拠: D:/NeNeCommander/evidence/release-beta3-20261122。
- 一時output予定: D:/NeNeCommander/outputs/release-beta3-20261122。
- 正式候補予定: D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01。
- beta.3は未公開。既存beta.2を差し替えていない。

## 検証の状況

最初のdeep review44934477990はApplication coverage99.93%で失敗。
唯一の不足分岐はActivePaneBookmarkDefaults.Fromで128文字を超えるfolder leafを
BookmarkDisplayNameとして拒否し、登録名を空にする経路だった。
#549で公開CommanderSession経由の128/129境界2件を追加した。
製品コード、gate、閾値、除外、waiverは変更していない。

局所検証: 登録7/7、Application843/843、Application branch-rate1.0、
build警告/エラー0、変更format成功。修正前841/841と99.93%も証拠に保持する。
exact commandsはquality記録。Application-only reportに同梱されるDomainの
部分coverageをDomain単独の退行と解釈しない。

第二deep review:
https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44936588484
job136944433359、03:46時点でRun deep review実行中、CodeQL analysis未完。
旧job136938009563と取り違えない。dependency review44934652602は成功済み。
03:44のCodeQL open alertは0件だが、新analysis後の確認はまだ必要。

## 再開手順

1. AGENTS/PROJECT_STATEとPR529の最新head、run44936588484を読み直す。
   実行中なら重複workflowを起動しない。成功後にreport/artifact/full job logを保存し、
   canonical/audit/adversarial/mutation全チェック、review対象入力、
   96時間以内の鮮度、CodeQL analysis成功とopen owned alert 0件を確認する。
   失敗は実原因を限定修正し、閾値を下げない。
2. PR529の最終diff/証拠/未解決threadを確認してReady、必要なdependency reviewを確認、
   正確なheadを条件に通常squash mergeする。mainをff同期し受理treeとmergeの一致を証明。
   この日報追加のdocs-only差分があるときは、9ca919dとの非docs入力一致も記録する。
3. 受理されたclean sourceを用い、既存の一つのpackaging入口を実行する。
   candidateが既に存在したら上書きせず、状態確認後に次の番号を選ぶ。

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory D:/NeNeCommander/releases/0.1.0-beta.3/candidate-20261122-01 -WindowsSdkLicensePath D:/NeNeCommander/evidence/issue-434/windows-sdk-license.rtf
```

4. 証拠領域のverify-candidate.ps1、verify-pri-locales.ps1を実行。
   manifest/source/locks、全payloadとZIP、notice原本、SDK license、
   en-US/ja-JPのPRI文字列、XBF11個、AMD64・runtime・version・署名状態を照合する。
   ZIPを専用D outputへ独立展開し、全展開fileのsize/hashも確認する。
   sourceがmain側へ移る場合は検証scriptのRepositoryRootと固定pathを見直す。
5. 正確な受理sourceへannotated tag v0.1.0-beta.3を付け、
   ZIP/release-manifest.json/SHA256SUMS.txtの3assetでPreview prereleaseを公開する。
   release notesには#523再アクティブ化と#550F6後focusの既知報告、
   未実施環境、deep evidenceとarchive hashを記載する。
6. 公開release metadataをpublished-release.jsonへ保存し、verify-public-assets.ps1で
   認証なし再downloadと3asset hash/service digest/local一致を確認する。
7. quality/report/handoff/PROJECT_STATEへ実結果を追加し、通常PRで恒久収載する。
   不要な専用生成物はarchive全件照合、絶対path、tree/未追跡/ignored、
   唯一の成果物、linkとlive参照を確認してから整理する。
   branch/commit/恒久証拠/正式配布候補は残す。

今回の文書だけの追加にproduct build/test/CIを起動しない。
通常のCommit hook、diff、相互linkと数字の確認を行う。
full qualityはdeep reviewに含まれるため別途起動しない。

## 残す作業物と安全境界

| path | 保持理由 |
|---|---|
| D:/NeNeCommander/worktrees/issue-495 | 03:46確認のhide利用App PID14492の実行元。終了・削除しない。 |
| D:/NeNeCommander/outputs/bookmark-ux-20261119 | 過去fixtureと設定参照の分類が残る。唯一の成果物/live参照確認前に削除しない。 |
| D:/NeNeCommander/outputs/bookmark-navigation-20261120 | issue495の未整理関連output。終了後に内容を確認する。 |
| D:/NeNeCommander/worktrees/issue-491 | #491/PR492の不成立native比較と記録を保持。停止系列を勝手に再開しない。 |
| D:/NeNeCommander/outputs/f6-continuation-comparison-20261119 | #491未完了の比較素材・記録。 |
| D:/NeNeCommander/worktrees/issue-514 | AppSDK PR517はnative確認未完、未統合。 |
| D:/NeNeCommander/worktrees/pr-498-review | 上記依存更新の元PR調査記録。 |
| D:/NeNeCommander/outputs/appsdk-270-20261122 | AppSDK未完了作業の出力。 |
| D:/NeNeCommander/outputs/pr-498-review | 同じく元PRのlocked restore調査。 |
| D:/NeNeCommander/outputs/quality-investigation-20261122 | #518/#519調査の元出力。恒久証拠との重複/唯一の成果物の整理確認が残る。 |
| D:/NeNeCommander/worktrees/issue-522 | beta.3公開作業中。 |
| D:/NeNeCommander/evidence/* と releases/* | 恒久証拠・正式配布成果物。整理対象にしない。 |

利用中Appと設定は別作業で変更され得る。過去backupで現在の設定を上書きしない。
#453/#491はnative応答の証明が未完で、非UI観測やmanaged benchmarkを代替にしない。
AppSDK PR517は今回のreleaseに混ぜない。

#518/#519の2,115file/644,652,085bytes、Bookmark旧5worktreeの
7,004file/1,582,125,646bytesは全件照合して保全・限定整理済み。
各evidenceのcompletion.jsonとretired-worktrees-completion.jsonが正本。
旧#454/#459の4対象は以前の読取確認で不存在、拒否された削除を再試行しない。
今回の整理に自動承認拒否・hide実行待ちはない。
未完了/稼働中は上表の理由で保持する。

## 追加要望を実装するとき

日報の28Issueを読み、release完了後に優先順位を選ぶ。
#523と#550は利用中に報告された問題で、最新版での再現条件を先に固定する。
125テーマはNeNeNib/NeNeFolioとの整合、日本語/英語/中国語選択、
lazy情報表示・演出off、keyboard/mouse同一commandの契約を保つ。
アーカイブはcharter/ADR、新しいglobal hotkeyはwindow lifecycle/keyboard契約を
先に設計し、無関係な依存や二重実行経路を増やさない。

## 最新の実行状態 — 2026-11-22 15:06 JST

この節が上の03:46時点の実行中・未確定情報を更新する。hideは13:25以降、
マイルストーン継続、必要な試験/計測、全試行の記録、停止指示までの継続を依頼した。
現在はまずbeta.3公開を完了する段階。新機能の実装へ勝手に優先順位を切り替えない。

- mainは08c2d9c。issue522作業branchの製品headは264cffa、PR529はDraft。
- 第二deep44936588484/9ca919dは19チェック成功。3つのCodeQL指摘を#553で修正。
- 全ローカル補完検証と独立レビューは完了。正確な数値・制約は
  [品質記録](../quality/2026-11-22-release-codeql-corrections.md)の最終節を参照。
- 完成SARIF: `D:/NeNeCommander/evidence/release-beta3-20261122/codeql-corrections/final.sarif`。
  3,548,275bytes、SHA256 `d255f59d3bc8ab1885deae032c446da04ac23551f5857a5a57b00ae23b9e9e64`。
- 自動承認レビューがこのpayloadの公開GitHub登録を2回拒否。送信先/正本規約を確認しても、
  ローカルパスを含む具体的な外部送信についてhideの明示承認が必要という理由だった。
  候補branchと統合後mainへの送信をhideへ質問済み。返答がなければ送信を進めない。
  別API・workflow・間接実行へ切り替えて拒否を回避しない。
- 解析・mutation・対象テストを再実行する必要はない。新しい入力変更、証拠の具体的欠落、
  96時間期限超過が起きた範囲だけを選ぶ。記録スクリプトの失敗も既に保全・修正した。
- 承認後は同じ固定SARIFを正規CLIで登録し、APIのprocessing success/エラー/警告/指摘を確認。
  CodeQLの異なるanalysis configurationで古い指摘が残る場合、削除/dismiss/baseline化しない。
- PRの最終docs/受理/通常squash後、accepted tree一致を根拠に同じ解析証拠をmainへ適用し、
  mainでも成功解析とopen alert0を確認する。packaging以降は上の再開手順のまま。
- 正式candidate、tag、releaseはまだ作成していない。公開済みとは報告しない。
- 後片付け拒否は発生していない。issue522/outputは未完了として残す。
  ユーザーApp/issue495、#491、AppSDK#514、他の恒久証拠を変更しない。

今回の最終記録の変更はMarkdownのみ。通常Commit hookとdiff/link確認だけを行い、
成功済みの製品テストやCIを文書更新だけで再実行しない。