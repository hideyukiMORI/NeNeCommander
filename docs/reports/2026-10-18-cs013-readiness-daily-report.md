# CS-013自動検査の準備 — 2026-10-18

[Issue #322](https://github.com/hideyukiMORI/NeNeCommander/issues/322)で、
hideが順に指定した3件目の棚卸しと分割・ゲート化の実施順をまとめた。
成果物は[計数定義・全体集計・分割計画](../quality/cs013-readiness-2026-10-18.md)。
製品の一括改修や全体gateの有効化は今回の範囲ではない。

基準mainは`9a6d45fb474bd8f017e9668761e9de0b4e05449e`。
tracked C# 694ファイルをC# 14の構文として解析し、parse errorは0だった。
製品側は型の300行超過5件、メソッド等の宣言40行超過7件、4引数超過42件。
テスト側はそれぞれ30件、52件、15件。行数は計画に明示した計数定義による。
最大の製品型は`WindowsLocalSettingsStore`の662行、書込みメソッドは200行。
認知複雑度は正式な計算方式が未確定で未測定、nestingは構文上の候補値として区別した。
固定native ABIの13宣言/実装と、managed mirrorの8署名を通常の引数整理から分けた。

計画は、計数/ABI境界のADR → 表示モデル → Appの描画・転送部品 →
共有ファイル操作契約とgateway → provider adapter → settingsのatomic write →
残るtest fixtureと複雑度 → 全対象を確認したうえで唯一のgateへ接続、という順序。
各段階に保持する状態所有権・安全境界・対象の既存testsを記した。
全体gateが未実装である事実にCODING_RULESのenforcement欄を合わせ、
active状態・閾値・baseline禁止・既存のcompiler/analyzerは維持した。

## 実行した検証

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/cs013-readiness-20261018/Measure-Cs013.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/cs013-readiness-20261018/Measure-Cs013.ps1 -FixturesOnly
git diff --check
```

監査はexit 0。工具の小さなfixtureはコメント/単独brace/文字列、4/5引数、
local function、nested type、深さ4、40/41行、300/301行、partialの同一性と集約を確認して成功。
後から加えた境界fixtureだけを再実行し、変更していない計数関数と製品入力の集計は再利用した。
初回のPowerShell/Roslyn getter呼出不整合は修正済みで、成功前の工具開発上の失敗として区別する。
根拠・相対リンク・文書差分を確認し、通常hookで
`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`を実行する。
build/product tests/CI/coverage/mutation、live provider/UI、release #94は実行していない。

恒久証拠は`D:/NeNeCommander/evidence/cs013-readiness-20261018/`。
全型・全メンバーCSV、集計JSON、694入力のSHA-256、再現scriptとfixture結果を保存した。
計数規則・cognitive/interopの正式決定をせずに「全CS-013合格」とはしない。

## 統合と整理

既存checkoutの`docs/322-cs013-readiness`で文書のみ変更し、追加worktree/outputは作っていない。
Issue PRと`completion.json`が最終Commitチェック、squash統合、cleanなmainへの同期を記録する。
先行#318はPR #319で統合・D側worktree/output削除済み。
#320もPR #321で統合・clean main同期済みで、追加作業物はない。
後片付けの実行待ちはない。
