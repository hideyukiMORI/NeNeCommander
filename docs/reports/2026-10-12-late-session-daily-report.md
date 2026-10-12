# 日報 — 2026-10-12後半（#190 診断・修正の再開、gate 運用と分担の更新）

Status: informational

この記録は #212 の文書準備時点の状態を示す。#190 は修正 commit `0216ade` があり、deep review の中断後に NeNe Commanderサナが作業を再開した段階。deep review、PR、統合、cleanup は未完了。

## 作業体制と範囲

今回 hide が明示した分担は、Astra が設計、Sol と Luna が実装。NeNe Commanderサナが進行と統合を担う。

Issue #212 の文書変更は日報、引き継ぎ書、`docs/PROJECT_STATE.md` の3ファイル。#190 と #212 の PR は、当日統合 Issue #213 の `integrate/2026-10-12` branch（base `c3eddd5`、origin push 済み）に入れる計画で、main 向けの日次統合 PR で full canonical gate を1回実行する。

## #190 の read-only 診断と修正チェックポイント

Issue #190 の live 診断は 2026-10-12 11:01 JST に実施。Ubuntu の専用 root `/tmp/NeNeCommander-Live-20261012c` のみへ fixture を作成し、UI 操作・入力送信なしで記録した。ログは `D:/NeNeCommander/outputs/diag-190/diag.log`。

- WSL UNC の通常表記では列挙時に `foo.` が見える一方、`File.Exists` / `FileInfo.Exists` は false。`Path.GetFullPath` は末尾ドットを落とし、`tree/sub./inner` を `tree/sub/inner` にする。
- 同じ path を `\\?\UNC\wsl.localhost\...` の extended-length 形式にすると末尾ドットを保って存在確認・読み取りができる。通常形式での `File.Copy` は `written.` を `written` として作るが、extended-length 形式では `written-ext.` を保つ。
- `a:b` は 9P が `:` を U+F03A に置換し、通常形式でも往復可能だったため、問題ではない。
- 問題の範囲は `ResolveDirectChild` だけではなく、WSL provider から Win32 API に渡る通常形式の path 全般。存在確認と検証も同じ正規化を通るため、collision 確認を抜けて別名で成功扱いになる可能性がある。

live 診断で直接確認した末尾ドットの挙動を受け、ADR-0060 は WSL provider の Win32 path を単一 resolver で extended-length 形式にし、prefix なしの経路をなくす方針。commit `0216ade` に resolver 修正とテストがある。末尾空白の fixture は live 診断に含まれないが、通常 Win32 path の同種の正規化を避ける設計として extended-length 形式を使う。Issue #190 は open、PR は未作成。deep review は中断しており、継続中。

## Actions pin の置き換え #208/#209

Dependabot PR #206 の提案を Issue #208 と PR #209 で置き換えた。`security-deep-review.yml` の CodeQL action 2箇所を v4.38.3 (`24c54180a607b1449ed407dd24f251e4e9147c8d`)、upload-artifact を v7.0.2 (`cf430e030ddbb5b0abf93d22962f4752f3646cd9`) に更新。PR #209 は canonical run `38195579969` と dependency review `38195559955` が pass し、squash SHA `3039ad3` で merge。置き換え対象の #206 は閉じられた。

## Gate 運用変更 #210/#211

hide の 2026-10-12 決定により、required check `canonical-gate` は常に実行し、main 向けで docs 以外を含む変更は full gate、それ以外（当日 integration branch 向け Issue PR と docs-only PR）は Commit mode を選ぶ。check 名・ruleset は維持される。PR #211 は canonical run `38200079459` と dependency review `38200061379` が pass し、squash SHA `c3eddd5` で main に merge。PR #211 自身は workflow / gate 変更を含む main 向け PR であり、run log にはテストと coverage を含む full gate の実行が記録されている。したがって Commit mode の新しい分岐は、この PR で実証されたわけではない。docs-only の本 Issue PR が integration branch で Commit mode を実行する結果は、Ready 後の CI log で確認する。

## #94 と次の手順

Issue #94 の実機確認は開発 blocker ではなくリリース前の環境確認。今回の文書・#190 の作業では UI 入力や表示設定変更を行っていない。Required なリリース環境セルが未実施であることは維持して記録し、開発の次作業を止める理由にはしない。

次は #190 deep review を完了し、その結果を記録して PR を作成する。続いて #190 と #212 を `integrate/2026-10-12` に統合し、#212 の Commit mode log を確認した後、#213 の main 向け full canonical gate を実行する。

文書のみの変更のため build と behavior tests は実行していない。確認は Markdown 3ファイルの BOM/CRLF/末尾改行、相対リンク、`git diff --check`。
