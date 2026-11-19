# 2026-11-19 Bookmark操作性改善・中間記録

Status: ongoing; Issue #457 integrated into feature PR402, hide acceptance pending

hideの「止めるまで進める」という依頼で再開した。最優先のBookmarkを、既存PR402の
キーボード操作から改善している。通常mainと公開Previewには反映していない。

## 変更と検証

- キー操作・画面構成・アクセシビリティの3専門家が、全キーモデル、実装、利用可能な画像を
  読み取り専用で検討し、相反する案を調整した。実際のNeNe Commanderリナにも相談した。
  この環境の`/design`はアクセス管理のコマンドだったため、同じClaude環境のデザイン批評・
  アクセシビリティ・引き継ぎスキルで続けた。クラウド権限は変更していない。
- 保存は編集画面限定のCtrl+Enter。通常Enterはコントロールへ渡す。検索中は
  Enter/Down/Escapeが「一覧へ」であることを案内し、次のEnterで開く契約を維持する。
- 一覧に入ると可視選択または先頭を選び、保存後は検索を解除して保存した項目を選ぶ。
  取消では元の検索・カテゴリを維持する。閲覧による設定書込みは発生しない。
- 全Bookmark文字入力のIMEと、開いたカテゴリ／枠の選択肢を保護する。
  独立レビューで見つかった通常Enter長押しの回帰は、失敗する試験を保存して修正した。
- アクティブ領域を既存トークンで区別し、入力ラベルと割当済みの枠名を示す。
  実画像レビューの指摘により、設定の保存状態には「設定:」を添えた。
- 実起動で保存ボタンのリソース取得エラーを検出して修正。さらに、空一覧と画面切替の
  フォーカス不良を再現した。既存の遅延処理を拡張し、後続Tab／pointerで予約を取消し、
  初期focus予約中のEnterを消費する。IME／選択肢のnative入力は維持する。

Applicationの95件と追加1件、Presentationの135件に対する変更後の案内8件・focus8件、
App Release build、view所有権20件、変更ファイルの書式確認が成功した。
ビルド失敗、停滞したMSBuildの停止、restore不足、CRLF修正、静的検査の追加指摘と解消も
[品質記録](../quality/2026-11-19-bookmark-keyboard-usability.md)へ残した。
無関係な全件回帰、coverage、mutation、hosted CIは実行していない。

## 再開した実機確認

11:07〜11:20にcandidate95549f5を専用プロセスで確認した。空一覧からの登録、取消後の再登録、
Ctrl+Enterと保存ボタンのEnter、検索から先頭選択・編集、空の名前の検証拒否、カテゴリ/枠の
popup、3入力欄の実IME、カテゴリフォームと確認画面のTab循環、両削除のCancel初期focusを観測した。
削除は実行していない。移動失敗では元フォルダーを保持し、専用移動先を作った後の再試行で
実際の移動を確認した。ただしReturnとpointer試行の間で取得が遅れ、キーだけの成功は分離できない。

操作ツールの窓検出・状態取得の遅れとtimeoutを記録し、後続観測で否定された起動/再試行の
失敗推測は撤回した。空ペイン移動後の復帰には既にmainへ入った#454の正典focus修正を取り込む。
保存後の案内が通常案内へ戻る点は、同値のSelect/Search/Filterが通知を捨てることを独立reviewで確認した。
既存state ownerで同値入力の選択と通知を維持し、カテゴリ解決と完全なstale検査を先行させた。
新規12件は修正前6失敗/6成功、修正後は関連28件すべて成功。追加reviewで必須修正はなかった。
元のnativeイベントの特定はしていない。
修正後の専用窓ではCtrl+Enter保存の約69秒後も通知と選択が残ることをAX/画像で確認した。
App buildは警告/エラー0、Inspect既存7件で追加なし、変更C#書式確認も成功。
専用1登録のみの差分を確認して終了後に元設定へ復元し、hash一致。通常アプリは保持した。
詳細と未観測項目は[品質記録](../quality/2026-11-19-bookmark-keyboard-usability.md)。

試験アプリはAlt+F4で終了。設定の差分は専用登録2件/カテゴリ1件だけと確認し、元の218byteへ
復元してSHA-256一致。通常Cアプリは保持した。PR458は機能ブランチへ統合済み、PR402はDraftのまま、main/Preview外。
hideの別途画面受理を代替しない。#459のPreview beta.2公開用deep reviewは別の受理済みmainで実行中。

未完了のD:/NeNeCommander/worktrees/issue-457とoutputs/bookmark-ux-20261119は保持する。
恒久証拠はD:/NeNeCommander/evidence/bookmark-ux-20261119。停止指示はなく、作業は継続中。

## main同期と空ペイン復帰

PR458を11:37に機能ブランチへsquash統合した（`9ba489a`）。続いてmain `c25dc30`を同期し、
文書・両言語リソース・分割済み試験fixtureの競合を解消した。新しい設定契約への移行は
既存のDisplayPreferencesを使い、独自のfocus経路を増やしていない。
結合後Application 206件、Presentation 74件、view所有権24件が全成功。Appを含むbuildは
警告/エラー0。競合解消C#8ファイルの書式確認は成功、Inspectは既存7件で追加なし。
独立したキーボードreviewでも結合による必須修正はなかった。

11:59〜12:02の専用窓で空一覧から登録し、Enterで空フォルダーへ移動した直後に、
マウスを使わずCtrl+Bで一覧を再表示し選択行を確認した。元の空ペイン復帰の未証明範囲を
この結合版では解消した。専用窓終了後、変更が専用1登録と想定のschema移行だけと照合し、
12:03に元の218byteへ復元してhash一致。通常Cアプリは保持した。
正確なコマンド、候補hash、観測の限界は品質記録に追記した。hideのPR402画面受理は別途必要。

PR402は`380739f`までpushし、説明を現在の実装・証拠・未観測範囲へ書き直した。Draftを維持。
続いて807×658ピクセルの専用窓で、空一覧と登録フォームの全入力・保存/取消・案内が収まり、
Tabで保存と取消へ到達しEnterで取消できることを確認した。寸法調整時の過縮小・無効なキー表記・
拒否された範囲外drag・反映の遅い取得も品質記録へ残した。サイズ調整後のfocusをpointerで確立したため、
窓のサイズ変更までキーだけで完結した証拠にはしない。保存はせず、設定は終了後も完全一致した。

## 12:33以降の追加確認と入力所有権

専用窓の失敗画面でEscapeによる一覧復帰、Enterによる再失敗、専用ディレクトリ作成後の
Enterだけの再試行成功を分離して確認した。819×658ピクセルで実データ行・ブックマーク削除確認・
カテゴリ入力・カテゴリ削除確認が収まり、Cancel初期focusからEnterで取消できた。削除はしない。
検索欄でも実IMEの候補表示、Enter確定、次の変換だけをEscapeで取消する動作を観測した。
12:40に専用窓を終了し、専用1登録/1カテゴリと想定schema移行だけの差分を確認して元設定へ復元した。

速い連続入力にはfocusの時点が不確かな試行が残った。独立したsource reviewで、実際の文字入力や
IME開始後も古いfocus予約が残る経路を確認した。元のnative症状の原因確定とは分けている。
既存mapperがnativeへ渡すと判定した入力を、実focus中のBookmark入力欄だけで観測し、
既存の2つの予約を取り消す修正を行った。mapped操作・確認前Enterの保護とraw文字repeatの判断は維持する。

修正後App/test buildは警告/エラー0、関連Presentation27件とview所有権24件が全成功。
1,034入力のhashが検証前後で一致し、書式確認も成功。Inspectは既存7件のまま
（windowは305→311 logical lines）。実差分の独立レビューにも必須修正なし。
12:51〜12:54の専用窓で名前/パス/カテゴリ名の連続入力、検索入力の維持、検索IME、
検索Enter→一覧→Aを確認した。高速Escape/slash/X/YはSearchの`/xy`として保持されたが、
slash時点の一覧復帰完了や予約callbackの実行順は証明していない。
物理長押しは操作ツール非対応のため未確認。試験窓は終了し、保存はせず元設定は完全一致した。
正確なコマンド・全試行・証拠の限界は品質記録と`native-input/`に残した。

Preview beta.2は12:19に公開済みで、公開asset照合と記録はmain `5b54b0a`まで統合済み。
今回のBookmarkはPR402の別途画面受理前で、main/Previewには含めない。停止指示はなく作業を継続する。

PR464は12:58:05に機能ブランチへsquash統合（`2e644bf`）。検証済みcommitとの全tree一致を確認した。
通常hookは112規則の整合性とsecurity確認を通過した。main `5b54b0a`の追加同期は公開記録4文書だけで、
PROJECT_STATEの競合はBookmarkと公開履歴の双方を保持した。製品入力不変のため再build/test/CIは行わない。

## 14:11時点のmain同期

main `3865060`のフッター再bind削減と観測/完了文書をBookmarkへ取り込んだ。製品/試験はmainの
4ファイルそのまま、Bookmark固有の処理は不変。文書競合1か所は双方の履歴を残した。
App/Presentation試験buildは警告・エラー0、通常フッターとBookmark案内の境界20件成功。
今回の1,040 inputsはhash不変。既存native/IME/focusとmain側の選択検証を再利用し、
無関係な試験・full CI・native再起動は追加していない。詳細は品質記録と引き継ぎに記載した。
PR402のDraft/画面受理条件、通常アプリと公開beta.2は維持する。

## 14:23時点のアクセシビリティ情報確認

変更していないfeature `277a003`を専用窓で起動し、名前・パス・カテゴリ・ショートカット・
カテゴリ名のラベルを実際のUI Automationで確認した。別カテゴリの同名2登録は、アクセシブル名が
「同名確認 · 確認カテゴリA/B」と分かれ、詳細から完全パスを取得できた。Narratorの音声確認ではない。
入力文字列のescape修正、取得遅延、要素clickのgeometry不足とキーボードによる回復も品質記録へ残した。
削除・移動は実行せず、専用窓終了後に専用2カテゴリ/2登録と想定schema移行だけの差分を照合し、
14:23:44に元の218byteへ復元してhash一致。通常アプリ38360は保持。build/test/CIは追加していない。
PR402はDraft、hideの画面受理待ち。恒久証拠は既存evidenceの`accessibility-readback/`。

## ! hide実行待ち：後片付け

従来の拒否15対象は10:47の読み取りで全件不在を確認し、旧依頼は解消した。
今回の#454完了分2対象は統合/未保存/ignored/リンク/稼働参照を確認し、出力15ファイルと検査結果を
D:/NeNeCommander/evidence/footer-20261119へ保全してhash一致。しかし11:05の自動承認レビューが
削除を起動前に `blocked by policy` で拒否した。削除は実行されず、別手段で再試行していない。

- D:/NeNeCommander/worktrees/issue-454
- D:/NeNeCommander/outputs/footer-20261119
- D:/NeNeCommander/worktrees/issue-459
- D:/NeNeCommander/outputs/release-beta2-20261119

#459の後半2対象も、4出力ファイルの保全と展開版586ファイルの照合、取込/未保存/リンク/稼働参照を
確認した後、12:29の自動承認レビューが削除を起動前に`blocked by policy`で拒否した。
こちらも削除を実行せず再試行していない。公開candidateと恒久証拠は保持する。

hide実行用（対象外のbranch/commit/恒久証拠は保持）:

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/release-beta2-20261119/cleanup-for-hide.ps1
```
