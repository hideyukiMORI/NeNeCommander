# Infrastructure.Windowsのテスト整理（#417）

## 受け入れ範囲

CS-013 / ADR-0075の26 findingsを、既存のテストケース・属性・assertion・競合と取消の順序を保持して解消する。
対象は`tests/NeNeCommander.Infrastructure.Windows.Tests`の34ファイル。製品コード、依存、閾値、除外、測定器、保存形式は変えない。
規則はCS-013、TST-001/003/006/010/011/012、QLT-015、GIT-001〜004、ADR-0061/0075。

元の基点は`69e2d486`、実装・前回検証の最終headは`f4bffc1721bc1d41b0fac392805e02ab4172b884`。
2026-11-18、hideの「Commanderの未完了から進める」指示で残る記録・レビュー・統合を再開した。

## 変更と意味の保持

- Bookmark設定の28テストは3クラスへ、WindowsLocalSettingsStoreの80テストは10クラスへ責務別に分けた。
- 長い復旧・競合テストは、同じscheduler、書込フック、公開結果、候補削除、flush後の残留bytes確認を小さいhelperへ移した。
- Live WSLのrootの受入、path、anchor、marker、所有権と終了処理を名前のある内部fixtureへ分けた。markerのCreateNew、identity照合、リンク解除→再検証→run child削除の順序を保つ。
- Providerの委譲とWindows/WSL転送のテストは、同じassertionとcopy→verify→deleteの判定順序を保って分けた。
- `ResolvePath(runRoot)`の純粋な文字列変換は、保存済みの値を再利用した箇所から各責務での再計算へ変わり、正常系で3回増える。全関数の呼出回数が完全一致する変更ではない。実際のresolverには副作用がないことを確認した。

## 前回の検証と今回の再利用

2026-11-16のraw記録を恒久保存先`D:/NeNeCommander/evidence/issue-417/original-20261116/`へ収載した。
32ファイル、14,160,722 bytesを原本とSHA256照合済み。`archive-manifest.json`が一覧。

| 証拠 | 結果 |
| --- | --- |
| `test-full-after.log`（Infrastructure.Windows.Tests、Release） | 850件、834成功、0失敗、16 live opt-in skip |
| `architecture.log` | 21件成功、0失敗、0 skip |
| `live-20261116-417a/live.log`とraw `live.trx` | clean `f4bffc1`、13/13 cell成功、0失敗、0 skip。Release buildは警告0・エラー0 |
| `tests-before.txt` / `tests-after.txt` | 検出された850ケースのmultisetが同じ |
| `compare-tests-result.txt` / `compare_tests.py` | 594 TestMethod宣言、method/class属性差0。93メソッドは本文完全一致で別ファイルへ移動、24メソッドの本文変更は個別照合 |
| `inspect-before.json` / `inspect-after.json` | cs013-v1の69→43 findings。対象26件消失、新しいfindingなし |
| `conformance.log` | 112規則成功 |

今回、旧Inspectに収録された901 C#入力を、測定器と同じ「BOMを除いた読込文字列のUTF-8」のSHA256で現作業木へ照合した。
901件とも一致し、worktreeはclean、HEADも`f4bffc1`だった（`inputs-reuse-20261118.json`）。
また、live記録はこのhead・cleanを明記し、現在の依存・toolingも同じcommitにある。
担当変更や文書追記だけを理由に、成功済みの通常・Architecture・live試験は再実行しない（ADR-0061）。
前回の通常テスト・formatのログには外側のコマンド引数とexitの別記録がないので、それらを今回実行したコマンドとしては記載しない。
実測結果はrawログの範囲で引用し、現在の書式・commit検査はこの継続で別に記録する。

最新main `b5d9af8`の取込みは#418のDomain.Tests2ファイルとPROJECT_STATEの1行のみで、#417の製品・テスト・tooling入力は変わらない。
旧Inspectの43は#418取込み前の値であり、統合候補全体の実測と区別する。

## 独立レビュー（2026-11-18）

hideの本日の並行許可により2席が読み取りだけで実差分を照合し、主担当が受理した。
設定系では28＋80件、残るWSL/Provider系では所有権・取消・転送と終了処理を確認。修正必須の指摘なし。
設定保存の76本文とBookmark設定の全本文は完全一致。設定保存の変更4件はhelper抽出のassertionと順序を個別照合した。
reviewは動的試験の代用とはせず、上の既存実行結果と合わせて判断する。

## この継続の確認

- `git merge --no-ff main -m 'chore(tests): 検証済みのDomainテスト整理を取り込む (#417)'`：成功、競合なし。
- 901 sourceのSHA256とclean HEAD照合：一致、差0。
- 旧出力32ファイルの恒久収載とSHA256照合：一致、差0。
- 最終の書式、Commit検査、PR、統合と作業領域の整理は、PRと`D:/NeNeCommander/evidence/issue-417/completion.json`へ記録する。

## 限界

追加waiverなし。製品の見た目・物理キー・別のWSL環境・release #94の確認ではない。
製品のtrust boundaryを変更していないため、全体gate・deep review・層全体のmutation・無関係なlive tierは今回選択しない。
