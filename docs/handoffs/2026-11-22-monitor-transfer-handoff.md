# モニター間移動 — 引き継ぎ

最終更新: 2026-11-24 21:06 JST。Issue #551はローカル実装と対象検証まで完了。物理操作・画面受理・公開・main統合は未完。以下の古い調査時点は履歴として保持する。

- 単一経路は既存WindowAdjustmentSession/Planner → WindowAdjustmentRoute → AppWindowPlacementAdapter。Applicationが純粋な画面順序・配置・資格条件を決め、Appだけが既存SDKで取得/適用する。新native import、provider経路、設定保存経路は追加していない。
- Ctrl+Wのn/p・Ctrl+左右で隣の画面へ移動。exact owner、停止、途中leave、再入・最小サイズ変更と不確かな結果をtyped receiptで守る。ヘルプはこの面だけ縦7群＋スクロール本文、固定Esc/PgUp/PgDnとfocus sink。半ページ送り・上下限clamp・新ownerの先頭復帰を共通経路で扱う。
- Application120＋3対象ケース、Presentation72＋68共有入力境界と後続scroll選択の記録を再利用。後続選択には重複があるので合計テスト数としない。App最終build10/診断23は警告・エラー0、112conformance、最終record閉包141/130/0fail。CS-013は既存4指摘と完全一致で成功扱いしない。
- 最終3言語×4画面×13outcome＋同一文言の新owner12件で7556layout checks。日本語・中国語の実ウィンドウroute各56を加えて7668/0fail。500x400physical、125/150/150/175%、現在のtext scaleだけの証拠。
- 170source・137XBF/PRI・4 DLL一致。1268入力を20:55に再照合し全一致。正常設定/hash/mtimeと入力desktopは全試行で不変。物理キー/IME/hold/speech/custom DPI/text scale/切断再接続/小さいclient/人間の画面受理は未確認。
- 失敗試行は全保存。実欠陥はfull-viewportで行が跨ぐこと、共通wrap panelのmeasure/arrange幅差で日本語末尾行が届かないこと。誤判定はscrollbar自身/zero-area barの収集と100ms固定の完了待ち。後者は20回以内の観測だけに変更し要求再送なし。共有wrap panelは製品変更していない。

作業コピーD:/NeNeCommander/worktrees/issue-551、証拠D:/NeNeCommander/evidence/monitor-transfer-20261124、診断出力D:/NeNeCommander/outputs/monitor-transfer-20261124を保持する。旧11月22日の証拠も残す。最新の[品質記録](../quality/2026-11-22-monitor-transfer-investigation.md)、[ADR0092](../adr/0092-display-transfer-through-window-adjustment.md)、[設計handoff](../design/2026-11-24-monitor-transfer-handoff.md)が技術判断の正本。

実装と記録は13b936aへ保存済み。branchはfeat/551-display-transfer、受理済みmain1d0fa30への接続はc09f355。接続の競合は4文書だけで、最新の公開記録と551固有の3項目を保持した。製品入力1260件のhashと全非文書diffが不変、通常hook成功。次は未実施の画面受理と、具体的な公開payloadの承認範囲を確認する。画面試遊は用意してからhideの受理を得る。共有desktopへの入力/CUA、通常設定の置換は行わない。ソース公開の過去の拒否payloadを再送せず、新しい具体的feature payloadができてから承認範囲を照合する。beta.4は別の3909039から公開済みで、この機能は入っていない。

## 過去の時系列記録
Issue #551 / 2026-11-22。調査のみ完了。製品実装・技術ADR・デザイン・実画面受理は未完了。

- worktree: `D:/NeNeCommander/worktrees/issue-551`
- branch: `docs/551-monitor-transfer-design`; base: `7ef9b96`
- 恒久証拠: `D:/NeNeCommander/evidence/monitor-transfer-20261122/`
- 診断出力: `D:/NeNeCommander/outputs/monitor-transfer-20261122/probe/`

単一経路は既存WindowAdjustmentSession/PlannerとWindowAdjustmentRoute/
AppWindowPlacementAdapter。純粋な配置データは既存契約に含まれるが、OSアクセスを
Applicationへ追加してはいけない。UI hostが配置方針を決める経路も追加しない。

[品質記録](../quality/2026-11-22-monitor-transfer-investigation.md)と
[設計資料](../design/2026-11-22-monitor-transfer-investigation.md)を先に読む。
36条件の初回と24条件の追加測定は成功し、XAML倍率の遅延20条件と一時的に別画面へ
所属する反例を保存した。結果はAPIのタイミング保証でも実画面の証明でもない。
成功した測定を工程変更だけで繰り返さない。通常アプリ・設定・共有デスクトップの
キーボード/マウスを操作しない。

次は二段階のqualifiedな完了か、宣言済みnative倍率境界かを決める。
前者は誤った画面倍率、古いstate、割込み、適用途中の失敗を明示する。後者はSEC-014の
Infrastructure/source-generated marshallingとADRを必要とし、診断のDllImportを
製品へコピーしない。選んだ契約に対してADR-0085または次の空き番号を使う。
ADR-0084はローカル#547が使用中。キー・順序・最大化/非表示・最小サイズ・丸めの
提案は未確定であり、本文を受理済み仕様として実装しない。

公開ソースpushの17:20拒否とClaude資料送信の18:09拒否は別の承認待ち。
新しいブランチ・ツールによる回避、15:24のSARIF承認の拡大解釈をしない。
ローカル文書commit以外の公開・PR・統合は未実施。cleanup拒否ではない。

このworktree、出力、証拠は未完了のため保持。#525/#542/#547/#548の候補、#523、
native#453/#491、AppSDK PR517、通常App/設定には触れない。main統合と恒久成果物の
収載が済んでから、未保存物・参照・リンクを確認して正確な対象だけ整理する。

2026-11-24 17:03 JST更新：現在の継続指示で調査を再開。D:/NeNeCommander/evidence/monitor-transfer-20261124
のjournal.md、design-advice.md、scale-api-observation-01.json、transfer-compensated-summary-01.jsonを参照。
製品変更/ADR受理/base統合なし。4画面倍率取得成功、window単位APIはE_NOTIMPL。新配置案24/24成功だが、
最小サイズ・切断/再接続・OS下限22621・custom DPI・最大化・途中失敗と停止所有権は設計未確定。
旧ソース送信拒否のpayloadは送っていない。公開Issueと一般UXだけの新規Claude文章相談を実施し、
tools/file access/session persistence無効。/design未対応は既知で、生成画面とは主張しない。
新出力D:/NeNeCommander/outputs/monitor-transfer-20261124を保持。通常設定/入力不変、診断プロセスは終了。
他作業：548はee1859aでlocal clean。531はApp/Infra mutation成功、Presentation実行中。571のf6push承認は
12:55に解消済み、14:45のSARIFだけの質問は未回答。merge/tag/releaseは未実施。古い承認待ちを混同しない。

17:35更新: f6基盤を8f34e7cへ製品同一で接続。ADR0092は独立再レビュー指摘なしを確認し技術受理。n/p＋Ctrl左右・通常表示状態・中央配置・単一ルートの二段階適用とowner/stop/partial receiptを実装する。minimum4条件は0警告/エラー、正常終了/通常設定・前面不変。製品実装・画面受理・公開/releaseは未完。次は所有者/計算のfocused proofから進める。
