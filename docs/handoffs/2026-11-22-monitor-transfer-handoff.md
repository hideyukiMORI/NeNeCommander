# モニター間移動 — 引き継ぎ

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
