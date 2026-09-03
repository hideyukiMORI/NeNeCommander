# 日報 — 2026-09-04（Windows local file-operation adapter）

Status: informational

## 本日の区切り

引き継ぎ書が指定した縦切り「`IFileOperationPort` の Windows local production adapter」を [Issue #12](https://github.com/hideyukiMORI/NeNeCommander/issues/12) / [PR #13](https://github.com/hideyukiMORI/NeNeCommander/pull/13) として実装し、squash merge した。`FileOperationGateway` が初めて実ファイルシステムで move と delete を完走する。

## 完了したこと

- Infrastructure.Windows に `WindowsLocalFileOperationAdapter` を追加した。WindowsLocal 以外は `ProviderUnavailable`。inspect は metadata identity（種別 | サイズ | 作成時刻 | 更新時刻）と `DeletionCapability.PermanentOnly` を返す。
- preflight / copy / verify / delete はすべて `WindowsLocalEntryIdentity.Revalidate` で snapshot を再検証してから実行する（不在は `NotFound`、変化は `IdentityChanged`、ADV-004）。
- preflight は destination の存在と provider、source ごとに「destination が source に含まれる」「target 名が既にある」を `Conflict` として全 source を検査してから成功する（ADV-006）。
- copy は `WindowsLocalTreeCopy` で file と directory tree を複製する。source が reparse point であるか tree 内に含む場合は何も書かずに `ProviderUnavailable` で閉じる（ADV-003）。verify は種別・entry 集合・byte count を照合する（ADV-007）。
- delete は `Permanent` だけを実行し、`Recycle` は `ProviderUnavailable` で閉じる。gateway は Windows local の削除に常に確認を要求する（ADV-008）。
- platform 例外は `UnauthorizedAccessException` と `IOException` だけを捕捉し、`WindowsFileFailureNormalizer` の未知 HRESULT は step の failure kind（Inspection / Copy / Verification / Delete）へ落とす。
- `TestOwnedTemporaryRoot` に `WriteFile` と `CreateJunction`（`mklink /J`、privilege 不要）を足し、junction は Dispose で link として先に外す。
- ADR-0014、COMMAND_MODEL registry 行、PROJECT_STATE を更新した。

## CI と deep review で是正したこと

初回 commit `bc1d38e` は CI とローカル deep review の反復で 1 件落ちた。directory を inspect した後に子 directory を作っていたため、更新時刻の変化で identity が変わり `IdentityChanged` になっていた（ローカル単発では時刻粒度で偶然通過）。fixture を先に組み立ててから inspect するよう `c833b59` で直し、3 回連続実行で安定を確認した。

`c833b59` の deep review は Infrastructure.Windows の mutation score が 88.89 % で閾値 90 % を下回った。`Revalidate` と重複する source の null guard（等価 mutant）を copy / verify / delete から外し、`WindowsLocalTreeCopy` と `WindowsLocalEntryIdentity` の null guard、上書き禁止、未知 HRESULT の step fallback、file を destination にした copy の `NotFound` を直接証明して `5e2d0fb` で 95.59 % にした。

## 検証証跡

- `pwsh -NoProfile -File ./eng/check.ps1`: PASS（pre-commit hook 経由）。
- GitHub quality run（PR #13、`5e2d0fb`）: [`33774610075`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33774610075) 成功、2分21秒。
- GitHub quality run（main、`f8a500b`）: [`33775079366`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33775079366) 成功、3分13秒。
- ローカル deep review（`5e2d0fb`）: passed（`c833b59` は Infrastructure.Windows 88.89 % で失敗、`5e2d0fb` で是正）。
- tests: 220 passed、0 failed、0 skipped（前回 198）。
- branch coverage: Domain 100.00%、Application 100.00%、Infrastructure.Windows 94.97%、Presentation.WinUI 96.00%。
- mutation score: Domain 96.88%、Application 97.05%、Infrastructure.Windows 95.59%、Presentation.WinUI 100.00%。
- gateway 結合（temp root）: file の move が copy → verify → delete の effects 3 件で成功し source が消える。未確認の permanent delete は `ConfirmationRequired` で file が残り、確認付きは `PermanentlyDeleted` で消える。

## 残した注意点

- identity は metadata tuple であり file ID ではない。同じサイズ・同じ時刻で内容だけ変わった置換は検出できない（ADR-0014 で Win32 file ID を hardening として保留）。
- verify は byte count 照合で hash は取らない。
- directory copy は一つの provider step で、cancellation は gateway が step 間で観測する。
- UI からの `F5` / `F6` / `F8` 接続と confirmation UI は未着手。
- ACL / junction fixture は NTFS 前提（CI の windows-latest も NTFS）。
- 前回からの注意点（同期列挙、ダークテーマでの TextBox 不可視、capacity 定数 20、`Tab` の実機未確認、Issue #2、旧 typo path）は継続。

## 次の推奨縦切り

`F6`（move）を `FileOperationGateway` に接続する。active pane の focus item（selection があれば selection）を passive pane の location へ move し、結果を両ペインの再読み込みと status に反映する。confirmation が要る delete（`F8`）、copy（`F5`）、collision UI は混ぜない。
