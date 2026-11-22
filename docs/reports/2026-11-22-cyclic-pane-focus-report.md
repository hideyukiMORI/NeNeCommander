# 2026-11-22 日報 — 一覧フォーカスの循環

#524の次版向け変更を実装した。ファイル一覧の末尾でj/Downを押すと先頭へ、
先頭でk/Upを押すと末尾へ移る。既存PaneReducerの一つの経路を使い、
選択・表示対象・左右ペインの独立性を保持する。半ページ移動や先頭末尾指定、
Locations/Settings/Bookmark内の規則は変更していない。

旧実装で追加10件中6件の期待位置が失敗し、4件が成功した。
修正後のApplication関連291件のうち、旧期待値が残った選択保持2件を補正し、
そのクラス12件を再検証した。重複を除いた291件の証拠が揃っている。
Presentation37件、App/対象testのRelease build、変更書式は成功。
build警告/エラー0。CS-013の5件は未変更の既存箇所と完全一致した。
改行だけの書式修正は正規化後の内容一致を記録し、製品試験を繰り返していない。

[品質記録](../quality/2026-11-22-cyclic-pane-focus.md)に正確なコマンド・失敗・再利用根拠を収載。
証拠はD:/NeNeCommander/evidence/cyclic-pane-focus-20261122。
これはmanaged状態と表示/入力mapperの証明で、共有デスクトップでのnative操作は行っていない。
PR統合と作業物整理の実結果はclosing PRとcompletion.jsonで確定する。

先行する[Preview beta.3](https://github.com/hideyukiMORI/NeNeCommander/releases/tag/v0.1.0-beta.3)
は15:46公開・15:49公開asset再download照合まで完了、完了記録PR554もmain2dbef98へ統合済み。
今回の変更をその公開assetへ差し替えていない。
release専用生成物は別の保全処理で整理中。遅いjunction調査の中断と
262文字のcache path対応は同release証拠領域に全試行を保存している。

#523再アクティブ化、#550移動後focus、#453/#491 native観測、AppSDK PR517は別。
利用中Appと設定は変更していない。hideの停止指示まで継続する。
