# 引き継ぎ 2026年10月18日 closed recordの外部継承調査

Status: informational

正本は[PROJECT_STATE](../PROJECT_STATE.md)。再現、型分類、比較、正確なコマンドは
[日報](../reports/2026-10-18-record-closure-daily-report.md)、対策案は
[ADR-0073](../adr/0073-close-record-cases-with-inaccessible-abstract-members.md)を参照する。
[Issue #305](https://github.com/hideyukiMORI/NeNeCommander/issues/305)のbaselineは
`1abea9392ab9421fe47246e761d655c62b49d112`。調査完了と修正完了を分ける。

## 確定した事実

- SDK 10.0.401 / C# 14.0、実Application DLL、別の非friend assemblyで未知WindowAdjustmentActionを生成できた。
- 通常のbase()はCS7036、base(existing)は成功。生成protected copy constructorが経路。
- 未知caseのwith/equalityが成立し、実plannerでは既知MoveLeftの-32pxに対して未知caseが0pxのWindowMovePlanになる。OS適用なし。
- 4層130 abstract recordsの公開119型中118型で外部具体派生をcompileできた。非公開11型は対象外。
- FileSystemPathだけはinternal abstract identity memberのためCS0534。全118型のruntime生成や全consumer被害は未検証。
- window-model testの同一assembly列挙とcopy constructor除外は、外部caseの閉包を証明しない。
- Bookmarkの既存未知case testはfriendからのdispatch防御。FileOperationGatewayの未知requestは通常switchで例外、port呼出なしというsource確認。
- private/private protected copy ctorはCS8878。構造的abstract member案は具体派生を拒否し、既知recordのcopy/equality/hashを保持する。
- 外部abstract派生とfriendからの具体派生は許す。敵対的managed codeのsandboxやユーザー入力からのexploitを実証した結果ではない。

## 次の実装で必要なこと

ADR-0073はproposedのまま。今回の承認範囲は調査と提案であり、一括製品変更は含まない。
[後続Issue #306](https://github.com/hideyukiMORI/NeNeCommander/issues/306)で定義と方針を受理し、
118 exposed rootsとsealed cases、friend testsを一つの整合した変更として移行する。
既存の意味ある非公開abstract memberは再利用し、非公開rootを一律に変更しない。
既知case/factory/with/equality、外部copy経路の拒否、friendの未知dispatch防御を検証する。
外部compile proofとそのnegativeを既存conformance/canonical gateへ組み込み、規約とproof registryも更新する。
現時点で新gateが稼働しているとは言わない。空overrideを呼ぶだけのcoverage対策、除外、閾値緩和は禁止。
全層共有モデルの移行では小さな試作だけで影響を証明できないためcanonical gateを選択し、成功した同一入力の証拠は再利用する。
plannerの未知action fallback変更は、必要なら独立した挙動として明記する。

## 保存・終了状態

恒久証拠は`D:/NeNeCommander/evidence/record-closure-20261018/`。
`artifacts/`、`artifact-manifest.json`、`reproduction-01.json`、`inventory.json`、`comparison.json`、build/run logsを保持する。
初回3層inventoryは`inventory-v1/`で、最終4層の集計と混ぜない。
最終11 compiler期待結果と3 runtime判定は成功。fixtureと参照製品buildは警告・エラー0。
製品suite/full/deep/coverage/mutation、live UI/NAS/WSL、release #94は実行していない。
sourceを読んだだけの既存testを新たな成功件数へ足さない。

今回の追加物は`D:/NeNeCommander/worktrees/issue-305`と`D:/NeNeCommander/outputs/issue-305`。
実行は全て終了している。PR統合、同期main、tree一致、整理結果はIssue PRと`completion.json`で確認する。
これらの最終記録ができる前に、本文の調査完了をworktree削除済みとは読み替えない。
branch/commitと恒久証拠は保持する。cleanupが拒否された場合は開発手順に従い、その正確な対象だけを引き継ぐ。
