# Entry visibility glossary — 2026-10-18

[Issue #320](https://github.com/hideyukiMORI/NeNeCommander/issues/320)で、
用語集の「entry nameから決して導出しない」という一律の説明をprovider別の既存契約に直した。

- Windows local / UNC: 列挙結果のHiddenまたはSystem属性で`Hidden`、それ以外は`Normal`。
- WSL: Linux名のdot-prefixで`Hidden`、それ以外は`Normal`。投影されたWindows属性は使わない。
- Application / Presentation: readerの分類済み`EntryVisibility`を消費し、名前から再分類しない。
  可視集合の決定は既存の`PaneReducer`だけが担う。

根拠は[ADR-0024](../adr/0024-hidden-item-visibility.md)、
[ADR-0035](../adr/0035-wsl-directory-read-provider.md)、
[ADR-0057](../adr/0057-unc-directory-read-provider.md)、
`WindowsDirectoryReadOperation.ClassifyByAttributes`と`WslDirectoryReader.ClassifyVisibility`。
既存のlocal/UNC testsはdot名を通常として、WSL testsは`.config`をhidden・
Hidden属性だけの`plain`を通常として記述しており、修正した定義と一致する。
今回はそのソースを照合しただけで、testを実行したとは主張しない。

ADR-0061の文書のみの変更として、根拠・相対リンク・`git diff --check`と
通常hookの`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`を検証範囲に選んだ。
製品、tests、依存、gateに変更はなく、build/product test/CI/coverage/mutationは開始しない。
waiverなし。先行#318の統合とcleanup完了も既存の日報・引き継ぎに反映した。

既存checkoutで`docs/320-provider-visibility`を使用し、追加worktree/outputは作っていない。
恒久証拠は`D:/NeNeCommander/evidence/visibility-glossary-20261018/`。
Issue PRと`completion.json`にCommit結果、squash統合、cleanなmainへの同期を記録する。
PR #321は19:55:57 JSTに`9a6d45fb474bd8f017e9668761e9de0b4e05449e`でsquash統合した。
mainの同期・clean・task tree一致を確認済み。追加作業物と後片付けの実行待ちはない。
後続のCS-013棚卸しと分割・gate化実施順の準備は別Issueで行う。
