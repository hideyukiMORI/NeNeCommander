# Volume GUID buffer report — 2026-11-22

Issue #512 reduces only the GUID-name output array in the existing Windows local
volume identity resolver from32,768 to50 characters. The mount-path buffer and
fresh source/destination queries remain unchanged. The array payload arithmetic
is65,436 bytes per allocation; no actual allocation or UI-speed measurement is
claimed. See the [quality record](../quality/2026-11-22-volume-guid-buffer.md).

Locked Release restore/build, all10 focused native adapter/gateway tests and
changed-file formatting pass; builds report zero warnings/errors. Corrected
complexity inspection retains exactly the same26 findings as accepted #510;
the earlier unsupported-argument attempt is preserved. There is no new finding,
suppression, full CI/mutation run or shared-desktop input.

All399 artifacts/129,170,742bytes are permanently archived and verified. The
closing PR and completion record own integration and exact D task cleanup.
PR402 still requires hide's screen acceptance; #491/#453 native proof and AppSDK
Issue514/PR498 remain separate. No release was made.
