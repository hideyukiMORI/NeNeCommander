# Content-overwrite interaction handoff — 2026-11-25

Status: engineering interaction accepted for implementation; product/native acceptance pending.

Issue #560 / ADR-0095. Actual Claude Skill(design) was invoked twice from authored briefs.
The installed skill exposed access management, not Canvas. No access change or remote
project write occurred. The second ordinary written advice supplies three-language
wording; it is not a visual Canvas specification. Permanent advice and exact invocation
are recorded in [quality](../quality/2026-11-25-transfer-conflicts.md).

## Existing surface and ownership

Extend the existing conflict modal and operation bar. Keep existing tokens, Skip /
Keep Both / Cancel order and initial Cancel focus. Do not introduce a result modal,
Close/OK workflow, new shortcut, default destructive Enter, palette or token values.
The one native template instance belongs to the exact conflict owner. Old callbacks
are ignored; they do not refresh details, move focus or request another confirmation.

When current observed capability admits overwrite, insert an inline overwrite group
above the existing ordinary-choice row. Show its consent explanation without a tooltip
or expander, the exact source/target details, the qualified overwrite scope and the
Overwrite contents button. Keep the alias/partial-result meaning visible before the
button. Unsupported capability shows a specific reason in that position, not a future
feature claim. Source/target path text wraps fully; narrow layouts stack the groups.

The overwrite-all checkbox is separate from the existing Skip/KeepBoth scope checkbox.
Both start off on a new owner; an existing non-destructive scope cannot become overwrite
consent. The overwrite checkbox names the count of other already-known eligible files
and exposes their source/target rows inline in a bounded scroll region. If the complete
set cannot be presented, offer only current-target consent. Scope changes retain focus
on that checkbox and do not activate the destructive button. Other conflicts remain
unresolved during complete-batch preflight, before effects; they are not promised a
new modal after preparation artifacts already exist.

## Core text

Use full localized resource templates and align source/destination terminology with
existing resources. These are engineering-adapted recommendations, not code keys.

| Meaning | English | 日本語 | 简体中文 |
| --- | --- | --- | --- |
| Action | Overwrite contents | 内容を上書き | 覆盖内容 |
| Button accessible name | Overwrite contents of {name} | {name} の内容を上書き | 覆盖 {name} 的内容 |
| Target label | Destination: {path} | 保存先: {path} | 目标：{path} |
| Content consent | Writes the source contents into the destination file shown above. | 表示した保存先ファイルに、コピー元の内容を書き込みます。 | 将源文件的内容写入上方显示的目标文件。 |
| Alias meaning | Other names for this same file, including names created during the operation, will show the changed contents too. | 同じファイルを指す別の名前（処理中に作られたものを含む）からも、変更後の内容が見えます。 | 同一文件的其他名称（包括操作期间新建的名称）也会显示更改后的内容。 |
| Partial result | A backup is saved before writing. If the operation stops partway, contents may have changed; nothing is restored automatically. | 書き込み前に元の内容を退避します。途中で止まると内容が変わっている場合があり、自動では元に戻りません。 | 写入前会备份原内容。中途停止时，内容可能已更改，且不会自动还原。 |
| Overwrite scope | Also overwrite the {n} other eligible files listed below | 下に表示した他の対象ファイル {n} 件も内容を上書きする | 同时覆盖下方列出的其他 {n} 个符合条件的文件的内容 |
| Later discoveries | Conflicts found later are not included. | 後で見つかった競合は含みません。 | 不包括之后发现的冲突。 |
| Cancel before target mutation | Cancelled before writing to the destination. | 保存先への書き込み前に取り消しました。 | 已在写入目标文件前取消。 |

Do not adopt the advice's "Applies only to this path" wording: aliases are explicitly
in scope as references to the same object. Do not say "before anything was written"
when backup/staging/record bytes may already exist. Implementation details such as
hashes, state enum names and native APIs stay out of normal-flow labels.

## Unavailable state

Show the observed first applicable reason: folder, unsupported location/filesystem,
source/target identity alias, observed multiple hardlinks, special/link attributes,
extra named streams (including empty ones), or unconfirmed facts. Do not call unknown
facts safe, remove foreign streams, or imply every Windows path supports this action.
The advice's ordinary labels are usable after alignment with canonical terminology.
Folder merge is not implemented or promised by this handoff.

## Result projection and focus

Use the existing operation bar with one target-content observation and retained-item
rows. Keep global operation failure/cancellation status and its concrete reason alongside
these facts; a verified-target count does not conceal a cleanup failure or source retained.

| Receipt observation | English | 日本語 | 简体中文 |
| --- | --- | --- | --- |
| Untouched by this operation | Destination not changed by this operation. | この操作では保存先を変更していません。 | 此操作未更改目标文件。 |
| Original bytes proved | Destination contents checked: unchanged. | 保存先の内容は変更されていないことを確認しました。 | 已验证目标文件的内容未更改。 |
| Mutation possible, unverified | Destination contents may have changed. Not verified. | 保存先の内容が変わっている可能性があります（未確認）。 | 目标文件的内容可能已更改（未验证）。 |
| Desired bytes proved | Destination contents verified against the source. | 保存先の内容がコピー元と一致することを確認しました。 | 已验证目标文件的内容与源文件一致。 |
| Source retained | Source kept: {path} | コピー元を保持: {path} | 已保留源文件：{path} |
| Backup retained | Earlier contents kept: {path} | 元の内容の退避先: {path} | 原内容的备份：{path} |
| Preparation retained | Prepared copy kept: {path} | 準備済みコピーを保持: {path} | 已保留准备好的副本：{path} |
| Recovery mapping retained | Recovery record kept: {path} | 復旧用記録を保持: {path} | 已保留恢复记录：{path} |
| Path no longer confirmed | Last known path: {path} | 最後に確認したパス: {path} | 上次确认的路径：{path} |

A write/truncate exception uses the uncertain state unless exact readback supports a
stronger statement. Empty-source truncation is also a mutation. A path is current only
when its owning observation confirms that fact; otherwise use the last-known label.
Do not hide retained preparations merely because target is untouched. Do not promise
rollback, automatic restore or power-loss durability. Recovery instructions require
manual comparison with the current file; the mapping itself grants no overwrite authority.

Retain existing operation completion/file-list focus behavior. Do not create a Close
button or focusable result heading based solely on the advice. Reuse the status
announcement path, once per meaningful state change, with no per-byte announcements.
A target change before effects can produce a fresh conflict and Cancel focus; a detected
change after an artifact exists ends with the partial receipt in this bar.

## Required native proof

Prove exact-owner stale callbacks, independent scopes, no default overwrite, current-
target versus explicit eligible-set consent, all three locales, wrapping/narrow/DPI/
high-contrast/accessible names, and truthful current/last-known result paths. Inspect
actual native views after implementation; written advice is not layout, physical-input,
IME, screen-reader or manual acceptance evidence. Reuse unchanged owner proof only
for unchanged relevant inputs. No final visual styling is introduced here.
