# Display transfer in the existing window-adjustment helper

Status: accepted engineering integration contract under ADR0092; product/visible proof remains

Issue #551 / 2026-11-24. This is an extension of the accepted Direction A helper,
not a new visual style. Background advice and raw consultation are retained with
the monitor-transfer investigation. No source or local diagnostics were sent.

## Components and resources

Reuse WindowAdjustmentOverlay, WindowAdjustmentSurface, WindowAdjustmentHelper,
WindowAdjustmentTitle, WindowAdjustmentOutcome and WindowAdjustmentKeyHints in
CommanderWindow.xaml. Keep the existing focus sink, transparent scrim, upper-centred
placement, KeyHintWrapPanel, tokenized border/padding/type/color and LiveSetting=Polite.
No fixed width/height, new icon asset, motion, color family or additional tab stop.
The existing wrapped outcome TextBlock carries the full localized sentence; no
ellipsis, line cap or hidden duplicate text is introduced. Its semantic text/warning
brush follows the typed outcome. Do not add a success color as the sole information.

Add two groups after ordinary movement in the canonical binding table: p / previous
display and n / next display. Ctrl+Left and Ctrl+Right are aliases in the same two hint groups. Each group
displays its letter and control-arrow cap, so the aliases also appear through
the existing generated keyboard help. Do not show duplicate
hint controls. New plain caps are localized resources KeyLabelP and KeyLabelN;
control caps use KeyLabelCtrlLeft and KeyLabelCtrlRight. Existing escape and all
other hints retain order and wording. The production key table owns every mapping.

## Complete state wording

The following complete strings are authored engineering content, not concatenated
fragments. Use stable resource keys and equivalent complete entries in all catalogs.
Existing maximized/minimized/not-overlapped refusals are retained.

| State | English | Japanese | Simplified Chinese |
|---|---|---|---|
| Previous hint | Previous display | 前のディスプレイ | 上一个显示器 |
| Next hint | Next display | 次のディスプレイ | 下一个显示器 |
| Previous planned | Move to previous display | 前のディスプレイへ移動 | 移到上一个显示器 |
| Next planned | Move to next display | 次のディスプレイへ移動 | 移到下一个显示器 |
| Previous confirmed immediately | Moved to previous display. | 前のディスプレイへ移動しました。 | 已移到上一个显示器。 |
| Next confirmed immediately | Moved to next display. | 次のディスプレイへ移動しました。 | 已移到下一个显示器。 |
| No other distinct work area | No other display is available. | ほかに移動できるディスプレイがありません。 | 没有其他可用显示器。 |
| Information unavailable | Display information is unavailable. | ディスプレイ情報を取得できません。 | 无法获取显示器信息。 |
| Hidden refusal | Show the window before moving it. | ウィンドウを表示してから移動してください。 | 请先显示窗口再移动。 |
| Minimum cannot fit | The target display cannot fit the window's minimum size. | 移動先の表示領域に最小サイズで収まりません。 | 目标显示区域无法容纳窗口的最小尺寸。 |
| Facts changed before apply | The window or displays changed; the move was cancelled. | ウィンドウまたはディスプレイの状態が変わったため、移動を中止しました。 | 窗口或显示器状态已改变，移动已取消。 |
| Effect unconfirmed | Display move could not be confirmed. Check the window position. | ディスプレイへの移動を確認できませんでした。ウィンドウの位置を確認してください。 | 无法确认显示器间的移动。请检查窗口位置。 |

Confirmed means the ADR's immediately observed qualification, never a promise about
later OS activity. Unconfirmed never asserts that an effect happened or did not happen.
A stale result is not rendered over a replacement owner. A stop renders nothing.
Do not expose internal IDs, rectangles, HRESULTs, API names, phase numbers or OS-build
numbers in the helper. The quality record carries those implementation details.

## Focus, input and responsiveness

The existing sink receives initial mode focus. Transfer does not intentionally move
keyboard focus or alter active pane, selection, row position, pending input or saved
preferences. Tab remains consumed. Produced letters, control aliases, initial/held
keys and IME admission follow the one mapper/repeat rule. Escape during a qualified
in-flight receipt may leave through the narrow owner bridge in ADR0092. Close stops
the view/route before tracking finalization; a returning Forward must not repaint.

All hints wrap with existing spacing tokens at narrow widths. The outcome receives
remaining width and wraps fully. Test long Japanese/Chinese refusals at the smallest
actual test window and all attached125/150/175% scales without changing system DPI.
Do not infer minimum-window fit from a wide English case. A helper too tall/narrow
is a measured layout defect; preserve the failing sample and fix only this surface.

## Selected proof and acceptance limits

Pure tests cover exact generated keys/groups/labels, repeat suppression, control
aliases, IME pass-through, no file-list chord change, and all typed outcome labels.
Actual owned-window proof covers the helper on the named non-input desktop only,
using private settings/fake pane readers and no real Shell/provider operation. Log
actual geometry, source/XBF/dependency parity, owner/focus/selected rows, immediate
and settled scale/rectangles, every failure, late/stop behavior and normal exit.
A diagnostic that intentionally activates a window must do so only on that isolated
non-input desktop, never on hide's interactive desktop. Never SwitchDesktop or
SendInput. Direct API and targeted-message evidence stay explicitly distinct from
physical key/IME/speech/visible acceptance. Do not force a hidden-window success
through the production visibility refusal or describe unshown arithmetic as UI proof.

Visible multi-monitor transitions, maximize/restore sequence, custom scale,
disconnect/reconnect, native keyboard hold, Narrator and final screen acceptance
remain separate. The old unshown24-case and minimum4-case probes support only the
facts they observed. Existing normal settings and unfinished work remain retained.
