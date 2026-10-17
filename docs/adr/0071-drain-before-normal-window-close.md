# ADR-0071: Drain owned work before normal window close

Status: accepted

Date: 2026-10-17

Accepted by the NeNe Commander design owner under hide's delegated authority for
the resumed implementation of Issue #251. This corrects ADR-0030's assumption
that starting asynchronous shutdown from the final window's `Closed` event keeps
the WinUI message loop alive until the drain finishes.

## Context

The current application starts `ShutdownAsync` from `Window.Closed`. Its task is
owned, but ownership alone does not keep a final WinUI window or its dispatcher
alive. Settings writes and cooperative operation cleanup can outlive that event.
Also, `AsyncWorkOwner.StopAsync` snapshots current work without refusing later
starts, so input can introduce work outside the shutdown snapshot.

The invariant is that an ordinary system close request must leave the final
window alive until admitted pane work and queued settings writes have completed
their existing cleanup, startup work has stopped, and the gateway is released.
Cancellation requests completion; it does not prove completion.

## Decision

- Keep `AsyncWorkOwner` as the sole framework-task ownership mechanism. Add the
  narrow framework-neutral `WindowShutdownOwner` in Presentation. It uses one
  `AsyncWorkOwner`, accepts a close request once, awaits the supplied drain, and
  invokes the final close callback only when that drain succeeds. A drain defect
  reaches the existing defect observer once and does not invoke normal close.
  The App replaces its old shutdown owner with this coordinator; it does not
  retain a second shutdown task or start shutdown from `Closed`.
- A small App `AppWindowCloseAdapter` translates `AppWindow.Closing`: set
  `Cancel = true` synchronously, then forward the request. Duplicate requests
  remain cancelled while the same drain is pending. After successful drain, the
  composition root queues the final action on the window dispatcher, detaches
  the adapter, and calls `Window.Close`. Queue rejection is an unexpected defect.
  Queuing also lets the original `Closing` handler return when drain completes
  synchronously. Framework translation contains no session policy.
- `AsyncWorkOwner.StopAsync` becomes terminal before taking its work snapshot.
  Every subsequent ordinary start and Escape interrupt is rejected, including
  when Stop was called while idle. Successful replacement before Stop retains
  its existing behavior. Repeated stops wait for the same admitted work.
  A throwing cancellation callback must not skip cancelling the other admitted
  run or awaiting either run's cleanup. Nested `finally` blocks preserve that
  ordering before the unexpected cancellation defect propagates to the host.
  A completed run remains owned until its defect-observer call has returned or
  thrown. Only then does its completion release Stop; duplicate completion cannot
  observe twice or detach a different run. No replacement is admitted while that
  completion phase is still active. This prevents final close from overtaking
  the existing fatal defect-publication attempt.
- `CommanderWindow.StopAsync` stops pointer hit testing, consumes subsequent
  keyboard/character events, and rejects late Loaded/intent callbacks through a
  host stopping flag. This also blocks the synchronous window-adjustment route
  without changing the visual tree. It stops pane work and
  drains `CommanderSession` settings in `finally`, including when stopping pane
  work faults. The existing application shutdown sequence then stops startup
  work and disposes the gateway in its nested `finally` blocks. No second write,
  operation-cancellation, rollback, or recovery path is introduced.
- Pending file operations use their current cancellation observation points and
  cleanup. A blocking synchronous filesystem call can delay normal close until
  it returns; there is no shutdown timeout or immediate `File.Copy` interruption.
  Settings drain preserves typed persistence failures and their warning; closing
  after a completed failed save does not assert that the save succeeded.
- A pane read already abandoned through ADR-0058 is outside this drain. Its caller
  has completed, its nonmutating provider work is still observed by `PaneRead`,
  and no shutdown registry or renewed wait is added. An active, unabandoned read
  or Locations query remains part of the admitted host work and is awaited.
- This contract covers ordinary system-affordance close, including the title-bar
  close and its standard keyboard equivalent. `AppWindow.Destroy`, a direct
  external `Window.Close`, OS session end, process termination, crash, and loss of
  the dispatcher are not a promise of awaited cleanup. Production's explicit
  `Window.Close` is restricted to the final callback above. No OS session-ending
  handler, new prompt, partial-copy deletion, or interrupted-file recovery is added.

## Framework evidence

Microsoft documents [AppWindow.Closing](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow.closing?view=windows-app-sdk-1.8)
as a system-affordance close event that is not raised by `AppWindow.Destroy`.
Its [Cancel property](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindowclosingeventargs.cancel?view=windows-app-sdk-2.0)
vetoes the pending close. [Window.Closed](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window.closed?view=windows-app-sdk-1.8)
occurs after closure, and closing the final window ends the application.
[WM_QUERYENDSESSION](https://learn.microsoft.com/en-us/windows/win32/shutdown/wm-queryendsession)
is a distinct operating-system protocol. These contracts motivate the ordering;
the pinned runtime still needs executable host evidence.

## Rejected alternatives

- Await only from `Closed`: a retained task is not a dispatcher-lifetime guarantee.
- Mark `WindowEventArgs.Handled`: this is not the documented close-cancellation
  contract.
- Block the UI thread while waiting: continuations that need the dispatcher could
  never finish.
- Start independent close tasks for repeated requests: this duplicates cleanup
  and can release the gateway while an earlier request is still draining.
- Treat cancellation as completion or impose a timeout: existing mutation and
  settings ownership cannot safely be abandoned after an arbitrary deadline.

## Consequences and removal

Normal close can visibly wait for cooperative work. Input stops admitting new
commands during that wait; no new visual design is required. Defects retain the
existing fatal reporting behavior, so refusal to invoke normal close is not a
guarantee that the process survives a defect. Removing this change requires
removing the adapter, coordinator, terminal-stop contract, composition wiring,
tests, and this lifetime guarantee together. Stored data and dependencies do not
change.

## Executable proof and selected scope

Use deterministic barriers to prove terminal Stop admission, cancellation-callback
failure without early resource release, existing cancellation and interrupt
cleanup, observer-before-drain completion including a throwing observer,
one close request, drain-before-close, duplicate requests,
and exact drain-defect observation without successful close. Preserve the failing
terminal-stop test source, hashes, commands, and raw output before implementation.
Build App and the directly affected test projects; check lifecycle architecture
ownership and the existing settings queue and session shutdown boundaries.

A separate isolated WinUI host exercises the production adapter source and
Presentation coordinator with pending work and settings barriers. The accepted
2026-10-17 evidence is composed from two bounded scenarios with unchanged
production inputs:

- `run-001` sends two real system-affordance close requests to its owned window.
  Both are cancelled, one drain remains pending at each barrier, and release
  completes pane work, the two settings writes and the fixture's resource-release
  callback before `Closed`. Its external recorder did not retain a process handle,
  so its null exit code is explicitly not proof of normal process exit.
- `run-002` forwards fixture-owned file markers on the UI dispatcher into the same
  `RequestClose`, drain and queued final `Window.Close`. A retained process handle
  proves exit code 0 after the same cleanup ordering. This scenario does not
  independently exercise `AppWindow.Closing`; it supplies the process-exit fact.

These facts establish the adapter and final-close boundaries separately; they are
not described as one end-to-end system-close run with an observed exit code 0.
The resource-release callback is a fixture substitute, not a real provider mutation.
Archive both fixture versions, pinned inputs, exact commands, hashes, and raw events.
This is host-boundary proof, not a full production UI or release proof. Do not test
OS shutdown, kill a user process, or access production settings.

ADR-0061 selects focused lifecycle, settings-boundary, architecture, build, and
runtime evidence. No provider mutation algorithm, filesystem trust boundary,
dependency, or gate changes; unrelated provider suites, all-layer mutation, full
quality CI, and deep review are not selected. Reuse unchanged successful evidence.
