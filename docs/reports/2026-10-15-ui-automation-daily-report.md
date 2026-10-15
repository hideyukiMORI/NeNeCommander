# 日報 2026年10月15日 UI Automation識別子

Status: informational

NeNe Commanderサナが日報・引き継ぎと現状を照合し、[Issue #240](https://github.com/hideyukiMORI/NeNeCommander/issues/240)として
ファイル行、設定ショートカット、削除確認の識別子を追加した。hideの今回の指定に従い、Astraが設計・判断・統合、
SOLの実装サナが調査・実装・対象検証、別のSOLが読み取り専用の独立レビューを担当した。
基点はmain `e0fe13b77b4acbbdc6d1445fd6082b7e5e970e46`。このIssueのPRに最終mergeと整理結果を記録する。

## 変更内容

- ファイル行の`ListViewItem`に`FileListRow_`と正確な名前からなる識別子を付ける。左右のfile list内で識別し、
  focus、selection、active側、ソート、refreshで同じ名前の識別子を保つ。名前はファイル自体の永続identityではない。
- 既存の設定shortcutのラベル`TextBlock`にだけ`SettingsEntryHint`を付ける。共用templateを使うpalette hintには空の値を供給する。
- 既存の操作バー内Gridに、永久削除の確認待ちだけ`DeletionConfirmation`を付ける。局所化済みstatusを`LabeledBy`にし、
  frameworkのnamed-container peerを使う。確認終了後は完全一致の識別子が消えるが、要素自体は残る。
- [design contract](../DESIGN_HANDOFF.md#automation-identification)とharness metadataを更新した。
  Input cellの有効化やF8送信許可は行っていない。新しいボタン、入力経路、視覚デザイン、provider操作は追加していない。

不変条件は、既存のPresentation投影とAppの`AutomationProperties`だけで表示対象を識別すること。
`PaneReducer`、`FileOperationGateway`、`KeyboardIntentMapper`の所有権と挙動を保つ。
別の削除dialogやcustom peerを作らず、既存のID宣言も保持した。

## 検証結果

| 対象 | 結果 |
|---|---|
| 最終App / Presentation.Tests Release build | それぞれ成功、警告・エラー0 |
| 対象behavior | 有効23件成功、失敗・skip 0。初回22件のうち不変21件を再利用し、移動した確認testとpalette hintの2件を最終実行 |
| 変更C#のformat | 11ファイルの限定verify成功 |
| UI evidence harness | selftest 103/103成功。F8拒否を保持 |
| 独立レビュー | peer生成と共用templateの2指摘を修正し、変更固有の阻害事項なし |

主な実行コマンドは次のとおり。全filter、順序、終了code、TRX、入力hashは
`D:/NeNeCommander/evidence/issue-240/verification/verification.md`と同ディレクトリ内のログに保存した。

```powershell
& 'D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe' restore NeNeCommander.slnx -p:Configuration=Release --locked-mode
& 'D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe' build src/NeNeCommander.App/NeNeCommander.App.csproj --configuration Release --no-restore
& 'D:/NeNeCommander/tools/dotnet-10.0.401/dotnet.exe' build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore
pwsh -NoProfile -File ./eng/ui-evidence/selftest.ps1
```

対象は3providerの名前表現、WSLの大小文字、行の差分再利用・再生成、hintのcontext、
削除確認の開始とEscape/Confirm終了、idle・rename・転送競合・recycle結果での誤表示防止。
provider名を使うtestは型と投影の検証であり、実ネットワークやWSLへのアクセスではない。
ADR-0061によりfull/deep CI、coverage、mutation、無関係なtest、実画面tierは選択しなかった。
最終文書・binding・改行・相対リンク・diffの確認と通常Commit modeの結果はIssue PRに収載する。

## 修正した失敗と環境

最初のrestoreは通常配置にSDK 10.0.402しかなく、固定10.0.401が見つからず開始前に失敗した。
Microsoft公式zipを公開SHA512と照合し、`D:/NeNeCommander/tools/dotnet-10.0.401`へ配置して解消した。
repoのpinとglobal PATHは変更していない。このSDKは次回にも必要な恒久toolとして残す。

レビューで、Grid/StackPanelにIDを付けるだけではpeerを作れない点と、palette hintに共用bindingのpropertyがない点を修正した。
修正途中の定数propertyがCA1822で拒否されたため、読み取り専用auto-propertyへ変更し、最終buildを通過させた。
初回App buildは修正前の入力による成功であり、最終buildの代用にはしていない。
確認testを専用fixtureへ移したため、その1件だけ再実行した。実行数24を、異なる24件の成功と数えていない。

実UIA tree、仮想化された実control、読み上げ、実focusは未実測。[Issue #94](https://github.com/hideyukiMORI/NeNeCommander/issues/94)の
手動表示確認は別途残る。既存region IDのすべてが実UIA祖先として見えるとは主張しない。

## 整理と次回

前回の`outputs/issue-236`は不在で、恒久証拠は残っていることを読み取り専用で確認した。
`prior-cleanup-confirmed.json`に収載済みであり、前回の削除待ちは完了として扱う。
今回のworktreeと不要出力は統合後に安全確認して整理し、その結果をIssue PRへ記録する。
ブランチ・commit・恒久証拠・固定SDKは保持する。

次回は[引き継ぎ書](../handoffs/2026-10-15-ui-automation-handoff.md)を起点にする。
#181は低優先度のまま。次の製品機能は未選定で、共有列挙やUNC変更操作を着手済みとして扱わない。
