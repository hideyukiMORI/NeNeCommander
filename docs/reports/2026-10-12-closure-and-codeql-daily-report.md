# 日報 — 2026-10-12(前回 closure の統合、CodeQL 修正、README 更新、#195 の live tier)

Status: informational

体制は前回のまま(設計リナが自走、実装リナは Opus 背景席)。hide は不在。入力を送る実測は行っていない。
live WSL tier の実行だけはこの機材で行った(UI 操作なし、専用 root のみに書き込み)。hide からの指示は
「README が古ければ最新に更新する」の 1 件。

## 開始時の状態

前回(10 日)は closure PR #197 を Ready にして canonical 成功まで進め、merge 直前で終わっていた。#195 は実装リナが
commit・push 済み(head `0a0314c`)で deep review が途中、Draft PR 未作成、live tier 未実行だった。

## 統合した PR(すべて canonical gate 成功 → squash → main 同期 → worktree 整理)

| Issue | PR | squash | canonical run | 内容 |
|---|---|---|---|---|
| #196 | #197 | `88fcfad` | (前回取得済み) | 10 日の日報・引き継ぎ書・PROJECT_STATE |
| #199 | #200 | `80c5b02` | `38159381027` | README を実装段階の現在状態に書き直し(到達範囲・未着手・層構成・検証の入口・規範文書の入口。件数と score は PROJECT_STATE へ誘導) |
| #198 | #201 | `31f6d21` | `38160720851` | main `4ea8847` の deep review が報告した CodeQL `cs/useless-assignment-to-local` 2 件(#109/#110、#189 の test)を code で修正。`using (FileStream locked = new(...))` → 変数なしの `using (new FileStream(...))`、`tree` の二重 snapshot → 一度。production 不変。Infrastructure.Windows 372 件(live 7 skip)、coverage 93.21、mutation 91.14%。#200 が先に入り BEHIND になったため Draft → main merge(`13af034`)→ Ready で取り直した |

統合後に #195 も入った(下記)。最終 baseline は `8028a2a`。

## main の deep review

- `4ea8847`(run `37966275802`): pass。mutation 95.52 / 96.10 / 91.04 / 95.02、coverage 100 / 100 / 93.21 / 94.06。
  CodeQL analysis `1925310830` が 2 件(上記 #198 で修正)。
- `31f6d21`(run `38161642734`): #198 merge 後に dispatch。pass。mutation 95.52 / 96.10 / 91.04 / 95.11、coverage 100 / 100 / 93.21 / 94.06、
  CodeQL analysis `1932351429` は 0 件、open alert 0(alert #109/#110 は code 修正で closed。手動 dismiss なし)。

## #195 Windows → WSL の move(第 2 段)

- live WSL tier を設計リナが実行: 2026-10-12 02:26 JST、head `0a0314c`、Ubuntu、root `/tmp/NeNeCommander-Live-20261012b`、
  **8/8 Passed**(宣言 8 と一致、14.7 秒)。新セル `…WindowsTreeMovesIntoDistributionDeletesSourceAfterVerifiedTargetAsync` Passed。
  record は `D:/NeNeCommander/outputs/wt-195/live-20261012b/`。Issue #195 にコメントで記録。
- 実装リナが #198 merge 後に main を取り込み(conflict なし、`9e23f9b`)、302 行目の同形 `using` を揃え(`fd6a9c8`)、deep review を最後まで:
  canonical 1289 件中 1281 成功(live 8 skip)、mutation 95.52 / 96.10 / 91.14 / 95.02、coverage 100 / 100 / 93.21 / 94.06、dependency audit 脆弱性なし。
  production は live tier 実行時の `0a0314c` から不変(`git diff -- src` 空)なので live tier の取り直しは不要と判断。
- 設計リナが ADR-0059 第 2 段の改訂と production 差分(capability の戻り値 1 行と改名)をレビューし、Issue の不変条件と一致を確認して Ready。
  **PR #203、canonical run `38164204300`、squash `8028a2a`**。merge 後に main deep run `38165064625` を dispatch。
- `8028a2a`(run `38165064625`): pass。mutation 95.52 / 96.10 / 91.04 / 95.02、CodeQL analysis `1932464155` は 0 件、open alert 0。

## live tier の手順上の発見(再現・原因・対処)

最初の実行は `A fixed read-only WSL admission query failed.`(`findmnt -T <root>` が 1)で止まった。調査の結果、
Ubuntu の distro instance は wsl.exe の session が切れると数秒で停止し、再起動時に systemd-tmpfiles が `/tmp` を空にする。
`mkdir -p` した root は script の admission query の時点で既に消えていた。`--exec` と `--` の差に見えたものは、`--` が
login shell に command line を再解釈させるため引用が崩れていた見かけ上の差で、本質ではない。Git Bash で wsl.exe の出力を
`/dev/null` へ向けると wsl.exe 自体が失敗するため、exit code の計測は `sh -c` の中で `echo $?` させる。
対処: 実行前に `wsl.exe --distribution Ubuntu --exec sleep 3600` を detached で常駐させ、その後 root を作って script を回す。
script と ADR-0043 は変えていない(手順の注意として引き継ぎ書に記す)。

## hide の決定(2026-10-12、closure 後)

- hide から「WSL は立ち上がっている」と指摘があり、確認すると起動していたのは hide の作業用 distro `Ubuntu-22.04` で、live tier が使う `Ubuntu` は呼び出しごとに冷間起動していた(root `…-20261012b` も再起動で消えていた)。
- hide の決定: live tier は `Ubuntu` のまま、実行時だけ常駐させる手順でよい。`Ubuntu-22.04` には live root を向けない。

## 判断の記録

- CodeQL の 2 件は test の中だが dismiss せず Issue #198 を立てて code で直した(#192 と同じ方針)。#195 が同じ file を
  改名・加筆しているため、#198 を先に merge し #195 が main を取り込む順にした。
- 宣言形 `using T x = ...;` で変数を読まない 4 か所(`WindowsLocalFileOperationAdapterTests` 2、`WindowsLocalSettingsStoreTests` 2)
  は CodeQL が報告しておらず、C# では宣言形を変数なしにできない(`using var _` は CS-014 と衝突)ため現状維持。
- README は hide の指示で更新した。鮮度の二重管理を避けるため件数・score・run 番号は書かず PROJECT_STATE に誘導する。
- ruleset の `strict_required_status_checks_policy` により BEHIND の branch は green でも merge できない。`--admin` は使わない。
  docs だけの PR を先に merge すると他の open PR が全部 BEHIND になるので、deep review の長い PR を最後にするか、canonical を
  1 回余分に払う。

## 残っている follow-up

- 前回までの承認待ち(4 つの資源 format、ネットワークドライブの事実の表示、`CommanderWindow` 6 引数、`CommanderSession` 295/300、
  GLOSSARY の可視性記述、AutomationId 未宣言、その他)は変更なし。
- README の「Windows → WSL の copy」の一文は本 closure(#202)で「copy or move」に直した。
