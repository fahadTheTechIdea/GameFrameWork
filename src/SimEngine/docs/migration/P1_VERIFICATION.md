# P1 extraction verification — 2026-10-01

P1 is verified; the overall P1–P6 separation/generalization goal remains in progress.

This is historical evidence for the P1 extraction/access-boundary source fingerprint below, not a
claim that later capability edits preserve those exact source bytes. Current P2 evidence is recorded
in [P2 capability verification](P2_CAPABILITIES_VERIFICATION.md). The ownership verifier keeps the
original archive hashes intact and names each later P2 source edit explicitly.

## Ownership and recovery

- Source HEAD before extraction: `7f9d4761ad0427fc1a199e11d77acb81eac32ca6`, plus dirty/untracked R7 changes.
- Sole runtime: `Beep.SimEngine/src/Beep.SimEngine/Beep.SimEngine.csproj`, assembly/namespace `Beep.SimEngine`.
- Oil extension and clients remain in the original active v2 tree, referencing the standalone project.
- Generic suites moved once. Mixed input validation was split; test runner/checks/TestKit have one
  owner in `Beep.SimEngine.Testing`. Counter/colony fixtures live in non-friend `ExtensionProof`.
  The nested-command capture check now uses the ordinary colony fixture instead of an oil scenario;
  generic test coverage therefore requires no oil factory. Its assertion is preserved.
- Shared build policies have one framework owner; the oil subtree imports them explicitly.
- All 121 original files have dispositions in `source-ownership.json`; original bytes/hashes and dirty
  status are captured in `source-inventory.json` and `source-before-extraction.zip` in this directory.
- `VerifyExtraction.ps1` verifies every recovery-entry hash, destination and standalone project reference;
  it confirms no original runtime directory remains. The archive is not compiled/packed and is ignored.
- Target Git initialization, publication, Tycoon, renderer/platform work and sibling-engine cleanup were not performed.

## Runtime gates

From the Beep.SimEngine root:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore --no-incremental
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- --all
& './docs/migration/VerifyExtraction.ps1'
```

Build: zero warnings/errors. Framework runner: **233 passed, zero failed, 30 suites**, none skipped,
0.4 seconds. Ownership/recovery gate passes for all 121 captured files. Standalone solution has no
OilFieldDays project/content dependency; the runtime has no package or project references.

From `Beep.OilFieldDays/src/oilCo-engine/src/SimEngine`:

```powershell
dotnet build SimEngine.slnx --no-restore
dotnet run --project OilCo.Tests --no-build -- --all
```

Build: zero warnings/errors. Full oil runner: **186 passed, zero failed, 22 suites**, none skipped,
435.4 seconds. This includes campaigns, programmed-player demo, scale allocation reporting,
offshore, incidents, visibility, research, lockstep, client purity and client interaction coverage.

Combined current coverage: **419 checks / 52 suites**. Original combined coverage was 410 / 50;
nine new boundary checks and one additional split-input suite explain the difference.

One overlapping rebuild failed because Windows locked DLLs in the still-running OilCo.Tests process.
The regression was left running and completed successfully; rebuilding after it exited passed with
zero warnings/errors. No code fallback or restart of that regression was used.

## P2 access-boundary slice

OilCo.Game no longer has framework friendship. Its public `World.ViewFor` projection validates seats;
exploration checks visibility through the same ID-based immutable query a player uses. `SimGame.World`
remains internal. Independent counter/colony modules and nine boundary checks verify this slice.
This does not close P2's capability/port/module-version work or the broader P3–P6 requirements.

Executable-source fingerprint after fixture separation: SHA-256
`1EC19AC0F5C2E90769ACCC7F4BAFF360121DDE6BA659C90CD303B27C94981529` over 159 sorted
source/content/project/build-policy paths and their SHA-256 hashes. This is working-tree evidence,
not a committed target revision.
