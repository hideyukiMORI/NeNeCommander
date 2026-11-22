# 2026-11-22 作業報告 — 新規テキストファイル作成の安全性調査

#544の設計を、既存の名前入力・FileOperationGateway・provider routerで具体化した。
製品コード、main、公開beta.3は変更していない。
[提案ADR](../adr/0085-create-empty-text-file.md)と
[品質記録](../quality/2026-11-22-text-file-creation-design.md)を正本とする。

専用Windows領域の試験で、通常のBCL CreateNewがリンク先のないsymlinkをたどり、
その先へファイルを作る事実を確認した。native CREATE_NEWとOPEN_REPARSE_POINTの
組み合わせは、そのリンクを変更せず拒否した。最初のnullable宣言不足のcompile失敗、
BCLの9成功/1失敗、nativeの10成功、両方に残る親リンク追跡をすべて保全している。
16組の同時作成は、各組1回だけ成功し0byteだった。

追加6境界も成功。hardlink、live/dangling directory link、後発directory衝突を拒否し、
255文字leaf/561文字pathは成功、256文字leafは拒否した。これらは製品providerやWSL、
キーボード経路の証拠ではない。親の最終検査後の差し替えを完全には防げない限界を明記した。

提案は空内容/BOMなし、編集可能な.txt付き初期名、Ctrl+F7、作成後の新規項目focus。
キャンセルで完了した作成を消さず、既存の単一操作経路で結果を報告する。
mouseの新規作成menuは#530/#531との統合が必要。独立技術レビューを依頼済みで、
現時点ではADRはproposed、実装・統合・機能完了ではない。

22入力をhash固定。試験のexact command、誤ったpath探索・summary読み取り等も品質記録へ記載。
製品build/test/coverage/mutation/CIは実行していない。
#542公開push、#547Claude送信の自動承認拒否は依然pendingで、別経路を試していない。

専用D worktree、3fixture、証拠は未完了として保持。fixture内のlinkはすべて専用領域内を指す。
後片付けの拒否は起きていない。技術受理、main統合、恒久収載後の限定整理が必要。
ユーザーApp、設定、他の案件には触れていない。
