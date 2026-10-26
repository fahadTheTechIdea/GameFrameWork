# P11-D9 live physical sensor coverage — scoped verification

2026-10-07. Canonical Beep.SimEngine only. Full goal/plan ACTIVE; no commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,0 warnings/errors,6.20s.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Live sensors: physical coverage, controls and remembered survey separation"`:
   exit1,53 passed,one failed assertion and one aborted suite,1194ms. Stationary fixture omitted normal
   player hold, so idle machine correctly returned home; final movement expected Idle instead of actual
   Arrived. Exact repair uses normal SetWorkerHeld commands and checks actual arrival, no engine shortcut.
3. SAME build command: exit0,0 warnings/errors,3.13s.
4. SAME exact new case command: exit0,75 passed/0 failed/0 aborted,465ms.

TWO builds, TWO SAME-case runtime invocations, ONE distinct NEW bounded case.99 older files/99 actual
build identity assertion lines migrate and compile ONLY; no old save expectations/layouts changed.
A read-only parallel metadata retrieval failed with UnknownProcessId before edits/builds; replaced
with one read-only metadata scan. No live build/runtime process was restarted. No older/family/full/
platform/stale/OilFieldDays gate. After final green source/public proof/JSON/tests frozen; docs only.
Historical reports/manifests/runtime evidence remain historical, NOT rerun.

## Single authoritative owners

ISpatialSensorDefinition/ISpatialSensor and abstract immutable SensorCoverage are geometry-neutral.
Providers own current coverage and opaque scope identity. LayerSensorVisibilityDef consumes contracts
only, guards actual provider Sync/entity/clock facts and checks active owned compatible sources.
Sensor union belongs within one authored rule; separately matching rules and layer ownership remain
conjunctive. Missing/empty/duplicate/incompatible sensor declarations fail composition explicitly.

TileSensorDef/TileSensor own native square tile geometry only. Existing ControlledExecution owns
authored enabled/range controls, signal gates, entity states and component Sync. Zero range explicitly
inactive. Native target bands/updates reject fractional/negative distances and foreign units. Coverage
uses actual TilePresence footprint, no copied position; wide containment avoids inflated-rectangle
overflow. The existing operation configure command/view remains sole control admission/commit owner.
Making shared CheckControl virtual allows provider-owned quantum validation, not scheduler branching.

KnowledgeVisibilityModule declares native component1 and sample target section. No sensor system,
clock, command, queue, fog grid, exploration store or persistent visibility ledger was introduced.
Existing map sample/page preflight and end-Update/restore scope refresh derive private cursors from
actual owned source/capability/geometry keys. Existing WorkBoard and CompanyKnowledgeService alone
own durable observation capture, stock expenditure and original/last-observed game times.

Build2.233.0/save136; new tile_sensor codec1, all existing codec/view schemas unchanged. One GameClock,
unchanged SHA256 `339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.

## Exact bounded native player workflow

Actual native-sensor-master/world/startup/visibility JSON selects native modules only, reusing existing
survey work/knowledge/navigation/retry/access documents. Three actual startup entities: owned site
with20 steel, moving worker at2,10 with range2, foreign stationary sensor at30,10 with range20. Those
coordinates/bands/48x32 bounds are explicit case content, not engine caps or platform restrictions.
No test gameplay/worldgen provider, raw test spawning/stock/progress/knowledge or economy/Tycoon.
Fixture submits normal hold/release, work, operation and manual move commands and bounded Update.

Own current samples are visible; distant field is initially unavailable. Foreign sensor can see public
terrain42 but cannot bypass private deposit ownership. Mixed known/unknown full reads refuse without
prefix values; unlisted enemy entities remain hidden. Invalid controls/foreign control and management
previews are pure. Held stationary scope is stable. Normal released work moves actual sensor, changing
coverage/cursor and invalidating old pages. Moving checkpoint/twin derives equal coverage and hash.

Physical approach reveals actual deposit31 but records no knowledge. Partial work consumes20→12 and
still earns no observation. Completed positive work captures durable31; partial save/twin agrees.
Disable removes current layer metadata/samples but preserves history and foreign cursor. Entity Update
settles authored stopped state; saved disable derives same sight. Explicit zero range stays inactive.
Re-enable restores coverage, normal manual move physically leaves the field and preserves recorded
chronology. Saved moving-away twins agree; held old coverage stays detached. Increasing range through
normal operation control reveals a previously unseen sample without granting new survey history.
Interleaved new session borrows no position/range/history. Invalid sensor identities/range/unit/space
content refuses explicitly. Case stages bound each wait to900 Updates with time/position/order diagnostics.

## Lifecycle review and remaining gates

Skill-guided review retained one native control/state codec, geometry/presence owner, selected policy,
map cursor and durable knowledge/work owner. Provider contracts permit other geometry without policy
branches, but this case proves only native tile sensors. It does NOT prove non-tile sensor providers,
actual signal/power/fuel/condition sensor gates, line-of-sight/occlusion/weather, entity sight targets,
persistent exploration, sharing/expiry/forgetting, arbitrary-provider global mutation purity/privacy,
CPU/allocation/fault/replay/lockstep/stress/soak/full regression or full P1–P12 acceptance. Keep those
gates open. No OilFieldDays/hosts/demos/platform/Draw/business gameplay changes. Publication waits for
the entire goal and plan, not this checkpoint.

The [scoped current source manifest](P11_LIVE_SENSOR_COVERAGE_SOURCE_MANIFEST.json) hashes inherited
relevant source plus actual new paths, checks local Markdown links and unchanged clock. It is not a
whole-repository audit or a rerun of historical runtime proofs.

Final scoped inventory:396 unique paths,70 Markdown documents,1449 local links,0 broken links and
0 manifest hash mismatches. `git diff --check` exit0; existing LF/CRLF normalization warnings remain.
