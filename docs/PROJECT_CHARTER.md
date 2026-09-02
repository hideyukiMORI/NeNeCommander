# Project Charter

Status: normative

## Mission

NeNe Commander is a Windows 11 dual-pane file manager optimized for fast, predictable keyboard operation. It provides one coherent interaction model across local Windows paths, UNC shares, removable media, and WSL distributions while preserving the real safety and capability differences between those filesystems.

## Product commitments

- The primary interface is a dual-pane file list with an explicit active pane.
- Movement uses Vim bindings by default; command keys remain compatible with familiar commander-style workflows.
- File operations are asynchronous, cancellable where the platform permits, observable, and never silently destructive.
- Every user command has one intent, one validation path, one execution path, and one typed outcome.
- Windows and WSL locations are first-class, but platform behavior is never guessed from path text after parsing.
- Accessibility, keyboard focus, high contrast, DPI scaling, and localization are structural requirements.
- Final visual design is supplied through Claude Design or ChatGPT design tooling and integrated through stable semantic design tokens.
- The codebase is constrained so that different competent humans or AI models converge on substantially the same implementation.

## MVP scope

The first usable release includes dual-pane navigation, address and history navigation, selection, copy, move, rename, new folder, delete with explicit policy, file launch, refresh, sorting, hidden-item display, drive and WSL-root discovery, operation progress, error presentation, settings, and Vim-first keyboard control.

## Non-goals for the first release

- A plugin system.
- An embedded editor, terminal, archive engine, FTP client, or cloud-drive protocol.
- Inventing a custom visual language before the external design handoff.
- Treating every provider as if it supports recycle bin, atomic rename, Windows ACLs, or identical timestamp semantics.
- Multiple interchangeable state-management, dependency-injection, command, result, or path libraries.

## Priority order

When requirements compete, decide in this order:

1. user data safety;
2. deterministic behavior and invariant preservation;
3. accessibility and input correctness;
4. architectural integrity;
5. responsiveness and performance;
6. feature breadth;
7. visual polish.

No lower priority justifies violating a higher one.
