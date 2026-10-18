# 日報 2026年10月18日 Windows予約名の上付き数字

Status: informational

hideの「対応を進めて」により、[Issue #245](https://github.com/hideyukiMORI/NeNeCommander/issues/245)を
P4の記録のみの段階から実装修正へ進めた。開始時のmainは`703f629a2b7315a83b0e3a697a163a295befd860`、
GitHubと同期・clean、open PRは0件だった。#181・#243・#250・#251の統合と整理は完了済みである。
NeNe Commanderサナが設計・判断、SOLが実装と対象検証、LUNAが直接consumerの調査、別のSOLが読み取り専用レビューを担当した。

## 修正と契約

[Microsoftの命名仕様](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file)で確認した
`COM¹`・`COM²`・`COM³`・`LPT¹`・`LPT²`・`LPT³`を既存の予約名判定に加えた。
製品差分は`FileSystemPath.IsReservedWindowsName`の1行のみ。ASCII 1–9、大小文字を区別しないstem比較、
最初のピリオドより前を調べる既存経路を維持し、拡張子やancestorの位置で拒否を回避できない。

不変条件は、Windowsの予約名をWindows名の境界で拒否し、WSL内の名前を変えないこと。
唯一の経路は既存の`FileSystemPath.Parse` / `Child`と共有Windows segment predicateである。
Windows local/UNCのchild segmentは拒否し、UNC server/shareやdistribution rootの規則は変えない。
WSL Parse/Childは元の名前を保持し、`HasWindowsCompatibleNames`が同じpredicateで非互換を返す。
既存Shell launcherはscheduleとShell呼出しの前に拒否し、Windows宛転送は既存のtyped child/preflightで止まる。

Unicode正規化や一般的な数字判定へ広げていない。`COM10`、`COM¹x`、`COM⁴`、全角数字などの
類似名を誤って拒否しない。既存の`COM0`/`LPT0`の受理は維持し、未立証の`CONIN$`を新たな仕様にしない。
FS-001・SEC-002・既存ADV-015の説明を更新した。既存契約の拒否漏れ修正であり、新しいADRや処理経路は追加していない。

## 選択した検証と結果

ADR-0061により、parser/Child/WSL互換性、名前要求、Shell admission、directory listing、transfer preflightを選択した。
閉じた6名の拒否集合の補完はこの直接境界の証拠で確認でき、provider I/O、identity、gateway、schema、依存関係は不変である。
full/deep CI、全件test、coverage/mutation、live UI/UNC/WSL、#94の手動画面確認は選択していない。

| 対象 | 成功した異なるcase |
| --- | ---: |
| Domain: parser、Child、WSL名互換性 | 132 |
| Application: directory作成・renameの名前要求 | 21 |
| Infrastructure: Shell、UNC列挙、Windows宛転送と直接対照 | 29 |
| 合計 | 182 |

失敗・skipは0。Infrastructureは最初の27件と、重複しない既存対照2件の結果を合わせた。
対照は有効な名前の転送成功と、local readerの表現できない名前の件数報告である。
UNCは注入したenumerator、転送は所有NTFS rootのfixtureを使い、実shareや実WSLの成功とは主張しない。
Shellの拒否はschedule/callが0、transfer preflightの拒否はeffectが0かつ宛先が空であることを確認した。

修正前のproductionを使ったDomain再現は133件中24失敗・109成功・skip0、exit 2。
失敗はParse 12件、Child 6件、WSL互換性6件だった。
当時のproduction/tests実体、SHA-256、patch、baseline、argv、raw logを`red/`へ保存した。
RED時に含まれていた追加の`CONIN$`成功rowは、未立証の名前を新しい契約として固定しない判断で最終変更から除いた。
このため最終Domainは132件であり、失敗を隠すために24件の反例を除いたものではない。

3対象test projectのRelease locked restore/buildは成功、buildの警告・エラーは0。
DomainのrestoreはRED時の成功を再利用した。最初の修正buildはIDE0048で停止し、
ASCII範囲patternへ必要な括弧を加えて解消した。抑止せず、元の失敗logを`green-build-failed-ide0048/`へ保持する。
変更9 C# fileのformatはexit 0、警告出力なし。`git diff --check`も成功した。
独立した読み取り専用レビューに具体的な未解決P1/P2はない。最終9 sourceと保存物のhash/patch、
baseline productionを使ったRED、filterと182件の対応、成功したbuild/formatを照合済み。
通常Commit hookは112規約・24security登録とcommit-message validatorが成功した。
記録は`root/commit.log`、技術受理とレビューは`root/acceptance.json`で確認できる。

## 実行コマンドと証拠

作業は`D:/NeNeCommander/worktrees/issue-245`、固定SDKは`D:/NeNeCommander/tools/dotnet-10.0.401`、
TEMP/TMPは`D:/NeNeCommander/outputs/issue-245/tmp`。設定は検証processだけに適用した。
次のprojectそれぞれを`dotnet restore <project> --locked-mode -p:Configuration=Release`、
`dotnet build <project> --configuration Release --no-restore`で確認し、すべてexit 0だった。

- `tests/NeNeCommander.Domain.Tests/NeNeCommander.Domain.Tests.csproj`
- `tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj`
- `tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj`

testの共通commandは`dotnet test --project <project> --configuration Release --no-build --no-restore --filter <filter>`。
採用したfilterは以下で、最終実行はすべてexit 0。

```text
Domain:
FullyQualifiedName~FileSystemPathTests|FullyQualifiedName~FileSystemPathChildTests|FullyQualifiedName~WslPathTests
Application:
FullyQualifiedName~CreateDirectoryRequestTests|FullyQualifiedName~RenameRequestTests
Infrastructure:
FullyQualifiedName~LaunchAsyncWhenPathCannotUseShellRefusesBeforeSchedulingAsync|FullyQualifiedName~LaunchAsyncWhenWslFileHasShellMetacharactersPreservesOneFilenameAsync|FullyQualifiedName~CopyWhenLaterSourceNameIsInvalidRejectsEntireBatch|FullyQualifiedName~CopyWhenLaterTreeContainsInvalidNameRejectsBeforeEffects|FullyQualifiedName~ReadAsyncWhenNamesCannotBeRepresentedCountsThemWithinTheBound|FullyQualifiedName~ReadAsyncWhenEntriesAreValidReturnsCanonicalChildrenInOneListing
Infrastructure direct controls:
FullyQualifiedName~CopyWhenTreeIsRepresentableKeepsNamesBytesAndSources|FullyQualifiedName~ReadAsyncWhenEntryNameCannotBeRepresentedCountsItWithoutFailing
```

formatは`dotnet format whitespace NeNeCommander.slnx --verify-no-changes --no-restore --include`へ
変更9 fileを個別引数として渡した。正確なexecutable・argv・cwd・日時・exitとraw logは
`D:/NeNeCommander/evidence/issue-245/`のstage別JSONに保持する。
`implementation-inventory.json`が件数・再利用・失敗履歴、`final-source-hashes.json`と
`final-source/`が最終source、`implementation.patch`が製品/test差分の対応先である。
文書・commit・push・mergeだけを理由に成功した製品検証を繰り返さない。

## 統合と次回の入口

branchは`fix/245-windows-reserved-superscripts`、受理commitは`8658f30b8489e60566448d14e3c9bb777912f4a6`。
[PR #278](https://github.com/hideyukiMORI/NeNeCommander/pull/278)は12:32:52 JSTに
`18a694571e6ea7eaa5b58b2156aae8bc6e5a3b44`へsquash統合され、Issue #245はclosedとなった。
main同期・cleanと、受理task/mainのtree `2c693ecc31127a35a269f4598b8d43026ffbd7e9`の一致を確認した。
追加worktree `D:/NeNeCommander/worktrees/issue-245`は削除済み。
branch・commit・恒久証拠・共有SDKを保持する。

未保存/未追跡物、ignored build出力1006 file、稼働command/module参照、linkを確認した。
一時出力にはcompiler/analyzer cache 23 DLLとIssue/PR本文2 fileがあり、本文は恒久領域へhash一致で保存済み。
出力削除は自動承認レビューが`blocked by policy`で拒否した。コマンドは開始されず、別手段で再試行していない。
12:35:35 JSTの読み取り確認で対象出力の残存とworktreeの不在を確認した。
`cleanup-preflight.json`、`cleanup-pending.json`と`completion.json`に実際の状態を記録する。
[Issue #279](https://github.com/hideyukiMORI/NeNeCommander/issues/279)で本書・引き継ぎ・PROJECT_STATEを更新する。
文面・link・`git diff --check`、準備済みscriptの構文だけの解析、通常Commit hookを選択し、製品検証やCIは再実行しない。

P4 #244・#246・#247・#248は今回の対象外で、#94の手動release確認も未実施のまま。
本件は予約名の字句的な拒否漏れを直したものであり、Windowsの全namespaceや実provider能力を網羅する証明ではない。

## ! hide実行待ち：後片付け

対象は`D:/NeNeCommander/outputs/issue-245`だけ。
恒久証拠`D:/NeNeCommander/evidence/issue-245/`、branch・commit、共有SDKは保持する。
自動承認レビューが再帰削除を`blocked by policy`で拒否したため、hideが次を実行する。
scriptは構文確認のみで、サナは実行していない。完了結果または対象不在の確認までは保留を維持する。

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/issue-245/cleanup-completed-output.ps1
```
