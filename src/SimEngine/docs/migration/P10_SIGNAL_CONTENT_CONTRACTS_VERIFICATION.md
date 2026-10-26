# P10-C8 — Open signal descriptor content verification

## Current M101: shared inventory, network and utility metadata observation — 2026-10-22

Native **2.737.0/save161/replay2**, preserved dirty `master` base
`cb61483ab81404c11944931aedb4703ba694edb5`. M100 was verified progress; current review
selects the three existing signal-binding owners under P10.022/P11.026 rather than more
sensor/fleet variants. No conditional quality/lot provider or financial feature is invented.

### One original observation owner, three existing bindings

InventorySignalBindingContracts.Observe, NetworkSignalBindingContracts.Observe and
UtilitySignalBindingContracts.Observe each had a second whole-entity capture/Verify in
finally inside EntityObservation.ReadProvider. A getter that paused/touched/corrupted
private state and then threw lost its original error to that finally verification.
Actual before cases reproduce **six lost causes per binding,18total**: each of three
combined modes in fresh and restored sessions. This is runtime evidence, not only a scan.

All three manual intervals are removed. Existing EntityObservation.ReadProvider now
owns the same whole target entity through observeTargetState:true, metadata provider and
clock. Existing named phases inventory.signal-metadata, network.signal-metadata and
utility.signal-metadata own both callback exceptions and verification failures. Plain callback
throws no longer use the unrelated outer generic phase; the first cause itself remains
original. No compatibility phase branch, swallowed exception or healthy fallback.
Existing alias/schema/consumer/inventory/network validation remains in each binding owner.
No new signal algorithm, provider, quantity ledger, topology, work queue, codec, service,
runtime loop, saved field or clock. Selected-space/global CLR purity is not newly claimed.

### Existing tests extended, no duplicate registry entries

The original three exact cases and original Native/Independent physical drivers are
extended in place. No new case alias, class, document, component or content module.
Each negative matrix is now7modes ×fresh/restored =14contexts,42across the three owners.
Existing pure throw/pause/touch/private probes remain; three combined modes extend their
existing authored metadata_fault and installed negative getters. Probe Update stays empty.
Tests verify original callback marker, named first phase, denied read/save/hash and stopped
sole clock/unchanged first fault. Negative installation is framework protocol instrumentation,
not host gameplay or a production authoring default.

The original positive games remain alive through complete recording, actual physical work,
saved twins and final hash. Native and independent bindings share identical chronology and
game hash. Replays now explicitly compare actual final live hash, not Success alone:

| Binding | Actual native work, unchanged quantities/timing | Initial → final / commands / checkpoints | Native and independent hash |
| --- | --- | --- | --- |
| Inventory | Machine travels4→20, response consumes8of20,12remain; saved repair/pause/numeric-only/private facts |0→32 /2 /32 each|EEFEFF058CBB334D|
| Network | Rate-limited4-per-minute injections earn8pool/22source and backpressure; physical depot haul5 does not create pool stock |0→28 /1 /28 each|7FC1298055748CDC|
| Utility | Real initial2fuel runs out, timed physical8-unit refuel, original32-minute manufacture burns8total and earns1output |0→90 /5 /90 each|2F30D62A96AFE94A|

All six final replays actually verify those final live hashes. Before-image positive
chronologies/hashes are the same; six before verifiers also run, not a passing whole case.
Original stock, rates, durations, geometry, work and failure bounds are not shortened.
Selected network/utility cases explicitly omit Tycoon; business source/rules are untouched.

### Every build and literal runtime attempt

Artifacts: `artifacts/optional-module-durability/d7f72c1b7f004323adbdc65c5c53dded/`.
Two clean builds, each only
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Build1 reports14.61s/outer14913ms; build2 reports11.71s/outer12052ms; both exit0,
zero warnings/errors. Three stale literal154 save assertions migrate before the baseline
to the existing independently authored ExpectedSaveFormat. No native schema is fabricated.

Only three distinct original literals, each once before and once after the coherent repair:

- `Inventory signal bindings: independent JSON facts follow physical stock and saved recovery`
- `Network signal bindings: independent JSON observes timed flow, backpressure and physical hauling`
- `Utility signal bindings: direct JSON observes timed fuel, physical refuel and saved manufacture`

Each uses the runner DLL with --bounded-exact and unchanged60000ms timeout,5000ms termination,
30000 output chars. These bounds match the existing three profile assignments. No full,
default, profile, family or soak executor. All six actual processes exit; no truncation.

| Log / build | Checks passed/failed/aborted | Suite / observer ms | Exit |
| --- | --- | --- | --- |
| inventory-before /1 |256/6/0|795/932|1|
| network-before /1 |249/6/0|2462/2591|1|
| utility-before /1 |241/6/0|2596/2736|1|
| inventory-current /2 |262/0/0|809/956|0|
| network-current /2 |255/0/0|2377/2507|0|
| utility-current /2 |247/0/0|2621/2759|0|

Final **764passed/0failed/0aborted**, all on the same final source/binaries. No executable
edits afterward, no unrelated old green reruns. Failed before cases remain failed records.
One explicit-path profile-info call exits0:1068registered/1048included/20excluded/zero
suite bodies. Profile JSON is byte-for-byte unchanged: no new entry, exclusion, limit,
reason or profile setting. Current compiled resource audit against final build2 rechecks
450source/PDB identities,137typed calls/14primitive writes/8layer writes and0unknown/
unmapped/indirect sites; structural classification, not additional gameplay execution.

### Self-review, documentation and remaining scope

Four native sources change: three existing binding owners plus build identity. Three
existing test sources, three existing proof sources and one existing protocol expectation
JSON change. Same450native/949proof input census; no new production JSON or source file.
Existing World/EntityObservation first-fault, results, replay and immutable views own
the affected error/event paths. No duplicate logger or swallowed failure. Broader
operational foundations remain in the [existing service/assurance owner](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).

Installed sdlc-orch guided existing-owner consolidation, meaningful before/after checks,
privacy/restore/first-error review and scoped claims. This is self-review, not CI,
independent review, allocation/performance, arbitrary-provider or global rollback proof.
The three P4 binding design/verification owners link this shared result; obsolete current
navigation is replaced, not copied into another fix/tracker document. Historical records
retain their actual images. No consumer/Oil/marine/business/platform/Draw/adapters or
staging/commit/push. Original221requirements remain210source-recorded checked/11open,
no original closure; broader P7/P8/P10/P11 gates and full goal remain ACTIVE.

Next: assess the original remaining requirement-to-owner matrix for actual missing
behavior or acceptance, not more overlapping versions of these42metadata faults.
Keep conditional lot/quality scope and all original requirements/bounds; no second
provider/stock authority or full-plan claim from this finite three-owner consolidation.

## Earlier C8 descriptor evidence — 2026-10-09

Scoped verified 2026-10-09; full P1–P12 and the active goal remain open.

## Ownership and implementation

IAutomationSignalContent extends IContentDefinition for authored kind, metric, unit and actual provider schemas. IAutomationSignalDefinition remains the distinct entity capability declaration. Native and independent descriptors use one AutomationSignalDeclaration JSON parser/resolver and one AutomationSignalContracts admission/reader/snapshot validator. Native AutomationSignalDef execution methods were deleted, not retained as compatibility wrappers. Engine consumers and public source callbacks now accept the contract: automation, profiles/views, execution and network gates, work requirements, condition/inventory/incident/utility/network/resource/transformation providers.

OperationsModule admits actual owned descriptor aliases and actual declared provider schemas before Initialize. The same existing unit/resource catalogs and quantity contracts remain authoritative; this slice does not claim a unit/resource catalog-contract migration. Source session identity, immutable typed snapshots, existing CaptureProvider/Sync observation, first fault latch and saved provider/control/work state remain unchanged. No new service, scheduler, state store, codec, stock path, cache or clock.

Master-listed signal-contracts.json supplies independent Boolean incident, numeric sensor and exact resource descriptors without AutomationSignalDef inheritance. The native incident rule, independent numeric rule wrapper and native work signal requirements resolve these descriptors. Shared existing test authoring/helpers provide one thin player workflow; driver code never reads World or writes gameplay progress/resources. Negative fault injection is explicitly an extension command on the existing probe, not player-side gameplay logic.

## Build and runtime evidence

All builds use `dotnet build Beep.SimEngine.slnx`. Attempts in order:

1. Failed2.55s, zero warnings/three errors: new validation assumed IUnitDefinition/IResourceDefinition were IContentDefinition. Replaced those checks with actual identities in their existing authoritative catalogs.
2. Clean6.35s, zero warnings/errors, core and callback migration.
3. Clean6.51s, zero warnings/errors, new proof.
4. Failed4.17s, zero warnings/one error: assertion used Codes.NotOrderable rather than existing StandardCodes.NotOrderable.
5. Clean4.33s, zero warnings/errors.
6. Clean4.20s, zero warnings/errors.
7. Final clean4.44s, zero warnings/errors.

Only ONE NEW exact case ran; repairs reran only that case:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Signal content contracts: independent descriptors drive shared automation, physical work and guarded saved facts" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

- Attempt1:117 passed/two failed,781ms; PID35164 exit1/observed933ms. Incorrectly expected incident-only response to be player-orderable. Engine correctly returned core.not-orderable.
- Attempt2:24 passed/one failed,479ms; PID29440 exit1/observed626ms. Looked for response order before physical dispatch.
- Attempt3:28 passed/one failed,554ms; PID6636 exit1/observed716ms. Driver compared raw content ID rather than canonical activity:pressure.response plan key.
- Final:120 passed/zero failed/zero aborted,696ms; PID29268 exit0/observed840ms. All output untruncated, no observer error. No engine behavior bypass added: assert direct preview refusal, inspect actual incident-created order after dispatch using its canonical key.

Verified independent actual aliases; original/native and external providers share descriptor reader; pure immutable typed/private facts; unavailable source reasons; actual incident order requirements; deferred emergency/restoration; real machine travel to position20, eight physical materials20→12 and time-consuming repair; pause; numeric thresholds/hysteresis/unavailable recovery with stale intent cancellation and fresh dwell; saved pending/physical/wait/settled twins; replay; fresh-session isolation.

Four fault modes through the same guarded reader cover unit mismatch, persisted hidden-state mutation, callback exception and snapshot identity mismatch. Foreign requests refuse before callback; existing first fault freezes Update/clock and prevents save/hash/further healthy reads, without poisoning another session. Twelve malformed independent JSON branches refuse before Initialize: kind, metric, unit, provider, schema version, empty/missing/duplicate providers, resource, quantity unit, resource kind and incompatible numeric provider kind.

## Protocol and limits

Core2.337.0/save154/replay1; automation5/operations-view11/WorkBoard52/WorkSystem45/work-view94/logistics26/logistics-view40 and all other state schemas unchanged. Sole GameClock SHA256 remains 35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1. Executable inputs frozen2026-10-09 02:59:20 UTC.

Historical callers/old runtime cases and QuickGame compile/source only, not rerun on2.337.0. Native AutomationSignalDef mentions in engine source remain only implementations and section registration. This is not malicious-mod isolation, exhaustive provider/network/transformation/quantity fault coverage, global purity, lockstep/soak/scale/full regression or final acceptance proof. Original primary acceptance checkbox texts/status remain unchanged. No Oil/Tycoon/platform/Draw runtime, business expansion, mandatory budget, compatibility fallback or second clock.

Full goal AND plan must genuinely finish and be verified before staging/commit/push. Publication condition is NOT satisfied.

[Stage](../../plans/enhancements/P10_SIGNAL_CONTENT_CONTRACTS_DESIGN.md), [tracker](../../plans/enhancements/MASTER_TRACKER.md), [audit](../../plans/enhancements/COMPLETION_AUDIT.md).
