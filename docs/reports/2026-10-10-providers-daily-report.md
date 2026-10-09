# 日報 — 2026-10-10(UNC 読み取り、読み込みの放棄、CodeQL 修正、Windows → WSL copy)

Status: informational

前日の体制(設計リナが自走、実装リナが Opus 背景席)のまま、hide 不在で進めた。入力を送る実測は行っていない。
live WSL tier の実行だけはこの機材で行った(UI 操作なし、専用 root のみに書き込み)。

## 統合した PR(すべて canonical gate 成功 → squash → main 同期 → worktree 整理)

| Issue | PR | squash | canonical run | 内容 |
|---|---|---|---|---|
| #183 | #188 | `8909491` | `37954208033` | ADR-0057。`WindowsUncDirectoryReader` を `ProviderDirectoryReadPort` の 3 番目の分岐に。可視性は Hidden/System 属性(`ClassifyByAttributes` を共通操作へ移し local と共有)。normalizer に 1326/1219 → `AccessDenied`、1231/1232/121/64/59 → `ProviderUnavailable`。mutation/launch/identity は `ProviderUnavailable` 据え置き。FS-011 改訂、SECURITY_MODEL に暗黙の credential、ADV-021(21 件)。Domain 81 / Infrastructure 339+6skip。deep review(`38a67ab`、27 分 50 秒)pass: 95.52 / 96.11 / 90.54 / 94.77 |
| #184 | #191 | `fec5b49` | `37960047699` | ADR-0058。`PaneLoading` 中の `Escape` が read を放棄(`_latestNavigation` の supersede、read ごとの linked CTS、`PaneReadAbandoned`、直前の listing/focus/selection/history は不変)。**潜在欠陥の修正**: App host の `AsyncWorkOwner` が work 実行中の入力を全部拒んでいたため、ADR-0018 の操作中 Escape も window から届いていなかった。`TryStartIntent` で `Escape` だけを interrupt として通し、放棄で待ち側の Task を TCS で早期完了、孤児 read の fault は次の session 呼び出しで defect として rethrow。bookmark 由来の read も放棄可。Application 599 / Presentation 226、mutation 96.10 / 95.02。`CommanderSession` 295 行のまま |
| #192 | #194 | `5ac5aa3` | `37963026238` | main `8909491` の deep review が CodeQL `cs/invalid-string-formatting` を 3 件(#106/#107/#108、#173/#179 の資源由来 format)。構造(`{0} · {1}`、`{0} {1}`、`{0:0.0} {1}`)を code の `CompositeFormat` 定数へ戻し、資源は語だけに。7 キーを resw から削除。`CompositeFormat.Parse(resource)` では query の対象から外れないことを github/codeql の `FormatInvalid.ql`/`Format.qll` で確認。語順が言語依存の 4 format は資源のまま(params 経由で query の対象外。例外として記録)。Presentation 226、mutation 95.02 |
| #189 | #193 | `4ea8847` | `37964710783` | ADR-0059。`TransferRoute`(SameWindowsLocal / SameWslDistribution / WindowsLocalToWsl / Unavailable)で router を provider の組で振り分け、`WindowsToWslCopyTransfer` が Windows adapter の source 再検証・reparse 検査と WSL adapter の target 導出・collision・partial 報告を再利用、copy/verify は `WindowsLocalTreeCopy`。cross の atomic capability は `Failed(ProviderUnavailable)` で move を効果ゼロで止める(第 2 段 #195 で解禁)。batch 内の同一 target も `Conflict`。drvfs alias は `AccessDenied` で閉じる(ADV-022、22 件)。FS-005/FS-012 改訂。Infrastructure 365+7skip、coverage 93.21、deep review(`45bde1e`、26 分)pass: 95.52 / 96.11 / 91.14 / 94.97。**live WSL tier を設計リナが実行: 7/7 PASS**(新セル `…WindowsTreeCopiesIntoDistribution…` Passed、root `/tmp/NeNeCommander-Live-20261010a`) |

#194 を先に merge したため #193 の base が変わり、規則どおり Draft に戻して main を取り込み、再度 Ready にして canonical を取り直した。

## main の deep review

- `8909491`(#183 後、run `37955166306`): mutation 95.52 / 96.11 / 90.45 / 94.97、CodeQL analysis `1924738233` が 3 件(上記 #192 で修正)。
- `4ea8847`(#189 後、run `37966275802`): 本 closure の時点で実行中。結果(4 層 score、CodeQL analysis、open alert が 0 に戻ること)は次回の最初に読んで PROJECT_STATE に記す。

## 進行中・Issue 化

- **#195 Windows → WSL の move(第 2 段)**: cross の atomic capability を `Unsupported` にして gateway の composite(copy → verify → source の permanent delete)を解禁。確認 modal は同一 provider の composite と同じく課さない。本 closure の時点で実装リナが deep review 中(Draft PR は未作成)。live セル(宣言 8)は設計リナが実行する。
- **#196**: 本 closure 文書。
- **#190**: 木の中の末尾ドット名が `Path.GetFullPath` で落ちる疑い(WSL 同一 distribution 内 copy と第 3 段の前提)。hide 在席時に read-only の live 診断。
- **#181**: ネットワークドライブ。読み取りで「削除は常に permanent + 確認、atomic move は volume GUID 一致」が既に満たされていると分かり、範囲を縮小(低優先)。

## 判断の記録

- cross-provider の経路は、port に stream 操作を足す案 (i) ではなく、既存の WSL adapter と同じ「Windows の file API で `\\wsl.localhost` を読み書き」する案 (ii) を採った。gateway・port・Application は不変。
- Issue #189 の「atomic は `Unsupported`、move は preflight で拒否」は Application を変えずには両立しないと席が指摘。fail closed を優先し capability を失敗にした。第 2 段で解禁。
- `AsyncWorkOwner` の interrupt について、席が「UI context なので競合しない」は `CommanderSession` の `ConfigureAwait(false)` 継続があるため一部しか成り立たないと指摘。ADR-0058 を「PaneSession/DualPaneSession は UI context で状態を書き、ADR-0051 の owner は各自の lock、interrupt は既存保証に乗る」に設計リナが書き直した。
- CodeQL の指摘は dismiss せず code で直した。残る 4 つの資源 format は query の仕様に頼った例外として記録。

## 残っている follow-up

- 4 つの資源 format(`CommandPaletteUnavailable*`、`LocationsRowAutomationNameFormat`、`LocationsDrivesUnrepresentableFormat`)を語と構造に分けるか。
- Infrastructure.Windows の mutation は 91.14% まで上がったが余裕は小さい。
- `CommanderWindow` 6 引数、`CommanderSession` 295/300、GLOSSARY の可視性記述、AutomationId 未宣言は前日のまま。
- window での Escape interrupt、UNC の実際の読み取り、列表示の見た目は hide 在席時の確認。
