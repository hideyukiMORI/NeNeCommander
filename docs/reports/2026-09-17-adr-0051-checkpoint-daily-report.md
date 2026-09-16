# 日報 — 2026-09-17（ADR-0050 / ADR-0051 の承認と ADR-0051 の着手 checkpoint）

Status: informational

体制は前日と同じ（Fable 5 の設計リナ、Opus 5 の実装リナ）。前日分の統合と docs 閉塞（PR #134、
`634906f`）のあと、hide とのやり取りで #100 の設計判断を確定し、ADR-0051 の実装に着手したところで hide の
指示により区切って止めた。

## hide の承認（2026-09-17）

hide は ADR-0050 の window mode の挙動（`Ctrl+W` で持続モードに入り、上部中央のヘルパーに現在の操作と
モード内のキーだけを表示し、`h j k l` で移動、`+` / `-` で左上固定の拡大縮小、`m` / `r` の冪等 2 命令、
`Esc` / `Ctrl+W` で抜ける）を理解した上で承認した。合わせて、ADR-0051（`CommanderSession` を palette /
address の scope owner に分割する）への着手と、CS-013 超過の他 5 型とゲート化を別 Issue に切り離すことも
承認した。承認は日付付きで memory と引き継ぎ書に記録し、ADR-0051 は実装 PR で accepted にする。

説明の過程で確認したこと: ヘルパーはビューアーでもアプリ全体のキーバインド一覧でもなく、`h j k l` の意味が
カーソル移動からウィンドウ移動に変わっていることを示す合図と、モード内 6 種のキーヒント、直前の操作結果
（拒否理由を含む）を出すだけの帯。palette と同じ scrim でペインは見えたまま操作を受けない。

## ADR-0051 の着手

実装リナに ADR-0051 を仕様として渡した（Issue 起票 → `refactor/<n>-commander-session-scope-owners` →
owner 2 つと record 2 つ → 生成箇所 5 つと snapshot 参照 2 つの追従 → ADR-0047:48 / ADR-0044:30 の
supersede 文、COMMAND_MODEL の行置換と 2 行追加、GLOSSARY の 2 語、ADR-0051 の accepted 化 → exact-head
deep → Ready → merge）。既存 49 テストの本体は変えないこと、`CommanderSession` が 300 論理行を超えたら
merge せず停止することを受入条件にした。

hide の指示で区切ったとき、実装リナは読み込みを終えて実装に入る直前だった。checkpoint は Issue #135
（`refactor(session): CommanderSession を scope owner に分割する`）、branch
`refactor/135-commander-session-scope-owners`、worktree `C:\Users\info\WORKS\NeNeCommander-135`、head は
main と同じ `634906f`（コード変更ゼロ、未 push、CI 未起動）。残手順 7 項目は Issue #135 のコメントに
記録済み。

読み込みで出た論点 1 つを設計側で決めた: palette の `Open` の admission は `PaneInteractionIsFrozen` と
`AnyPaneExternalWorkIsRunning` の両方を読むが、owner はそれを自前で判定できない（freeze 判定は session に
残す決定のため）。よって `Open` は pane snapshot と、session が freeze・外部作業・settings・address の状態から
導いた `InteractionOwnership` の 2 引数にする。ADR-0051 の該当文を本 docs 閉塞で明確化した。

## 前日分の訂正

09-16 の日報に「両 ADR は `proposed` のまま scratchpad にあり」とあるが、同日の docs 閉塞 PR #134 で
`docs/adr/0050-window-adjustment-mode.md` と `docs/adr/0051-commander-session-scope-owners.md` として
main に入っている（引き継ぎ書側の記述が正）。

## やらなかったこと

#100 の runtime spike（アプリ起動とキー送出を伴うため、hide の在席中は行わない。hide の合図待ち）、#94、
統合済み worktree の後始末。
