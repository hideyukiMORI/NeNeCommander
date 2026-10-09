# 日報 — 2026-10-09(#167 の closure 統合、#94 の環境調査、#169 の UI 証拠ハーネス)

Status: informational

## 体制

Fable 5 セッションの NeNeリナ(設計リナ)が設計・判断・ADR・文書・レビューを担当し、Opus のバックグラウンドの
NeNeリナ(実装リナ)が読み取り調査と実装・自己検査・gate の運転を担当した。実装リナは 2 席を順に使った
(環境調査の席、#169 実装の席)。hide はこの日は在席していない前提で進め、入力を送る実測は行っていない。

## 1. PR #168(Issue #167、前回終了時の closure 文書)の統合

- 前回の終了時に Draft のまま残っていた PR #168 は、head `5f82e04`、base `55d911b`(main 先頭)、文書 3 件のみ、
  worktree clean を確認して Ready にした。
- canonical gate run `37921124654` が成功(11 分 8 秒)。dependency-review `37795388776` は成功済みを再利用。
- squash merge は `bc1a594`。main を `git pull --ff-only` で同期し、clean を確認した。
- `D:/NeNeCommander/wt-167` は、committed tree が merge 結果と一致、未保存・未追跡・無視ファイルなし、
  稼働 process なしを確認して `git worktree remove` した。`D:/NeNeCommander/outputs/closure-20261008/` の
  本文 2 件と空の `tmp` も削除した。branch `docs/167-session-closure` と commit は保持している。

## 2. Issue #94 の専用環境の読み取り調査(実装リナ、変更なし)

ホスト HERO を read-only で調べた。設定変更・UI 起動・入力送信・インストール・VM 作成は行っていない。

- Windows 11 Pro build 26300。Hyper-V のサービス(`vmms`/`vmcompute`/`HvHost`)は稼働しているが、`info` は
  UAC で deny-only の管理者であり、`Get-VM` と機能状態の読み取りは昇格が要る。Windows Sandbox は無い。
  Windows 11 の ISO/VHDX は D:・E:・Downloads・Documents・Hyper-V 既定の置き場に無い。
- 画面は 4 枚。primary 4K 125%(taskbar はこの画面の下端だけ)、左 4K 150%、右 4K 175%、右上 2560×1440 150%
  (右画面の上。primary と x が 22 px 重なる)。100/200/300% の画面は無く、taskbar を挟む上下の seam も無い。
- 対話 session は `info` の console 1 本。Enabled なローカルアカウントは `info` と Codex sandbox 用の 2 つで、
  テスト専用の対話アカウントは無い。
- production settings は `WindowsLocalSettingsLocation`(Known Folder `LocalApplicationData` +
  `NeNeCommander\settings.json`)で解決され、`LOCALAPPDATA` の上書きでは隔離できない。実際の path は
  `C:\Users\info\AppData\Local\NeNeCommander\settings.json`(69 B、`nene-dark`)。
- 旧ドライバ `D:/NeNeCommander/evidence-100/Invoke-WindowAdjustmentEvidence.ps1` の 3 欠陥を行番号で確認した。
  foreground 判定が HWND だけ(L119-122)、既定 binary が削除済み `wt-100` を指す(L43)、`Stop-Run` が所有喪失後に
  held-key の release を送る(L112-117)。settings 保護・screenshot hash も無い。再利用できる完成品は無い。

cell の割り当て案を Issue #94 にコメントした(hide の判断待ち)。Hyper-V guest で DPI 4 段・high contrast・
8 scheme・keyboard modal・Narrator・helper、hide 在席のホスト console で cross-scale move(125→150、125→175)、
ローカルのテスト用アカウントで taskbar seam。guest には hide の昇格(または `info` を Hyper-V Administrators
へ追加)と ISO が要る。

## 3. Issue #169 — UI release 証拠ハーネスと preflight(PR #170)

#94 の実測は専用環境と hide の在席を待つが、環境が揃ったときに即実行できる記録装置は今作れる。子 Issue #169 を
立て、ADR-0052 で契約を決め、実装リナが `eng/ui-evidence/` に実装した。

- 不変条件: production code・tests・XAML・token・key map・test hook に変更なし(`src/`・`tests/` の差分は空)。
  production 状態には従来の keyboard 経路だけで到達する。
- 送信ごとの admission: `Test-InputAdmission` は Win32 を呼ばない純関数で、foreground の root HWND・PID・
  session、UIA focused element の process と AutomationId、期待する mode/modal 要素の存在/不在を判定する。
  唯一の送信経路 `Send-AdmittedKey` は送信直前に再観測・再判定し、down+up を 1 回の `SendInput` で送る。
  held key の関数は無い。不一致で停止を latch し、以後は key-up も含めて一切送らない。
- キーの admission: F5/F6/Delete/Enter/Space は常に拒否。F2/F7/F8 は step が宣言した modal と一致するときだけ。
  削除確認 modal に AutomationId が無いため F8 は今は拒否のまま。
- 所有: `-Binary` は絶対 path 必須。`ProductVersion` の `+<sha>` を `-ExpectedCommit` と照合し、不一致なら
  開始前に停止。root HWND は所有 PID から求め、終了は UIA `WindowPattern.Close` と所有 PID の Kill のみ。
- 効果の境界: `-TestRoot` は空または未存在の絶対 path。settings は C# と同じ Known Folder で解決し、
  bytes/absence と SHA-256 を記録・復元・照合する。`-OwnedProfile` が無ければ scheme セルは SKIP
  `profile-not-owned`。
- 記録: `evidence.json` と `summary.md`(UTF-8 BOM なし)。環境(monitor/DPI/high contrast/taskbar/Narrator/
  session/SID)、binary hash、cell ごとの PASS/FAIL/SKIP と理由、DIP/physical bounds の二重読み、所有 PID/HWND、
  UIA focus、screenshot の方式(`PrintWindow(PW_RENDERFULLCONTENT)`、一様な画なら foreground 確認付きの領域
  capture)と SHA-256。
- cells.json: 8 scheme × normal/narrow の Observe セル、Input の `ctrl-w-helper` と `f7-name-entry`、残り
  (f2/f8/conflict/settings/narrator/cross-scale/taskbar-seam/dpi-100〜300/high-contrast)は `not-implemented`
  または `environment-required` の SKIP。
- 設計リナの判断: `.gitattributes` に `*.psm1 text eol=crlf` を採用。1342 行の module が SEC-011 の走査外に
  なるのは受け入れず、SEC-011 の script 走査と SEC-005 の秘密走査に `.psm1` を追加した(gate の強化、13→14 件)。
  `eng/prove-security.ps1` に負例 `unsafe-module` を追加し、通しで exit 0。
- ADR-0052(accepted)は、ハーネスを #94 の証拠と synthetic input の唯一の機構として記録し、観測と `SendInput`
  の 2 段階の間に残る競合を帰結として明記した。

### 検証(ホスト HERO 上で app 起動・SendInput・settings 書き込みなし)

- 構文: `Parser::ParseFile` で `.ps1`/`.psm1` 3 本の parse error 0。`cells.json` は `ConvertFrom-Json` で読める。
- `pwsh -NoProfile -File ./eng/ui-evidence/selftest.ps1`: 103 ケース中 103 PASS、exit 0。session 判定を 1 か所
  削った scratch 複製では 2 件 FAIL、exit 1(複製は削除済み)。
- `Invoke-UiEvidence.ps1 -Preflight`: exit 0。monitor 4 枚・high contrast off・taskbar bottom・Narrator 停止・
  Windows App SDK 2.5.1・settings の path と hash を記録。app の process 数は前後とも 0、`-TestRoot` は未作成。
  `-ExpectedCommit 55d911b` 付きでは古い binary(`2b5e2ee`)に対して `binary-commit-mismatch` で exit 1。
- `pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`: 各 HEAD で PASS。`pwsh -NoProfile -File
  ./eng/prove-security.ps1`: exit 0(TEMP/TMP は D: に向けた)。
- PR #170 の統合結果は末尾「統合」に記す。

### 作業場所

worktree `D:/NeNeCommander/wt-169`(branch `test/169-ui-evidence-harness`)。一時出力は
`D:/NeNeCommander/outputs/wt-169/`(preflight の evidence.json、commit message、PR 本文)。C ドライブには
本体リポジトリ以外に何も作っていない。

## 統合

- PR #170 は head `1315861`、base `bc1a594` で Ready にし、dependency-review `37924954906` と canonical gate
  `37924997096` が成功した。squash merge は `71d4d96`。main を `git pull --ff-only` で同期し、clean を確認した。
  Issue #169 は closed。
- `D:/NeNeCommander/wt-169` は committed tree が `71d4d96` と一致、未保存・未追跡なし、稼働 process なしを確認して
  `git worktree remove` した。branch `test/169-ui-evidence-harness` と commit は保持。
  `D:/NeNeCommander/outputs/wt-169/`(preflight の evidence.json、commit message、PR 本文)は本日報の統合後に
  削除してよい。
- 本 closure 文書は Issue #171 で保存する。

## 残っている環境 proof

#94 の必須 matrix cell は一つも実行していない。Issue #94 は open のままで、release readiness は未達である。
専用環境(Hyper-V guest / テスト用アカウント)の用意、cross-scale move のホスト実測の許可は hide の判断待ち。
