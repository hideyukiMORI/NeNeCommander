# 日報 — 2026-09-10（#101 統合、#105 実 WSL identity、#99 統合）

Status: informational

体制は前日と同じ。Fable 5 セッションの NeNeリナが設計・判断・文書、Opus 5 のバックグラウンド
NeNeリナが実装・診断・ゲート運転を担当した。この日は実装リナ 3 本（#101 運転、#99 運転、#105
実装）と診断リナ 1 本（#105 読み取り専用診断）を立ち上げ、#99 の merge は日付が変わった直後に
設計側で行った。

## 統合したもの

| Issue | PR | 内容 | 証拠 | merge |
|---|---|---|---|---|
| #120 | #121 | 未明の日報・引き継ぎ書・PROJECT_STATE | 両チェック緑のまま未 merge だったものを設計側で merge | `47bfb0c1` |
| #101 | #113 | ADR-0047 command palette | dependency `34472244089`、exact-head deep `34472326634`、canonical `34474891436`、main deep `34475830281` | `7b8abe04` |
| #105 | #122 | ADR-0049 実 WSL の 9P entry identity | dependency `34484473296`、exact-head deep `34484497758`、canonical `34487983447`、main deep `34489001939` | `94079848` |
| #99 | #102 | ADR-0041 category bookmarks | dependency `34491933717`、exact-head deep `34491927767`、canonical `34494972324`（head `c0c67eae`） | `008c4f03` |

isolating host の真の score（exact-head deep）:

| 層 | #101 `9aa6a2d5` | #105 `c8cdb0b8` | #99 `c0c67eae` |
|---|---|---|---|
| Domain | 95.52% | 95.52% | 95.52% |
| Application | 95.72% | 95.72% | 95.87% |
| Infrastructure.Windows | 90.62% | 90.74% | 90.86% |
| Presentation.WinUI | 94.14% | 94.14% | 94.30% |

## #101 command palette

checkpoint `41bbdf9b` を main `47bfb0c1` に rebase した head `9aa6a2d5` で全ゲートが一発で通り、
テスト追加も production 変更も不要だった。deep の score は checkpoint の isolating host 計測値と
一致した。

CodeQL alert #102（`cs/dereferenced-value-may-be-null`、`CommanderWindow.RenderPane` の
`_addressPresentation` 二重参照）は #101 固有ではなく、#107 で入った code として main `f25cfb26`
と `feat/111`、`test/116` の各 ref でも open だった。deep-review workflow はこの alert で失敗しない。
SECURITY_MODEL は「workflow 成功は open alert ゼロの証拠ではなく API で読み戻す」と定めているので、
#101 branch に含まれていた 1 回 capture の修正を統合して閉じる判断をした。統合後の main deep run
`34475830281` で main の instance は `fixed`。統合済み branch の stale ref に残る instance は ref が
消えれば閉じる。新 SDK ピン 10.0.401 での scheduled deep review `34449715280` も success。

## #105 実 WSL identity

### 読み取り専用診断

Running 中の Ubuntu-22.04 だけを対象に、WSL 内無変更・access 0 の handle だけで採取した
（WSL 2.7.13.0、kernel 6.18.33.2、Windows 11 26200.9445）。Stopped な distribution は share を開くと
起動する可能性があるため触れていない。

- 9P redirector は `FileIdInfo`、`FileStatLxInformation`、`FileIdInformation`、
  `FileFsObjectIdInformation`、`FileIdExtdDirectoryInfo` をすべて拒否する。volume serial は全 API で
  0、`FileRemoteProtocolInfo` は全 path で同一（Reserved 全 0）。namespace 側の distribution 識別子
  は share 名だけ。
- `NtQueryInformationFile(FileInternalInformation)` は Linux inode と 21/21 一致、`NumberOfLinks`
  は nlink と一致。ただし inode は distribution 内の 12 の mount root（`/proc` `/dev` `/run` `/sys`
  `/mnt/wsl` 他）ですべて 1 で衝突し、`/` と `/tmp/.X11-unix` が 2、`/bin` と `/run/user` が 15 で
  衝突する。列挙（`FileIdBothDirectoryInfo`）は mount point の stub inode を返し、open は mount 先
  root の inode を返すので両者は mount point で食い違う。
- symlink は `FILE_FLAG_OPEN_REPARSE_POINT` で follow されず、attribute `0x400` と tag
  `0xA000001D` で target と区別できる（`/bin` 15 vs `/usr/bin` 46）。
- `CreationTime` は birth time ではなく `min(atime, mtime, ctime)` で、読み取りだけで動く。
  `FileBasicInfo` の `ChangeTime` / `LastWriteTime` / `LastAccessTime` は Linux の ctime / mtime /
  atime と 29/29 一致し、読み取りアクセスでは ctime と mtime は 9/9 不変。
- `GetVolumeInformationByHandleW` の fs name は WSL 側 43/43 で `9P`、ローカル volume は `NTFS`。
  drvfs（`/mnt/c`）は share 経由ではどの access でも `ERROR_ACCESS_DENIED`。`\\wsl$` と
  `\\wsl.localhost` は同値。

含意: 現行の `WindowsWslFileSystem` は実 WSL で identity 取得が fail closed するので、ADR-0036 /
ADR-0037 の WSL mutation は実環境で一度も動いていなかった。

### 設計判断: ADR-0049

`WindowsFileIdentifier` を単一 owner のまま、`WslPath` からだけ選ばれる `ReadWslFacts` を追加し、
`wsl-v2 = distribution | inode | kind | reparse tag | nlink | length | mtime | ctime` を identity
にする。fs name が `9P` のときだけ許可し、`FileIdInfo` 失敗を契機にした fallback は作らない。列挙
由来の id は identity にしない。distribution の epoch は主張しない。ctime は kernel だけが更新し
非特権では偽装できないので、inode 再利用による replacement を検出できる。mount 越しの inode 衝突は
distro root か本人の FUSE mount が必要で、identity の threat scope 外とする。hide は「リナの推しで
いい」としてこの scope を受理した。ADR-0049、ADR 索引、FS-001 / FS-012、SECURITY_MODEL の identity
段落を設計側で commit（`45918c9`）し、実装を実装リナに発行した。

### 実装結果（PR #122）

`79217c2`（実装）、`5cc08b4`（ADV-020 と SEC-014 rule）、`c8cdb0b`（mutant 対策テスト）。変更の要点:
`WslHandleFacts`（sealed record class、FILETIME をそのまま保持）、`WindowsFileIdentifier` の
`ReadHandleFacts`（unguarded）/ `ReadWslFacts`（`9P` guard、production の唯一の入口）/
`ComposeWslToken`（純関数）、ntdll `NtQueryInformationFile(FileInternalInformation)` と
`RtlNtStatusToDosError` の `LibraryImport`、`WindowsWslFileSystem` の `wsl-v1` 撤去と reader seam、
SEC-014 rule（`9P` literal と両 reader の定義は `WindowsFileIdentifier.cs` のみ、`ReadWslFacts` の参照
は `WindowsWslFileSystem.cs` のみ）と negative proof 2 件、ADV-020、`TestOwnedTemporaryRoot.CreateHardLink`
（`mklink /H`）。`Describe` / `DescribeHandle` の ADR-0033 契約と settings store は不変。

証拠: Infrastructure.Windows 236/236（新規 14 件、うち ADV-020 が 8 件）、branch coverage 100.00 /
100.00 / 92.85 / 94.36。殺した mutant は `WindowsFileIdentifier.cs:195` と
`WindowsWslFileSystem.cs:129`。`:204` の `TrimEnd` 等価変異は `Array.IndexOf` ベースに書き換えて
mutant 自体を消した。等価として残したのは `ReadInode` の `status == 0`（直後の query が同じ error を
返す）と `Marshal.FreeHGlobal` の statement removal の 2 件。

実 WSL の参考観測（read-only、未請求）: Running 中の Ubuntu-22.04 で `ReadWslFacts` が `/`、
`/etc/hostname`、`/bin`、`/usr/bin` の token を返し、2 回連続で同一、`/bin` は `link|A000001D`、
`/usr/bin` は `directory` で別 token。inode・nlink・size・mtime・ctime は Linux `stat` と一致。

設計側の事後判断: drvfs は `ERROR_ACCESS_DENIED` で開けないため共有 normalizer どおり
`AccessDenied` に閉じる。ADR-0049 の「`ProviderUnavailable`」は誤りで、ADV-017 の既存契約を優先し
本 docs 変更で訂正した。`WslHandleFacts` は CS-003 に従い record class、timestamp は FILETIME 値の
まま、`IsDirectory` は CS-002 に従い attributes から導出。canonical constructor は 7 引数で CS-013 の
「4 引数」を超えるが、CS-013 は method の複雑さ上限で analyzer 強制は未実装、record の canonical
constructor には `WslFileSystemEntry`（5 引数）の前例がある。規則は変えず、機械強制と record 例外の
明文化を follow-up Issue として提案する。

## #99 category bookmarks

### 再開設計

branch は main から 8 commits 後ろ、111 files、両側で変わる file が 16 件。branch 更新は逐次 rebase
ではなく `git merge origin/main` の 1 回で解消した。force push 禁止の規約に沿い、pre-commit hook が
旧 SDK ピン（10.0.400）で止まる問題も merge commit なら回避できる。ADR-0041 は ADR-0044〜0048 より
前に書かれているため、統合規則（modal の優先順位 palette → Settings → Bookmarks → address → idle、
直接 slot 解決の idle dispatch への移動、`PaneLaunching` の freeze、履歴の追記、`CommandCatalog`
非包含、鍵盤 context 不変、isolating host の結果だけを証拠にすること、SendKeys 系 smoke の撤回）を
設計側で書き、stash と patch で渡した。

### 運転結果

`ac3599a`（main merge、12 file 競合）→ `98a9e0e`（ADR-0041 統合節）→ `7eb0551`（統合契約 8 件）→
`de20f14`（Application 変異対策）→ `3f5da10`（再入判定の位置修正）。`3f5da10` で一度 Ready・全チェック
緑に到達したが、直後に main が #122 を取り込み BEHIND になったため、DEVELOPMENT_WORKFLOW どおり
Draft に戻して `c0c67ea` で証拠を取り直した。force push は使っていない。deep は 2 回とも一発 success。

競合解消の要点: `CommanderSession.HandleAsync` を統合節の一本の順序に整理し、直接 slot の解決を
`DispatchIdleIntentAsync` の内側へ移動、`BookmarkInteractionIsFrozen` は main 由来の
`AnyPaneExternalWorkIsRunning`（`PaneLaunching` を含む）へ寄せた。`KeyHintPresenter` は main の
`CommandLabelCatalog` 方式で `OpenCommandPalette` と `OpenBookmarks` を並置（11 件）。
`PaneListingPresenter` は branch が抽出した `PaneActivityStatusPresenter` に main の launch 系写像を
集約。`CommandCatalog` は 15 件のまま。KeyboardIntentMapper の宣言数は FileList 39 /
NavigationSurface 20 で、`Ctrl+B` と `Ctrl+1..9` は他 context に未宣言。

Application は isolating host で初回 90.38% と閾値未達で、テスト追加だけで 95.87% へ。殺したのは
bookmark 系 record 群の null guard（`BookmarkCatalog.cs` `BookmarkEditorSession.cs`
`BookmarkEditorAction.cs` 等、`NullGuardTests` に 4 メソッド）、2 件目カテゴリの rename / delete が
別カテゴリを付け替えない契約（`BookmarkCatalog.cs:156,165,181,190,269`）、`BookmarkKey` の hash
依存、`BookmarkEditorSession` の宣言済み空文字。等価として残した主なものは `ConfigureAwait` 18 件、
多重防御の null guard、`BookmarkCatalog.cs:315,360`、例外 message、`CommanderSession.cs:167,294,320,322`
で、理由は PR #102 本文と実装リナの報告に記録した。CodeQL alert #101 は再解析で fixed、open alert 0。

設計側の事後判断: 再入判定を idle dispatch の slot 解決直後へ移して `TryBeginBookmarkNavigation` の
一つの gate に戻した変更（`3f5da10`）は、単一 mechanism の原則に沿うので事後承認した。manager
navigation を `PaneLaunching` で凍結していない点は、manager が開いている間は launch が始まらず、
launch 中は manager が開かないので到達不能であり、ADR-0041 の proof 文言をそのように訂正した。
`KEYBOARD_MODEL.md` に `Ctrl+B` と `Ctrl+1`〜`Ctrl+9` の行を追加した。実装リナの commit は harness
の指示どおり `Co-Authored-By: Claude Opus 5 (1M context)` を付けており、作者が Opus である事実に
沿うのでそのままにする。

## 途中で起きたこと

- #99 の実装リナが Opus の session limit（00:50 リセット）で PR 本文更新の直後に停止した。全チェック
  緑・本文更新済みだったので、設計側で merge・main 同期・main deep 起動を行い、最終報告だけを
  同じセッションから受け取った。
- `-99` worktree は追跡ファイルが全 LF で checkout されていて `dotnet format` が落ちる状態だった。
  index の内容は変えずに renormalize して解決した。
- `eng/verify-coverage.ps1` は `--no-build` のため、古い Release build を測って偽の PASS を出しうる。
  今回 merge 直後の「Application 100.00%」が偽だった。canonical `check.ps1` は build するので影響
  しないが、単独実行では Release を明示 build してから測る。
- #101 の worktree・ローカル branch の削除は権限で止まり、`-99` の分と合わせて hide に委ねる。

## やらなかったこと

#93 harness の実装、#94 Windows UI matrix、#100 window shortcuts。`CommanderWindow.RenderSession`
の `_defaultFileListFocusSuppressedState` リセット条件が Settings のみで Bookmarks を含まない点
（modal 閉止後の既定フォーカス復帰）は production の小修正になるため別 Issue にする。
`CommanderSession` が 440 行・25 member になった分割は follow-up。
