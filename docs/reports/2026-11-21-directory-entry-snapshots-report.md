# 2026-11-21 ディレクトリ一覧の所有権修正・進捗報告

Issue #510は、公開一覧が後から変更され、表示中のファイルをOpenFocusedで
見つけられなくなる不整合を修正する。

- 修正前8ケース中7ケースで再現。ローカル・UNC・WSLの型付き入力とfake起動portで確認。
- #508の既存所有機構をDirectoryEntry専用の内部型へ移し、一覧生成・並べ替え・表示抽出で共有。
  各ownerの検証・順序・可視判定、entry/path参照、focus・選択・履歴・実行経路は維持。
- Application212件と追加起動14件、Presentation36件、reader境界67件が全成功。
  対象Release buildは警告/error0、形式成功、CS-013は既存と同じ26件。
- 正確な範囲・初回テスト構文エラー・コマンド・環境上の限界は[品質記録](../quality/2026-11-21-directory-entry-snapshots.md)。
  UI速度や実UNC/WSL、全件gate、releaseの成功は主張しない。
- 通常PRと`D:/NeNeCommander/evidence/entry-snapshots-20261121/completion.json`が
  統合・受理tree一致・保全・今回のD側作業物の整理結果を記録する。

先行#508/PR509はmain f0f7b31へ統合済み。700成果物を全件保全・検証し、専用作業物を整理済み。
hideの停止までの継続指示に従って進める。PR402の画面受理、#491/#453、AppSDK PR498は別件。
旧#454/#459の後片付け4対象は既に不在確認済みで、再依頼しない。
