# 日報 — 2026-09-06（カテゴリ別ブックマーク）

Status: informational

## 現在の範囲

[Issue #99](https://github.com/hideyukiMORI/NeNeCommander/issues/99) は、表示名、保存 path、
カテゴリ、任意の固定ショートカットを持つ bookmark catalog を実装中である。
[ADR-0041](../adr/0041-category-bookmarks-through-settings-and-navigation.md) は、既存の
`settings.json`、`WindowsLocalSettingsStore`、`SettingsSession` ordered queue、active pane の
`IDirectoryReadPort` 経路だけを使う契約を採択した。別 bookmark file、writer、navigation engine、
dynamic key map は追加しない。

初版は user category 32件、bookmark 128件、category name 64 UTF-16 code units、display name
128 UTF-16 code unitsに制限する。Uncategorized は persisted null で、UIだけがlocalized labelへ
変換する。bookmark key は category/name のcase-insensitive pairで、manager selectionはkeyに加えて
完全なimmutable entryを保持する。同じkeyのpath等が差し替わったstale actionはI/Oとmetadata変更前に
拒否する。category削除は全entryを一括でUncategorizedへ移し、同名衝突時は全体を無変更で拒否する。

schema version 2はbookmark arraysを含むexact shapeで、valid version 1だけをempty catalogとして
明示移行する。readerはinvalid UTF-8とescaped lone surrogateをtyped rejectionへ閉じる。writerは
完全なversion 2 documentをUTF-8へserializeし、65,536 bytesを超える場合はdirectory/temp作成前に
`TooLarge`で拒否する。既存ADR-0040のancestor、document、temporary identityとatomic publish契約は
変更しない。

keyboardはcanonical tableへ`Ctrl+B`と固定`Ctrl+1`から`Ctrl+9`だけを追加する。`Ctrl+P`は
[Issue #101](https://github.com/hideyukiMORI/NeNeCommander/issues/101)用に未割当のまま保持する。
bookmarkからのnavigationはcurrent catalogをsession ownerが再解決した後、既存DualPane/Pane routeへ
typed pathを渡す。未割当、stale、modal、operation busyではprovider readを開始しない。

## failure-firstと現在の検証

Infrastructure担当のisolated filterでは、旧productionにversion 2、strict UTF-8、UTF-8 size、
version 2 serializationを要求する7件のうち5件が失敗し、valid version 1 migrationとclosed nested
shapeの2件だけが成功した。修正後のfocused persistence proofは10/10、Infrastructure全219件、
Application全256件、Domain全66件、Presentation全86件、Architecture全5件に成功した。
Ctrl+B追加後に既存modal proofのhint件数期待が9のまま残って1件失敗したが、canonical Ctrl+Bと
既存Ctrl+,/Escapeの順序を含む10件の表示契約へ更新し、Presentation全86件で再確認した。
現時点は実装途中であり、coverage、mutation、security deep、Draft-to-Ready canonical CIを
merge-readiness evidenceとしてまだ主張しない。

## GUI handoff

Claude Designとの相談は別担当が継続中である。採択済みのbehaviorは、上段search、左categoryと右bookmark
の2列、独立Bookmark modal、明示Save/Cancel draft、category削除専用confirm、成功時だけmodalを閉じる
navigation、固定status/warning領域である。最終artifact URL、focus、narrow-width、semantic resources、
AutomationIds、localized product copyはhandoff確定後に同期する。生成mockの数値styleや新しいtoken名は
未承認でありproductionへ持ち込まない。

## 次の作業

- Infrastructure full regressionと統合buildを確定する。
- session-owned bookmark browse/edit/confirm stateとsole `CommanderSession.HandleAsync` actionsを完成する。
- approved Canvas handoffを既存semantic resourcesだけでPresentation/XAMLへ反映する。
- affected coverage/mutation、dependency、Commit checks後にDraft checkpointを更新する。
- final exact headでsecurity deepを実行し、Ready後にcanonical CIを一度取得する。

[Issue #100](https://github.com/hideyukiMORI/NeNeCommander/issues/100)のwindow shortcutと
[Issue #101](https://github.com/hideyukiMORI/NeNeCommander/issues/101)のcommand searchは追跡のみで、
このbranchでは実装しない。live WSLとWindows UI matrixは引き続きIssue #93/#94のrelease tierである。
