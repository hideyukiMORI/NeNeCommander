# 全体検証のマージ直前集約 — 日報

日付: 2026-09-05 / Issue: #51 / ADR: ADR-0025

hideの明示指示により、通常の全体検証をIssue/PRのマージ直前へ集約した。開発中は対象と影響範囲のテスト、コミット時は同じcheck.ps1のCommitモード、統合直前は既定の全体ゲートを使う。coverage/mutation閾値、mainの必須check、strict freshness、bypass禁止は変更しない。

qualityのpush/通常PRイベントを削除し、DraftからReadyへの遷移だけで全体CIを要求する。Ready後に修正した場合はDraftへ戻し、対象テスト後に再度Readyにする。main更新で統合候補が変わる場合もbranch更新と再検証が必要。新規PRはdraftで作成する。定期deepと安全性変更・リリース時の検証は別枠で維持する。

直接実行した `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit` はexit 0、約9.7秒。規約110件、安全性検査が成功し、build/全テスト/coverage/負例コピーは実行されなかった。改行正規化に関するGit通知は出たが、whitespace検査は成功した。

この文書を含む候補の全体結果は、マージ直前のPR `canonical-gate` と安全性変更のdeep記録を正本とする。本記載時点では候補の全体検証は未実行であり、軽量結果を全体成功としていない。最終結果はPR/CI履歴とセッション報告で確認する。日報に結果を書き戻すためだけの追加commitと全体再実行は行わない。

引き継ぎ: 今後のセッションはAGENTS、QLT-015、DEVELOPMENT_WORKFLOWに従う。過去の日報にある「毎試行で全ゲート/deep」の実行例を、新しい変更への義務として継承しない。Issue #49固有の未完了の安全性・実機証明は消さず、統合時に実施する。アプリ実装と検証fixtureコピーの高速化は今回変更していない。
