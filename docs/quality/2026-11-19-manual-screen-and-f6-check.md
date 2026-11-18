# 実機の表示確認とF6移動 — 2026-11-19

Issue: #94。関連: #448、#453、#454。
規則: QLT-009/011/015、GIT-001〜004、ADR-0054/0061。

## 実施したことと証拠の範囲

hideの依頼で、通常のRelease版を起動し、確認用のSQL/TXTと移動先を用意した。
表示設定はhideが操作する方針を維持し、サナはWindowsの倍率・高コントラストを変更していない。

| 確認 | 結果と根拠 |
| --- | --- |
| 100%または200%の画面で両一覧・アドレス・状態・キー案内とキー操作 | hideが確認項目2を「大丈夫」と報告。選んだ倍率の特定と独立したcaptureは無い |
| Ctrl+W、F2/F7/F8の画面、Escで取消 | hideが確認項目3を「大丈夫」と報告 |
| 高コントラスト表示 | hideが確認項目4を「大丈夫」と報告。案内には設定を元に戻す操作も含む。復元の独立測定は無い |
| 専用の2ファイルをSpaceで選択してF6 | hideが「問題なさそう」と報告。移動後の内容とアプリの継続をサナが独立確認 |
| 狭い窓のフッター | hideが右側から案内・情報が隠れると報告。未解決として#454へ分離 |

#94は一度の確認を実施し結果を記録した、という意味で完了とする。
全ての幅で表示が良好、PR402のブックマーク画面を受理、次版の公開を承認、という意味には広げない。

## 実行版とF6の照合

- source: `6b0d2316101614ab8bc1cb4a979816e5a98416eb`。
- ProductVersion: `0.1.0-beta.1+6b0d2316101614ab8bc1cb4a979816e5a98416eb`。
- exe: `C:/Users/info/WORKS/NeNeCommander/src/NeNeCommander.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe`。
- exe SHA256: `1B333A6B5F70F433E46EA924FAA910B48B1C566A88F5092CD8AE460B4A2DD4E8`。
- 専用領域: `D:/NeNeCommander/outputs/f6-check-20261119-0038/`。左を`source`、右を空の`target`へ合わせた。
- サナはF6を押していない。hideがSpace、下、Space、F6で移動した。

| ファイル | byte | 移動前後で一致したSHA256 |
| --- | ---: | --- |
| f6-check.sql | 106 | `25DACE8D753FA546A9F8D1904BC6FEA5D3F21F8D040B2B897C9BB4231AD4A5EA` |
| f6-check.txt | 105 | `2BE0A3799A573997076A39371BA756B0019DE3C23F2BE01AD71B81E63F009E09` |

00:46 JSTの読取でsourceは0ファイル、targetは2ファイル、両SHA256一致。
起動したプロセスは応答を続け、画面には「移動が完了しました」と表示された。
一度の管理されたケースの成功であり、元のDownloadsからの全条件を再現したとはしない。
公開beta.1はsource `46f1a82`のままで、この修正を含まない。

原本は`D:/NeNeCommander/evidence/f6-check-20261119-0038/`の
`before.json`、`after.json`、`manual-observations.json`。
アプリが専用領域を参照しているため、今回のfixtureは保持する。

## 残った調査

- [#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453): F6受付から完了まで一瞬もたつく、1秒未満という本人の体感。時間は未実測。キー受付、安全確認、実移動、左右の再読込、表示を分けて調べる。UIを通らない前日の約57msの移動試験やmemory-only診断は今回の応答時間を証明しない。原因やAVの影響は未確定。
- [#454](https://github.com/hideyukiMORI/NeNeCommander/issues/454): 狭幅でフッターを2段・4段などへ折り返す案と、キーヘルプ表示の設定を検討。次回、バックエンドのNeNe Commanderリナに`/design`を使ってもらい相談する。案は未採択で、再現幅の測定・相談・実装は未着手。

hideの停止指示に従い、今日は記録まで。設計の相談や追加性能測定は実行していない。

## 今回の文書変更の検証

README、現在欄、この品質記録、日報、引き継ぎの5文書だけを変更する。
実行した確認コマンドと最終結果はPRと
`D:/NeNeCommander/evidence/session-20261119/completion.json`に記録する。
ADR-0061に従い、対象は本文・local link・差分の確認と通常のCommit hook。
製品のbuild、test、full/deep CIは追加実行しない。製品・依存pin・schema・gateは不変。
Waivers: none。
