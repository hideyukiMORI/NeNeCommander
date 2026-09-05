# Windows local atomic move handoff — 2026-09-06

Status: informational

## Work item

- Issue: #75
- Branch: `feat/75-atomic-same-volume-move`
- Decision: ADR-0032
- Invariant: all capability decisions precede mutation, and every atomic effect repeats identity, volume, link, containment, and collision checks without overwrite.
- Canonical mechanism: `FileOperationGateway.ExecuteTransferAsync` consumes the closed provider capability and selects either one provider move or the existing composite steps.

## Verification checkpoint

- Failure-first real-adapter gateway proof observed the old three-effect composite result where one atomic effect was required.
- Conformance and all gate negative proofs pass, including the generated-interop scope constraint.
- Release build passes with zero warnings/errors.
- Application 182, Infrastructure.Windows 84, Presentation.WinUI 76, and Architecture 5 tests pass with zero skips.
- Protected branch coverage is Application 100.00% and Infrastructure.Windows 93.55%.
- Initial deep run `33974827825` failed only the unchanged 90% Infrastructure.Windows mutation threshold (87.38%). Added boundary proofs produce a focused 90.77%; this correction still requires a fresh exact-head deep run.

## Integration steps

1. Review mounted-volume identity, complete capability preflight, exact effect reporting, and absence of overwrite/fallback mutation.
2. Commit through the existing Commit-mode hook, push, and create a Draft PR closing #75.
3. Confirm the remote PR head equals the local commit and run one security deep review for that exact candidate.
4. If the head or base changes, return to Draft and refresh evidence. Otherwise mark Ready and require the final canonical CI gate before squash merge.
5. Synchronize clean `main`, then proceed to Win32 file-identifier hardening without replacing the gateway route.

## Remaining environmental proof

A real cross-volume capability query needs a controlled second mounted volume. That environment was unavailable, so only the typed unsupported route is automated; no drive-prefix simulation is accepted as equivalent proof.
