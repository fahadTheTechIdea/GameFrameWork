# P11-D10 actual entity sensor sight — scoped verification

## Current M100: one target spatial read and original provider errors — 2026-10-22

Native **2.736.0/save161/replay2**, preserved dirty `master` base
`cb61483ab81404c11944931aedb4703ba694edb5`. Engine-only; no platform, Draw,
consumer/Oil/marine or business-rule changes. M99 was concrete progress; this slice
reviews the remaining P11.026 boundary rather than repeating fleet or pressure variants.

### Demonstrated defect and sole owner

EntitySensorVisibilityDef.AllowsEntity used a separate presence.Capture/try-finally
interval. Null or foreign captures became ordinary invisible results, and a callback
that touched its entity and threw lost its original exception to finally verification.
Direct invocation also missed the whole target's private sibling state. The real native
visibility/lookup/count/list/inspect paths reproduce these defects on2.735:729passed/
60failed, not an inferred search result. Blank snapshots already fail construction;
the direct-rule path still lacked native session fault latching for that exception.

That interval is deleted. The rule now calls existing EntityObservation.ReadSpatial,
the same owner used by entity projection: actual presence, complete target entity codec,
selected space, clock, non-null/space consistency and first-error latching. Existing
KnowledgeVisibility, sensor coverage/control, content bindings and entity state remain
authoritative. No alternate geometry reader, visibility cache, scheduler, saved field,
codec, resource model, clock or compatibility path. Global build identity changes;
save161/replay2 and actual component/service/view schemas do not.

### Actual contract and physical coverage

Existing SensorEntityObservationTests.Run is extended once, not copied. Its original
master-listed point sensor and native logistics physically move/load/deliver1supply,
remove the route, reveal the foreign contact and continue equal saved twins8steps.
Unsupplied interleaved session stays unchanged. Existing positive stock, distance,
handling, duration and stage bounds remain; no injected inventory or shortened travel.
The new recorder starts before the original inputs and verifies complete **0-to17,
2commands/17checkpoints**, expected/actual hash **112C5F1A92225CBF**. The positive
chronology/hash is identical before and after the native repair. The original game
is not replaced with a loaded twin. Existing20sensor-side callback contexts also run
inside this shared body; their old case name is not invoked separately.

PointPresence receives transient negative instrumentation in its existing external
proof assembly, not a new presence implementation. Two existing proof files change;
no new proof class replaces positive geometry, no negative field enters its codec.
The existing CompletionProbe type/module is explicitly authored on the contact too.
Two real supplied whole saves at9and17 ×7fault modes ×5paths =70target contexts:
null, foreign, blank, throw, touch-and-throw, private-sibling-and-throw, private sibling;
lookup, count, list, inspect and direct original contract diagnostic. The final checks
verify once-only capture, original thrown cause where applicable, no list/inspect prefix,
terminal save/hash/update refusal, preserved first fault/sole clock/retained snapshot and
interleaved isolation. Direct rule access/arming is negative framework diagnostics,
not host gameplay. This is not global CLR rollback or arbitrary-provider qualification.

### Every build and runtime attempt

Artifacts: `artifacts/optional-module-durability/34b33ca696ce4b6a86ccb51e029c2bcb/`.
All three builds use only
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
All exit0/zero warnings/errors; reported14.25/13.15/5.75seconds, outer14593/13500/6095ms.

Runtime command form, literal only:
`dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact '<literal>' --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.

| Image / log / literal | Outcome | Suite / observer ms | Scope |
| --- | --- | --- | --- |
| Build1 / targets-before.log / Sensor targets: invalid non-tile captures preserve first errors and full physical replay |729/60/0aborted,exit1|704/844| Actual pre-fix failures; complete positive replay still matches |
| Build2 / targets-current.log / same target literal |1001/0/0aborted,exit0|717/854| Native shared-read fix and all70target/20sensor contexts |
| Build2 / native-sight-current.log / Entity sensors: physical sight, scoped snapshots and shared rule ownership |79/1/0aborted,exit1|1971/2110| Old exact oracle expected NotOwned; existing command returns NotFound for foreign control |
| Build3 / native-sight-final.log / same native literal |80/0/0aborted,exit0|1988/2127| Exact private refusal expectation corrected, healthy tile-sensor behavior retained |

All four processes actually exited; stdout/stderr untruncated. No failures are relabeled
passing. Two actual complete replay verifiers run in the two target-literal attempts;
the native tile case has no replay recorder and is not represented as another verifier.
Latest selected total **1081/0/0**, two distinct focused scopes, not full regression.

The native refusal correction changes only EntitySensorTests.cs: ConfigureOperationHandler
already explicitly returns NotFound for absent/dead/foreign/unobservable targets; its
Operations.cs hash is unchanged before/after this slice. No authorization/visibility rule
is relaxed. Build3 changes only that unrelated-to-target-body test source and runner DLL
versus build2; all native/proof binaries and target-case/JSON/source inputs match.
The1001checks retain their actual build2 execution evidence; they are NOT claimed rerun
on build3 after this test-only oracle correction. The80affected checks are rerun on build3.

One explicit-path profile-info metadata call exits0:1068registered/1048included/20excluded,
zero suite bodies. All1067prior rows/bounds/reasons/settings preserved; new literal only.
Current compiled resource-caller audit uses final build3 native identities, actual450
source/PDB checks,137typed calls/14primitive writes/8layer writes and0unknown/unmapped/
indirect sites. Structural classification is not additional gameplay execution.

### Self-review, maintenance and remaining gates

Two native source changes: original target rule and build identity. Two existing test
sources, two existing proof sources and two existing JSON expectation/profile files;
949proof source/content inputs remain the same census, only those two sources differ.
No new source file/module/provider/content-document owner. Existing World first-fault,
EntityObservation, results/replay and immutable views own the affected event/error paths;
no exception is swallowed or changed to healthy invisibility. Broader service foundations
remain with the [existing operational/assurance owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
UI/deployment/dependency changes are N/A; no new sink, notification channel or clock.

Installed sdlc-orch guided existing-owner consolidation, baseline reproduction, denied
access/restore/isolation review and exact evidence. Self-review only, not CI, independent
review, performance or arbitrary mod certification. Existing design/verification owners
and master navigation are updated; obsolete duplicate current banners are replaced rather
than copied again. Original221requirements remain210source-recorded checked/11open/no
closure. P11.026's whole utility/remote/condition/occlusion/freshness/exploration/sharing/
forgetting/protected-bulk scope and P11.035's EVERY optional-module matrix stay open.
No full/default/profile/family/soak executor, consumer/Oil/marine/business/platform/Draw,
staging/commit/push. Full goal ACTIVE.

Next: assess the original remaining requirement-to-owner matrix for another actual
missing behavior or acceptance boundary, not another overlapping sensor-query variant.
Preserve conditional lot/quality scope, original stock/timing/budgets and focused-only
authorization; no parallel owners or global completion claim from this narrow result.

## Earlier D10 evidence — 2026-10-07

2026-10-07. Canonical Beep.SimEngine only. Full goal/plan ACTIVE; no commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,0 warnings/errors,4.91s.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Entity sensors: physical sight, scoped snapshots and shared rule ownership"`:
   exit0,80 checks passed/0 failed/0 aborted,467ms, first runtime attempt.

ONE build, ONE runtime invocation, ONE distinct exact NEW bounded case.100 older files/100 actual
build identity assertion lines migrate and compile ONLY. D9 fixture receives an optional explicit
master/additional-documents assembly overload shared by both tests; no simulation rule or old runtime
proof added. Source preparation initially expected CRLF, detected absent delimiter before any edits,
then normalized read text and applied the same direct replacement. No build/runtime restart/failure.
No older/family/full/platform/stale/OilFieldDays gate. After green source/public proof/JSON/tests
frozen; docs only. Historical evidence/manifests remain historical, NOT rerun.

## Single authoritative owners

IVisibilityEntityTarget optionally restricts actual entities on the same IVisibilityTarget selected
policy. KnowledgeVisibility first checks actual world/entity/alive/seat, preserves explicit observer
and own-entity permission, then conjoins every matching prototype and actual-entity predicate.
Prototype knowledge or active source metadata alone cannot authorize actual entity geometry.

SensorVisibilityDef<T> replaces the old layer-only implementation, not a second path. SAME explicit
named sensor selection/composition/runtime binding/pure Sync reads/coverage containment/derived keys
serve layer and entity specializations. LayerSensorVisibilityDef stays the existing layer contract;
EntitySensorVisibilityDef resolves actual prototype spatial metadata and pure-captures the actual
live ISpatialPresence. Geometry belongs to providers, never an entity-ID/position copy or kernel tile
switch. The old Standard/LayerSensorVisibility.cs is removed; implementation resides once in
Standard/SensorVisibility.cs. Existing PlayerView/count/list/lookup/Inspect, TileMapView queries and
GameSnapshot scoped identity differences remain the projection owners. No new visibility store,
clock, codec, system, command or knowledge grant. All existing codecs/views remain unchanged.

Build2.234.0/save136. One unchanged GameClock SHA256
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Exact bounded native player workflow

Shared native master/world/startup/work/knowledge/navigation assets plus actual master-listed
native-entity-sight/startup/permission JSON. Pure fixture assembly selects explicit authored policy/
startup profile and module documents; no test gameplay/world-generation/economy provider or raw test
spawn/stock/progress/knowledge mutation. Actual setup creates four entities: own site/machine and two
foreign instances of the same prototype at30,10 and45,10. Coordinates/world bounds are this authored
case, not engine caps or platform restrictions. Normal hold/release/work/move/operation commands and
bounded Game.Update supply all progression. Every wait has900-Update bound and actual diagnostics.

Initial scoped counts include only actual owned entities. Hidden target lookup/inspect/owner list/
spatial query/scoped snapshot all refuse/omit. Exact observer intentionally sees all; invalid seats
borrow no sensor coverage. Ordinary hidden other-company sensor control changes reveal no private
map cursor, entity count or never-seen spawn/removal identity. Read projections are pure.

Actual positive machine work travels, spends20→12 materials and captures survey through native work/
knowledge authority; partial saved twins agree. Another company's real longer sensor sees approaching
worker, but unlisted foreign site remains hidden. Normal manual move to28,10 physically admits only
near foreign instance; far same-prototype instance remains hidden. Exact counts, owner/spatial lists
and scoped appearance delta agree. Public inspect opens actual seen object, but private control view/
command ownership remains protected. Entity sight does not alter remembered geological chronology.

Normal sensor disable removes enemy sight but preserves own-entity policy, actual history and held
immutable snapshots. Lost sight emits removal for previously seen identity only; checkpoint has no
second fog codec. Re-enable reacquires current geometry and emits appearance. Real moving departure
with saved continuation removes sight/identity; retained snapshots never accumulate unseen siblings.
Interleaved startup has independent actual positions/sight. Separate authored conjunctive policy
physically covers near target but another unknown-fact restriction still hides it/count. Missing
prototype/sensor and nonspatial prototype content refuse explicitly.

## Lifecycle review and remaining gates

Skill-guided ownership review generalized layer-only selection before adding entity rules;
one generic owner and existing visibility/projection/control/knowledge authorities remain. This is
native actual-entity sight proof, NOT arbitrary providers or universal privacy. Non-tile sensors,
actual sensor signal/utility/condition gating, occlusion/weather, persistent last-seen/exploration/
sharing/expiry, arbitrary command-target/private-projection/global purity/privacy/CPU/allocation/
fault/replay/lockstep/stress/soak/full regression and full P1–P12 acceptance stay open. Visibility does
not remove physical route blockers or confer command authority. No Oil/host/demo/platform/Draw/
business gameplay changes. Publication waits for entire goal and plan.

The [scoped current source manifest](P11_ENTITY_SENSOR_VISIBILITY_SOURCE_MANIFEST.json) hashes inherited
relevant source plus actual new paths, excludes deleted layer-only source, checks local Markdown
links/unchanged clock. It is not a whole-repository audit or rerun of historical runtime gates.

Final scoped inventory:403 unique paths,72 Markdown documents,1457 local links,0 broken links and
0 current manifest hash mismatches. `git diff --check` exit0; existing LF/CRLF warnings remain.
