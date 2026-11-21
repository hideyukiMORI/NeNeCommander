# 2026-11-22 日報 — Preview beta.3公開前

記録時点: 2026-11-22 03:46 JST。これは公開前の途中記録。
beta.3はまだ公開していない。最新の実行状態はリンク先と
[引き継ぎ書](../handoffs/2026-11-22-preview-beta3-handoff.md)で確認する。

## 今日の結果

hideがPR402の画面を受理し、「まずリリース、その後に改修」を指定した。
Bookmarkモーダルと表示専用パス/Ctrl+L編集はmainへ統合した。
再アクティブ化時のキー受付など、報告された問題を解決済みとは扱わない。

| 対象 | 結果 |
|---|---|
| #513 / [PR515](https://github.com/hideyukiMORI/NeNeCommander/pull/515) | 既存mainでの非UI移動観測を統合、main e2f675b。画面応答の計測ではない。 |
| #512 / [PR516](https://github.com/hideyukiMORI/NeNeCommander/pull/516) | volume GUID出力配列を50文字へ限定、main 7f0e6d3。関連10件成功。 |
| #518 / [PR520](https://github.com/hideyukiMORI/NeNeCommander/pull/520) | 受理済み操作対象の可変コレクション露出を修正、main d1f5b18。関連134件成功。 |
| #519 / [PR521](https://github.com/hideyukiMORI/NeNeCommander/pull/521) | 同じ並べ替え条件の投影を再利用、main 800844c。Application218件・Presentation36件成功。 |
| [PR402](https://github.com/hideyukiMORI/NeNeCommander/pull/402) | hideの画面受理後にmain 08c2d9cへ統合。受理した3764d7eとtree一致、結合時の関連71件成功。 |
| #514 / [PR517](https://github.com/hideyukiMORI/NeNeCommander/pull/517) | AppSDKの依存lock整合をDraft公開。関連78件成功。native確認未完のため未統合、今回の配布対象外。 |
| #522 / [PR529](https://github.com/hideyukiMORI/NeNeCommander/pull/529) | beta.3の版番号・配布説明・必要なテスト補完を準備。Draft、公開前検証中。 |

#519の固定ABBA比較では、10,000項目・同一既定順・preferred focusなしの
割り当てが401,096から96 bytes/callになった。測定batch平均は
9.215600–9.910630から0.045935–0.059680 ms/call。
全576記録・1,035入力を検証した値であり、native UI応答やアプリ全体の倍率ではない。
preferred focusの処理は残る。

## 不変条件と検証

操作対象はFileOperationRequestの一つのsnapshotが所有し、実行は既存
FileOperationGatewayが担う。並べ替えは既存EntryOrdering/PaneReducerで
所有済み投影だけを再利用し、focus回復を飛ばさない。
UI入力は既存CommanderSessionのBookmark/AddressEditorStateから実行する。
配布は既存eng/publish-release.ps1だけを用いる。

通常変更では影響範囲に限った試験を実行し、受理済み入力が同じ証拠を再利用した。
対象Release buildは警告・エラー0、変更formatは成功。
既存のCS-013検出26件は既存記録と同じで、新規免除や閾値変更はない。
詳細コマンドと失敗した準備試行は次の記録へ収載済み。

- [非UI観測](../quality/2026-11-21-background-move-phases.md)
- [GUID配列](../quality/2026-11-22-volume-guid-buffer.md)
- [操作対象の所有権](../quality/2026-11-22-operation-source-ownership.md)
- [同一並べ替え条件の再利用](../quality/2026-11-22-equal-sort-projection.md)
- [Bookmark受理と結合確認](../quality/2026-11-22-bookmark-acceptance.md)
- [beta.3公開前検証](../quality/2026-11-22-preview-beta3.md)

## リリースの残り

最初のdeep review [44934477990](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44934477990)
は機能試験後、Application branch coverage 99.93%で失敗した。
必要な100%を下げず、#549で128/129文字のfolder leafからBookmark登録する
公開操作の境界を2件追加した。製品コードは変更していない。

修正後の登録試験は7/7、Applicationは843/843、
同じcoverage.settingsでApplication branch coverage 100%。
Release buildと変更formatも成功した。
修正前の841件成功・coverage不足と最初のログは残している。

現在のdeep reviewは
[44936588484](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/44936588484)。
対象は9ca919d9c7d51388bfc79718c145b363560edb03、03:46時点で実行中。
canonical gate、依存audit、反復adversarial、mutationを含むため、
同じ入力のqualityを別途実行しない。
dependency review 44934652602は成功済み。03:44のopen CodeQL alert照会は0件だが、
今回のanalysis完了後に再照会する必要がある。

残作業はdeep reviewの全結果確認、CodeQL analysis成功とopen alert 0件の確認、
PR529の通常受理・squash統合、受理したclean sourceからの正式packaging、
全payload/ZIP/入力/notice/PRI/展開結果の照合、tagとPreview公開、
公開3assetの再download照合、完了記録・安全な整理。
まだ正式候補、beta.3 tag、公開assetの作成完了を主張しない。

#94の一度限りの画面確認はhideの11月19日報告で完了、
PR402の画面受理は11月22日に完了した。#94で選んだ倍率の正確な値は未記録。
新しいPC・全provider実環境・全locale視覚確認・native速度改善は未証明。
共有デスクトップのキー・マウス操作を再開していない。

## 次版の要望28件

以下はIssue登録済みで、現在の配布候補へ実装を混ぜない。
キー割当、常時topmostの要否、中国語の表記系など未決定の細部は、
各Issueの設計時に確定する。

| Issue | 内容 |
|---|---|
| [#523](https://github.com/hideyukiMORI/NeNeCommander/issues/523) | 非アクティブからペイン外クリックで戻った際のキー受付を回復する |
| [#524](https://github.com/hideyukiMORI/NeNeCommander/issues/524) | 一覧の上下端でフォーカスを循環させる |
| [#525](https://github.com/hideyukiMORI/NeNeCommander/issues/525) | コマンドパレットを幅を抑えたモーダルへ調整する |
| [#526](https://github.com/hideyukiMORI/NeNeCommander/issues/526) | 操作対象ペインの表示を一か所へ集約する |
| [#527](https://github.com/hideyukiMORI/NeNeCommander/issues/527) | NibとFolioに揃えた125種類のテーマを選択可能にする |
| [#528](https://github.com/hideyukiMORI/NeNeCommander/issues/528) | 日本語・英語・中国語を設定で選択可能にする |
| [#530](https://github.com/hideyukiMORI/NeNeCommander/issues/530) | マウスで一覧操作と主要画面への入口を使えるようにする |
| [#531](https://github.com/hideyukiMORI/NeNeCommander/issues/531) | 右クリックとキーボードで共通のコンテキストメニューを開く |
| [#532](https://github.com/hideyukiMORI/NeNeCommander/issues/532) | コピーと移動の結果が分かるドラッグ＆ドロップを追加する |
| [#533](https://github.com/hideyukiMORI/NeNeCommander/issues/533) | 選択ファイルのテキスト内容を読み取り専用で表示する |
| [#534](https://github.com/hideyukiMORI/NeNeCommander/issues/534) | 選択画像を読み取り専用でプレビューする |
| [#535](https://github.com/hideyukiMORI/NeNeCommander/issues/535) | 処理状況や詳細情報をコンソール風の画面で確認する |
| [#536](https://github.com/hideyukiMORI/NeNeCommander/issues/536) | Telescope風に候補を絞ってプレビューできる検索画面を設計する |
| [#537](https://github.com/hideyukiMORI/NeNeCommander/issues/537) | オンオフ可能なサイバー風フォーカス演出を設計する |
| [#538](https://github.com/hideyukiMORI/NeNeCommander/issues/538) | 選択ファイルの詳細情報を段階的に表示する |
| [#539](https://github.com/hideyukiMORI/NeNeCommander/issues/539) | フォルダーのファイル数と合計サイズを遅延集計する |
| [#540](https://github.com/hideyukiMORI/NeNeCommander/issues/540) | ペイン幅をキーボードとマウスで調整する |
| [#541](https://github.com/hideyukiMORI/NeNeCommander/issues/541) | CPU・メモリ・ネットワーク表示を設定とキーで切り替える |
| [#542](https://github.com/hideyukiMORI/NeNeCommander/issues/542) | 現在フォルダーのファイル数とフォルダー数を表示する |
| [#543](https://github.com/hideyukiMORI/NeNeCommander/issues/543) | アーカイブを仮想フォルダーとして閲覧・取り出す |
| [#544](https://github.com/hideyukiMORI/NeNeCommander/issues/544) | キーボードとメニューから新規テキストファイルを作成する |
| [#545](https://github.com/hideyukiMORI/NeNeCommander/issues/545) | 現在フォルダーの一覧をキーワードで絞り込む |
| [#546](https://github.com/hideyukiMORI/NeNeCommander/issues/546) | パス入力モーダルからドライブを視覚的に選択する |
| [#547](https://github.com/hideyukiMORI/NeNeCommander/issues/547) | 反対側のペインを現在と同じフォルダーへ移動する |
| [#548](https://github.com/hideyukiMORI/NeNeCommander/issues/548) | Ctrl+Gの場所一覧を全幅ではないモーダルへ調整する |
| [#550](https://github.com/hideyukiMORI/NeNeCommander/issues/550) | F6移動後に元の位置に近い項目へフォーカスを戻す |
| [#551](https://github.com/hideyukiMORI/NeNeCommander/issues/551) | キーボードから別モニターへウィンドウを移動する |
| [#552](https://github.com/hideyukiMORI/NeNeCommander/issues/552) | グローバルホットキーで表示を切り替え前面へ呼び出す |

情報取得は重い集計を遅延実行し、文字の変化/読込表示から確定値へ移る要望を保持する。
演出offでは静的表示にし、古い選択の結果を新しい選択へ適用しない。
mouseとkeyboardは同じcommandを呼ぶ。
アーカイブ閲覧は初期charterの非目標を変更するため、将来のADRが必要。
コンソール風表示から、埋込terminalを追加するとは解釈しない。
新規フォルダーのF7は既に存在する。
モニター移動と表示hotkeyは#551/#552に記録し、
表示時の前面化・入力focus回復、異なるDPI、状態保持を受入候補とした。

## 作業物と保全

#518/#519は受理treeとmergeの一致確認後、2,115ファイル/644,652,085 bytesを
全件hash照合して恒久保全し、専用worktree/outputを削除した。
#512は399ファイル/129,170,742 bytesを保全して専用作業物を削除済み。
Bookmarkの398/430/431/445/457 worktreeは7,004ファイル/1,582,125,646 bytesを
保全・全件照合して03:34:45に削除した。branch/commit/証拠は保持する。

03:46時点でhideのApp PID14492はissue-495のbinaryから稼働中。
そのworktreeと関連する未整理outputは残す。設定を過去のbackupで上書きしない。
issue-491、issue-514、pr-498-review、issue-522も未完了のため保持する。
正確なpathと再開手順は引き継ぎ書にある。
今回の整理に自動承認による拒否・hide実行待ちはない。
稼働中/未完了の作業物は意図して残している。
