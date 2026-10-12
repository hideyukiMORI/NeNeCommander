# 日報 — 2026-10-12 検証範囲の見直し

Status: informational

hideの追加指定をIssue #218 / ADR-0061で反映した。PR #219は2026-10-12
16:38:53 JSTに`1d78e09ec5e4af0a9607a2a9a922ce058a671861`でmainへ統合済み。

## 確定した運用

- 文書更新は自動CIを起動しない。通常修正は変更機能と直接影響する境界だけを検証する。
- 大きな機能・共通基盤など、局所検証では影響を確認できない場合に全件検証を明示実行する。
- 必要な安全性確認・リリース前には深掘りを明示実行する。3日おきの定期実行は停止した。
- 深掘りに含まれる全件ゲートを、同じ入力のqualityで二重実行しない。入力不変の成功証跡は再利用する。
- 依存ファイルを変更したPRだけがReady時にdependency-reviewを実行する。
- mainのPR必須、squashのみ、review thread解消、bypassなし、force push/削除禁止を維持した。
  一律required canonical-gateのみを外し、必要証跡の確認はmerge担当が行う。

## この変更の検証

製品コード・製品テストの差分は空。CIと検証ツールだけが対象なので、製品の全件テスト、
coverage、mutation、live WSLは実行していない。

```powershell
pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit
pwsh -NoProfile -File ./eng/prove-gates.ps1 -Rule QLT-015
pwsh -NoProfile -File ./eng/prove-security.ps1 -Rule SEC-009
git diff --check
```

Commit modeは112規約/22登録security casesで成功。QLT-015の不正fixture 7件、
SEC-009の不正fixture 2件も成功。変更したPowerShell 6ファイルはParserで構文確認した。
最初の正常例検証でCRLF比較の正規化不足を検出し、修正後の正常例と不正例はすべて通過。
既存の全件既定・閾値・監査レベルを変更していない。

main ruleset `22126335`は変更前後のJSONを比較し、required-status ruleだけが削除され、
残るrules、conditions、bypass、enforcementが一致することを確認した。
PR #219のReady後に起動したのはdependency-review `38218825616`だけで、結果はsuccess。
qualityとdeep-reviewは起動していない。

証跡は`D:/NeNeCommander/evidence/issue-218/2026-10-12/`へ収載する。
今回の3文書だけのclosure PRで、Ready後もworkflow runが0件のまま通常mergeできることを
確認し、最終結果をそのPR本文へ記録する。PR #219からclosure PRを参照できるようにする。
この文書の記録時点ではclosure PRのmergeと作業領域の整理は未完了。

## 次の作業

closure PRの現在状態を確認し、完了済みの検証や整理を繰り返さない。その後は#216の
WSL→Windows転送設計を続ける。#94はリリース前の実機確認として残る。
前作業#190の一時出力2か所は削除が自動承認レビューに拒否されたため保持しており、
正確なパスと理由はPR #217のcleanup checkpointを参照する。
