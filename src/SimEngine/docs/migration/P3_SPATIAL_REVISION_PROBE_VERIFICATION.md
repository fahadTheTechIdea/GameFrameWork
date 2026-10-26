# P3-S28.C — Same-payload spatial revision probe verification

Date2026-10-18. [Design](../../plans/enhancements/P3_SPATIAL_REVISION_PROBE_DESIGN.md).
Engine2.658.0/save160/replay1; production code/content unchanged. This corrects
negative instrumentation, not the observer or ordinary point-layer admission.
Original221 requirements/209 recorded checked/12 open; no original broad gate
closes and the whole goal remains ACTIVE.

## Cause and correction

SpatialMutationProbe.Change's first PointSpace.WriteLayer genuinely changed the
sample and sole original revision. Its second WriteLayer entered CheckWrite ->
CanObserve -> SpatialLayerAccess.CanSeeDefinition -> ReadProvider. The enclosing
observer correctly detected the still-mutated actual sample before restoration.
MutationCompleted/restoration/revision reporting and the intended RevisionThrow
exception were never reached.

The existing negative helper now captures the actual PointLayer once. First
write still takes ordinary public authorization/preflight/commit. Only negative
restoration uses that layer's existing internal Write, preserving the genuine
first-write revision. No counter, copied commit, field/reflection mutation or
production bypass was added. Existing guard.sample is public/fixed/non-resource
master-listed JSON. Original sample validation/storage/notification remain.

SharedSpatialObservationTests.cs and all776 assertions are unchanged. Final
revision cases really restore the complete codec payload and reach the outer
original revision refusal; throwing cases retain their intentional first error.
Native/independent spaces, normal/half-minute quanta, physical construction/
hauling/research, partial saves, pause, isolated sessions, replay and terminal
refusals are covered by this ONE literal, not a full/all-provider regression.

## Commands and all current attempts

Framework directory: C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Shared spatial observation: original codecs and revisions guard physical native and point callbacks' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 18000
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal
# After terminal build success, SAME literal/SAME bounds once.
```

| Attempt | Passed/failed | Worker ms | Outer ms | Result |
|---|---:|---:|---:|---|
| Pre-edit current baseline |728/48|11970|12191|exit1, untruncated|
| Targeted Release build |N/A|8310 MSBuild|8979 command|exit0,0 warnings/errors|
| Final same literal |776/0|10727|10890|exit0, untruncated|

Yielded baseline19852/final15876 handles were each awaited to terminal, not
restarted. No tests ran during build. Three combined patches failed preflight on
guessed document headings and made no changes; corrected literal headings were
used. Earlier scalar/optimized A/B failures remain in parent evidence, not erased
or counted as passing.

## Final identities

Unchanged core448 C# sorted-path/hash LF SHA256:
240a93de06e6f22446dd0419b4589a4b3b6efaf41c6db1357d54d1ed41491aef.

| Artifact | SHA256 |
|---|---|
| Core DLL, Tests copied core, consumer copied core |7A817CA29F7FAF1EA14E89CCD13C62DF07F3764800E836864DE1424A4909E062|
| Final Tests DLL |9D17A3ECDFFF5889A63CC21B7871DE64C7AC39E81C07F02F6875614404EBF127|
| Final ExtensionProof DLL |BF68C397433AEC05801D4B3997A7C135A91669B88FE7B53B06090D1A89465121|
| Changed ContentAccessContractProof.cs |518F603ABAD9BBA487744030A94BC050B3797D11D74558754FE886FD8AE5F865|
| Unchanged SharedSpatialObservationTests.cs |10DD7B3B12D5A66DD75EF7E59FE7B2DD84B2539F57E76A5B5B1D3362E1776F65|
| Unchanged OilCo.PublicProof DLL |2F3ACF3BBF74ACA4CA068FB647B018D5A13D2254753751EFD2832B5AD02CAD2B|

## Native physical continuation — separate scope

At resumption no bounded-exact/worker-exact dotnet process remained live.
Previously launched harbour continuation's terminal output was lost; newest
artifact b06ef02476b347389221bf51c7ae0c9e/checkpoint.json loads at1991 with actual
hashC12D942CABAA614B, harbour_progress/next harbour_execution. Do not infer a
wall-time pass:540 minutes after1451 is not a day. Source permits healthy
partial retention on original120000ms stage failure; no completed-day claim.

Only two separate selections ran this turn in consumer directory
Beep.OilFieldDays/src/oilCo-engine/src/SimEngine, each using literal
`Offshore: vessels survey, drill and ship from the sea`, bounded-exact,
timeout30000/termination5000/output12000. Explicit case-input prefixes were
`inspect:` then `harbour-step:`, both pointing to the full absolute
`C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.OilFieldDays/src/oilCo-engine/src/SimEngine/artifacts/marine/b06ef02476b347389221bf51c7ae0c9e/checkpoint.json`.
No absent-input full workflow was selected.

Inspect18/0, worker2311/outer2463ms: no commands/Update/save, hash unchanged.
One-update24/0, worker2663/outer2802ms: actual Update983ms; public home reads
1.2–18.6ms. Two trucks still move to supply grid while crane earns shipyard
erection445->446/3600. No missing-home diagnosis, teleport, granted material,
reduced work, changed deadline, host gameplay or second clock.

CURRENT consumer checkpoint:
artifacts/marine/206d9a85888943b58838dcfb03f3aeff/checkpoint.json,
minute1992/hash9C330B2441C954AF, harbour_progress/next harbour_execution.
Memory/disk reload hashes verified by original writer. Preserve until justified
engine change; never edit headers. Harbour/fleet/full offshore acceptance OPEN.
Next investigate expensive actual work/route/provider observation before another
long day, not larger bounds, hash caches, skipped guards or blind restarts.

## Limits and hygiene

No full/profile/family/default suite. No production engine/Oil/gameplay/business/
platform/Draw edits. Fresh original text/checkbox audit221/209 checked/12 open,
unchanged. Other earlier selected dock/combined-transformation failures remain
open; no global green claim. No staging/commit/push before whole goal AND plan.

Final read-only hygiene: framework diff --check exit0,35 existing LF warnings,
no other findings. Both repositories staged0; no live bounded-exact/worker-exact
dotnet process. Changed helper/unchanged assertion source hashes still match
the tested identities. Current native game.sav SHA256:
3D12A021826DD7399AB30351C55AD04F02335038552BC9DD700E84BC51CE9371.
No executable edits followed final targeted runtime verification.
