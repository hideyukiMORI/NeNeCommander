NeNe Commander Preview — Windows 11 x64

This is an early prerelease. The one-time display, keyboard and high-contrast
confirmation was reported complete by the maintainer on 2026-11-19 (Issue #94).
The exact selected display scale and independent captures were not recorded.
Not every live provider environment has been verified. See the release notes for
the checks performed on this package. Report problems through the issue tracker.

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
- Settings can switch Japanese, English or Simplified Chinese immediately.
- The current-theme button or Ctrl+P :colorscheme opens 129 searchable themes.
  Moving through the list previews a theme; Enter or a click accepts it.
  Escape cancels the preview and restores the accepted theme.
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

Changes in beta.6
Ctrl+A selects all visible entries, * inverts their selection, and Shift with
Up/Down, PageUp/PageDown, Home/End extends a range. Escape clears file-list marks.
Press / to filter the current folder's loaded names. Every entered word must
occur in the name. Enter keeps the filter and returns to the list; Escape in
the filter editor clears it. This is not recursive or fuzzy search.

Copy and move conflicts can explicitly overwrite an eligible existing file's
contents on a supported local NTFS drive. The confirmation shows the original
source and destination details and any other eligible files included by the
all-conflicts choice. Folder merging is not available.
Before destination writes, the app prepares retained files and a recovery record.
An interrupted operation may have changed destination contents. Read the result's
separate source, destination and retained-file observations before retrying or
recovering; saved observations can become outdated. There is no automatic restore.

Changes in beta.4
The command palette uses a smaller width and one shared target-pane description.
The file list wraps at its ends, and refresh after a move retains focus near the
removed item. Each pane reports direct file/folder and selection counts; these
are not recursive folder totals. Keyboard commands recover when returning through
the inactive pane's blank area (Issue #523); the maintainer confirmed both sides.
Normal close saves window position, size and maximized state. Restart restores
the window within a current display work area. A killed or crashed process cannot
guarantee a final settings save.

Settings and updates
Settings are saved in %LOCALAPPDATA%\NeNeCommander\settings.json, separately from
this folder. Current writes use schema version 6; valid older versions 1 through 5
are read without a startup rewrite. An accepted settings change or changed window
placement at normal close saves version 6. This includes the display language and
window placement as well as themes, bookmarks, sort order and other preferences.
Beta.3 and older binaries reject version 6, show their settings warning and use
defaults without overwriting the rejected document. No downgrade is provided.
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
