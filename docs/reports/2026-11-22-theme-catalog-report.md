# 2026-11-22 作業報告 — 共通テーマ125種の調査

#527、main7ef9b96起点。製品配色・設定・UIは変更していない。
[設計調査](../design/2026-11-22-theme-catalog-investigation.md)、
[品質記録](../quality/2026-11-22-theme-catalog-investigation.md)に全試行を保存。

固定commitの正本から125種の名前/順序/明暗/本文16色/出典を抽出。
Nibは125、Folioは共通125＋従来2。Commanderにだけある既存4を保持すると129になるため、
20:11にhideへ選択肢を確認中。既定nene-darkを勝手に別テーマへ移行していない。

125の識別子・順序・出典は一致。vscode-high-contrast-lightだけNibの10月20日修正版と
Folioの旧暗色が相違。旧取込commitとの比較で、この1種以外の本文色/明暗不変を確認。
CommanderのSolarizedには同名でも基礎色差があり、将来の見た目変更として記録した。

palette notice本文は一致。最初の末尾全体比較はFolio固有のMermaid/Pele追記まで含めたため
差分となり、原結果を保持して範囲を限定した。許諾の無条件な断定はしていない。
現配布経路へテーマnoticeを明示収載する必要がある。125本文色対の計算は全て4.5以上、
最小4.7478778。Commanderの全状態・実画面の可読性とは区別する。

静的診断4件はexit0。製品build/test/CI・UI操作・外部送信なし。
docs/527-theme-catalog-investigation、D:/NeNeCommander/worktrees/issue-527。
証拠D:/NeNeCommander/evidence/theme-catalog-20261122（固定source27ファイルを含む）。
未統合なのでworktreeと空の専用output領域も保持。Nib/Folio・通常App/設定は変更せず、
#542/#547の既存承認待ちを維持。hideの継続指示は有効。
