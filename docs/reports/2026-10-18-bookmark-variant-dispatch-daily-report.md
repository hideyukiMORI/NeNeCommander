# 日報 2026年10月18日 ブックマークの型分岐整理

Status: informational

hideの「うん、次に進めて」により、[Issue #248](https://github.com/hideyukiMORI/NeNeCommander/issues/248)を進めた。
開始時のmainは`df26956a6c9fef80c8f3c20b2cfd07155d143d0a`、同期済み・clean、open PRは0件。
#244と#245は統合・整理済み。NeNe Commanderサナが設計・判断と最終レビュー、SOL実装サナが調査・実装・対象検証を担当した。

## 変更と維持する契約

`BookmarkEditorSession`を唯一のeditor state ownerとして維持し、次の残余castを明示分岐へ置き換えた。

- `Apply`に`CancelAction => Cancel()`を明記し、castだけを行っていた`ApplyCancel`を削除した。
- `ResolveFilter`はAll/Uncategorizedをそのまま返し、`BookmarkUserCategoryFilter`をtype patternで束縛して既存のcatalog検索へ進む。
- 未知action/filterは、既存Applicationの不可能状態の扱いに合わせて`InvalidOperationException`を送出し、stateを変更しない。

元の実装でも未知variantは`InvalidCastException`で拒否されていた。今回の変更はdefect時の例外型を明確化するもので、
未知型を黙ってCancelへ吸収していた不具合や、通常操作の脆弱性を修正したとは扱わない。
現行14 actionと3 filter、取消・navigation pending中のfreeze・カテゴリのrebind/rename/deleteの契約は不変である。
新しいowner、utility、公開API、schema、依存、gateはなく、型階層の閉鎖方法も変更しない。
ADR-0041の動作と既存Applicationの例外規約に沿う限定refactorであり、新ADRは不要と判断した。

## 対象検証

修正前のproductionで新しい未知action/filterの2ケースだけを実行し、2件とも期待した例外型の違いで失敗した。
REDは合計2、失敗2、成功0、skip0、test exit 2。元productionとの一致、test実体、patch、hash、argv、raw logを`red/`に保持する。

最終検証は34件すべて成功、失敗・skipは0。
内訳は`BookmarkEditorSessionTests` 31件、既存BookmarkSessions null guard 1件、SettingsSession直接consumer 2件。
未知actionはdraftを取消せず、未知filterは検索・user filterを含むbrowse状態を保持することを確認した。
既存nested Cancelテストはbookmark draft・category draft・削除確認の3通りについて、検索・filter・selectionを含む
元の`BookmarkBrowseContext`参照全体と`StateChanged`を確認するよう強化した。
これにより、開始actionが何もせずbrowse Cancelへ流れる偽成功も防ぐ。
既存のbuilt-in filter保持、現catalogのcategory rebind、missing/stale category拒否、rename/delete後filterも同じownerのテストで確認した。

RED後、診断文を仕様として固定しないため新2テストのmessage一致assertを除き、Cancelの`StateChanged`確認を追加した。
REDでは例外型のassertで失敗しmessage確認は未到達だったため、そのREDは再実行していない。
RED snapshotと最終testの差は`implementation-inventory.json`に明記する。

対象Application Release buildは警告・エラー0。locked restoreはRED時の成功を、依存入力不変のため再利用した。
変更4 C#ファイルのformatはexit 0、警告出力なし。
Application ownerと直結するsettingsの取消/停止境界だけを確認した。
full/deep CI、全bookmarks/settings回帰、coverage/mutation、live UI/provider、#94は実行していない。
入力parser・永続化・filesystem/provider・公開契約を変えないため、この範囲で影響を確認できると判断した。

## 実行コマンドと証拠

作業は`D:/NeNeCommander/worktrees/issue-248`、SDKは`D:/NeNeCommander/tools/dotnet-10.0.401`、
TEMP/TMPは`D:/NeNeCommander/outputs/issue-248/tmp`。環境変数は検証processだけに適用した。

```text
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-restore
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-build --no-restore --filter <filter>
```

最終filterは次のとおり。

```text
FullyQualifiedName~BookmarkEditorSessionTests|FullyQualifiedName~NullGuardTests.BookmarkSessionsWhenRequiredArgumentIsNullThrowArgumentNullException|FullyQualifiedName~SettingsSessionTests.ApplyBookmarkEditorActionWhenBrowsingCancelClosesWithoutWriteAsync|FullyQualifiedName~SettingsSessionTests.ApplyBookmarkEditorActionAfterStopRejectsBeforeStateOrIoChangesAsync
```

formatは`dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore --include`へ変更4 fileを渡した。
正確なexecutable・argv・cwd・日時・exit・raw logは`D:/NeNeCommander/evidence/issue-248/`のstage別JSONに保持する。
`run-selected.ps1`、`implementation-inventory.json`、source snapshot/hash/patchを照合する。
rootの差分・証拠・文書レビューは具体的な未解決事項なし。最終4 sourceは保存hashと一致し、RED productionもbaselineと一致した。
文書の相対linkはbroken 0、`git diff --check`はexit 0。
通常commitの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`は112規約・24security登録とcommit message検査に成功した。
`acceptance.json`、`commit.log/json`に記録する。
文書更新・push・mergeだけを理由に成功済みの製品検証を繰り返さない。

## 統合と残件

branchは`refactor/248-bookmark-variant-dispatch`、受理commitは`0ae9a9954bf86bb40036a3a4f6d730b419eb9c42`。
[PR #284](https://github.com/hideyukiMORI/NeNeCommander/pull/284)は13:14:50 JSTに
`8c68c60127f89953a364ecad80a8f3e30d96fa75`へsquash統合され、Issue #248はclosedとなった。
main同期・cleanと、受理task/mainのtree `cd171df03da63eef6aaf5fe1b97c7138d42a64bf`の一致を確認した。
追加worktree `D:/NeNeCommander/worktrees/issue-248`は削除済み。branch・commit・恒久証拠・共有SDKを保持する。

未保存/未追跡物、ignored bin/obj 385 file、link、稼働command/module参照を確認した。
一時出力はcompiler/analyzer cache 21 DLLだけで、唯一の実装証拠は残っていない。
初回確認ではtaskが起動したidle compilerが出力DLLを保持していたため、
`dotnet build-server shutdown --vbcscompiler --msbuild`でtaskのcompiler/MSBuild serverを正常終了し、
PIDとmodule参照の不在を確認した。実行中の製品testを止めたり、processを強制終了したものではない。
観測はアクセス可能なcommand line/moduleに限定され、全handle調査ではない。

出力削除は自動承認レビューが`blocked by policy`で拒否した。コマンドは開始されず、別手段で再試行していない。
13:17:18 JSTの読み取り確認で出力の残存とworktreeの不在を確認した。
`cleanup-preflight-before-server-shutdown.json`、shutdown log/JSON、`cleanup-preflight.json`、
`cleanup-pending.json`、`completion.json`に実際の状態を保持する。
[Issue #285](https://github.com/hideyukiMORI/NeNeCommander/issues/285)で既存3文書を更新する。
文面・link・diff・通常Commit hookだけを確認し、成功済み製品検証は再実行しない。

残る独立案件は#246の自然順sort、#247のUNC同期列挙待機、#94の手動release確認。
#246の比較仕様や#247の実環境計測を、本件の検証で決定・完了したとは扱わない。

## ! hide実行待ち：後片付け

対象は`D:/NeNeCommander/outputs/issue-248`だけ。
恒久証拠`D:/NeNeCommander/evidence/issue-248/`、branch・commit・共有SDKは保持する。
自動承認レビューが再帰削除を`blocked by policy`で拒否したため、hideが次を実行する。
scriptは構文解析のみ成功し、サナは実行していない。完了結果と対象不在の確認までは保留を維持する。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-248/cleanup-completed-output.ps1
```
