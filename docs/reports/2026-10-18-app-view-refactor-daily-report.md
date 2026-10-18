# App画面の責務整理 — 2026-10-18

[Issue #330](https://github.com/hideyukiMORI/NeNeCommander/issues/330)は、hide指定の3件のうち③。
基準は②を統合した`552c695b5172fe6fafea0df1409569a2bc04c89e`。
①#326 / PR #327、②#328 / PR #329はmain同期・追加作業物整理まで完了した。

`CommanderWindow`からパレット、操作バー/競合modal、各paneの描画を
`CommandPaletteView`、`OperationBarView`、`CommanderPaneView`へ移した。
`BookmarkManagerView`はoverlayの描画抑止と初期focusの調整を保持し、一覧と編集部品を
`BookmarkBrowseView`、`BookmarkDetailsView`へ分けた。新しいviewはApplication状態や
`AsyncWorkOwner`を所有せず、既存のtyped intentを唯一の`CommanderWindow.ForwardIntent`へ返す。
旧表示メソッドとXAML handler属性は削除し、各生成時に一度だけ接続する。
Application/Presentation/providerの実装、資源、設定schema、依存、品質gateは不変。

| 型 | 変更前 logical lines | 変更後 |
|---|---:|---:|
| CommanderWindow | 538 | 294 |
| BookmarkManagerView | 312 | 71 |
| CommandPaletteView | — | 163 |
| OperationBarView | — | 164 |
| CommanderPaneView | — | 59 |
| BookmarkBrowseView | — | 141 |
| BookmarkDetailsView | — | 164 |

旧5引数のRenderPaneは既存コントロールに結び付いたviewの3引数Renderへ移した。
旧51行のbookmark constructor、認知15のbrowse処理、深さ4のfocus選択も責務を分けた。
partialファイルへの分散、引数袋、閾値変更、baseline導入は行っていない。

## 検証

SDK 10.0.401、Release、作業treeは`D:/NeNeCommander/worktrees/issue-330`、
TEMP/TMPは`D:/NeNeCommander/outputs/issue-330/tmp`。

```powershell
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~ViewCompositionOwnershipTests|FullyQualifiedName~CommanderWindowCompositionTests|FullyQualifiedName~AsyncLifecycleOwnershipTests'
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~BookmarkManagerPresenterTests|FullyQualifiedName~BookmarkPresentationBehaviorTests|FullyQualifiedName~AddressEditorPresenterTests|FullyQualifiedName~AsyncWorkOwnerTests'
python D:/NeNeCommander/evidence/app-view-refactor-20261018/Verify-ViewMigration.py
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
git diff --check
```

Appを含むArchitecture.Tests buildとPresentation.Tests buildは最終警告0・エラー0。
構成/所有15ケースと表示/非同期50ケースが全成功、失敗/skip 0。
新しい12ケースは6 viewのsession/async所有禁止とconstructor上限を守る。
changed C#の`dotnet format whitespace NeNeCommander.slnx --no-restore --include
<changed C# paths> --verify-no-changes --verbosity minimal`はexit 0。
formatterのworkspace読込み警告は記録する。初回の機械移行で`OperationBarIcon.NameEntry`の
誤置換と、そのC#失敗に続くXAML解決失敗/警告が発生した。次のIDE0032もauto-propertyへ
修正済みで、失敗ログと最終成功を分けて保存した。
初回CommitチェックのCS-002指摘も修正した。modeの真偽値を渡さず、overlayに一度だけ
保持した前回の状態型と現在の状態型から、編集viewの表示更新を判定する。

構造確認では36イベント接続が元と一致し、90コントロール参照の名前/型がXAMLと一致。
欠落・二重接続・誤ったcontrol名の3負例を拒否した。XAMLの構造・内容・属性は移した
10イベント属性以外同一。windowの29メソッド、paletteの12メソッド、operationの10メソッド、
bookmarkの10イベント処理とpersistence処理が等価であることをソース比較した。
bookmark初期focusはname/category/cancel-delete/retry/searchの同じ対応を保持する。

QLT-011のfocus/selection/inactive/busy/cancel/errorは、上記の対象状態テストと、
関連入力不変を確認した①の23ケース・②の74ケースを再利用する。
高contrast・localization expansion・design tokenについては、変更していない資源と
XAMLのstyle/layout/binding同一性を確認した。これはライブ画面観測ではない。
実際のdesktop、provider/live、release #94、full/deep CI、全件回帰、coverage/mutationは
ADR-0061の今回範囲に含めない。既存coverage除外も変更していない。

最終計測は754 C# files、1,194 types、7,903 callables、未解決invocation 7,285。
159→153指摘（製品60・tests93・eng0）で期待どおりexit 1。
method/cognitive/nesting/parameters/typeは53/23/10/37/30。変更/追加C#の指摘は0。
今回の3件で180→173→159→153となった。工具の定義とself-proofは#324から再利用する。
通常Commit hookを使用し、成功した関連入力不変のbuild/testをpush/mergeで繰り返さない。

## 統合と整理

恒久証拠は`D:/NeNeCommander/evidence/app-view-refactor-20261018/`。
Issue PRと`completion.json`が最終Commit、squash、clean main同期、証拠保存後の
task worktree/output整理を記録する。branch/commit・恒久証拠・共有SDKは保持する。
残る153指摘のApplication/provider/settings/test責務分割とglobal gate化は後続の別範囲。
