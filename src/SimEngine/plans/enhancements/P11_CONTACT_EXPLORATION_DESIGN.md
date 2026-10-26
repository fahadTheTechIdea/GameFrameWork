# P11-D13 — Engine-owned timed contact exploration

2026-10-08. Scoped implementation verified:117 checks/ONE exact NEW bounded case0 failures/aborts,
462ms,FIRST attempt; ONE clean --no-restore build12.36s,0 warnings/errors. Full P1–P12 goal remains
active. [Evidence](../../docs/migration/P11_CONTACT_EXPLORATION_VERIFICATION.md).

## 0. Problem statement

Standard/SpatialSensors.cs owns live observation; CompanyKnowledge.cs owns remembered observations.
Only authored work completion currently records discovery. A host must otherwise orchestrate regional
exploration. Add an optional engine mechanism, not a host fog map or another knowledge/work/clock owner.

## 1. Goals

1. Master-selected profile JSON declares stable contact entries, actual regions, knowledge subjects,
   positive observation durations and explicit retain/reset interruption policy.
2. Machines physically enter regions; their actual named sensor must observe their actual footprint.
   Ordinary Update spends game time before remembering a subject. No grant on placement or view read.
3. Existing knowledge ledger owns learned data and original visibility policy/cursors reveal it.
4. Private immutable diagnostics, native partial saves, pause, interruption and independent sessions.
5. Adding a compatible exploration provider needs contracts/content, not a coordinator special case.

## 2. Design

Optional ExplorationModule requires only CompanyKnowledgeModule. Other selected modules provide actual
presence/sensor capabilities; no mandatory Work/Logistics/economy/coordinate provider dependency.
ExplorationSystem visits actual IExplorationSource capabilities in stable entity/component order420,
after native movement410. It issues a source-specific ExplorationUpdateContext from the sole clock.
Native ContactExploration owns only unfinished observation WorkProgress per stable authored entry;
the original ledger owns known membership. There is no learned flag, expiry table or second clock.

ContactExplorationProfileDef alone owns immutable shared entries/states and resolves original
SpatialRegionDef and KnowledgeSubjectDef. ContactExplorationDef selects that actual profile and named
sensor; no copied per-machine policy/configuration. Entry subjects must
be first-learned, not auto-refreshed geological readings; explicit authored facts describe discovered
areas. Typed spatial first observations remain subject to their actual CheckTarget. Positive duration
conversion is validated before Update. Current source footprint must be fully Inside its region and
observable through the actual named ISpatialSensor. Subject.CheckTarget/Observe/ledger.CheckRecord
retain original owner/provider rules. Pure predicates use existing scoped observation guards.

Eligible entries advance one actual interval. Completion captures through the original native provider
and ledger, then clears unfinished progress. Already-known entries are skipped and clear any unfinished
progress; learned membership is never mirrored. Reset interruptions clear elapsed observation; retain
interruptions pause it. Dwell can accumulate while moving inside the region, never while outside or
sensor-disabled. This is contact exploration, not a remote sensor sweep or material-consuming survey.
Entries are independent, canonical by stable ID; progress belongs to the source, not its company table.

Native source participates in existing IEntityStateSource and authored sampling state, settled by
Entity.Update. The system alone advances its progress; state/view reads are pure. Every provider owns
its existing component codec. Native context guards reentry/repeated same-update callbacks; context is
engine-issued permission, not a timer. External source callback conformance is not hostile-mod rollback.

## 3. API surface

IExplorationDefinition validates declared binding against its actual world. IExplorationSource exposes
pure ReadExploration(World), native validation and AdvanceExploration(ExplorationUpdateContext).
Immutable ExplorationSnapshot/entry facts expose source/entry/region/subject IDs, duration/required
steps/progress, known membership and typed current blocker, never observed payload or foreign policy.
ExplorationView reads or copies actual owned providers, with NotFound/null on foreign/missing targets.
Existing ConfigureOperationCommand controls the real sensor; ordinary MoveVehicleCommand drives travel.
No instant reveal command, new move/work queue, collector ledger or second control settings owner.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| New | Standard/Exploration.cs | 288 | high |
| New | Standard/ExplorationModule.cs | 60 | medium |
| New | Views/ExplorationView.cs | 45 | medium |
| Modify | Kernel/SaveGame.cs | 1 | low |
| Modify | Tests/LiveSensorTests.cs native factory assembly | 8 | low |
| New | Native contact profile/region/knowledge/visibility JSON documents | 35 | medium |
| New | Tests/ContactExplorationTests.cs | 240 | high |
| Modify | Program.cs/resource list/older strict identities | mechanical | low |

## 5. Error handling matrix

| Condition | Outcome |
|---|---|
| Missing/duplicate entry, region/subject/sensor/space mismatch | Content/composition error |
| Zero/negative/overflowing dwell or invalid interruption | Explicit authoring refusal |
| Refreshable subject | Refuse; no implicit repeated sampling policy |
| Outside/unsupported region, inactive sensor, target check refusal | Typed blocker, reset or retain |
| Foreign diagnostics | NotFound and no facts |
| Bad progress/key/count on decode | Original component rejects; OnLoaded validates time bounds |
| Reentered/repeated/out-of-phase native advancement | Contract error; no extra earned interval |

## 6. Backward compatibility

Development-only direct authoring; no legacy reader/default/shim. Build2.262.0/save140; new optional
component/system/view1, unchanged existing payloads. Older strict identity assertions compile only.

## 7. Verification

ONE exact new bounded case: `Contact exploration: physical regional discovery and saved sensor dwell`.
Native master startup, actual movement/control, no raw spawn/stock/knowledge/progress. Two independent
entries demonstrate reset and retain, private/known region visibility, no free survey, actual sampling
entity state, paused partial saved twins, permanent remembered facts and independent sessions.
Reset/retain interruptions are exercised through actual sensor disable commands; unknown outside-region
interruption, multiple-source sharing and external/non-tile provider runtime are NOT proven here.
Invalid content and forged context refusal inside the same case. Physical stages bound1000 Updates
with clock/position/progress/blocker diagnostics. One build then exact case; repair/rerun only observed
failures. Freeze code/proof/JSON/tests after green, then evidence/master/parent/AGENTS only.
No old/family/full/platform/Oil suite; P6 full gate remains awaiting explicit authorization.

## 8. Risks

Progress must be unfinished observation only; do not duplicate known membership. Check company/subject
ownership at capture; physical sensor coverage is not permission to read arbitrary private layers.
Remote scan/cell-wide exploration is a separate remaining provider breadth, not claimed by this slice.
Read-only diagnostics do not initiate movement, discovery or time. Load binds/validates actual providers
after original ledger restore; no generation/reveal on load. Session-local cursors remain derived.

## 9. Out of scope

Remote automatic sweeps, sharing/forgetting, generalized elapsed-resource consumption, external provider
runtime/fault/global purity/performance/replay/lockstep/stress/full P11/P1–P12 acceptance, business/Tycoon,
OilFieldDays, hosts/demos/Draw/Godot/Unity and publication before full completion.

## 10. Execution order

1. Define immutable contracts/context/native subject-region validation and unfinished source codec.
2. Register optional contract-only update owner/private view and native sampling entity state.
3. Author native master-selected region/knowledge/visibility fixtures and normal player proof.
4. Build once; run only the exact new bounded case. Repair observed failures only.
5. Freeze sources; record every invocation/scoped source inventory and update authoritative trackers.
