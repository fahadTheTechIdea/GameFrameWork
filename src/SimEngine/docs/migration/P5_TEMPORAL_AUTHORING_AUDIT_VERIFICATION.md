# P5-TA1 — Original temporal authoring reconciliation

2026-10-16, unchanged engine2.639.0/save160/replay1.
[Design](../../plans/enhancements/P5_TEMPORAL_AUTHORING_AUDIT_DESIGN.md).
Three distinct focused final cases: **2477 passed /0 failed**; no aborted/timeout suite.
Original221 requirements remain210 recorded checked/11 open; P5.005/full goal stay open.

## Implementation and evidence ownership

Extended the original OilManifestContentTests with one Timing partial and literal case.
Its existing original archive reader is extracted as OriginalDocuments, retaining the
archive identity and each document's captured SHA256 check. AuthoredDocuments now owns
the shared actual-file inventory; ResolveOil delegates to actual public OilCoGame.LoadContent
and the existing ContentSet resolver. Main and financial cases reuse these helpers.
There is no new game loader, catalog, simulator, archive copy or gameplay state.

Current facts use resolved construction/removal/work/technology/transformation/line,
mobility/worker/capacity and handling contracts. Named company capacity is enumerated
as typed declaration metadata, not queried through a prohibited singular lookup.
Duration conversion uses original GameTime over explicitly checked one-minute metadata;
it creates neither a World nor a clock. Financial amounts/checks and all game assets
are unchanged. Historical JSON is test provenance only, never runtime fallback content.

## Complete fact inventory

Seven immutable original documents: structures, vehicles, networks, work, production,
tech and scenarios. Full expected/current fact key sets and values agree:

| Coverage | Actual inventory | Facts |
|---|---|---:|
| Construction |18 structures,34 staffed nodes; bindings, node count, duration, roles, prerequisites, executor|172|
| Removal/recovery prerequisite |18 one-node removal workflows with actual construction binding|108|
| Ordinary Oil work |10 original workflows;14 original nodes→16 declared nodes after explicit drilling splits|84|
| Research |16 studies, nominal points→days, bound capacity executor and complete technology prerequisites|112|
| Corridors |8 actual segment workflows, including1.5h rational pipe rates and marine roles|48|
| Batch production |21 original recipes, exact duration/mode/actual entity output|63|
| Deliberate new disposal |Existing continuous disposal classified separately, not substituted for an old recipe|1|
| Research capacity |Original lab12 points/day→same nominal12 in oil.research pool|2|
| Machines |17 original movement speeds, work ratings and role sets|51|
| Physical handling |4 actual carrier bindings,36 declared resource rows; original capacity basis, full/half loading/unloading|184|
| Session profiles |6 explicit minute quantum/frame/pacing selections|24|
| Total |No generated worlds or updates|849|

Drilling's original complete stage becomes casing_cementing at unchanged24h/36h and
machine role; a dependent zero-duration complete effect follows. This is an explicit
authored migration, not a tolerated old graph or a total-duration-only comparison.
Both onshore108h and offshore180h productive graphs remain exact.

The original omitted worker rating explicitly defaulted to1 in the archived WorkerDef.
The current engine instead authors1 and the audit compares it; no new runtime default.
Old research used hourly point allocation. Nominal study effort and capacity are preserved,
not a claim that its obsolete hourly scheduler's exact notification boundary survives.

Old load/unload was instantaneous. The required new physical policy is explicitly one
hour at original carrier capacity (40/120/400/120), half-hour at half load. Native
CheckHandling verifies both phases for all36 currently declared support rows. The
support-row inventory comes from explicit current authoring, not a historical support
matrix that never existed; this is not universal cross-resource compatibility proof.

Eleven separate in-memory drifts are detected: build/removal/work/recipe/research/line
duration, drilling dependency, movement speed, worker rating, handling duration and
profile quantum. No normal game JSON is edited by the test.

## Targeted build and every attempt

Framework working directory:
`C:/Users/f_ald/source/repos/The-Tech-Idea/Beep.SimEngine`.

Only build command:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v minimal`.
First5.31s fails two CS1503 errors in new proof: Resource(string) is not the catalog
lookup API. Use existing TryResource and actual ResourceId, with explicit refusal if
missing. Second5.93s clean; no stale binary runs after the failed build.

Initial temporal runtime10/1,363ms suite/525ms process, child26560, exit1/untruncated:
the new proof queried named ICompanyActivityRateDefinition as singular. Original
EntityDef correctly refuses that. Enumerate actual typed declaration inventory; no
engine API is weakened or new single-capacity assumption added. Third build5.40s clean.

Final exact invocations (each60000ms child bound,5000ms termination, output caps16000
for temporal and12000 for the other two):

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil temporal migration: original work graphs machine ratings and explicit physical handling are preserved" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 16000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil financial migration: immutable original amounts credit bindings and schedules are preserved" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 12000
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil master content: every actual document has one owner and resolves through native contracts" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 12000
```

| Exact case | Final checks | Child | Suite/process ms |
|---|---:|---:|---:|
| Temporal authoring |871/0|5816|638/766|
| Financial authoring, affected shared readers |648/0|11456|428/559|
| Master authoring, affected shared inventory |958/0|29028|387/518|

All final children Exited/exit0, untruncated, no timeout/abort or outstanding worker.
Financial/master checks overlap in wall time; no controlled performance claim. Financial
facts remain624 with8 detected drifts. Master remains22 modules/40 documents/654 definitions/
229 native generic definitions/6 profiles and7 intended refusals. Counts are current observed
checks, not substituted historical totals. Initial10/1 is retained, not added to2477.
No full/profile/family run, physical workflow rerun or deadline increase.

## Current identities and remaining requirement

Core444 path/content SHA256 remains
`0327e6ff4473656c75e2e8a889d804e5cc8d8029b027065aa772b5bb27b7973a`.
Engine DLL remains`E811B410F353520502501CF34A6E848B47EB67F0B72DEA42E1E645D3ADA0A6D7`;
actual OilCo.Game remains`2E0C64518B20CA4CCB0AB1FC459BC52D6DE3BACC18742F97C58CD652049DFFA1`.
Framework test DLL:
`06125F778C5C617470DD3606834C8449744C0B6462D2A5DBC570669CC65EA6B7`.
Main/Financial/Timing source hashes respectively:

- `0126B2E9987390666981200279B92ABA678621DB8829C1A46F6AD2E3AB805AF9`
- `24B4DEA5EC92FD7B5A4351BD43921120F3CC77296D9448352321426FBEB20601`
- `16CF00594124AE0681F1488978D73A01A73080C0D744F969810742D476EB9B56`

Existing canonical manufacturing, supplied lab/grid/research, survey, drilling, loaded
recovery and rival proofs remain separately scoped evidence; they are not rerun or
added here. No production or canonical-data mismatch was found in this fact inventory.
Static authoring is not physical liveness or all dynamic route/environment/modifier gates.

The original marine workflow remains a concrete P5.005 gap: OffshoreTests' full survey,
vessel manufacture, construction, drilling, hauling and subsea case was compile-only in
P5-A46. Its source still uses a potentially79-world seed search and long native stage waits.
Do not count the short field-classification proof as that workflow. Next improve its
existing bounded stage/fixture path without a duplicate sea driver or shortened work,
then verify the relevant physical stage. P5.008/full-profile/replay/soak gates remain open.

The lifecycle skill required staged source/evidence/tracker reconciliation, not overriding
the user's focused-only rule. No original criterion/checkbox/audit wording, production,
business rule, platform/Draw/adapter/host gameplay or memory change. No staging/commit/push
before the entire goal and plan finish. Full goal remains active.
