# P2 immutable module registration — 2026-10-01

P2 remains in progress; this slice closes its live metadata-getter dependency, not the entire stage.
This is historical slice evidence; newer source/gates are recorded in
[mobility/supply verification](P2_MOBILITY_SUPPLY_VERIFICATION.md).

## Implementation and ownership

GameDefinition's one existing registration list now holds ModuleRegistration instead of mutable
GameModule references. Each entry captures a stable ID, exact version and copied/read-only requirements
once. Dependencies are sorted by ordinal ID and reject null lists/default missing identities/duplicates.
The original executable module is internal to its entry; no second module container or facade exists.

All registered metadata is captured and the complete graph validated/sorted before DeclareContent.
Even an independent earlier module cannot run a declaration when a later module has a dependency cycle.
Modules and Select results are read-only; Find, selection, ContentSet manifest validation, World.Build,
save-menu identity and restoration all consume those same registrations, never later module getters.

An external non-friend MutableModule probe changes its ID/version/requirements after declaration,
including mutations to the original supplied list, and another module mutates it during declaration.
Tests prove unchanged identities, dependencies, selection, manifest acceptance, execution ownership,
save/load/hash and peer hello. Read-only collection casts refuse mutation; getters are read exactly once.
This freezes metadata, not arbitrary executable module behavior or module-authored application state.

## Current gates

```powershell
# Framework root
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- Composition -v
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all
pwsh -NoProfile -File docs/migration/VerifyExtraction.ps1

# Oil active-v2 root
dotnet build SimEngine.slnx --no-restore
dotnet run --project OilCo.Tests --no-build
```

Both builds: zero warnings/errors. Composition: **88 passed / seven suites**, zero failed, 0.1 seconds.
Full framework: **404 passed / 45 suites**, zero failed, none skipped, 0.9 seconds. Two new suites add
**29 checks** (20 external mutation/durability; nine graph/declaration validation). No original assertion
was removed. Fast oil: **155 passed / 18 suites**, zero failed, **four long suites skipped**, 27.0 seconds.
Current-source long oil gates have not been rerun; this is not a new full-oil completion claim.

The prior inventory build's still-running full oil process was observed to terminal success before
rebuilding oil against the new registration source: **186 passed / 22 suites**, none skipped, 548.5
seconds. That earlier full result belongs to the source/DLL fingerprints in the inventory report only.

Ownership verification: all **121** dispositions/recovery hashes, **52** unchanged namespace-normalized
C# transfers (named P2 edits excluded), standalone dependencies and absence of the original runtime pass.
Current executable identity: **139 files**, SHA-256
`D985F380572CFC8B34B0ACE7A6818D51DD289D408B1E5DE8F5409276BB0C1FF4`.
Framework runtime DLL SHA-256:
`9F862400BC5270EE2E8D2B1F849C0ECA6F84ABEE5A29F397A83B627914EB314C`.

The first combined edit attempt failed context validation and applied no changes; source inspection
confirmed that before corrected scoped edits were applied. All subsequently executed builds/tests passed.
No source was reverted, no running test was restarted, and no recovery archive was modified.

## Remaining acceptance

P2 still needs typed roles/requirements, supplier and mobility-definition consumers, operation/signal/
response providers and full event/state/view identity/schema ownership. P3–P6 remain planned. The
module-registration API changed directly; no old GameModule-list API, compatibility overload or shim
remains. No Tycoon, platform/renderer, oil-client gameplay, remote or package-publication changes occurred.
