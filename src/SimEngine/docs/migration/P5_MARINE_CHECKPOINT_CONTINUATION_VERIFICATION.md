# P5-MC2 — Marine checkpoint continuation evidence

Date2026-10-16. Engine unchanged2.639.0/save160/replay1. This stage does not
close original P5.005 or certify the original full marine workflow.

## Implemented boundary

Actual consumer OilCo.PublicProof/OffshoreTests.cs retains the same literal marine
case and one MarineRun. New explicit `harbour:<absolute checkpoint.json>` input
loads native state, never creates another world or posts construction again.
The active schema rejects missing/unknown/duplicate keys, wrong stage/configuration,
unmatched native hash or mismatched project/duration/yard/field facts. GameFactory
Describe/Load remains sole native configuration/codec authority. These refusal paths
are source-reviewed, not a completed negative-runtime matrix.

Preparation and continuation share one native checkpoint writer with memory/disk
load/hash checks and CreateNew output. Healthy progress at a wall-time failure is
retained before throwing; a failed observation remains FAILED. No faulted partial
state may be saved. Original25-day total deadline and120s observation remain.

## Input identity

Actual consumer artifacts/marine/6d6947e26fba4cc48646468eaff420e5/checkpoint.json,
native game hashC68151411C737F1C, minute11, canonical96x96 Gulf seed1/two humans,
playing seat1. Original yard[71,74], offshore field21. No content overrides,
money activation, changed supplies/durations/geometry or alternate-world search.

Native save SHA2568055FD3E8ED560111E76260974F03D1807D2AE64CFC020ACCD9E232319BF0BAE.

Second healthy progress save47308bytes, SHA256
207190DB4744D12A6DCD5281D3C15FCDA28F0F7E4095A3D54F7F4B4B8BEA942D;
intent215bytes, SHA2565E6CC9DE47EEF56EC51C55F4741470CD4837DA649685020560DEF27F4C3155D1.

Current OffshoreTests.cs SHA256E524EA43E54BA124BF61DB6FB16201326BC34F119350D63B2B6644CD1BFA8B7A;
OilCo.PublicProof DLL45A7C8C2C1285F0D22F231BE769C638E74C17108A991A3CA894F33BFCE51FAB8.
Actual consumer OilCo.Game DLL unchanged2E0C64518B20CA4CCB0AB1FC459BC52D6DE3BACC18742F97C58CD652049DFFA1;
engine DLL unchangedE811B410F353520502501CF34A6E848B47EB67F0B72DEA42E1E645D3ADA0A6D7.
Recomputed444 core source fingerprint unchanged
0327e6ff4473656c75e2e8a889d804e5cc8d8029b027065aa772b5bb27b7973a.
Current original requirement texts/count verified221/210 checked/11 open; no original
checkbox was modified by this stage. No full regression inference from these identities.

## Builds and exact attempts

Targeted Release build of OilCo.PublicProof.csproj with --no-restore -v minimal:
first1.44s, second1.33s (headquarters identity read moved to existing CompanyView.Hq
instead of guessing a prototype name), third1.42s (healthy timeout persistence).
All exit0, zero warnings/errors. No tests launched by these builds.

Same literal only:

```
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Offshore: vessels survey, drill and ship from the sea" --timeout-ms 180000 --termination-timeout-ms 5000 --output-chars 16000 --case-input "harbour:<absolute original checkpoint.json>"
```

FIRST: child5352/session12620, exit1,17 passed/1 failed, child124745ms,
outer124848ms, untruncated. Native observation reached minute1031 after1020
normal minute Updates, no fault.120s stage limit observed123123ms at the next
60-update boundary. Shipyard clearing completed; native erect3600-minute node
waits work.resource.stock. Bulldozer moves to depot; both actual haul trucks active,
one loading22.666 and one moving20 units. Not a physical deadlock conclusion.
No output checkpoint was written by this first implementation on timeout.

One live stack sample of the same5352 process showed MapLayer<byte>.ValidateValues
inside TileMap.Sync→EntityObservation spatial guard→KnowledgeVisibility.CopyObservationKeys
→SpatialLayerAccess.CopyObservationKeys→TileMap.RefreshVisibility→World.Step.
This is a sampled hotspot, not a measured complete performance attribution.
No guards, state checks, hashing, deadlines or canonical work were weakened.

SECOND: child33000/session73055, exit1,20 passed/1 failed, child124191ms,
outer124297ms, untruncated. Same input/world and unchanged bounds; again minute1031,
native hash1A339090853E5F36. Stage failure observed122564ms including checkpoint
publication. All three native memory/disk/no-mutation hash checks passed before the
same wall-time failure. Fresh output:
artifacts/marine/d9a63ae0f9eb49dd811a5a4725f567ae/checkpoint.json.
Its verified stage is harbour_progress, next harbour_execution, not completed work.

THIRD: uses this actual progressed native checkpoint, not the minute11 preparation.
Child32360/session78753, exit1,20 passed/1 failed, child176052ms, outer176157ms,
untruncated. No executable edits or builds between SECOND and THIRD. Native game
advanced another540 minutes to1571 with no fault. Shipyard erection now125/3600
productive minutes, crane state.working; depot clearing complete, erect1800 waits
materials; grid clearing assigned, bulldozer moving. Actual haul trucks carry30 and8.
Thus loaded native work resumes and physical material supply unlocks real construction;
this is not a completed harbour or a successful one-day observation.

Latest healthy checkpoint:
artifacts/marine/a3949d936eb14c52978db859f4fcea67/checkpoint.json,
native hashFE63E4F0CD3B2B5E, game.sav135654bytes,
SHA256C88AB16C01C34AABFFB7D1700508727C66C1F87F8E9624B8ECDB6BE19F99BA60.
Intent215bytes, SHA2567A81DB5550A75F7754AF477A500D7F7C869F732674F9216F3497810BC26EF84C.
Verified stage harbour_progress, next harbour_execution. Original/second artifacts
remain unchanged; all are engine2.639-bound native saves, not compatibility formats.

The third failure's printed174373ms includes post-limit native save/load verification.
The120s threshold is checked after a batch, and checkpoint publication may overrun
that threshold;180s outer runner remains the process cap. Separate Update/batch/save/
load costs were NOT instrumented, so no measured attribution of the54s excess to one
operation is claimed. Strict hard wall bounds for individual callbacks are not proven.
Another attempted stack read found32360 already terminal; authoritative session result
then confirmed normal exit1, not an observation timeout/kill. No process was restarted
because a tool observation expired. Final native worker inventory is empty.

These are THREE attempts of ONE literal case, all wall-time failures. Do not add their
passed assertions together or describe them as green runtime acceptance.

## Limits and next work

Full harbour construction, fleet, marine survey, offshore drilling, delivery and
subsea completion remain unverified. Changed full-workflow branches from P5-MS1
remain compile-only. Original221/210 recorded checked/11 open; full goal OPEN.
No full/profile/family/default full-case run, production engine or business/JSON/
platform/Draw change, staging/commit/push. Prior passing checks were not rerun.

Next use the latest actual native checkpoint above (not regenerated setup), retain
its source/hash/physical diagnostics and measure/resolve canonical Update and native
checkpoint costs before claiming
the original harbour acceptance. Do not replace the workflow with a smaller map,
less work, more resources or a successful pending-state oracle.

Repository hygiene: framework git diff --check exit0,35 pre-existing LF conversion
warnings/no other diagnostics; consumer targeted check exit0. No staged files in
either repository. One initial read-only hash-inventory command had a PowerShell
pipeline syntax error; corrected inventory completed successfully without mutations.
