# 引き継ぎ書 — 2026-10-12 検証範囲の見直し

Status: informational

最初に[ADR-0061](../adr/0061-impact-scoped-verification.md)と
[今回の日報](../reports/2026-10-12-verification-policy-daily-report.md)を読む。
Issue #218 / PR #219はmain `1d78e09`へ統合済み。過去の毎回全件・3日おき深掘り・
Readyで全件CI・日次統合必須という指示は現在の運用ではない。

文書はCIなし、通常修正は対象機能と直接影響する境界だけを確認する。全件/深掘りは
必要性をPRへ記録して明示実行する。深掘りは全件ゲートを含むので二重に実行しない。
同じ入力の成功証跡を工程が変わっただけで取り直さない。依存ファイルの変更には
Ready時のdependency-reviewを使う。mainの一律必須CIは削除済みだが、PR・squash・
force push/削除禁止などは維持されている。merge担当が選んだ証跡を確認する。

CI規約の局所検証はQLT-015 7件、SEC-009 2件、Commit mode、PowerShell構文、
diff確認で成功。製品コード/テストは不変のため、製品全件・coverage・mutation・
live WSLは再実行していない。必要なコマンド、失敗の修正経緯、証跡先は日報を参照。

この3文書のclosure PRでは、文書だけならCIを起動せずmergeできることを確認する。
その最終結果とmain SHA、作業領域の整理結果はclosure PR本文を正本とする。
PR #219本文からclosure PRを参照し、既に終わっている工程を再実行しない。

次の開発は#216（WSL→Windows転送設計）。#94の実機確認はリリース前に実施する。
前作業#190の保持された一時出力はPR #217に記録済み。削除拒否を迂回しない。
