# 初期基盤 引き継ぎ書 — 2026-09-03

Status: informational

## 開始地点

- Codename: `NeNe Commander`
- Local root: `C:\Users\info\WORKS\NeNeCommander`
- Public repository: [`hideyukiMORI/NeNeCommander`](https://github.com/hideyukiMORI/NeNeCommander)
- Default branch: `main`
- Verified code baseline: `a386406b6269b7de14cad9653c0270ac391e6ecc`
- Initial scope: [Issue #1](https://github.com/hideyukiMORI/NeNeCommander/issues/1)
- Security follow-up: [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2)

新しいセッションは `AGENTS.md`、`docs/PROJECT_STATE.md`、変更領域の規範文書を順に全文読む。fresh clone では最初に次を実行する。

```powershell
pwsh -NoProfile -File ./eng/bootstrap.ps1
```

最終判定は常に次の一つだけである。

```powershell
pwsh -NoProfile -File ./eng/check.ps1
```

## 現在の実装境界

- Domain は Windows local / UNC / WSL path、parse failure、provider-native identity を所有する。
- Application は immutable pane state、単一 reducer、typed intent、operation request/outcome、confirmation と operation serialization を所有する。
- Infrastructure.Windows は containment、HRESULT normalization、strict settings、HMAC path fingerprint、monotonic clock を所有する。
- Presentation.WinUI は deterministic keyboard translation と intent mapping を所有し、WinUI runtime package を参照しない。
- App は WinUI composition root、live input event translation、resource、空の dual-pane shell だけを所有する。

文字列は境界で一度だけ parse する。失敗は closed outcome にする。Windows / UNC / WSL の能力を path label から推測しない。ファイル変更は必ず Application の単一 gateway を通す。UI は intent だけを発行し、domain decision を持たない。

## 動作している入力

Vim navigation は `j` / `k` / `h` / `l`、`gg` / `G`、`Ctrl+D` / `Ctrl+U` / `Ctrl+L` / `Ctrl+R`、`Alt+Up` を単一 mapper で処理する。F-key command も同じ intent 境界へ入る。IME composition、text entry、modal state では global command を発火させない。

## 安全性の現在地

- device namespace、root escape、invalid segment を parse 時に拒否する。
- `\\wsl$` と `\\wsl.localhost` は一つの WSL canonical form に正規化する。
- move は copy → verify → delete。verify failure では source を削除しない。
- recycle capability がなければ permanent delete は exact confirmation なしに実行しない。
- diagnostics は raw full path を出さず、salted fingerprint を使う。
- 18件の ADV ID を executable adversarial test へ関連付けている。

## 確認済み証跡

- quality: [`33653839474`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33653839474)、success。
- deep security/adversarial/CodeQL: [`33654215824`](https://github.com/hideyukiMORI/NeNeCommander/actions/runs/33654215824)、success。
- tests: 118/118 pass。
- protected branch coverage: 100.00 / 100.00 / 97.33 / 96.47%。
- mutation: 96.93 / 95.98 / 98.86 / 92.17%。

deep review は UTC の3日周期 `23 2 */3 * *` と手動 dispatch で動く。release 時点では96時間以内の成功が必要である。CodeQL の open alert 48件は [Issue #2](https://github.com/hideyukiMORI/NeNeCommander/issues/2) で追跡中であり、42件は generated code、6件は所有コードの maintainability note である。

## 次の一つの縦切り

次は新しい focused Issue と branch を作り、「Windows directory を一つ読み、左ペインへ表示する」だけを実装する。

受け入れ境界:

1. Application に provider-neutral な directory-read request と closed outcome を一つだけ定義する。
2. Infrastructure.Windows に非再帰の Windows local adapter を一つ実装する。
3. test-owned temp root で empty、file、directory、access failure、cancellation、10,000件上限を contract test にする。
4. entry order と focus identity を決定的にし、左ペインだけへ投影する。
5. ADV-011 を実列挙の打ち切りと変化耐性まで広げる。
6. 正本ゲートと必要な deep review を通し、環境未実施項目を明記する。

この縦切りへ WSL live adapter、再帰、file mutation、final styling、history、collision UI を混ぜない。WSL は同じ port の別 provider として後続 Issue で追加する。

## その次に待つ仕事

- `IFileOperationPort` の Windows local production adapter と temp-root contract。
- same-provider atomic move を capability と ADR で選ぶ。
- ADV-016 を dual-pane session identity freeze の実動作まで拡張する。
- Copy / Rename / CreateDirectory を既存 gateway の closed request hierarchy へ追加する。
- live WSL、UNC、removable media、high DPI、high contrast、Narrator の明示的な環境試験。

## 禁止事項

- `System.IO` や shell API を Application / Presentation から直接呼ばない。
- second gateway、parallel parser、UI code-behind decision を作らない。
- `CommunityToolkit.Mvvm` を ViewModel の最初の利用と別に先行追加しない。
- branch coverage、mutation、analyzer、CodeQL query suite を都合で弱めない。
- final visual design を semantic token の外へ hard-code しない。
- WSL home や利用者の実データを test root として使わない。

## 環境上の注意

旧 typo path `C:\Users\info\WORKS\NeNeComander` は空だが、現在の対話 process が current directory handle を保持しているため削除できない。終了後、内容0件と正しい root の存在を確認して、旧空 directory のみ削除する。
