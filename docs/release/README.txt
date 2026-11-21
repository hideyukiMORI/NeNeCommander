NeNe Commander Preview — Windows 11 x64

This is an early prerelease. The one-time display, keyboard and high-contrast
confirmation was reported complete by the maintainer on 2026-11-19 (Issue #94).
The exact selected display scale and independent captures were not recorded.
Launch and all live provider environments have not been verified for this shipping
package. Report problems through the project issue tracker.

Start
1. Extract the whole ZIP into a folder where this account can read and run files.
2. Keep every extracted file and folder together.
3. Open NeNeCommander.App.exe. There is no installer and no administrator access is required.

The .NET runtime and Windows App SDK runtime are included. Windows 11 x64
(build 22000 or later) is required. This package has not been prepared for ARM64.
The NeNe Commander executable is unsigned. Windows may show an unrecognized-app
warning; verify the download source and the published SHA-256 before deciding to run it.
The package does not install a certificate or change Windows security settings.

Use
- Tab switches the active pane. h/j/k/l and the arrow keys navigate.
- F5 copies, F6 moves, F2 renames, F7 creates a folder, and F8 deletes.
  Read the confirmation: recycle-bin support depends on the selected provider
  and entry. Some operations require explicit permanent-deletion confirmation.
- Escape cancels or abandons the current interaction where supported.
- Ctrl+P opens the command palette; Ctrl+G opens drives and WSL roots.
- Ctrl+L edits the active pane path in a modal; ordinary path labels are display-only.
- Ctrl+B opens bookmarks; Tab changes region, j/k or arrows move, / searches,
  a/e/d add/edit/delete, Ctrl+Enter saves, and Escape cancels or closes.
- Ctrl+, opens settings; Ctrl+W adjusts the window.
- F1 opens the in-app keyboard help with the full current key map.
- The footer wraps to the available width. Settings can hide normal key hints;
  F1 and the confirmation/name-entry Enter/Escape guidance remain visible.

Scope
Dual panes, address/history navigation, selection, sort order, hidden entries,
file operations, progress, error feedback and settings are included.
Windows local files support navigation, operations and Windows association launch.
WSL distributions support their declared reads, operations and transfers, including
Windows/WSL and cross-distribution transfers. A registered WSL distribution is
required only for WSL features. UNC shares support directory reads and compatible
Windows association launch. UNC mutations and mapped-network-drive mutations are
not supported. WSL/UNC launch rejects incompatible names and paths of 260 or more
UTF-16 code units. Provider capabilities and failures are reported by the app.
There is no embedded editor, terminal, archive tool, FTP client or cloud protocol.

Known issue
Clicking outside the focused pane to reactivate an inactive window may leave
keyboard commands unresponsive (reported by the maintainer; Issue #523).
A fix is planned for a later Preview; this package does not claim it resolved.

Settings and updates
Settings are saved in %LOCALAPPDATA%\NeNeCommander\settings.json, separately from
this folder. Current writes use schema version 4; valid older versions 1, 2 and 3
are read without a startup rewrite. A later accepted settings change saves version 4.
The beta.1 and older binaries reject version 4, show their settings warning and
use defaults without overwriting the rejected document. No downgrade is provided.
Different copies run by the same Windows account share this settings file.
To update, close the app and extract the new ZIP into a new folder. There is no
automatic updater. Deleting an extracted application folder leaves settings intact.

Version and notices
RELEASE.txt identifies this build and source commit. The download's accompanying
release-manifest.json and SHA256SUMS.txt record its contents and checksums.
Third-party components retain their own license terms. Read licenses/index.json
and the accompanying unmodified license/notice files before using the bundled
components. Do not use those components unless their applicable terms are accepted.
The original package metadata and URLs are retained; the Windows SDK license
document accompanies packages that supply its URL without local license text.
The index also includes resolved build-time packages, which does not mean their
tools are shipped as executable parts of the application.

Project and issue reports: https://github.com/hideyukiMORI/NeNeCommander
