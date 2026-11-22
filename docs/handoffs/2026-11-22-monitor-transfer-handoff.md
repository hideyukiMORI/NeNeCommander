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
