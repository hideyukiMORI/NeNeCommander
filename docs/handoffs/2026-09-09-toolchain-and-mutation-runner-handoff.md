# 引き継ぎ書 — 2026-09-09（SDK ピン更新と mutation runner の真正性）

Status: informational

## 体制

Fable 5 セッションの NeNeリナが設計・判断・文書を担当し、Opus 5 のバックグラウンド NeNeリナが
実装・診断・ゲート運転を担当する。実装側は完全な仕様と停止規則を受け取り、証拠を日本語で報告する。
判断は設計側が行い、文書は設計側が書く。

## 統合済み

| Issue | PR | 内容 | 証拠 | merge |
|---|---|---|---|---|
| #114 | #115 | .NET SDK ピンを 10.0.401（10.0.12 security servicing）へ | ローカル full check.ps1 PASS、dependency `34366942079`、canonical `34366988886` | `ca992888` |
| #116 | #117 | ADR-0048: mutation tier を isolating VSTest host で実行 | ローカル check.ps1 / deep-review.ps1 PASS、dependency `34374571737`、exact-head deep `34374617932`、canonical `34377550574` | `0b663c51` |

`rollForward: disable`、CFG-001 の検証 3 条件、lock ファイルは不変。ADR-0002 に更新事実を追記。
新ピンでの scheduled deep review は未実行で、次回スケジュール実行で確認される。

## 確定した事実（mutation tier）

- Stryker 4.16.0 の MTP runner は test host を再利用し、mutant 切替はメモリマップドファイル書き込み
  のみ。static 初期化子の mutant は各 host の最初の 1 件しか発現せず、per-test coverage も取れない。
  static は大量の偽 Survived、非 static を含めて killedBy の誤帰属が起きる。
- VSTest runner は static mutant ごとに host を起こし直し、per-test coverage を取る。ground truth
  （`OperationStatus.*AwaitingConflict` の ResourceKey、`KeyboardInput.Create` の null guard に
  テストが無い）と一致する判定を出す。
- main `ca99288` の真の score: Domain 95.02% / Application 94.85% / Infrastructure.Windows 90.74% /
  Presentation.WinUI 89.10%。過去の MTP 合格（`34239166946` など）は証拠にならない。
- MSTest.Sdk 4.4.0 は `UseVSTest` 無しで `Microsoft.NET.Test.Sdk` の同梱を許し、MTP の
  `dotnet test`・coverage・locked restore はそのまま通る（診断 E5）。

## 決定: ADR-0048

mutation tier だけ isolating VSTest host で実行する。canonical test execution は ADR-0006 どおり
MTP。4 つの mutation test project が `Microsoft.NET.Test.Sdk` 18.9.0 を central pin で常時参照し、
`stryker-config.json` が `test-runner: vstest` を唯一の宣言として持ち、TST-008 rule がそれを保護
する。閾値・level・baseline/exclusion 禁止・`eng/deep-review.ps1` は不変。閾値未達層は同じ変更で
行動テストを足す。upstream（stryker-net #3742 / PR #3695）が MTP で isolation と per-test coverage を
出荷したら `mtp` に戻し ADR-0048 を superseded にする。

## 進行中と次の一手

1. ADR-0048 は Issue #116 / PR #117 で統合済み。真の score は Domain 95.52%（余裕 1 mutant。
   `FileSystemPath.cs` の生存 9 件は等価変異と判断済み。次に Domain を触る変更は同時に生存を減らす）/
   Application 95.65% / Infrastructure.Windows 90.62% / Presentation.WinUI 92.27%。
2. #101（PR #113）を main へ rebase し、真の生存（新規コードの 7 件と AwaitingConflict）に
   行動テストを足し、CodeQL alert #102 の修正を再解析させ、exact-head deep → Ready → merge。
3. 次に #99（PR #102）を同様に再開する。#99 の記録にある「単独 run 35/35 Killed」は偽合格なので根拠にしない。
4. docs 閉塞: 9/8 遡及日報・引き継ぎ書、9/9 日報・引き継ぎ書、`docs/PROJECT_STATE.md` の
   #103 / #107 / #109 / #111 / #114 / ADR-0048 の記録を一つの docs PR で正本化する。
5. #93（live WSL、blocker #105）と #94（Windows UI matrix）は未実施のまま。

## 再開時の注意

- 診断 worktree は削除済み。`-101` は HEAD `358c294` で clean、`-99` と `-issue93` は未接触。
- レポートは `.gitattributes` で CRLF 固定。`sed -i` は行末を壊すので使わない。
- `gh pr merge --squash --delete-branch` は main が worktree に占有されていると exit 1 を返すが merge
  自体は成功している。`gh pr view --json state` で確認して worktree を手動削除する。
- MTP runner の Stryker 結果を根拠に「テストを足せば殺せる」と判断しない。
