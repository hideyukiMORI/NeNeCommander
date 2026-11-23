## 言語実機所見と追加改訂 — 2026-11-23 17:45 JST

hideは言語の保存・設定再表示・再起動反映、文字欠け/翻訳、他設定保持を確認し、
英語表示中のパレットで日本語漢字入力もできたと報告。言語ごとの実施順は未列挙。
即時言語切替を明示要求し、既存#525/#526の幅/対象表示整理を再指定。
ウィンドウ位置・サイズの保存と画面外補正（Nib参照）も追加依頼。

現在は `D:/NeNeCommander/worktrees/issue-526` / `refactor/526-palette-context`。
使用中のissue527/設定（17:21観測PID38108）を保持し、ここでbuild/終了しない。
#525を取り込み、#526の共通対象と例外説明を実装。6件の旧実装失敗から、選択34件成功、
App/test build0警告0error、実3言語/125%の36配置成功まで記録した。
[品質記録](../quality/2026-11-23-palette-context.md)と同専用evidenceが正本。
公開/統合はまだ行わず、変更した画面の実機受理は別。次は即時言語切替、
位置サイズ保存・初期/復元時の画面内補正。Nibの読取main64d612e/int50では
中央配置だけを確認し、保存実装は未確認。存在すると決めつけて複製しない。
#548の既存Locations幅候補、#523/#453/#491、AppSDK517は別の保持対象。
未完worktree/output/fixtureを保持し、新しい後片付け拒否はない。

## テーマ実機受理の引き継ぎ — 2026-11-23 15:36 JST

hideが通常Appの起動と観点一覧を依頼。検証済み製品6182b0dを15:19:06に起動し、10観点提示後に
「うん、いいと思う。」と返答した。現候補のテーマ画面・操作は受理済みとして扱い、同じ画面の
受理を再び要求しない。ただし個別条件の報告や計時値ではなく、#523/復帰/resize性能・言語再起動・
各IME/speech/DPI/contrast条件の包括的成功を追加しない。即時言語切替は未実装である。

稼働Appは15:36確認でPID37804、実行元は
`D:/NeNeCommander/worktrees/issue-527/src/NeNeCommander.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe`。
プロセス終了、再起動、同じ実行元のbuild/削除、現在設定の復元/上書きを勝手に行わない。
起動前settings backupは `D:/NeNeCommander/evidence/theme-picker-20261123/manual-launch-20261123-1518/`
内のsettings-before.json。古い写しよりhideの現在設定を優先する。launch.json、startup-check.json、
native-acceptance.jsonに候補/設定hash/所感/限界を保存した。青枠CUA禁止は継続する。

公開は最後に確認したPR565/Draft/6182b0dのまま。今回の受理文書はローカルに保存するだけで、
先のexact payload承認を新たな送信へ流用しない。main統合前には依存PR559等の必要証拠を確認する。
同じテーマUIの受理待ちを理由に止め直さない。未完かつ稼働参照のあるworktree/outputは保持する。

## 公開完了の引き継ぎ — 2026-11-23 12:27 JST

hideは12:25に準備済みpayloadを「いいよ」と承認した。4commit/43変更ファイルの6182b0dと
4,647-byte本文/タイトルを既存Draft PR565へ送信済み。読戻しはOPEN/Draft/head6182b0d、
mainbaseb82aa1e、全252変更ファイル、本文/タイトル一致。下の「送信承認待ち」は履歴である。
連携APIの本文編集403は既存ghによる同一payload更新で解決。新しいauto-review拒否はない。

正本は `D:/NeNeCommander/evidence/theme-picker-20261123/` のpublication-payload.json、
publication-approval.json、pr-readback-approved.json、local-completion.json。今回の公開記録は
ローカル保存のみで、次回はgit log/statusと公開6182b0dを照合する。記録commitを新しい実装や
公開承認と取り違えない。native/画面受理、復帰/resize計測、即時言語設計は未完のまま。
main/publicbeta.3・通常App/設定は維持。未完worktree/fixture/outputを保持し、cleanup待ちはない。

## テーマ選択改訂の引き継ぎ — 2026-11-23 12:20 JST

hideは10:26に再開、10:31頃にNib型のEnter/click採用・閉じる操作を選択した。
11:09の一時停止後、12:06に「再開して」。以下の古い停止・未採用案は現在の指示ではない。

作業は `D:/NeNeCommander/worktrees/issue-527` / `feat/527-theme-catalog`。
改訂開始head15f5244、最終local commitはgit logと
`D:/NeNeCommander/evidence/theme-picker-20261123/local-completion.json`を照合する。
公開PR565は12:18読戻しでOPEN/Draft/head35776de、base main/b82aa1e。
旧公開承認を新しい差分・本文へ流用しない。送信可否・対象は同evidenceの
`publication-payload.json`と以後のhide返答で確認する。新しい送信試行/拒否は現時点でない。

### 実装・証拠

設定の現在配色ボタン/Ctrl+P `:colorscheme`から同じSettingsSession所有のpickerへ入る。
仮表示と採用を分離し、exact-state提出、既存queue、親surface復帰、同色/取消のno-write、
遅延完了時のquery/preview維持を実装。ListViewだけがscrollし、search/hint/Cancelは固定。
既存palette key mapを再利用し、held Escapeのowner越えも修正した。
[設計](../design/2026-11-23-theme-picker-revision.md)と
[品質記録](../quality/2026-11-23-theme-picker-revision.md)が現在の正本。

Application46、Presentation61、重複keyboard22、hint1成功。最終App/対象test build0警告0error、
規約112、型compiler proof7、書式成功。CS-013は既存5と完全一致。12:19独立read-only reviewは
修正必須指摘なし。成功済み範囲は同じ入力なら再利用し、工程だけを理由に再実行しない。
同91assertion×3localeの非表示controlsが実125%・3サイズで成功。536文言/locale、132 XBF、
4 DLLと169 App sourceの診断差分を照合。最終1,227入力は`final-input-hashes.json`。
同一locale/重複filterを別test総数として加算しない。

直接virtualized末尾APIjumpには英語で4DIP欠けが残る。通常MoveNextで全候補を移動すると
最終行が完全に入ることは別途成功した。前者を「修正済み」にしない。
全失敗・fixture実装・起動PID・専用設定は同evidenceへ保全。fixtureは
`D:/NeNeCommander/outputs/theme-picker-20261123/control-fixture`、全診断processは終了済み。

### 次の作業と制約

1. local commit/hookと入力不変を確認し、具体的payloadへの公開承認があればDraft PR565を更新。
   新本文は旧radio画面説明を最終picker説明へ置換する。native未受理のままmergeしない。
2. hideの実機比較結果が届いたら事実として記録する。必要な入力/見た目/IME/speechの受理は
   hideの操作か隔離環境で扱う。青枠computer-useは起動/確認も禁止、共有入力/focusも禁止。
   通常App/設定を終了・上書き・古いbackupへ復元しない。
3. 復帰後Ctrl+,の遅れとresize描画消失を別々に測り、原因に対応した変更だけを行う。
   今回hidden fixtureの計時は可視frame/activation/Notepad比較ではない。
4. 即時言語切替はADR-0086の次回起動契約を見直す設計作業。今回の実装完了に含めない。
   Draft依存PR559、#523/#453/#491、AppSDK PR517、他候補は下の保持理由のまま。

main/publicbeta.3は変更なし。#527とfixture/outputは未受理・未統合のため保持し、
新しいcleanup拒否やhide実行待ちはない。

## 最終引き継ぎ・停止中 — 2026-11-23 02:37 JST

hideの「今日はこの辺で区切ろう。日報と引き継ぎ書を書いて」に従い終了。
明示的な再開があるまで実装・計測・起動・公開送信・CIを続けない。
本節が最新で、下の未起動/未公開/承認待ち記述は各時点の履歴。
[日報の最終節](../reports/2026-11-23-milestone-checkpoint-daily-report.md)に全10観点を記録した。

### 最優先の訂正

**参照するNibは Ctrl+P → `:colorscheme` の選択経路。**
サナが直前に説明した右側設定パネルではない。Folioのテーマ画面も比較するが、
Folio寄りの別ページ案、移動プレビュー/Enter採用/取消復帰は相談案のままで未承認。
hideは配色一覧の機能成立を認めても、現在のデザイン/操作モデルを受理していない。
設定drawerの調査を、指定されたNib経路の調査完了として引き継がない。

### 再開時の順番

1. 今のworktree/head/diff、実行中Appと最新のhide指示を確認する。古い継続許可で自動再開しない。
2. Nibの指定経路をコードから追い、Ctrl+Pからの入口、検索文字列、候補移動、
   色見本/実画面プレビュー、Enter/クリックの採用、Escape/戻るでの取消と
   採用済み値の復帰、保存時期、検索中IME、スクロールとfocusを確認する。
   Folioの同等経路との差を具体化し、Commanderの操作案として相談する。
3. 二重スクロールを解消し、見出し/検索/下部操作の固定、文字とスクロールバーの非重複、
   明確な候補/採用中表示を設計する。右パネル/別ページ等の配置は未決定。
   ADR-0087/設計handoffの旧画面契約を、採用された新案と合わせてから実装する。
4. 復帰後Ctrl+,の数秒待ちとリサイズ時の描画消失を別々に測る。
   入力到達→session処理→コントロール構築/配置→表示、設定closed/open、
   初回/再openを区別。hideの0.5～0.8秒は体感報告で、計測値や確定原因にしない。
   非仮想化129行の再生成、キーごとの木探索、余分な全surface描画/ScrollIntoViewは
   調査候補。原因が支持された箇所だけ変更し、関連範囲だけ検証する。
5. 即時言語切替を設計し、次回起動限定のADR-0086を見直す。
   一つの既存翻訳資源/設定保存の正本を保ち、静的/動的文言・tooltip・UIAを更新する。
   ペイン位置、選択、スクロール、検索/IME、進行中操作を壊さない境界を決める。
   現行PrimaryLanguageOverrideを後から変更するだけで成功すると仮定しない。
6. 改訂候補の実機/画面受理を取る。今回①は良好、⑤は入力可能、⑧は暫定OK。
   ③④⑥⑦は機能成立と体験不一致、②⑨は具体的不具合、⑩は設計変更相談。
   #523全体の解決、#527/#528完成、言語再起動試験成功を宣言しない。

### 現在の保存先と証拠

- 作業: `D:/NeNeCommander/worktrees/issue-527`、
  `feat/527-theme-catalog`。今回更新前のlocal HEADは9768c8b、公開headは35776de。
  最終文書commitはgit logと下記closing-verification.jsonを参照。製品入力は1700b19から不変。
- 公開: PR565はDraft/main、231files。承認は元の35776deと準備済み本文に限定し実行済み。
  終了文書を自動でpushしない。今回、新しい公開承認待ちや送信試行はない。
- 実機: hideの起動依頼で02:10頃に無改変候補を起動、当時PID39872。
  実行元は `src/NeNeCommander.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/NeNeCommander.App.exe`。
  稼働有無は通常のプロセス照会で再確認し、古いPIDだけでkill/再起動しない。
- 設定: 起動前の518byteバックアップは
  `D:/NeNeCommander/evidence/theme-catalog-20261122/manual-launch-20261123-0209/settings-before.json`。
  hideの試用後の設定をこの古い写しで復元しない。現在設定はユーザーの成果として保つ。
- 実機画像と所見:
  `D:/NeNeCommander/evidence/settings-feedback-20261123/manual-feedback-and-source-review.json`、
  `hide-settings-20261123-022521.png`（元画像とSHA256一致）。
  `closing-record.json`にはNib参照先訂正と今回の停止指示を追加。
- 調査はローカルNib6fb3686/Foliof7d673f。今回読んだNib設定drawerは参照指定の対象外。
  Commanderのsource読み取りとMicrosoft一次文書確認だけで、性能計測・実機比較なし。

### 操作制約・検証・後片付け

**青い枠が出るcomputer-useは、起動・状態確認も含め禁止。**
起動依頼をこの禁止の解除と解釈しない。今回誤って使用し、hideから訂正された後は
画面取得/キー/クリックを停止した。通常コマンドで起動/プロセス照会し、共有画面への
入力・フォーカス取得も行わない。必要なら隔離した検証環境を具体化する。
現在のApp/設定を無断終了・復元・上書きしない。

文書だけの変更なのでADR-0061によりdiff/linkと通常commit hookのみ。
`git diff --check`、`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`
の実行結果・最終commitは `closing-verification.json`。
既存の成功済みbuild/test/資源照合を文書保存のため再実行しない。

#527/#528/#547と他の未完候補、fixture/outputは保持。稼働参照があるworktreeは削除不可。
新たな後片付け拒否はなく、hideへの削除依頼はない。mainはb82aa1eを維持し、
既存の公開beta.3を今回の試用候補で置換しない。旧#453/#491、AppSDK PR517、
#525の簡体字狭幅欠け、#544のmouse/deep/native、#551/#526調査は後続節のまま別件。

## 承認済みDraft公開・停止継続 — 2026-11-23 01:58 JST

hideの「承認」を受け、先に提示した35776deの231ファイルと同一本文だけを送信し、
[Draft PR565](https://github.com/hideyukiMORI/NeNeCommander/pull/565)を作成した。
OPEN/Draft、base main、head35776de、231変更ファイル、本文・title一致、checksなしを読戻し確認。
01:24の自動承認拒否は、この限定payloadについて解消済み。後から加えた停止記録84e062cと
この公開記録はローカルに保持し、承認された送信範囲へ黙って含めていない。

停止指示は継続する。実機/feature受理、main統合、release、追加CI、新規実装は行っていない。
言語依存PR559と各native確認は下記のまま未完。未完worktree/outputは保持する。
正本証拠: publication-approved.json、push-approved.log、pr-readback-approved.json、
publication-hold.json（解消追記）。いずれもD:/NeNeCommander/evidence/theme-catalog-20261122/。

## hideの終了指示による停止 — 2026-11-23 01:48 JST

hideが「そろそろ上がろうと思う。区切りのいいところで止めて。実機確認が必要なものはある？」
と指示したため、この追記と通常のローカル保存を最後に停止する。01:01の再開や以前の継続指示を、
この停止後の自動再開の許可として扱わない。新しい実装・起動・CI・公開送信は行わない。

再開後の製品入力は1700b19から不変、検証記録は35776deに保存済み。
終了前の読取では専用fixture/候補プロセス0、main b82aa1eはclean、使い捨てcheckoutは不存在。
通常のユーザーAppと設定は変更していない。今回の停止保存commitは
D:/NeNeCommander/evidence/theme-catalog-20261122/final-stop-commit.jsonとgit logで確認する。

公開#527は未送信・PR未作成。01:24のgit pushは、自動承認審査が「今回の231ファイル・約2MBの
公開payload/宛先に対するhideの明示承認が未確認」を理由に、プロセス作成前に拒否した。
D:/NeNeCommander/evidence/theme-catalog-20261122/publication-hold.jsonが正本。
過去のSARIF/別PR/Claude brief承認を流用せず、別手段でも再試行していない。
既存の承認質問は35776deと保存済み本文を対象とし、今回追加する停止文書まで承認されたことには
しない。再開時は送信対象head/files/bodyを照合してから扱う。今夜は新たな確認を求めない。

### 次回の実機確認

以下は現物のウィンドウ・入力・OS環境を必要とする未完事項。非表示fixture成功を実機受理に
置き換えない。必要なものはサナが準備し、hideの通常設定を保全した確認用環境で実施する。
今夜は確認用Appを起動しない。

| 優先・対象 | 実機で確認する内容 | 現在の限界 |
|---|---|---|
| 最優先: #523 再activation | 別アプリへ切替えて戻り、左右一覧でj/k・上下・Tab・Enter等が受理されるか。症状が出た操作順とfocus位置を記録。 | 未再現で、修正済みとは言えない。これは修正受理ではなく再現調査。 |
| #527 テーマ・設定検索 | 検索入力とIME、0件、選択→保存、検索解除後のcheck、Tab/Shift+Tab/j/k/Space/Enter/Escape、Closeから一覧へのfocus復帰。代表明暗色で選択/focus/進捗を目視。 |27条件×3locale・129資源・125%簡体字の小型geometryは成功。実キー/focus/IME/speechと見た目は未受理。 |
| #528 表示言語 | System/日本語/English/简体中文を保存し、無改変candidateを終了・再起動して次回起動から反映。地域書式とIME、framework所有文字列も確認。 | コピーfixtureの起動10状態は成功。通常のstartup/restartとOS所有文字列は未証明。 |
| #525/#548 小型palette/Locations | 狭い窓でも末尾候補・詳細・操作hintへ到達できること、tooltip、キー/IMEとfocus復帰。 | #525の合成中国語320×300では約1.6DIP欠けが既知。実際の中国語統合候補で修正・再測定してから受理を求める。 |
| #547 反対ペインで開く | 左右それぞれからCtrl+Eとheader操作。元のactive/focusを保ち、反対側だけ同じfolderへ移動すること。 | Draft PR558、native/画面受理と統合後の言語資源確認は未完。 |
| #544 空テキスト作成 | 専用の空の確認用folderでCtrl+F7、名前入力/IME/取消/既存名の拒否、作成後focus。マウスNew導線は#530/#531統合後。 | local/WSL backend証拠あり。mouse導線・完成candidateのdeep/native受理は未完。 |
| 共通の環境確認 | 代表100–300%倍率、Windows contrast mode、Narrator等で設定とpaletteの名前/選択/件数/Close到達を確認。 | テーマ名high-contrastはWindows contrast modeの証拠ではない。共有desktop設定を無断変更しない。 |

次回は#523の短い再現確認、続いて#527/#528の設定操作と再起動を先にする。
旧#453/#491のF6応答・体感比較は別の未完調査で、このテーマ追加の回帰試験として実行しない。
#551モニター移動と#526対象表示は調査段階で、完成版の実機受理対象ではない。

停止までに追加したread-only照合では、fixture全132XBFが最終製品/PRIと同一で、3専用設定も
意図した配色以外は不変だった。fixture-product-xbf-and-settings-parity.jsonに保存。
#526は23commandの対象契約を読み、global/transfer/反対側切替/Bookmarkを一律にactive対象と
説明しない必要を確認した。調査記録はD:/NeNeCommander/evidence/palette-context-20261123/
contract-investigation.json。コード変更・追加branch・テスト・外部相談はまだない。

### 保持と後片付け

#527のfeature worktreeと専用fixture/outputは未公開・native未受理のため保持し、他の未完候補も
下表の既存保持理由に従う。使い捨てcheckoutは整理済み。新しい削除拒否やhide実行待ちはない。
公開承認待ちと後片付け待ちは別であり、今回は後片付けの依頼は不要。

## 再開後の検証追加 — 2026-11-23 01:21 JST

hideの01:01再開指示に従って継続。製品1,227入力はcheckpoint1700b19から不変。
非表示の実コントロールfixtureで27条件×3言語が成功し、保存は各2回の明示選択だけ。
検索/再表示/遅延保存後の最新チェック状態、query/行instance、不要な保存の抑止を確認。
簡体字・125%の320×300/640×360 DIPsでは19条件が成功し、Closeと一覧先頭/末尾の
APIスクロール到達を測定。全129実行時辞書の18色/18brush値とbrush同一性も一致した。
最終Appの527key×3localeと132XBF、実製品DLLの型制約7件、clean checkoutの131生成物と
71,394byte通知も一致。最終複雑度は従来5指摘と完全一致。失敗したfixture書式/PRI検査も
[品質記録](../quality/2026-11-22-theme-catalog-implementation.md)と恒久evidenceに保存。
追加した使い捨てcheckoutは01:14:55に整理済み。現在のfeature/fixtureは未受理のため保持。

共有画面の入力/フォーカス/表示は使っていない。実キー・IME・speech・Windows contrast
mode・他倍率・無改変起動/再起動・hide画面受理は残る。focused証拠は実際の影響を覆い、
保存やDraft公開のためだけのfull/deep再実行は不要。公開payloadを具体化する段階であり、
現時点の#527は未公開・未統合。下の停止記述は00:52時点の履歴として読む。

## 再開指示 — 2026-11-23 01:01 JST

hideが「まだ大丈夫なのでもう少し続けよう」と再開を指示した。以下の終了記録は
ローカルcheckpoint時点の履歴として保持し、停止待ちを現在の指示として扱わない。
checkpoint commit: 1700b197bb7b9f8dddbab85d04ef0d4c9fa9d109。次は非表示fixtureで検索/最新選択/不要な保存の抑止を確認する。
共有desktop入力・ユーザーApp/設定を変更しない制約は継続。新しい成功証拠は別節へ追記する。

# 引き継ぎ — 2026-11-23 終了時点

hideは00:52に「区切りのいいところでとめて日報と引き継ぎ書」を依頼した。
今回は停止。以前の「止めるまで継続」を、この時点以降の自動再開の許可として扱わない。
[日報](../reports/2026-11-23-milestone-checkpoint-daily-report.md)が全体、
[テーマ品質記録](../quality/2026-11-22-theme-catalog-implementation.md)が最新実装の詳細。

## 再開位置

最優先の未完了候補は#527。作業場所 `D:/NeNeCommander/worktrees/issue-527`、
branch `feat/527-theme-catalog`。開始head440d7d2から129テーマと検索画面を実装し、
今回のローカルcheckpointに保存する。実際の保存commitは `git log -1` と
`D:/NeNeCommander/evidence/theme-catalog-20261122/checkpoint-commit.json` で照合する。
mainはb82aa1e。Draft PR559/91e06f5を明示的に含む依存候補であり、言語機能の
native受理やmain統合を済ませたという意味ではない。

初めにAGENTS、PROJECT_STATE、受理済みADR-0087、
[設計handoff](../design/2026-11-23-theme-catalog-handoff.md)、品質記録の最新節を読む。
元125配色はNib5d76cc7、Folio1411261比較。129表示をhideが承認済み。
四つの旧配色はbyte不変、重なる4配色は見た目の変更あり。新依存/runtimeテーマ読込はない。

1. 現在のtree/head/差分を確認し、stop-input-hashes.jsonと比較する。未変更の成功証拠を再利用する。
2. 次は未確認の実コントロール境界。新しくrealize/filterしたradioが最新適用を示すこと、
   renderから保存しないこと、保存中もquery/row/focusが維持されることを隔離fixtureで検証する。
3. 小型window（320×300、640×360 DIPs）・代表倍率・nested scroll・Close到達、
   native Tab/Shift+Tab/j/k/Space/Enter/Escape/IME、NarratorとWindows contrast modeを分けて確認。
   共有desktopのキーボード/マウス/フォーカスを奪う操作は禁止のまま。
   非表示fixtureは実操作/可視画面受理の代用にしない。今回テーマcandidateは一度も起動していない。
4. 最終App PRI/XBFの全テーマ・3locale527key、clean-checkout再生成とnotice LF/bytes、
   実製品DLLに対する閉じた型compiler proof、最後のhelper/test後の対象複雑度を確認する。
   full canonical gateが必要かは最終変更の影響で選び、必要な証拠を確定する。
5. 現在の技術review指摘は解消済み。新しい具体的差分だけ再レビュー対象にする。
   公開先/payloadが具体化してから正規の送信・Draft PRへ進む。今回#527のpushは未試行。
   以前のSARIFや別PR/Claude briefの限定承認を、新payloadの包括許可へ拡張しない。
6. native/feature受理、通常PR/squash/main統合、成果物保全が済んでから専用worktree/outputを整理。
   現時点では未完了なので削除しない。

`eng/theme-catalog` の固定manifest/templatesを編集し、単一の `eng/import-theme-catalog.ps1`
で出力する。生成対象の手修正や別のカタログ/URI作成経路を作らない。通知は固定71,394bytes、
SHA25631629c34013fc98b7778528cf2bbb9c1d32ecd5967b29321c82f2b723450851a、eol=lf。

## 使える証拠

Application32 distinct / Infrastructure34 / Presentation50 / Architecture3。
最終App build0warning/0error、規約112、ARC正常1+異常16、import131出力一致、
2,250色/全調整ステップ一致、notice55ZIPファイル一致・破損拒否。
複雑度は測定時に既存5件と完全一致。最後の小helper/test後の測定は未実施。
UIA通知修正は00:55:02の独立読取再レビューで受理、最終App buildに含む。
実UIA speechやnative焦点の成功は主張していない。

正本証拠: `D:/NeNeCommander/evidence/theme-catalog-20261122/`。
`app-review-fix-build.log`、`conformance-stop.log`、`format-stop-verify.log`、
`implementation-review-stop.json`、`stop-github-readback.json` と各対象test logを読む。
失敗ログも保全し、失敗buildの古いDLLを成功証拠に使わない。

## 他の候補と保持理由

| 絶対path / branch head | 状態・保持理由 |
|---|---|
| `D:/NeNeCommander/worktrees/issue-525` / f1762f8 | パレット幅。125%のAPIスクロール等まで。合成中国語320×300の約1.6DIP欠けとnative/画面受理が残る。 |
| `D:/NeNeCommander/worktrees/issue-548` /5a26d32 | Locations幅。限定fixture成功、native/他倍率/画面受理と公開が残る。 |
| `D:/NeNeCommander/worktrees/issue-551` /eabae9e | モニター移動調査。36+24条件と倍率遅延20条件の反例。契約決定前。旧文書の空きADR番号は使わず最新indexを読む。 |
| `D:/NeNeCommander/worktrees/issue-544` /b593ef4 | 空テキスト作成候補。liveWSL4、local/queue等の証拠あり。mouse New#530/#531、完成candidate deep/native、公開・統合が残る。 |
| `D:/NeNeCommander/worktrees/issue-547` /65a6fb2 | PR558 OPEN/Draft/mainbase/49files。Ctrl+E+header操作、native受理未完。 |
| `D:/NeNeCommander/worktrees/issue-528` /91e06f5 | PR559 OPEN/Draft/mainbase/62files。language所有権修正まで送信済み。無改変起動/再起動/native/IME/speech受理未完。 |
| `D:/NeNeCommander/worktrees/issue-527` /今回checkpoint | 現在の未公開テーマ候補と唯一の未統合実装。 |
| `D:/NeNeCommander/worktrees/issue-495` /672f9b6 | ユーザー利用Appの実行元として保持。過去backupで現在設定を上書きしない。 |
| `D:/NeNeCommander/worktrees/issue-491` /d36ffac | native応答比較が未完。不成立試験の記録を保持。 |
| `D:/NeNeCommander/worktrees/issue-514` /b27a43a と `D:/NeNeCommander/worktrees/pr-498-review` /3da9729 | AppSDK PR517と元PRの未完調査。今回リリース/テーマへ混入させない。 |

各候補の `D:/NeNeCommander/outputs/<task>-20261122/`（theme-catalog、language-selection、
text-file-creation、open-other-pane、compact-modal-design、compact-locations、monitor-transfer）、
旧bookmark/continuation/AppSDK出力を未完了のため保持する。恒久evidenceとreleasesは削除対象外。
一時gate-proof rootはlauncherが検証して削除済み。新たな削除拒否やhide実行待ちはない。
旧#454/#459の4対象は既に不存在の記録があり、拒否操作を別方式で再試行しない。

#524/#550/#542とbeta.3の統合・cleanupは完了記録を優先し、古いhandoffの承認待ち/実行中を
現在状態として扱わない。#542の922生成物保全とworktree不存在はcompletion.jsonで確定。
#523は未再現、#526とその他追加要望は未完。Store向け基本操作はIssue564が追跡元で、
560–563の追加と545/536/530/531/523の既存課題を接続している。正式release cutoffは未定。

## 承認・停止の扱い

SARIF、#542、#547 brief/PR、#528 PR、#527 brief、基本TODO5件の具体的な過去送信承認は
それぞれ履行済み。現在、返答待ちの承認質問はない。別payloadへ黙って流用しない。
#527の公開承認を今は求めず、hideの再開後に実装検証とreviewableな内容を揃える。
単体実装を既定、必要な独立reviewだけ読取専用。全ユーザー向け文末に実時刻JSTを付ける。
これ以降はhideの再開指示を待つ。
