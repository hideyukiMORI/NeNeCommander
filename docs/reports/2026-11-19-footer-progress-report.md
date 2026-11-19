# フッター改善の進捗 — 2026-11-19

hideの継続依頼に従い作業中。区切りでプレビューを公開する追加依頼も受けた。
この報告は停止や公開完了を意味しない。

## 完了した実装と確認

#454のフッターは、操作情報とキー案内それぞれを幅に応じて折り返す。
キー案内は設定で非表示にでき、F1と確認・名前入力のEnter/Escは残る。
実際のNeNe Commanderリナとの設計相談を[handoff](../design/2026-11-19-footer-key-hints.md)、
設定形式4と単一経路を[ADR-0082](../adr/0082-footer-key-hint-visibility.md)へ記録した。

設定画面を開くだけで保存してしまう遅いCheckedイベントと、読み込み前の初期focusを
実機で見つけて修正した。初期focusはLoaded後に一度だけ実行し、操作が始まったら取り消す。
閉じた後は既存の一覧focus経路へ戻し、空の一覧にも戻れる。

対象のbuild、設定・JSON・表示・compositionの試験が成功。
Application139+3、Presentation63、Infrastructure188（185の再利用と修正3）、
Architecture18。App buildは警告0・エラー0。record closureは失敗0。
最終の書式確認はexit0、Inspectは28→26件で追加0。
詳細な試行・失敗と修正・コマンドは[品質記録](../quality/2026-11-19-footer-key-hints.md)。

日本語Monokaiの954×658ピクセルの窓で、通常案内12組が2行に表示された。
Space/Enterによる切替、F1だけの状態、再起動での保持、左右と空ペインへのfocus復帰、
F7名前入力、Helpを閉じてからのF7、専用ファイルのコピー進捗と完了を確認した。
元・先のhashは一致。試験アプリを終了し、設定は元の218byteに復元してhash一致。
通常C側アプリとNibの画面は保持している。

操作直後のAX結果が実画面より遅れる場合と、古いFocused表示を確認した。
一度疑ったHelp復帰不具合は、クリックなしの成功を実画像で再確認し撤回した。
captionを使う一時診断の終了事象は保存したが、通常版の不具合とは断定しない。
明示的な100%/200%、英語、高コントラスト、Narrator、長い失敗文のnative確認は未実施。

## 次の区切り

#454のPR統合後、受理済みmainを基点に次のPreview版と配布物を準備する。
公開には新しい入力に適用できるdeep review・CodeQL・依存確認と配布物照合が必要。
deep reviewに含まれる全体gateを別途重複実行しない。
#94の一度の確認はhide本人報告で完了しているが、PR402の受理を兼ねない。

Bookmark #457はDraft PR458、candidate95549f5としてPR402側に保持。
Computer Useが復旧したため、その最終native確認へ戻れる。
PR402の別途受理前にmainやPreviewへ混ぜない。#453の時間計測も未完了。
GitHub連携の書込は403となったが、読取は成功し、認証済みghでPR作成・Issue更新は成功。
秘密情報は証拠へ記録していない。

## 作業物

証拠は `D:/NeNeCommander/evidence/footer-20261119/`。
未統合の作業木 `D:/NeNeCommander/worktrees/issue-454` と出力
`D:/NeNeCommander/outputs/footer-20261119/` は、この区切りの統合・保全まで保持する。
PR402/#457の未完了物と通常アプリ参照fixtureも保持する。

以前の削除拒否15対象は、10:47 JSTの読み取り専用確認ですべて不在だった。
`prior-cleanup-readback.json`に全絶対パスと結果を保存した。
拒否された削除は再実行していない。旧15対象についてhideへの実行依頼は解消する。
今回の作業物整理の最終結果はPR統合後に記録する。
