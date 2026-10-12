# 引き継ぎ書 — 2026-10-12(#195 統合済み、次は hide 在席時の確認と第 3 段の前の診断)

Status: informational

## 最初に読むもの

1. [12 日の日報](../reports/2026-10-12-closure-and-codeql-daily-report.md)、[10 日の日報](../reports/2026-10-10-providers-daily-report.md)。
2. ADR-0059(Windows → WSL の transfer route、#195 で第 2 段に改訂)、ADR-0043(live WSL tier)。
3. `docs/PROJECT_STATE.md` の Current checkpoint と Next focused work。

## 体制(変更なし)

設計リナ(Fable 席)が自走して Issue・ADR・判断・merge・文書を行い、hide しか決められないことだけを聞く。実装リナ(Opus 背景席)が
実装・test・coverage・mutation・deep review・Draft PR を担当。席ごとに `D:/NeNeCommander/wt-<issue>` と branch、出力は
`D:/NeNeCommander/outputs/wt-<issue>/`。触るファイルが重なる席は直列。実装リナの commit の `Co-Authored-By` は
`Claude Opus 5.5`、設計リナは `Claude Fable 5.1`。

## main の状態

- 統合 baseline は日報のとおり: #197 `88fcfad` → #200 `80c5b02` → #201 `31f6d21` → #203 `8028a2a` → 本 closure。
- main deep review: `4ea8847`(`37966275802`)pass、95.52 / 96.10 / 91.04 / 95.02、CodeQL 2 件(#198 で修正済み)。
  `31f6d21`(`38161642734`)も pass、95.52 / 96.10 / 91.04 / 95.11、CodeQL analysis `1932351429` は 0 件、open alert 0。
  #195 merge 後の main deep run `38165064625`(`8028a2a`)も pass(日報参照)。次の production 変更の merge 後に同じく dispatch して記録する。alert が 0 でなければ dismiss せず code で直す Issue を立てる。
- open Issue: #94(配布前の確認、hide 在席の日)、#181(ネットワークドライブ、低優先)、#190(末尾ドットの診断、hide 在席時)、本 closure(#202/#205)。
- open PR: **#206(Dependabot、github-actions group)**。`security-deep-review.yml` の `github/codeql-action/init`・`analyze` を 4.38.2 → 4.38.3(`24c54180a607b1449ed407dd24f251e4e9147c8d`)、`actions/upload-artifact` を 7.0.1 → 7.0.2(`cf430e030ddbb5b0abf93d22962f4752f3646cd9`)に上げる提案。12 日 19:27 UTC に開いた。未対応。

## 次の作業(この順で自走する)

0. **Dependabot PR #206 の置き換え**: 先例(#148/#149、#155/#156)どおり Issue 起点の action-pin PR で同じ SHA と version comment に揃え、SEC-005 の supply-chain 走査と canonical を通して squash し、#206 を close する。実装リナに回せる定型作業。
1. **#190 の live 診断**(hide 在席時): 専用 root に `foo.` と `a:b` を作り、`\\wsl.localhost` 越しの列挙と `Path.GetFullPath` を観察。
2. **第 3 段 WSL → Windows**(#190 の後): 木の事前走査、case 衝突、KeepBoth の判断。
3. **#181**(低優先)、**sort の永続化**・**Locations の share 列挙**・**UNC の mutation/launch**(hide が欲しがれば)。
4. hide 在席の日に: ADR-0054 の配布前確認(#94)、window での Escape interrupt と列表示の見た目、#190 の診断。実行前にチャットで合図。

## live WSL tier の実行手順(12 日に判明した注意を含む)

hide の決定(2026-10-12): distro は `Ubuntu`(live tier 専用に使っているもの)のまま。`Ubuntu-22.04` は hide の作業用で常時起動しているが、live root を向けない。

1. `Start-Process wsl.exe -ArgumentList '--distribution','Ubuntu','--exec','sleep','3600' -WindowStyle Hidden` で Ubuntu を常駐させる
   (session が切れると distro が停止し、再起動で `/tmp` が消える)。
2. `wsl.exe --distribution Ubuntu --exec mkdir -p /tmp/NeNeCommander-Live-<stamp>`(新しい stamp)。
3. worktree で `$env:NENE_COMMANDER_WSL_TEST_ROOT='\\wsl.localhost\Ubuntu\tmp\<leaf>'`、`TEMP/TMP` を
   `D:/NeNeCommander/outputs/wt-<issue>/live-<stamp>/` に向け、`pwsh -NoProfile -File ./eng/run-live-wsl-tests.ps1`。
4. 宣言セル数と一致する Passed を Issue/PR にコメント。record は `live.trx`。root は保持(次の distro 再起動で消える)。

## hide の承認待ち(変更なし)

- 4 つの資源 format を語と構造に分けるか。ネットワークドライブの事実を pane に出すか。`CommanderWindow` 6 引数、
  `CommanderSession` 295/300、GLOSSARY の可視性記述と WSL の dot 名規則、AutomationId 未宣言、以前からの follow-up は
  9 日・10 日の引き継ぎ書のとおり。

## 作業場所と整理

- 本体 `C:/Users/info/WORKS/NeNeCommander` は main、clean、同期済み。
- 本 closure の worktree `D:/NeNeCommander/wt-202` は PR 統合後に tree 一致・clean・稼働参照なしを確認して削除。
- `outputs/wt-195`・`outputs/wt-198`・残っていた `outputs/wt-183`・`commit-196.txt`・`pr-196.md` は #195 merge 時に削除した。
- `D:/NeNeCommander/outputs/resume-20261008/` は保持(自動承認審査が削除を拒否)。
