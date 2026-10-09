# 引き継ぎ書 — 2026-10-10(provider 拡張の途中、次は #195 の統合と第 3 段の診断)

Status: informational

## 最初に読むもの

1. [10 日の日報](../reports/2026-10-10-providers-daily-report.md)、[9 日後半の日報](../reports/2026-10-09-mvp-features-daily-report.md)。
2. ADR-0057(UNC 読み取り)、ADR-0058(読み込みの放棄と host の interrupt)、ADR-0059(Windows → WSL の transfer route)。
3. `docs/PROJECT_STATE.md` の Current checkpoint と Next focused work。

## 体制(変更なし)

設計リナ(Fable 席)が自走して Issue・ADR・判断・merge・文書を行い、hide しか決められないことだけを聞く。実装リナ(Opus 背景席)が
実装・test・coverage・mutation・deep review・Draft PR を担当。席ごとに `D:/NeNeCommander/wt-<issue>` と branch、出力は
`D:/NeNeCommander/outputs/wt-<issue>/`。触るファイルが重なる席は直列、重ならなければ並行(ADR README の索引の衝突だけは
設計リナが merge 時に解く)。実装リナの commit の `Co-Authored-By` は `Claude Opus 5.5`、設計リナは `Claude Fable 5.1`。

## main の状態

- 統合 baseline は日報の表のとおり: #188 `8909491` → #191 `fec5b49` → #194 `5ac5aa3` → #193 `4ea8847` → 本 closure。
- main deep review: `8909491` は mutation 95.52 / 96.11 / 90.45 / 94.97 で pass、CodeQL 3 件(#192 で修正)。`4ea8847` の
  run `37966275802` の結果(4 層 score、CodeQL analysis、open alert 数)を PROJECT_STATE に記録する。alert が 0 でなければ
  dismiss せず code で直す Issue を立てる。
- open Issue: #94(配布前の確認、hide 在席の日)、#181(ネットワークドライブ、低優先)、#190(末尾ドットの診断、hide 在席時)、
  #195(Windows → WSL move、Draft PR 作成中)、本 closure。

## 次の作業(この順で自走する)

1. **#195 Windows → WSL の move**: 実装リナの Draft PR を受け取り、deep review の結果を確認。live tier は設計リナがこの機材で
   実行する: 専用 root を `wsl.exe --distribution Ubuntu --exec mkdir -p /tmp/NeNeCommander-Live-<stamp>` で作り(空・非 link)、
   worktree で `$env:NENE_COMMANDER_WSL_TEST_ROOT='\\wsl.localhost\Ubuntu\tmp\<leaf>'`、`TEMP/TMP` を D: に向けて
   `pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1`。宣言セル数(8)と一致する Passed を PR にコメントして Ready。
2. **#190 の live 診断**(hide 在席時): 専用 root に `foo.` と `a:b` を作り、`\\wsl.localhost` 越しの列挙と `Path.GetFullPath`
   の結果を観察。事実なら defect として木の中の子も destination の `Child` 規則で導出する修正へ。
3. **第 3 段 WSL → Windows**(#190 の後): 木の事前走査、case 衝突、KeepBoth の判断。
4. **#181**(低優先)、**sort の永続化**・**Locations の share 列挙**・**UNC の mutation/launch**(hide が欲しがれば)。
5. hide 在席の日に: ADR-0054 の配布前確認(#94)、window での Escape interrupt と列表示の見た目、#190 の診断をまとめて行う。
   実行前にチャットで合図を出す。

## hide の承認待ち(前日分に追加)

- 4 つの資源 format(`CommandPaletteUnavailable*`、`LocationsRowAutomationNameFormat`、`LocationsDrivesUnrepresentableFormat`)を
  語と構造に分けるか(今は CodeQL の query の仕様に頼った例外)。
- ネットワークドライブの事実(`DriveKind.Network`)を pane の address/status に出すか。
- `CommanderWindow` 6 引数、`CommanderSession` 295/300、GLOSSARY の可視性記述と WSL の dot 名規則、AutomationId 未宣言、
  以前からの follow-up は 9 日の引き継ぎ書のとおり。

## live WSL tier の実行記録

- 2026-10-10 01:52 JST、head `df8a15b`(#193)、root `/tmp/NeNeCommander-Live-20261010a`(Ubuntu)、7/7 PASS、6.7 秒。
  record は `live.trx`(root・distribution・account 名を含まない)。root は ADR-0043 のとおり保持(空)。再実行時は新しい root を作る。

## 作業場所と整理

- 本体 `C:/Users/info/WORKS/NeNeCommander` は main、clean、同期済み。
- `D:/NeNeCommander/wt-195`(#195 の席)と本 closure の worktree は、各 PR の統合後に tree 一致・clean・稼働参照なしを確認して削除。
- `D:/NeNeCommander/outputs/resume-20261008/` は保持(自動承認審査が削除を拒否)。他の `outputs/wt-*`・`live-*` は merge 時に削除済み。
