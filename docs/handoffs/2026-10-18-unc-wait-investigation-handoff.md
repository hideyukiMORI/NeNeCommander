# 引き継ぎ 2026年10月18日 UNC待機の調査と測定計画

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)、既存ADR-0027/0057/0058/0071。
調査結果・source根拠・外部API資料・測定計画は[日報](../reports/2026-10-18-unc-wait-investigation-daily-report.md)。
hideの「推しの進め方で」によりIssue #289で#247の調査と測定計画を行った。

## 次回も維持する区別

- UNCは既存read port/router、WindowsUncDirectoryReader、共有enumerationとexecution boundaryを使う。
- tokenは同期OS呼出の途中では観測されない。外側iteratorの最初のMoveNextはBCL生成/開始も含む。
  どのnative呼出で何秒待つかは未実測。一定のSMB timeoutや後続呼出の非待機を仮定しない。
- EscapeはPaneReadAbandonedとcaller完了を先に返し、providerは残り得る。
  late resultの破棄とfault観測は既存PaneRead/PaneSessionが所有する。
- 未放棄readの通常closeはhost workを待つ。放棄済みreadはそのdrain外（ADR-0071）。
  close開始後は入力が止まるため、後からEscapeを送れる前提の計画にしない。
- 現在のCancelSynchronousIo不使用を「Windowsには同期I/O取消手段が存在しない」に拡張しない。
  thread poolへnative取消を足すだけでは対象I/Oの所有と完了を保証できない。

## 次に実行する範囲

親Issue #247はopenを維持する。#289の完了はlive測定やtimeout採用の完了ではない。
現在の推奨は契約を維持し、専用環境で日報のM1〜M5を最初は各1回測ること。

必要入力は、所有者が指定する専用SMB root/テストidentity、client/serverの条件、
応答停止と復旧が可能なラボ手順。任意のserverへの探査やhost設定変更で代用しない。
fixtureは未実装。既存productionのread/session/ownerを組み合わせ、製品に別経路を作らない。
実WinUI closeとprocess exitを観測した証拠は、session APIだけの証拠から分ける。

測るのはqueue待ち、最初の列挙、Escape→caller復帰、provider完了、close→drain/Closed/exit。
120秒は観測窓の提案値であり製品timeoutではない。超過は未完了としてラボ復旧後も追い、
未観測の完了・通常終了を推定しない。ログはcase IDと相対時間/typed outcome中心とし、UNCや秘密を載せない。
追加sample/重複readは初回結果に具体的な調査理由がある場合だけ範囲を決める。

## 今回の証拠・統合・整理

baseline `6708712a7c847d4d188d73cb9a4c1c666ae8be39`での静的調査。
source hashとIssue本文、document確認、通常commit hook、PR、最終状態は
`D:/NeNeCommander/evidence/issue-247/investigation-20261018/`にある。
既存テストはsourceを読んだだけで、新しい成功実行には数えない。
今回の変更は日報/本引き継ぎ/PROJECT_STATEの3文書だけ。
build、製品test、CI、coverage/mutation、live UNC、UI/環境設定操作は実行していない。

branch `docs/289-unc-wait-investigation`は既存checkoutを使う。
Issue #289のPRと`completion.json`で最終統合・main同期・#247 openを確認する。
追加worktree/一時outputはなく、後片付け待ちはない。恒久証拠・branch/commitは保持する。
#244/#245/#248は統合・整理済み。#246は自然順sortの仕様判断、#94は別の手動release確認として残す。
