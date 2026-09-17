# 日報 — 2026-09-17 第 2 セッション（ADR-0051 統合、MSTest.Sdk 4.4.1、#100 の runtime spike と実装 checkpoint）

Status: informational

体制は前日と同じ（Fable 5 の設計リナ、Opus 5 のバックグラウンド実装リナ）。同日の第 1 セッション
（`2026-09-17-adr-0051-checkpoint-daily-report.md`）の checkpoint から再開し、hide が席を外して自由に
進めてよいとしたあと、3 本の実装リナを並行させた。hide の指示で #100 の実装途中で区切った。

## 統合したもの

### Issue #135 / PR #141 — ADR-0051（`CommanderSession` を scope owner に分割）

squash merge `5046947`。dependency run `35211277500`、exact-head deep run `35211278944`（head `5247b5d`、
Domain 95.52% / Application 95.98% / Infrastructure.Windows 90.86% / Presentation.WinUI 94.30%）、
canonical Ready run `35214506785` が成功し、main deep run `35215425806`（`5046947`）も同じ 4 層 score と
CodeQL 0 件で成功した。`CommanderSession` は型内 324 → **261** 論理行、
`CommandPaletteSession` と `AddressEditorSession` は各 66 行。ADR-0051 は accepted。

設計レビュー（実装リナの初版 `d84e3cc` に対して設計リナが元コードと 1 分岐ずつ突き合わせた）で挙動の等価と
3 つの順序不変条件の保持を確認し、次を決めて ADR-0051 に反映した。

- address owner の開始は `Admit`（state を変えない admission）と `Open(admitted)` の 2 段。順序不変条件
  「editor state は `ActivateOtherPane` の後に、その前に読んだ snapshot から設定」と「拒否時は activate
  しない」を両立する唯一の形。address の `Validate` は pane snapshot を取らない。
- 独立した `Close` は公開しない。close 遷移はすべて `Open` / `Validate` の内側で起きる。
- `TransientScopeOwners` の ctor は `public`（composition root が App にある）。`TransientScopeSnapshot` は
  `internal`。
- owner の操作は全域。初版は `AddressEditorClosed` に対する `Escape` や Closed 参照で qualified された
  `AddressSubmission` で `InvalidCastException` になり得た（session の precedence 判定と owner 呼び出しが別
  lock 区間になったため）。guard と owner 直接の behavior test（`AddressEditorSessionTests`、
  `CommandPaletteSessionTests`）を足した。既存テスト本体は不変で、差分は生成箇所と `.Scopes.` 経路のみ。

前日の引き継ぎ書の「`CommanderWindow.xaml.cs` の 2 箇所」は誤りで、`snapshot.Scopes.…` への読み替えは 7 箇所
だった。

### Issue #139 / PR #140 — MSTest.Sdk 4.4.1（Dependabot PR #138 の置き換え）

squash merge `c536978`。dependency run `35209616663`、canonical Ready run `35209710185` が成功し、main deep
run `35210661442` は 95.52% / 95.87% / 90.86% / 94.30%、CodeQL 0 件で、前回の main deep `35120031829` と完全に
一致した。4.4.1 は修正 8 件の patch で runner 機構の変更なし。推移的に動いた package は 8 つ（MSTest 3 +
Microsoft.Testing.* 5 が 2.4.0 → 2.4.1）、`Microsoft.NET.Test.Sdk` は 18.9.0 のままで ADR-0048 の「gate と
mutation の package graph が同一」は維持。CFG-002 の期待値（`eng/conformance.ps1`）と QLT-007 の負例
（`eng/prove-gates.ps1`）、ADR-0006 / ADR-0048 の現在 pin の記述を揃えた。exact-head deep は先例 #68 と同じく
不要と判断し、代わりに branch 上で Domain の mutation を同形で実測して 95.52% の一致を確認した。squash
commit 本文の「Microsoft.Testing.Platform 系 6 package」は 5 の誤記で、PR #140 のコメントで訂正済み。PR #138
は置き換え理由をコメントして close。

## Issue #100 の runtime spike（ADR-0050 Executable proof の第 1 手順）

hide の許可のもと、使い捨ての detached worktree で計装した実アプリに `SendInput` でキーを送った（1 打ごとに
前面ウィンドウを検証、中断 0 件、commit なし、worktree 削除済み）。環境: Windows 11 build 26200、Windows App
SDK 2.4.0、display 4 枚（125 / 150 / 175 / 150%、100% なし）、日本語 input locale の下の US-101 配列。

| 事実 | 結果 |
|---|---|
| focus sink に focus がある状態で `h` / `+` / `Ctrl+W` が root input surface に届く | 前提どおり。handled な `PreviewKeyDown` は `CharacterReceived` を抑止。repeat は `WasKeyDown` のみ |
| sink の focus 保持、`Tab` の消費 | 前提どおり。ただし sink を collapse すると focus が `LeftAddress` に飛ぶ |
| `MoveAndResize` の 32 px | 3 つの scale すべてで指定 physical px ちょうど、size 不変、scale 非依存。100% は未検証 |
| 未宣言の `PreferredMinimum*` | **前提と違う**: `int?` の `null`（0 ではない） |

設計リナの判断（ADR-0050 第 4 次改訂。hide が承認した挙動は変えない）:

- `null` は adapter が 0 に翻訳する。最小 0 は何も制約しないので番兵値ではなく、Application に nullable を
  持ち込まない。非 0 の未宣言最小は存在しないので `AtMinimumSize` の意味は変わらない。
- 100% のセルは Issue #94 の release matrix に寄せる。
- DPI の違う display へ move すると、OS が直後に window を scale 比でリサイズする（実測 1200×800 →
  1440×960）。mode は予測も打ち消しもしない（OS の持ち物を保持・推測しない）。caption rule は要求 bounds で
  評価し、`m` と OS 操作で常に復旧可能であることを明記。
- leave の host 順序を規範化: captured pane の file list に focus を戻してから overlay を collapse する
  （逆順だと `OnAddressGotFocus` がアドレス編集を開く）。
- mode open 中は `GetKeyboardContext` が focused-element 判定より先に session 状態から `WindowAdjustment`
  を返す。既存の idle focus 復帰は `FileList` のときだけ動くので、第 2 の抑止述語は足さない。
- `Ctrl+W` の character route は `w` と U+0017。`+` / `-` は Shift を見ない character 照合なので、US の
  `Shift+=`、JIS の `Shift+;`、テンキーが追加宣言なしで通る。

hide への未決の問い（blocking ではない）: hide のキーボードは US-101 で、拡大は `Shift+=` かテンキー `+`。
Vim の `Ctrl-W +` / `Ctrl-W -` と同じ形。`=` alias は JIS の理由で却下のまま、後から 1 行で足せる。設計リナの
推奨は決定どおり出荷して使用後に再考。

## Issue #100 の実装 checkpoint

hide の指示で区切った。branch `feat/100-window-adjustment-mode`、head `ef6e7e2`（push 済み、PR 未作成、
working tree clean、アプリ未起動）。本文に「未検証の checkpoint」と明記した commit で、ADR-0050 の第 4 次改訂と
design handoff の 1 文を含む。Release build は 0 warning、既存 test は Application 379/379、
Presentation.WinUI 136/136、Domain 72/72、Architecture 5/5、`eng/check.ps1 -Mode Commit` は PASS。新規 test は
まだ 0 件で、canonical gate・runtime 証拠・mutation 実測は未実施。

済んだもの: Application の window 型群と `WindowAdjustmentPlanner`、`WindowAdjustmentSession`
（`Current` / `Open` / `Adjust` / `Leave`、全域）、`TransientScopeOwners` / `TransientScopeSnapshot` の 3 つ目の
member、`CommanderSession` の admission・freeze・同期 2 member（owner への委譲のみ）、
`UserIntent.OpenWindowAdjustment`、Presentation の key・translator 両 route・`WindowAdjustment` context・専用
binding table（14 entry / 9 action）・`Map` の mode 分岐と context-aware な chord 取消規則・presenter 2 つ。
`CommanderSession` は実装リナの計測で 281 論理行（PR #141 の数え方なら 283 相当）で 300 以下。既存 test の
変更は binding 件数の期待値 1 件（FileList 39→40、NavigationSurface 20→21）のみ。

実装リナの確認事項に対する設計リナの判断:

- **ADR-0050 の「`Other` を含め declared 以外は `KeyboardConsumed`」は実装不能で、改訂する。** virtual-key
  route で `Other` になる `PreviewKeyDown` を handled にすると `CharacterReceived` が抑止され（spike の事実）、
  `h j k l m + -` が mode に届かない。virtual-key route の `Other` は palette と同じく pass through、識別済みの
  未宣言キーと未宣言の文字は consume とする。sink は Content のない `ContentControl` なので素通りは無害。
  ADR の該当文は次回 #100 の branch で設計リナが書き換える（実装は既にこの形）。
- `WindowPlacement` の 7 つの事実を `WindowSizeConstraint`（scale と preferred minimum）と `WindowWorkAreas`
  （current と attached）の 2 つの closed record にまとめて CS-013 の 4 引数に収める: 承認。
- move group は 4 cap を 1 hint で出すので既存 `KeyHintTemplate`（cap 1 + label 1）では足りない。同じ
  semantic resource だけを使う multi-cap の template を overlay 内に置く: 承認。design handoff に 1 文足す。

Issue #100 への checkpoint コメントは、実装リナのセッションで外部書き込みの権限が拒否され、投稿されていない。
設計リナは拒否された操作を代行しない。残手順は引き継ぎ書に書いた。

区切りの直前、main worktree の 5 ファイル（`CommanderApplication.xaml.cs`、`CommanderSessionTests.cs`、
`NullGuardTests.cs`、`CommandPalettePresenterTests.cs`、`KeyboardIntentMapperTests.cs`）に先頭 BOM だけの
未 commit 差分が同一時刻（20:50:15）で付いていた。差分が BOM のみであることを確認して復元した。書いた主は
特定できていない。

## 実装リナからの気づきに対する設計判断

- `Microsoft.Testing.Platform` 2.4.x に独立 pin がない: 現状維持。locked-mode restore と lock file が固定の
  機構であり、独立検査は同じ固定の第 2 の機構になる。
- ADR-0048 の review 契機に test platform の pin 変更を足す案: 候補として引き継ぐ（hide の承認待ち）。
- `CommanderSession.Current` が毎回 `TransientScopeSnapshot` を生成する点、precedence の読みと owner 呼び出し
  の間の窓: 後者は owner の全域化で安全になった。どちらも今は対処しない。

## やらなかったこと

#100 の残り（checkpoint の残手順）、#94、統合済み worktree の後始末。
