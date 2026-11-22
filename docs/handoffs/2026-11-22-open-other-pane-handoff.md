# 反対ペインで現在の場所を開く — 引き継ぎ

Issue #547 / 2026-11-22。技術コア・キー入力はローカル受理済み。
アイコン、native、公開、統合、後片付けは完了扱いにしない。

## 再開場所と単一経路

- 作業コピー: `D:/NeNeCommander/worktrees/issue-547`
- branch: `feat/547-open-other-pane`; base: `7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2`
- 証拠: `D:/NeNeCommander/evidence/open-other-pane-20261122/`
- 一時出力の予約先: `D:/NeNeCommander/outputs/open-other-pane-20261122/`

ADR-0084、CMD-002/006、KEYBOARD_MODELが正本。DualPaneSessionが表示済みの
元の場所と反対側を捕捉し、既存NavigateAsyncを使う。readinessはsnapshotの
派生値。Idle/PendingはEscapeの宛先だけを保持し、read/tokenを所有しない。
同一PaneLoading参照だけへEscapeを渡し、finallyは自分のcaptureだけを消す。
既存AsyncWorkOwnerはEscape以外の並行入力を拒否する。

Ctrl+EはEの文字イベントとU+0005から正規mapperを通る。raw Eは先行イベントの
まま。既存repeat guardを利用。パレット行と上部は選択コマンドの実際の対象を示す。
選択なしの検索では捕捉時の既定対象を保つ。各ダイアログの所有を越えない。

## 検証と残件

[品質記録](../quality/2026-11-22-open-other-pane-local.md)に全試行と範囲を記載。
基本12、lifetime7、admission9、scope/operation13、palette Application14、
既存ペイン境界72が成功。Presentationは160/161の旧宣言数を更新したうえで、
レビュー修正の影響する33条件が全成功。重複は合算しない。
AppはRelease buildで警告0/エラー0。書式と複雑度・入力hashの最終結果は証拠JSON。
独立レビューは見出しと遅延完了証拠を再確認して18:52 JSTに技術受理。
初回commitのCS-002違反は新規試験5ファイルのboolケース分岐。既存の型を
引数にするhelperと明示テストへ修正し、規約112項目を再確認済み。最終試験記録・
入力manifestが古い保存点に優先する。製品コードはこの修正では変えていない。
同じ入力の成功済み検証を、commit/push/工程変更だけで繰り返さない。

1. Claude原稿の送信承認を確認してから設計相談を再開する。
2. active-headerの方向アイコン、同じintentを送るpointer配線、意味を明確にした
   tooltip/UIAを既存semantic resourceで実装し、設計・実装・画面を検証する。
3. 隔離環境が確保された場合だけ、Ctrl+E・クリック・focus・両方向・同一場所・
   読込中Escape・狭幅/高コントラスト/DPIを確認する。共有デスクトップ入力はしない。
4. 外部送信が明示承認された範囲だけ公開し、Draft PRから適切な受理へ進める。
5. main統合と恒久成果物の確認後、正確な対象・未保存物・稼働参照を確認して整理する。

## 承認待ちと保持理由

- Claude: 18:09に2,593バイトの`design-brief.txt`送信が自動承認レビューで拒否。
  未公開仕様/計画の具体的payload・送信先承認が必要という理由。質問は未回答。
- public GitHub: 17:20の#542 source push拒否は未解消。これを別branch/toolで回避しない。
  15:24のSARIF送信承認をソース公開やClaude相談へ広げない。
- 未完了のためこのworktree、bin/obj、専用outputs、証拠は保持。cleanup拒否ではない。
- #525/#542/#548のローカル候補、#523、native#453/#491、AppSDK PR517は別件。
  既存通常App/設定、issue-495、issue-491、issue-514とpr-498-reviewを触らない。
