# 引き継ぎ書 — 2026-10-09(#169 ハーネスは統合、次は #94 の専用環境と最初の実行)

Status: informational

## 最初に読むもの

1. [日報 2026-10-09](../reports/2026-10-09-ui-evidence-harness-daily-report.md)。
2. [Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94) の本文、2026-10-07 の #100 追補コメント、
   2026-10-09 の環境調査コメント。これが実行契約と cell の割り当て案である。
3. [ADR-0052](../adr/0052-ui-release-evidence-harness.md)。ハーネスの契約(所有 process、送信ごとの admission、
   held key なし、test-owned root と settings の bytes 復元、正直な記録、`.psm1` の走査)。
4. `eng/ui-evidence/cells.json`。どの cell が実装済みで、どれが `not-implemented` / `environment-required` か。

## main の状態

- 統合 baseline は `71d4d96`(PR #170、Issue #169)。その下は `bc1a594`(#167 closure)、`55d911b`(#165 MSTest.Sdk
  4.5.1)。本 closure 文書(Issue #171)の squash がその上に乗る。
- 最後に結果を読んだ全層 deep review は `2ef9cb0` の run `37641629223`(mutation 95.52% / 96.24% / 90.86% /
  94.57%、CodeQL 0 件)。#165・#167・#169 は production code を変えていないので、この proof はそのまま有効。
- open Issue は #94 だけ。open PR は無い(本 closure 文書の PR を除く)。

## hide の判断待ち(2026-10-09 に提示)

1. **Hyper-V guest を作るか。** DPI 100/150/200/300 の単画面、high contrast、8 scheme、keyboard modal、Narrator、
   helper の cell の本命。必要なのは hide の昇格(または `info` を Hyper-V Administrators へ追加)と Windows 11 の
   ISO。guest 名は `NeNe-UITest` のような名札にし、ハーネスの `-EnvironmentId` に同じ名前を入れる。
2. **cross-scale move をホストの console で取ることを許可するか。** 125%→150%(左)と 125%→175%(右)はこのホスト
   でだけ取れる。hide 在席・明示の go・実行中はキーボードとマウスに触らない、が条件。
3. **taskbar seam 用にローカルのテスト用アカウントを作るか。** per-user の「taskbar を全画面に表示」で右上画面の
   下端に taskbar が来る。console を占有するので hide の作業とは並行できない。

## 専用環境ができたときの最初の手順

1. 環境側で Windows の scale・high contrast・画面配置を事前に設定し、ハーネスでは変えない。
2. `D:/NeNeCommander/wt-<issue>` で Release build を作り、`NeNeCommander.App.exe` を環境へ持ち込む
   (self-contained・unpackaged なので出力フォルダーごとコピーで動く見込み。未確認)。
3. `pwsh -NoProfile -File ./eng/ui-evidence/Invoke-UiEvidence.ps1 -Preflight -Binary <絶対 path> -OutputRoot
   <絶対 path> -TestRoot <空の絶対 path> -ExpectedCommit <sha>` で環境を記録する。process は起動しない。
4. `-Mode Observe` で scheme × narrow の Observe cell を先に取る。ここで UIA bounds、`PrintWindow` の黒画の有無、
   foreground 待ちの挙動を初めて確かめる。黒画なら `captureMethod` に fallback が記録される。
5. `-Mode Input -AllowInput -EnvironmentId <名札>` で `ctrl-w-helper` と `f7-name-entry` を取る。所有 window が
   自分で foreground にならなければ SKIP `owned-window-not-foreground` になる(activate はしない)。
6. 記録(commit、binary hash、環境、bounds、所有 PID/HWND、UIA、screenshot hash)を #94 に貼り、SKIP は理由付きで残す。

## 次の実装候補(hide の承認後、別 Issue)

- F2/F8/conflict/settings の Input cell。app の初期 pane は `C:\` と `C:\Users` 固定なので、`-TestRoot` へ移動する
  手順(address 入力)の設計が要る。削除確認 modal と settings entry(`Ctrl+,` の key hint)、file list の行に
  AutomationId が無いので、宣言するかどうかは QLT-011 と ADR-0023 の範囲で判断する(production 変更になる)。
- Narrator の cell は読み上げの証拠が要る。guest で録音か、UIA の LiveSetting/Name の読み取りと併記する。
- 従来の follow-up 候補(held `l` の 11/15 repeat、closed abstract record の copy constructor、commit-mode への
  format check、ADR-0047 catalog への `OpenWindowAdjustment`/`OpenBookmarks`、CS-013、`AsyncWorkOwner` survivor、
  test-platform pin の ADR-0048 review trigger、WSL link-entry deletion ADR)は前回の引き継ぎ書のまま。

## 実測のときの約束(変わっていない)

- キーを送る前に hide の合図を必ずもらう。再実行には新しい合図が要る。
- ツール経由のコンソールは hide から見えない。手動の手順は実行前にチャットで「画面がこう動いたら〜」と伝える。
- mode のキーは mode が開いていないと file list のキーとして効く。ハーネスは `Present` で helper/modal の存在を
  送信ごとに確かめる。

## 作業場所と整理

- 本体 `C:/Users/info/WORKS/NeNeCommander` は main、clean、同期済みを保つ。
- `D:/NeNeCommander/wt-169` は PR #170 統合後に安全確認のうえ削除済み(branch と commit は保持)。
  `D:/NeNeCommander/outputs/wt-169/` の preflight 出力は、日報に要点が載っているので削除してよい。
- `D:/NeNeCommander/wt-171`(本 closure 文書の worktree)は PR 統合後に同じ確認をして削除する。
- `D:/NeNeCommander/outputs/resume-20261008/`、`evidence-100`、`mutation-100`、`run-*`、`evidence-codeql-window`、
  `closure-158` は過去の方針どおり保持(自動承認審査が再帰削除を拒否したもの、または別 task の成果物)。
  `evidence-100` のドライバは観察用の参照であり、二度と実行しない(ADR-0052)。
