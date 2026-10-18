# 日報 2026年10月18日 パス等価性の境界

Status: informational

hideの「次に進んで」により、[Issue #244](https://github.com/hideyukiMORI/NeNeCommander/issues/244)を調査した。
開始時のmainは`443b11a5072d89f34ed93161c096c0dd9bdcdf50`、同期済み・clean、open PRは0件。
#245の製品統合と後片付けは完了している。
NeNe Commanderサナが設計と判断、SOL実装サナが製品consumerの読み取り監査と指定2ファイルのXML説明を担当した。

## 判断と維持する契約

調査した現行consumerに、generated record equalityをfilesystem identity判定へ誤用する箇所は見つからなかった。
既存の`FileSystemPathIdentityComparer`を唯一のprovider-aware path comparerとして維持する。
型の生成`Equals`・`==`・hashやconsumerを変更せず、既存の意味を説明して本件を閉じる。

| 比較の用途 | 現行の仕組みと確認内容 |
|---|---|
| 保存された表現の等価性 | path recordは格納されたcase-preserving値を比較する。既定equalityは専用comparerへ委譲されない。 |
| providerごとのパス同一性 | `FileSystemPathIdentityComparer.Instance`。Windows local/UNCとWSL distributionはcase-insensitive、WSL Linux pathはcase-sensitive。 |
| 表示・選択snapshotの一致 | Locationsのexpected-stateとlisted-item所属、bookmarkの表示record全体の比較。path同一性だけでsnapshot所属を広げない。 |
| OS上の実体の同一性 | providerが取得する既存`FileIdentity`等の証拠をeffect境界で再確認。文字列やrecordの一致で代替しない。 |

最小例は`C:\Work\File.txt`と`C:\work\file.TXT`。
保存された値は異なるが、Windows path identityは一致する。
WSLのdistribution名だけのcase違いも同様だが、Linux pathのcase違いは別identityである。
これらのgenerated equalityとの差は型定義とC# recordの生成規則から判断したsource-level例であり、
今回追加した実行テストや実ファイルによる再現ではない。

`RenameRequest`は完全に同じ名前だけをOrdinal比較で拒否する。case-only renameは有効なので、
ここをpath identity比較へ置換しない。provider-aware path identity自体も字句的な契約であり、
host alias・link・実ファイルを解決せず、Windowsで実際に観測されるすべてのcase能力の証明でもない。

## 監査の範囲と既存証拠

path型をkeyにしたHashSet/Dictionary、Contains/Remove/UnionWith、path列のSequenceEqual、
operation snapshot・receipt・確認、pane/history、bookmarks mutationを追跡した。
これらのpath identity判断は専用comparerを使用していた。
Locationsとbookmark presentationのrecord比較はsnapshot全体の意味であることを別に確認した。

Windows-local adapterには既存の`IsSameEntryText`によるOrdinalIgnoreCase比較と、
`Preflight`の`HashSet<string>`予約もある。これらはWindows-localに限定された文字列処理で、
現行path identityと同じcase規則である。record equalityの誤用とは認めず、本件では変更しない。
「製品内のすべての文字列比較が専用comparer呼び出しである」とは主張しない。

既存テストは次を読み、用途に対応するassertを確認した。今回の新規実行成功とは扱わない。

- `FileSystemPathIdentityComparerTests`: Windows/UNC case、WSL mixed-case、UNC host aliasの区別。
- `DirectoryListingTests.CreateWhenEntriesShareIdentityDuplicateEntryRejection`: 同じpath identityの重複拒否。
- `RenameRequestTests.CreateWhenOnlyCaseChangesAcceptsTheRequest`: case-only renameの受理。
- `BookmarkCatalogTests`: Windows/UNC caseだけの変化を同一視し、WSL Linux pathのcase変化をstaleとして拒否。
- `LocationsSessionTests.ValidateWhenSelectionIsStaleOrForeignChangesNothingAsync`: stale stateとforeign itemの拒否。

監査の詳細とsource参照は`D:/NeNeCommander/evidence/issue-244/consumer-audit.md`に保存する。
静的調査は将来の誤用を自動検出するゲートではなく、すべての実環境動作やデータ損失不存在の証明でもない。

## 変更と検証範囲

変更は`FileSystemPath`と`FileSystemPathIdentityComparer`のXMLコメント、
[FS-001](../FILESYSTEM_BOUNDARIES.md#fs-001--normalize-without-changing-identity)、
[PROJECT_STATE](../PROJECT_STATE.md)、本日報と[引き継ぎ](../handoffs/2026-10-18-path-equality-boundary-handoff.md)。
既存契約の説明のみで、新ADR・API・比較経路・test・依存・schema・gateは追加しない。

ADR-0061により、文面・XML構造と参照名・相対link・`git diff --check`、
通常commitが実行する`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`を選択する。
2 C#ファイルはbaselineとの比較でXMLコメントを除いた内容が一致することを確認する。
build・product tests・format・full/deep CI・coverage・mutation・live provider/UIは実行しない。
正確な確認結果は`document-checks.json`と`commit.log`へ保存する。

## 統合と残件

branchは`docs/244-path-equality-boundary`。既存checkoutを使い、追加worktreeと一時outputは作らない。
Issueを閉じるPRのsquash commit、main同期・clean・taskとのtree一致は
`D:/NeNeCommander/evidence/issue-244/completion.json`と実際のPR状態から読む。
branch・commit・恒久証拠・共有SDKは保持する。本件の後片付け待ちはない。

次の独立した検討候補は#248のclosed-state cast表現。#246の自然順sort、
#247のUNC同期列挙待機、#94の手動release確認は本件の対象外であり、未実施を成功扱いしない。
