# 2026-11-19 F6描画区間と完了合図・引き継ぎ

Status: #482/PR483統合・今回の整理完了。hide停止まで継続。

1. #453はOPEN。主経路と安全確認は変更せず、受理main d0023d6のD診断コピーで4外側点と
   既存の初回/進捗描画3区間を観測。host観測・診断資源所有とcomposition解放1行以外の
   元1,347fileは一致。診断3file込み1,352ソース/9helper入力は採取後も一致。
2. 初期化時にcurrent-user/session限定のmanual-reset eventを6個作り、名前衝突は拒否。
   事前起動observerが全て未通知であることを確認。正常な最終tick/facts/span記録の後にのみSet。
   対応するDONEを読み、ackしてから明示的なlog/AX/画像/fixture読取。全6回で成立した。
   tool内部・入力注入・背景OS影響の排除ではなく、event通知後の表示も測定していない。
3. 固定warmup1＋測定5をPID32792で完了。効果2/未転送0/元0/先2/idle/12内容hashが合格。
   本測定total27.9512〜243.7985ms。長い02の234.0125msは3描画括弧の外だが原因は未分離。
   warmup965.8245msも保存、事前計画だけで除外。24外側tick/18描画span/6totalは独立検算一致。
   純provider時間・表示遅延・percentile・#479との改善比較・#453解決としない。追加標本を採らない。
4. 準備の曖昧matchと2回の診断build失敗を保存し、所有/解放等を抑制せず修正。最終selfcheck/
   App buildは警告/error0。事前レビューのsignal条件修正済み。数値・観測順序の事後レビューは
   必須修正なし。別REPLセルのスカラーreadback不一致は最終fixture確認後で、保存列はF6/ack各6件。
   追加F6なし、保存列で検証してAlt+F4通常終了した。全経緯を保持する。
5. 手順監査で初回restoreのRelease指定不足を検出。実測時2,029file/188,598,393byteのzip保全後、
   QLT-014の明示Release restore/build成功、警告/error0。355配布file中354件は完全一致。
   差は生成API説明XMLの7項目のみ、実行DLL/EXE/PDB/resource等は一致してnative証拠を再利用。
   最初の全件一致assert失敗と両XMLを保存。生成出力8件の別zipを加え現在2,029file/188,600,262byteを
   全保全した。native再測定も製品test/CI/coverage/mutation追加も不要。PR483統合済み。
6. 全source/raw/patch/commands/AX/画像/記録は`D:/NeNeCommander/evidence/f6-ui-render-spans-20261119/`。
   observer exit0、専用32792終了、通常38360と元218byte設定/hash一致。復元不要。
   作業木`D:/NeNeCommander/worktrees/issue-482`、出力`D:/NeNeCommander/outputs/f6-ui-render-spans-20261119`は
   PR483/57da22f（18:42:16、受理f348661と同じtree）統合と保全・clean/未追跡/ignored/参照0/link0を
   確認後、18:42:53に削除し不在確認済み。12数値行/208link/通常hook合格、branch/commit/恒久証拠を保持。
7. PR402は63f4769のDraftでhide画面受理待ち。公開beta.2と通常appは不変。次の調査は残り区間を
   さらに識別できる根拠がある場合に限定し、現時点で製品修正の原因を推測しない。
   正確な境界・全数値・コマンドは[品質記録](../quality/2026-11-19-f6-ui-render-spans.md)。

## ! hide実行待ち：後片付け

以前の自動承認レビューで起動前に`blocked by policy`となった4対象。別手段でも再試行しない。
保全内容は[進捗報告](../reports/2026-11-19-f6-ui-render-spans-progress-report.md)。

- `D:/NeNeCommander/worktrees/issue-454`
- `D:/NeNeCommander/outputs/footer-20261119`
- `D:/NeNeCommander/worktrees/issue-459`
- `D:/NeNeCommander/outputs/release-beta2-20261119`

hide実行用:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
