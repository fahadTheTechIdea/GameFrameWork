# P7-D2 resource conversion and physical spill verification

## Current M142 complete simultaneous-incident stream — 2026-10-23

Unchanged native2.767.0/save161/replay2, master
cb61483ab81404c11944931aedb4703ba694edb5 plus preserved dirty work. Previous M141 made
progress on recovery/manufacturing durability. This slice closes the specific finite M72
proof gap: a complete raised stream through simultaneous scarcity, physical replenishment,
restart cancellation and both incident waves. No new simulation subsystem is introduced.

### Existing-driver change and boundary

Only ResourceDispositionTests.IncidentContention.cs changes. Its old recordings start
AFTER each wave's raises, and Save replaces the primary between recordings. Those results
proved supplied continuations, not full raised streams. Current native baseline3638/0
still passes before editing: missing coverage, not a reproduced engine simulation defect.

The same continuation now records once BEFORE initial plan cancellation and all raises.
Save restores only the independent twin, retaining the original recorded primary. One
complete recording replaces two partial-wave recordings. The separate loaded-haul setup
replay remains. No new case, JSON, provider, registration, profile, gameplay, native code,
codec or protocol change. No original accounting/physical/privacy assertion is removed.

Retained checks cover whole scarce bills, source/carrier/scoped-buffer stock plus classified
destruction, machine/bay exclusivity, timed cleanup/restart, NoStock waits, physical refills,
saved partial/wait/completed twins, private episodes, monotonic order IDs, cancellation
without repeated cleanup, cumulative history after eviction and session isolation. Both
variants retain20 supply/10 spares, four sources/two waves,180-update stage bounds,32MiB
per-update/1GiB total allocation budgets and60000ms process bounds.

New assertions bind actual initial/final identities, all15 commands, every earned update,
description/recorder counts, verifier step/expected/actual/restored-game hashes to the live
completed primary. Raises use existing owned RaiseIncidentCommand/handler/IIncident
admission; the driver does not mutate episodes or implement natural hazard propagation.

### Every current build/run and actual evidence

One clean framework-test Release --no-restore -v quiet target build:12.34s/12690ms observer,
zero warnings/errors. Live build36178 was polled to terminal exit0 before runtime. Each
exact literal below uses the existing net8.0 tests DLL, --bounded-exact, --timeout-ms60000,
--termination-timeout-ms5000 and --output-chars50000. No full/profile/family/soak executor.

| Exact literal | Checks/failures | PID | Suite/observer ms |
|---|---:|---:|---:|
| Incident contention: simultaneous local responses preserve scarce physical supply saved episodes and allocation budgets — baseline |3638/0|10340|4120/4258|
| Incident contention: simultaneous local responses preserve scarce physical supply saved episodes and allocation budgets — strengthened final |3644/0|14168|4382/4520|
| Incident contention: simultaneous scoped independent effects preserve half-minute physical supply saved episodes and allocation budgets — strengthened final |6994/0|23516|5821/5961|

Two affected final literals **10638/0**, three actual invocations. No failure, truncation,
raised bound, restart or unrelated green rerun. Baseline is excluded from final counts.
No executable edits after final runtime; documentation/identity checks only.

| Actual incident recording | Initial/final step | Inputs/checkpoints | Live/verified hash | Peak/total Update bytes |
|---|---|---|---|---|
| Native minute |155→590|15/435|8C9501BCD1532801|1237120/120743952|
| Independent scoped effect, half-minute |307→1162|15/855|84E24FDC5B3DC49C|1102808/191900048|

Each has eight actual episodes and exact16 supply/8 spare classified destruction. Native
baseline435 updates/1236688 peak/114057296 total. Complete recording adds measured
allocation but still passes original budgets. These begin at the actual supplied partial
plan, NOT game creation at0. All eight raises, four refills, two restart cancellations and
initial plan cancellation are inside each15-input stream. Counts/hashes are printed
current facts, not historical substitutions or equal-across-quanta claims.

FOUR verifier executions in the two finals: one loaded-supply setup and one complete
incident stream per variant. Baseline executes its original three (setup plus two partial
waves): seven executed in this stage, not seven complete incident recordings. No lockstep,
natural propagation or arbitrary-provider certificate is implied.

### Preservation, self-review and remaining work

[M142 artifacts](../../artifacts/optional-module-durability/651a9fe5e24e48be916309a4ce7b7042/)
retain2260 baseline inputs, tested/final identities, build/three runtime logs and original
gate/structural records. Only ONE existing test source and its DLL change. All453 native
source/project/DLL/PDB and actual Oil source/content/binaries are preserved; no consumer
build/runtime. Earlier450source/138typed calls/14primitive/8layer-write compiled audit
is reused after preservation, not fresh. Profile/content/case registration unchanged.

SELF-REVIEW: one recorded primary, independent loaded twins, original public commands/views;
no copied stock, claims, episodes, retry policy or clock. Original first-fault/results/
journal/notifications and allocation reader remain authoritative. No extra logger/audit/
notification owner; headless UI N/A. Existing operational mapping remains in
[P9 services](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
Independent review and broader provider/channel/fault behavior are not claimed. An initial
documentation patch failed context validation without writing files; corrected against
actual text. No source edit or test rerun resulted from that documentation-only failure.

The finite incident matrix row now has complete raised-stream durability for these two
declared variants; remove that specific gap from active instructions. Original221/210
source-recorded checked/11 open stays unchanged. P7.017 broader accounting/fault obligations,
conditional P7.019, P11.029 pressure/propagation/lockstep and P11.035 other optional-module
durability stay OPEN. Next audit actual remaining declared caller/fault branches, not
another working response engine or hypothetical future-provider expansion. No staging,
commit or push before the whole goal and plan finish.

Current native2.767 follow-up: [M136 flow observation, physical topology contention and native spill recovery](P7_TOPOLOGY_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-m136-native-flow-observation-and-physical-contention--2026-10-23).
The lookup purity fix below remains in its original owner; its2.766 results retain
that actual historical image. No blanket requalification or original gate closure.

<a id="current-m135-scoped-endpoint-resolution-before-accounting-admission--2026-10-23"></a>

## Earlier M135: scoped endpoint resolution before accounting admission — 2026-10-23

Native **2.766.0/save161/replay2** on master base
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. This is a
verified defect/fix in the original P7.acceptance.017 fault boundary, not closure of
that whole accounting/stress gate. The preceding M134 freeze matched all2227 inputs
before changes. No original acceptance words or checkboxes change.

### Cause and single ownership

`IResourceEndpointProvider.Resolve` already promises a pure lookup. The original
ResourceTransactions.Resolve called it before capturing endpoint stock/receipts.
A scoped provider could therefore mutate its own stock or the game clock, return
an endpoint or null, and receive healthy conversion admission/refusal. The new
literal reproduces **106passed/8failed** on unchanged2.765: stock and clock changes,
present and missing endpoints, for both EvaluateConversion and CommitConversion.

The same resolver now uses the existing EntityObservation.ReadProvider boundary
over the actual provider's codec and clock facts, including null returns and
returned identity validation. Original callback exceptions retain their identity;
existing World first-fault/results/history/save cutoff remains responsible. There
is no new ledger, endpoint registry, transaction service, recovery owner, clock,
compatibility path or rollback fiction. Scope/World remain immutable composition
metadata. The entity-endpoint lookup is unchanged.

Initial use of the guard exposed legitimate in-flight native states. NetworkService
Sync still refuses interrupted checkpoints/normal hashes; its **observation-only**
path fingerprints actual staged pools using their existing Inventory.Sync alongside
the original live graph. TransformerManager's stateless system Sync observes actual
WIP-owning entities using their existing entity/component codecs during observation;
normal stable validation and OnLoaded retain ValidateLive. Named WIP capabilities
are enumerated from actual components, not fetched without an identity. No saved
payload, codec version, module version or view schema changes. Engine build identity
advances to2.766; old saves are not header-rewritten or made compatible.

### Checks and retained failures

The existing EndpointPools gains only an unsaved negative ResolveObservation hook.
The existing ResourceTransactionFailureTests owns the new literal. Actual existing
stock is seeded through the original transaction protocol, a real input receipt is
acquired and saved, and independent loaded branches exercise healthy, missing,
stock-changing, clock-changing and original-throwing lookups. These are explicitly
**negative/protocol instrumentation**, not public-player physical acceptance.
Faults retain actual illegal stock changes without consuming the input, acquiring
output receipts or publishing success; caught errors cannot resume, save or hash;
Update earns no time and another session remains unchanged. Healthy conversion and
absence remain typed, and a healthy commit settles original events before saving.

Five targeted test-project Release builds are clean:15.81,11.26,8.34,7.49,7.70seconds.
All fourteen literal runtime attempts are retained. The first new-case attempt
57/5 also exposed a fixture checkpoint before pending events settled; its one
ordinary Update correction precedes the complete106/8 baseline. The first guard
image reproduces original WIP1/1 and topology12/1 in-flight validation failures;
the next image retains a named-capability lookup mistake1/1. Those failures are
not relabelled as passing, hidden or excluded.

Final build5, no subsequent executable edits:

| Exact case | Passed/failed | Suite/observer ms | PID/exit |
| --- | --- | --- | --- |
| Resource endpoint resolution: conversion admission rejects resolver stock and clock mutation including missing endpoints |218/0|231/358|22456/0|
| Resource transaction failures: direct provider violations are terminal at the original owner |224/0|239/375|26760/0|
| Resource conversion: timed transformer completion and provider fault boundaries |100/0|347/482|16964/0|
| Network topology: saved multi-network ownership and current-state execution |30/0|348/476|12328/0|
| Manufacturing accounting: physical supply blocked WIP return and saved catalyst wear conserve actual resources |1022/0|3030/3167|23000/0|

**1594/0**, five individually bounded literal cases, not a family/profile/full run.
Commands use the existing Release/net8.0 tests DLL, `--bounded-exact` with the exact
table name,60000ms timeout/5000ms termination/50000output characters. Build command:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
No bounds or assertions are relaxed. The physical manufacturing case remains an
ordinary JSON-authored player workflow: actual hauling, partial WIP, physically
blocked return, cancellation, refill, reusable-tool release and earned catalyst wear.
Its one complete replay verifies0→314,7commands/314checkpoints, expected/live and
verified hash **2FC40ECC2CB7F998**. Protocol fault branches are not counted as replays.

### Current census, profile and limits

Fresh compiled audit matches450 native source/PDB checksums, **138typed calls**,
14primitive writes/8layer writes/zero unknown, unmapped or indirect sites. The one
new call versus137 is `NetworkService.Sync` invoking the **existing endpoint-codec**
for staged pool observation; no new material mutation or semantic resource owner.
DLL `071EDA70FA9D6783089B8101090A538A29DFFD458B5E72B14734EF841870C65E`,
PDB `56C0F3E010A819A334EC1EDE321EC803EA224C50639337BFBD2BEE94301B102A`.
Explicit-path profile metadata reports1098registered/1078included/20excluded,
**zero bodies**. Removing only the one new60000ms include row restores the exact
previous profile SHA; no exclusion or prior bound changes.

[Before](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/before.json),
[all attempts](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/attempts.json),
[final tested inputs](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/tested-build-5-inputs.json),
[fresh compiled census](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/compiled-resource-audit.json),
[classified call delta](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/semantic-call-delta.json),
[profile preservation](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/profile-preservation.json),
[original gates](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/structural-checks.json),
[frozen inputs](../../artifacts/optional-module-durability/ac4ebd78cb844202b690dbf51d865737/final-inputs.json).

Final2163 tested inputs retain zero drift:450native C#,955proof/source-content,
735test C#. Eleven existing documents are maintained; no new Markdown authority.
The first read-only file-link checker incorrectly used an empty parent for root
documents; its invalid result is retained separately. Corrected absolute-parent
scan finds no missing file targets. This checks existence only, not all anchors or
global semantic consistency. No runtime is repeated for documentation-only edits.

Self-review only. Reuse original operational owners described in the
[affected-event matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
This proves selected persisted service/clock lookup faults and affected native WIP/
topology continuity, not arbitrary private CLR state, every provider, cross-company
or material path, all incident/cargo/reference fault combinations, lockstep/fuzz/soak,
allocation/performance, CI or full solution/regression. Original221/210source-recorded
checked/11open and full goal ACTIVE remain. Next: reconcile the existing classified
caller/stage-stress matrix with these actual lookup boundaries and remaining native
provider/reference faults; do not duplicate resource ownership or rerun unrelated
historical cases. No consumer/Oil/marine/business/platform/Draw/adapter edits,
staging, commit or push.

<a id="current-m90-interrupted-recovered-cargo-reaches-timed-disposal--2026-10-22"></a>

## Earlier M90: interrupted recovered cargo reaches timed disposal — 2026-10-22

Unchanged native **2.729.0/save161/replay2**, master base HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. All450 native sources,
949 proof/content inputs and native/proof binaries match M89. Only existing
SpillResourceAccountingTests and its Cargo partial change. No observed missing engine feature
or new scheduler, stock balance, resource book, incident response, recovery service, clock,
codec, JSON rule or business behavior is claimed.

### Original gap and same-driver composition

M59 physically qualified interrupted recovered cargo through treatment; timed disposal was a
separate path. The existing disposal branch returned before the cargo-interruption branch,
so selecting both flags did not exercise interrupted cargo before disposal. Both now compose
through the same Dispose and InterruptedCargo helpers, not a second workflow implementation.
The existing receiver parameter is generalized to destination. Only the selected disposal
variant explicitly authors its original JSON storage public, because automatic recovery
discovers public ports. Treatment-only receiver authoring remains unchanged; no admission
rule is relaxed and no default port/demand is invented.

Two independently selectable native/direct contract literals use actual delayed incident
propagation to move3 of the finite5oil units into the selected quantity cell. Existing response
work receives physical steel, earns repair/cleanup, pauses and resumes. Six-minute machine
recovery returns those same3units to site stock. A normal haul loads them, an ordinary player
hold retires that job without losing/refunding cargo, and release waits for the authored
demand-only policy. Stock policy then admits one new loaded return, without a second pickup
or purchase. Saved unassigned/committed cargo, one-unit-per-minute partial unload, departure
and empty terminal receipts qualify before four-minute disposal work begins.

Partial disposal is paused, disabled and cancelled without destroying oil. A new ordinary
order earns the same authored duration and commits exactly one attributed3unit Destroyed
fact. The original2unspilled units remain, no Lost/Consumed/Produced oil balances a fabricated
sink, and the engine objective completes once. Actual per-update oil stock+cell+cargo+WIP+
explicit destruction stays5. Common deposit/withdrawal assertions now run before either
treatment/disposal branch: one3unit transfer each, not a second ledger.

All decisions, reservations, delays, motion, resource disposition and objective rules stay in
the engine. The driver submits normal commands and reads facts/saves; the reused terrain
cleanup helper also makes existing read-only framework layer assertions. It never writes
material/progress or implements a cleanup/dispatch fallback. Saves replace only independent
twins, retaining the recorded primary.

### Actual execution

ONE targeted Release build11.81s/0warnings/0errors/exit0:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.

| Literal case | Passed/failed | Suite/observer ms | PID/exit |
| --- | --- | --- | --- |
| Spill disposal recovery: native interrupted cargo completes physical timed disposal |482/0 |5708/5849 |22664/0 |
| Spill disposal recovery: independent interrupted cargo completes physical timed disposal |482/0 |5755/5900 |15108/0 |
| Spill cargo interruption: native and independent saved load returns through authored recovery |856/0 |9638/9783 |28700/0 |
| Spill disposal: native recovery hauling cancellation and earned destruction |423/0 |4781/4922 |30616/0 |
| Spill disposal: independent contracts recovery hauling cancellation and earned destruction |423/0 |5359/5508 |16736/0 |

Latest **2666/0**, five actual literal invocations on one build. Each uses `--bounded-exact`,
60000ms timeout/5000ms termination/30000-character output; normal exit/no truncation. Original
240-update/20-second physical-stage limits remain unchanged. No executable edits follow runs.
Six actual verifier calls: two new full initial0→213/17commands/213checkpoints workflows plus
four original cargo/disposal calls. New expected live/actual verified hashes respectively
**B247BEA74DE3E140** native and **A7DA1126E98C5E8C** independent. Original unprinted counts/hashes
are not fabricated. The17commands include initial incident, both supplied cleanup/control
sequences, recovery, haul, cargo hold/release/demand and disposal order/control/cancellation.

### Separate baseline gap observed, not silently waived

Read-only `--list-cases` returns1059registered cases; existing framework-validation-profile
has864entries,196missing assignments (including the two new cases) and one obsolete combined
atomic-haul name. Metadata-only `--profile-info` exits2 on that unknown name. Both commands
invoke **zero suite bodies**; no full/profile executor ran. Existing strict profile parser is
correct and remains unchanged. M91 subsequently repairs that one original profile's names/
ownership/exclusions/bounds, preserving mixed optional-mode boundaries and original deadlines:
[current metadata evidence](P6_EXPLICIT_VALIDATION_PROFILE_VERIFICATION.md#current-m91-industrial-profile-metadata-reconciliation--2026-10-22).
No profile executor or runtime gate waiver; this M90 failure retains its actual captured scope.

[Before](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/before.json),
[all attempts](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/attempts.json),
[tested inputs](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/build-1-inputs.json),
[source identities](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/native-scope-identities.json),
[registry difference](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/profile-registry-diff.json),
[original gates](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/original-gates-current.json),
[frozen inputs](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/66e470232d974ad29c47c348aeb0f39c/structural-checks.json).
Native SHA256 **0011A893B5023013489852284614EF30516D9D71B92A69C16B08A9447A0E6DD7**;
test runner **08E1E66C94A405510FFAE8B66149B3D949626CBFDCE0E748CEBFE0C3C11CAC25**.
M89 matched137-call compiled audit remains valid for identical native source/DLL/PDB identity;
it was not rerun and is not gameplay execution of all callers.

Self-review only; existing World first-fault/results/notifications and
[operational assurance](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain responsible. No logger/exception/default/dependency rewrite. CI, allocation/performance,
independent review, full/default/profile/family/soak, consumer/Oil/marine, business/platform/
Draw/adapters and staging/commit/push were not performed. Original221/210source-recorded
checked/11open/no original closure/full goal ACTIVE. This finite composed chain does not
qualify every cargo/incident/provider, cross-company competition, topology, faults, lockstep,
stress or the wider C3/P8/P10/P11/every-module requirements.

<a id="current-m88-complete-physical-manufacturing-replay--2026-10-22"></a>

## Earlier M88: complete physical manufacturing replay — 2026-10-22

[Authoritative WIP evidence and remaining gates](P8_TRANSFORMER_WIP_VERIFICATION.md#current-m88-complete-physical-manufacturing-replay--2026-10-22).
Same original finite physical accounting workflow, now recorded from initial unsupplied session
through all seven player commands and314updates, including two actually loaded/saved checkpoints.
Original primary remains alive; only its independent twin is reloaded. ONE exact1022/0, one
full0→314 verifier/final live hash47C6DA990C1D6D22. This replaces no material rules or models:
native2.728/save161/replay2/all450sources/949proof inputs and binaries unchanged. M87 other
green cases/compiled audit retain their own test image; no unrelated rerun. Earlier M70/M87
suffix recordings are not relabeled full recordings. No original gate closes or global
conservation/provider/stress/every-module claim; full goal ACTIVE, focused-only/no publication.

<a id="current-m77-exact-external-coupled-provider-qualification--2026-10-22"></a>

## Earlier M77: exact external coupled-provider qualification — 2026-10-22

[Current D10 contract proof and finite remaining review](P7_COUPLED_CONVERSION_PROVIDERS_VERIFICATION.md#current-m77-exact-external-coupled-provider-qualification--2026-10-22)
resolves the separate CoupledClaimOutlet path, not just AtomicInventory:32 actual
contexts/626/0 on unchanged M76 build2/no rebuild or executable edits. Exact partial
local/peer receipts, original tool release, unrelated claims, four attributed facts,
healthy refusal versus original first faults and saved isolation qualify narrowly.
Direct contract/callback probes are not physical hauling, replay or allocation.
Source review distinguishes recovery whole movement from authored Produced yield
and terrain from quantity; remaining classification/economic owner review stays
explicit in existing C3/M70 map. No native feature or global accounting certificate;
original221/210 source-recorded checked/11open/full goal ACTIVE. Earlier records
remain their own actual images and are not rerun as a final regression.

<a id="current-m76-physical-mixed-machine-and-material-result--2026-10-22"></a>

## Earlier M76: physical mixed machine and material result — 2026-10-22

[Current P8 result-owner evidence](P8_ENTITY_CONVERSION_VERIFICATION.md#current-m76-physical-mixed-machine-and-material-result--2026-10-22)
adds actual physical supply/capacity clearing/saved rollout refusal/complete machine
and byproduct to the original conversion matrix. Source-reviewed independent atomic
inventory, independent Pulse homes and native entity/fault baselines254/0/three
replays stay their captured M75 build2 image. New public-only literal2759/0/one
full-final-hash replay on M76 build2, original allocation budgets pass. Combined
3013/0/four replays is not one final-image regression, every provider or broad gate
closure. Native450sources/DLL unchanged; no new transaction/result/stock/clock owner.
Protocol stock/geometry/fault instrumentation remains explicitly labelled. Original
221/210 source-recorded checked/11open/full goal ACTIVE; P7-D10 partial-receipt
external owner remains separate, not automatically qualified by AtomicInventory.

<a id="current-m75-physical-continuous-wear-accounting--2026-10-22"></a>

## Earlier M75: physical continuous wear accounting — 2026-10-22

[Current P8 owner/evidence](P8_CONTINUOUS_CATALYSTS_VERIFICATION.md#current-m75-physically-hauled-continuous-catalyst-accounting--2026-10-22)
adds the selected real hauling/catalyst-wear branch to the original M70 semantic
accounting matrix: feed +discrete wear +elapsed fuel share one actual inventory,
complete conversion and cumulative journal. Real loaded supply, backpressure,
product clearing, finite original-store refill, same receipt/execution, slower work
and stop qualify without a native edit, parallel ledger or instant fallback.
Two new literals9472/0/two final-live-hash replays, original allocation budgets pass.
Initial768/3 content-handling refusal/two clean builds retained. Not scoped/independent
stores, every provider, fault/lockstep or whole accounting certification; original
221/210 source-recorded checked/11 open/full goal active. Earlier records retain
their actual images and are not rerun as an unrelated final regression.

<a id="current-m72-simultaneous-incident-scarcity-and-recovery--2026-10-22"></a>

## Earlier M72: simultaneous incident scarcity and recovery — 2026-10-22

SELF-REVIEW via installed sdlc-orch implementation/verification/handoff procedures,
snapshot `a80d41ad0affd81b549f5f063787e186d162e37f`. Canonical Beep.SimEngine/master
HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. Previous
M71 added finite physical dock-contention proof: progress. Its remaining stage
matrix names simultaneous incidents/topology; this slice supplies the missing
incident contention evidence, not a duplicate scheduling/material implementation.

### Original ownership and new proof

Unchanged native **2.723.0/save161/replay2**, all450 native source hashes and native
DLL unchanged from M71. Original ResponseWork binds orders by target/named source/
episode; WorkBoard owns DAG, worker exclusivity and retry/progress; ports and
transactions own actual whole bills/books; native home/motion/dock owners perform
travel and exclusive service. Existing IncidentsView and ResourceFlowJournal own
private lifecycle/phase/cumulative facts. No new engine defect/feature, response
scheduler, stock/book/queue/ledger, clock/state/codec or provider fallback is claimed.

Existing ResourceDispositionTests gains one IncidentContention partial and two
individually selectable literals. It reuses PortPhysical/PortFactory's REAL loaded
two-resource supply/partial-work setup. New embedded
`work-effect-incident-contention.json` authors four named source declarations and
two waves; declarations are folded into the original selected module/scenario
before composition. Original response JSON still authors4-minute staffed cleanup,
3-minute dependent restart, whole2 supply+1 spare bill and original NoStock retry
delays1→4 minutes. No work/rate/material/handling bound is weakened.

One inspect-role machine and one haul-role carrier are actual existing entities.
Local/native inventory effect at one-minute quantum and opaque scoped/independent
effect at half-minute quantum run separately. The independent EFFECT is not a new
incident runtime, and synthetic `proof.disposition` events named damage/alarm/
release/contamination are NOT chemical/fire/spill propagation proof. Four sources,
two waves and this geometry are fixture content, never engine incident/world caps.

Original stress Profile becomes internal within the TEST assembly so the incident
proof reuses its one existing budget reader. No copied JSON reader, new default or
old capacity/dock tier execution. Both original33554432 peak/1073741824 total managed
Update-byte budgets apply PER measured continuation tier, not setup/native/all-thread
allocations or wall-FPS. The new continuation bounds each stage at180 updates with
recent jobs, response identities, phases and typed blockers; original shared setup
keeps its own240-update/20-second stage bounds.

### Actual concurrent lifecycle

The new driver has no World/provider/stock/job/progress writer, direct acquisition
or response dispatch logic. It submits normal commands and observes engine-owned
views/Update/Save/Load/replay. Existing setup physically delivers the initial2 supply/
1 spare, saves loaded transit, earns partial ordinary work and records one actual
loaded-delivery replay per variant. Player cancellation leaves that bill unspent.

For EACH wave, four Raise commands in one Update produce distinct actual response
orders/episodes on the same site. It does not choose which incident wins the worker
or scarce bill. First wave has only one funded cleanup; second wave starts empty.
Remaining earned cleanup nodes wait at typed NoStock, dependent restart remains
unearned, and the real response machine returns to its authored home. Saved twins
retain every response ID, episode, progress and retry state; no recreated orders.

Physical replenishment supplies all missing feed first. Without spares, none of the
competing whole bills partially consumes or monopolizes those supplied units. The
carrier actually unloads/departs/returns before a second physical spare delivery.
At a partially earned restart, normal CancelOrder resumes the SAME automatic response
and preserves its done cleanup/progress rather than replaying the effect. All four
responses then settle independently. A second simultaneous wave allocates fresh
monotonic orders for all four source episode2 states; old history remains factual.

Every observed Update balances original20 supply/10 spare against actual entity
stock/cargo/opaque buffers plus classified Destroyed facts. Assignment never shares
one machine across responses/supply; source and destination service bays never contain
both physical actors. Both waves destroy exactly16 supply/8 spare per variant,
leaving actual4/2 elsewhere, no selected stock or inbound/outbound response promises.
Every source retains its own done cleanup/restart/order, private-company views refuse,
an idle session is unchanged, and actual two-record history eviction preserves exact
cumulative accounting. No extra material ledger or extra game clock is introduced.

### Execution and honest replay scope

Two clean target builds6.61s/6.00s, zero warnings/errors. Both initial new literals
pass8020/0 on build1; final adds explicit shared-bay geometry, terminal inbound
promises and cumulative-eviction assertions. ONLY those affected new literals rerun.
No previous IncidentPorts four-context suite, dock/other green case, profile/family/
full/default/soak test ran. All four runtimes/builds are retained, no hidden failures.
Harmless positional Work*.cs source-discovery error was corrected to directory+filter.

| Final exact literal | Checks | Case / observer ms | PID/exit | Measured updates / episodes | Peak / total Update bytes |
| --- | ---: | ---: | --- | ---: | ---: |
| Incident contention: simultaneous local responses preserve scarce physical supply saved episodes and allocation budgets |3638/0|4142 /4280|5656/0|435 /8|1236200 /113001240|
| Incident contention: simultaneous scoped independent effects preserve half-minute physical supply saved episodes and allocation budgets |6988/0|6618 /6764|29820/0|855 /8|1093328 /176640904|

Final **10,626/0**,16 actual episodes, **six actual replay verifications** on one
captured build2 image. Allocation measures primary Update calls in the NEW contention
continuation; original setup, twin Update and view/hash/assertions are outside that
interval. Initial local2755/0 (4313/4462ms,PID30376) and scoped5265/0
(6090/6236ms,PID428) retain build1, not added to final totals. Both final processes
exit0 normally, untruncated; each original budget passes per tier.

Six replays = one original loaded-supply setup replay plus two saved incident-wait
refill/cancel/resolve continuation replays PER VARIANT. Same-update Raise inputs occur
before those continuation recordings. Do not claim full Raise-stream replay, shuffled
arrival/lockstep or every quantum/provider combination from saved twins or these runs.
[All commands/attempts/metrics/images](../../artifacts/optional-module-durability/20452e941dad41dc8aa651378fe490fc/attempts.json).
Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each literal uses Release/net8.0 DLL `--bounded-exact` with
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.

### Limits and continuation

[Final exact build inputs](../../artifacts/optional-module-durability/20452e941dad41dc8aa651378fe490fc/build-2-inputs.json),
[frozen stage inputs](../../artifacts/optional-module-durability/20452e941dad41dc8aa651378fe490fc/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/20452e941dad41dc8aa651378fe490fc/structural-checks.json),
[original requirements](../../artifacts/optional-module-durability/20452e941dad41dc8aa651378fe490fc/original-gates-current.json).
This qualifies a finite simultaneous response/scarcity/cancellation/allocation chain,
not all hazard physics, every incident/provider/dock/scoped selector, topology cuts,
hostile callbacks, fuzz/soak/lockstep or global accounting. No original gate closes:
221/210source-recorded checked/11open/full goal ACTIVE. Next actual topology
contention/failure and remaining finite semantic/provider obligations through their
existing owners; do not repeat unchanged incident/dock/reference families or add
another scheduler/ledger to manufacture progress.

Affected typed results/refusals, first-fault, private detached lifecycle/phase views,
cumulative resource facts and notifications reuse existing owners. Wider
[operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain; self-review is not independent review or external sink/outage/retention
qualification. Headless UI/renderer/store/distribution N/A; no new dependency,
consumer/Oil/marine, business-rule, platform/Draw work, staging/commit/push or new
branch/PR. Publication still waits for the entire goal AND plan.

<a id="current-m70-classified-accounting-and-physical-wip-return--2026-10-22"></a>

## Earlier M70: classified accounting and physical WIP return — 2026-10-22

SELF-REVIEW using installed sdlc-orch implementation/verification procedures and
the existing C3 tracker, snapshot `a80d41ad0affd81b549f5f063787e186d162e37f`.
Canonical Beep.SimEngine/master HEAD `cb61483ab81404c11944931aedb4703ba694edb5`
plus preserved dirty work. Previous M69 completed selected finite reference-owner
qualification and changed evidence/continuation state: progress. This slice moves
to the existing material/WIP/tool accounting obligations, not another reference audit.

### Ownership and actual gap

Native **2.723.0/save161/replay2**, all450 native source hashes and native DLL remain
unchanged. Existing ResourceTransactions/ResourceConversions, Transformer WIP/tools,
resource ports, Logistics/WorkBoard and ResourceFlowJournal already implement the
selected physical rules. No native defect found or new engine implementation is
claimed. The existing sixteen caller groups cover137 exact typed calls, with the
same matched native image/PDB/source as M68; no new compiled census is required for
unchanged native bytes. [Semantic call/kind matrix](../../artifacts/optional-module-durability/39ae216246f5474f88b501f3c7abbad6/semantic-accounting-matrix.json)
relates each group to its original owner, effects, current selected proof or retained
earlier evidence, and limitations. It is not a whole-engine accounting certificate.

The existing mixed-disposition protocol case passed400/0 BEFORE test edits. Its
native/scoped, coupled/separate and minute/half-minute contexts check explicit
Consumed/Destroyed/Lost classification, partial receipts, release-only rows, foreign
claims, original endpoint attribution, saved reduced claims and16 actual replays.
It intentionally uses a test-only conversion command: protocol evidence, not a
player physically supplying a factory. Older named tool/catalyst/input-port cases
also use TransferProbeCommand to place materials; they cannot be relabeled as
real hauling acceptance merely because the transaction itself runs in the engine.

### New physical proof through the original owner

Existing TransformationSupplyTests is now partial and registers one new literal;
its original factory gains only an optional pre-game basis-authoring callback.
The new partial reuses the factory and ordinary engine, not a second player/scheduler/
material model. New embedded `transformation-accounting.json` explicitly authors a
product, five-unit factory capacity, three-unit feed, two-unit reusable tool hold,
one-unit completion wear, two-unit product and the original thirty-minute duration.
It is folded into the original master-selected module document BEFORE composition,
not discovered by a second runtime loader. The existing source's authored20 units,
carrier, point geometry, original WIP20 capacity and all original work/handling/route
rules stay intact. Five-unit capacity is this scenario's content, never an engine
world/inventory size ceiling. No Tycoon module is selected or business rule altered.

The driver has no World/provider/material/progress writer or TransferProbeCommand.
It uses JSON authoring, public commands/previews/results, immutable views, native
Update, Save/Load and replay only:

1. Enqueue normally. Automatic logistics must really load/travel with the full
   required supply; WIP and work progress remain zero during transport. Save/load
   that loaded state, then earn partial original batch work.
2. Disable the operation without losing progress. The actual WIP holds3 feed while
   the original factory stock retains2 tool units. An explicit physical haul brings
   another3 units into the five-unit store; wait for actual carrier departure.
3. Cancel the partially earned head. Original ReturnWip refuses whole return because
   the source is full, reports blocker.transformation-return-full, and retains3 WIP
   plus the original2 tool promises. Save/load this state and wait4 real updates;
   no consumption/wear, refund, loss or output is fabricated.
4. Haul the3 unpromised units back out using the actual carrier. Only earned pickup
   frees capacity for the original WIP transfer/reusable claim release. Save-twin
   observations continue through every Update; returned feed is NOT Produced.
5. Enqueue again and enable normally. The second batch earns all original30 minutes,
   consumes exactly3 feed+1 wear, releases the unused tool remainder, and creates
   exactly2 product. Final supply16/product2 exist in actual buffers; WIP and tool
   receipts are empty. Destroyed/Lost and fictitious product consumption are zero.

Per-resource temporary observations sum actual entity stock/cargo plus original
WIP and compare with the ORIGINAL cumulative journal classifications. No shadow
ledger or cross-unit/resource sum. The actual two-record retained window evicts;
saved cumulative history still accounts exactly. Replay from the saved blocked
return includes actual capacity-freeing haul and second batch commands. Saved twins,
detached private views, no-business operation, terminal restore and isolated session
identity are checked. The engine owns every decision, transfer, reservation and
elapsed duration; test helpers only submit intent and observe bounded progress.

### Every build/run and exact scope

Two targeted tests-project builds clean:7.30s and6.26s, zero warnings/errors.
First new physical literal1004/0 is retained; final adds explicit produced/no-loss/
no-refund/eviction assertions and passes1012/0. Only this affected new literal was
rerun; the already-green mixed-disposition400 case was NOT rerun after test-only
edits. No native or proof-provider gameplay source changed; proof DLL changes to
embed the new fixture, tests DLL changes for the same original partial owner.

| Literal scope | Checks | Case / observer ms | PID/exit | Replays | Image |
| --- | ---: | ---: | --- | ---: | --- |
| Resource disposition conversion: explicit mixed receipt kinds preserve partial claims scoped journals and replay |400/0|350 /500|12100/0|16|captured baseline|
| Manufacturing accounting: physical supply blocked WIP return and saved catalyst wear conserve actual resources |1004/0|3269 /3449|8424/0|1|interim build1|
| Same new physical literal, final exact accounting assertions |1012/0|2781 /2917|7820/0|1|final build2|

Latest two distinct selected scopes **1412/0/seventeen actual replay verifications**
span baseline/final images, not a final-image regression; interim1004 is not added.
All exit0 and untruncated, no hidden failed build/runtime. Failed guessed reads/
positional Windows globs were harmless source-discovery errors, corrected with rg;
they are not engine/test failures. [Every attempt and image](../../artifacts/optional-module-durability/39ae216246f5474f88b501f3c7abbad6/attempts.json).

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each exact literal uses the Release/net8.0 tests DLL with `--bounded-exact`,
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
The new driver bounds each physical stage at240 updates with phase/blocker/jobs
diagnostics; no duration, resource quantum, movement rule or existing limit is lowered.

### Limits, assurance and next action

[Final build identities](../../artifacts/optional-module-durability/39ae216246f5474f88b501f3c7abbad6/build-2-inputs.json),
[frozen inputs](../../artifacts/optional-module-durability/39ae216246f5474f88b501f3c7abbad6/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/39ae216246f5474f88b501f3c7abbad6/structural-checks.json),
[original gates](../../artifacts/optional-module-durability/39ae216246f5474f88b501f3c7abbad6/original-gates-current.json).
This qualifies one missing current public physical WIP/tool cancellation/wear chain,
not every continuous/operating/independent processor, scoped tool binding, incident/
spill/topology path or whole-accounting/stage-stress/allocation/fuzz/lockstep guarantee.
Existing other evidence keeps its actual image and precise scope. No original gate
closes:221/210source-recorded checked/11open; original P7.017/P8.017/full goal ACTIVE.

Next finish the finite remaining classified semantic/provider matrix using the
existing actual source/cases, then implement genuinely missing bounded stage
contention/allocation proofs through the original dock/incident/topology owners.
Do not repeat unchanged reference review or invent a second ledger/provider model.
Affected first-fault, command results, private views, flow history and notifications
reuse existing services. Wider [operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain, including external durable sink/outage/retention qualification; self-review
is not independent review. Headless UI/renderer/store/distribution is N/A. No new
dependency, full/default/profile/family/soak even final, consumer/Oil/marine/business-
rule/platform/Draw change, staging/commit/push or extra branch/PR.

<a id="current-d2f-m59-spill-cargo-interruption"></a>

## Earlier D2f/M59: spill cargo interruption

The selected next connected-topology continuation is now [qualified at D7b/M60](P7_TOPOLOGY_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-d7b-m60-connected-claim-continuation). D2f's following material/cargo evidence retains its original image/scope; remaining topology/capacity/provider work belongs to that existing D7 owner, not another document/subsystem.

UNCHANGED core2.717.0/save161/replay2; master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. All450 native sources/native DLL remain identical to M58/M57. This stage requalifies existing physical spill/disposal/treatment and adds missing current public-player cargo-interruption coverage; no new native feature/defect or duplicate state owner is claimed.

Four literal baselines ran BEFORE test edits on actual M58 proof/test image, captured in [baseline identities](../../artifacts/optional-module-durability/1230a863774a48baaaae1a0fcb56e7a4/baseline-binaries.json). Native/independent disposal421/0 each and native/independent treatment322/0 each total1486/0/four actual replays. Their source was inspected: delayed incident exposure transfers three of five real oil units into the original quantity layer; logical terrain cleanup cannot delete that stock; timed machine recovery restores it, saved physical haul/WIP leads to conversion or a separately earned disposal sink. Material plus recorded disposition is conserved per Update, saves/twins agree, environmental objectives read the real history and private/session isolation remains. These are build-bound baselines, NOT final-image re-runs.

The original SpillResourceAccountingTests.Run now selects one additional interruption branch, not another workflow driver. New spill-cargo-recovery.json is explicitly embedded and master-listed through the existing optional LogisticsRecoveryPolicyModule; it authors demand-only cargo return and disabled evacuation. The shared tile factory's scenario authoring callback selects that real profile. No new recovery implementation, stock/book, clock, queue or host decision. The new partial owner uses ONLY public commands/views/Save/Load/Update. Existing original Run continues to own the complete incident/spill/recovery/treatment/replay scenario.

After real three-unit loading, SetWorkerHeldCommand retires the original haul, releases its commitments and keeps the actual oil in its carrier as unassigned cargo. A held saved twin waits four updates; no receiver stock or new unload/refund/loss facts appear. Another company cannot inspect it. Releasing the carrier does not invent a destination: the JSON demand-only policy returns logistics.recovery-no-demand, keeps cargo loaded and survives save plus sole-clock pause. A real ConfigureStockPolicyCommand creates a three-unit demand on the named material port. Existing logistics selects/acquires a NEW loaded return with no source pickup/purchase, protecting the SAME original cargo endpoint and exactly three units as committed cargo. Save the real receipt partition, travel, observe one delivered/two still aboard at the authored one-unit-per-minute unloading rate, save again, finish unload/departure with an empty partition. Continue the original five-minute WIP treatment and engine objective. Each native/independent geometry context preserves the finite five-unit balance and completes an actual whole-workflow replay containing hold/release/stock-policy commands.

The first new run797/1 timed out after240 updates at clock399: the original treatment receiver is private. Source diagnosis found the deliberate VisitPublicPorts eligibility filter; configuring a stock demand does not make a private port discoverable for automatic cargo return. This was fixture authoring, not a missing engine permission. Only the new automatic-return variant now authors that receiver public before play. Original private scenarios, selectors, receipt/ownership rules and bounds are unchanged; no runtime flag mutation, fallback or eligibility exemption. The failed result is retained. Corrected final856/0 includes both geometries and two actual replays.

| Selected literal | Checks | Suite/observer ms | PID/exit | Actual replays | Image |
| --- | ---: | --- | --- | ---: | --- |
| Spill disposal: native recovery hauling cancellation and earned destruction | 421/0 | 5579/5740 | 13176/0 | 1 | baseline M58 |
| Spill disposal: independent contracts recovery hauling cancellation and earned destruction | 421/0 | 5322/5480 | 9608/0 | 1 | baseline M58 |
| Spill accounting: native physical deposition recovery hauling and treatment | 322/0 | 4749/4903 | 18852/0 | 1 | baseline M58 |
| Spill accounting: independent physical deposition recovery hauling and treatment | 322/0 | 4324/4479 | 14164/0 | 1 | baseline M58 |
| Spill cargo interruption: native and independent saved load returns through authored recovery | 856/0 | 10603/10818 | 26396/0 | 2 | final build2 |

Successful selected evidence totals2342/0/six replays across the explicitly separate images; only the new856/0 is claimed on final build2. Two targeted builds8.03/7.28s clean0warnings/0errors. Six total runtimes retain initial new797/1 (PID15740/exit1/suite4991/observer5145ms) and final856/0, plus four baselines. The final observer session10329 was polled terminal0, not restarted. No unrelated passing case was rerun after test-only edits. One literal contains two directly relevant geometry contexts, not a profile/family workaround.

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Each selected name uses Release/net8.0 tests DLL with `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Per-stage240-update/20-second failure limits remain. Profile enrollment is metadata only. NO full/default/profile/family/soak, consumer build/runtime, native protocol rerun or bound increase.

[All attempts](../../artifacts/optional-module-durability/1230a863774a48baaaae1a0fcb56e7a4/attempts.json), [logs](../../artifacts/optional-module-durability/1230a863774a48baaaae1a0fcb56e7a4/), [frozen inputs](../../artifacts/optional-module-durability/1230a863774a48baaaae1a0fcb56e7a4/final-inputs.json), [original221 reconciliation](../../artifacts/optional-module-durability/1230a863774a48baaaae1a0fcb56e7a4/original-gates-current.json), [structural checks](../../artifacts/optional-module-durability/1230a863774a48baaaae1a0fcb56e7a4/structural-checks.json). Final native75BFEE4612E242906F880107BF7AB7F5A77DF0EBAAE59C652DD9B2BC891FDD24 unchanged; proofB635CE3A26642A5F05394A7F9830A6C33ED7A0EA4BACE007298118CD4762647C / tests5CDBBC5BB7E5B42FCAB48417C26E3779D397394F9A387D4271CE3F6984A350C1. Baseline proof4A701500.../testsFA0F1643... are separately captured; uncaptured first-build hashes are not invented. No native/schema/header changes, business/platform/Draw/Oil/adapters, staging/commit/push.

Self-review, not independent review: original incident/layer/transport/material/recovery/demand/work/WIP/history/clock owners remain authoritative; player interruption has no raw writes, no fictional delivery/loss and no privileged destination bypass. Existing original notifications/results/reporting remain; [wider operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) are not newly certified. Current D2/P12/master/P6/phase navigation updated in place, earlier evidence retained with anchors. Original221/210 source-recorded checked/11 open unchanged; no original gate closure/full-goal completion. Next qualify connected-store/topology redistribution/removal with live promises and interrupted physical workflows through existing owners. Broader provider/reentry/lockstep/stress/every-module requirements remain OPEN. No live owned worker or staged files at handoff.

<a id="current-d2e-m58-named-port-incident-continuation"></a>

## Earlier D2e/M58: named-port incident continuation

UNCHANGED core2.717.0/save161/replay2; master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. All450 native sources and native DLL remain identical to M57. No native defect was observed or engine feature fabricated: existing ResponseWork/Incident/WorkBoard/ResourcePortContracts/InventoryWorkEffectContracts/IResourceTransaction already provide this path. This stage adds missing current authored content and actual player-command evidence, not another response engine or host gameplay.

New engine-side proof document work-effect-incident-ports.json is explicitly embedded and added to the existing master module's documents list by PortFactory. It authors a non-player-orderable response DAG: four-minute staffed cleanup with a native or independent inventory effect, dependent three-minute staffed restart, bound response completion, two phase labels and an explicit NoStock retry policy. The existing target gets named native damage/alarm capabilities via scenario authoring; scoped variants have no local inventory. The workflow/effect/ports/claims/timing/episode rules stay inside existing engine contracts. No default inferred ports, native inheritance requirement, second material/progress/state/clock model or compatibility path.

The EXISTING physical setup drives actual two-resource hauling, loaded-delivery save/replay, partial four-minute ordinary work and a saved checkpoint. Shared PortPhysical now distinguishes explicit binding-fault configuration from positive authored continuations, preserving the old branch rather than inferring a fault profile from any callback. The new continuation has NO World/provider access: JSON selection, commands, immutable views, Save/Load, replay and actual Update only. Cancel the ordinary partially earned plan with its bill intact. Raise a named incident; reject a duplicate pure raise preview; verify another named capability stays inactive and foreign-company lifecycle is hidden.

Earn partial physical cleanup, save, and record cancellation before its effect. The response module resumes the SAME episode/order without resetting progress. After actual cleanup removes exactly two supply/one spare and the machine earns part of restart, cancel again: the original done cleanup/progress/effect remains, no refund or second removal. Save/load the actual completed-phase state, finish the timed restart/bound episode, idle eight updates and replay both real cancellation commands. Resolution follows at least the authored seven game minutes; the driver does not complete an incident itself.

Raise episode2 on that same named source: its order identity increases, cleanup earns real work then waits at the NoStock effect, and the dependent restart cannot start. Save that actual retry/earned-progress checkpoint. Record two actual physical refill deliveries, waiting for the first unload and carrier's real home return before sending the second. The first supplied resource stays unpromised and unspent while the second is missing. Full supply commits the pending existing cleanup once, then restart resolves episode2. Replay from that actual waiting checkpoint. Four local/scoped × native/direct-effect contexts conserve original20 supply/10 spare plus classified removal and match loaded twins after every Update. Final cumulative destruction is exactly4 supply/2 spare; both public phases are complete, the other named source remains episode0 and an idle independent session is unchanged.

| Final selected literal | Checks | Suite/observer ms | PID/exit | Actual replays |
| --- | ---: | --- | --- | ---: |
| Incident effect ports: saved physical supply cancellation and repeated episodes retain original phase ownership | 3990/0 | 5604/5773 | 27152/0 | 12 |
| Work effect ports: native and independent whole bills wait for physical supply and saved timed completion | 5896/0 | 6115/6292 | 27616/0 | 24 |
| Work effect bindings: opaque stores availability and acquired claim faults preserve original ownership | 550/0 | 1301/1449 | 30176/0 | 3 |

Three directly affected scopes **10436/0, thirty-nine actual replays** on one final image. One targeted test-project build clean0warnings/0errors/9.32s, initial live session85483 polled terminal0. All three runtimes first-pass/exit0. Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Each literal uses Release/net8.0 tests DLL with `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Profile enrollment is metadata only. No native protocol/old incident/other unrelated green rerun, full/default/profile/family/soak, consumer build/runtime or increased limit.

[Attempts](../../artifacts/optional-module-durability/7ba05aca0e444cfeac9701d9e6665152/attempts.json), [raw logs](../../artifacts/optional-module-durability/7ba05aca0e444cfeac9701d9e6665152/), [frozen inputs](../../artifacts/optional-module-durability/7ba05aca0e444cfeac9701d9e6665152/final-inputs.json), [original221 source map](../../artifacts/optional-module-durability/7ba05aca0e444cfeac9701d9e6665152/original-gates-current.json), [structural checks](../../artifacts/optional-module-durability/7ba05aca0e444cfeac9701d9e6665152/structural-checks.json). Native SHA75BFEE4612E242906F880107BF7AB7F5A77DF0EBAAE59C652DD9B2BC891FDD24 remains M57's actual image; proof4A7015005ED8655B33E345EFBF7DA4FD5CB0D03E3968FB20370D6D48817C0C2A / testsFA0F1643E8C15C59FABE92A6A623DFF1D0341B30CCA43CCCF4302434E5844E88. No native/schema/build/header rewrite, platform/business/consumer change or Git publication.

Self-review, not independent review: one authored workflow over the original graph/episode/port/work/transaction owners; same-order cancellation retains actual earned/spent state; whole-bill refusal leaves no acquired prefix; saved pending effect/refill and resolved history remain deterministic/private. Existing reporting/events/notifications and [wider operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) retain their owner, not newly certified globally. Current D2/P12/master/P6/phase navigation updated in place; prior evidence/anchors remain historical. Original221/210 source-recorded checked/11 open unchanged, no original closure/full completion. Next qualify existing physical spill/deposition/recovery/hauling/treatment and cargo/topology interruptions through their existing authored owners. Broader provider/reentry/lockstep/stress/every-module gates remain OPEN. No live owned worker or staged files at handoff; commit/push only after the full plan/goal is genuinely achieved.

<a id="current-d2d-m57-logical-effect-port-identity"></a>

## Earlier D2d/M57: logical effect-port identity

Core2.717.0/save161/replay2; master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty work. Existing IResourceEndpointProvider permits fresh wrappers over its actual stock/book. The new positive wrapper case REPRODUCED443/1 on2.716: earned whole-claim acquisition was followed by a false `disposition port changed after complete claim acquisition`, solely from CLR store-reference comparison. The existing InventoryWorkEffectContracts removes ONLY that comparison. Actual ready state, endpoint/company/policy identities, native BindsEntityInventory exact alias checks and original transaction receipt/quantity/version guards remain. Core contract documentation clarifies logical identity; no new resolver/cache/transaction/stock/claim owner, API fallback, codec or clock. Build identity advances; old headers are never rewritten.

The same existing physical driver supplies both authored resources through actual haul/load/travel/unload/return, earns part of its four-minute machine node and saves that state. A protocol callback branches six independent loaded sessions from that actual checkpoint. Its raw provider access is explicitly negative fault arming/diagnostics, not gameplay or fabricated stock/work. The normal driver still owns no engine rules. One declared empty alternate pool, selected JSON retry policy and original provider hooks are test content/instrumentation only; transient hooks do not enter authoritative Sync.

Verified cases:

- Fresh independent wrappers retain the same actual endpoint/book, pure whole-bill readiness, correct completion, exact destroyed2 supply/1 spare, retired original claims, save/load and actual replay.
- Pre-acquisition unavailability returns its exact typed blocker. Selected JSON, not a hardcoded exception, makes it retryable. Twelve actual updates retain the original order, done four-minute node, unapplied final effect, retry failures/deadline, full stock and zero claims/disposition. Its real waiting checkpoint loads/hash-matches and then replays completion after the transient test outage clears.
- A real first reserve redirects the port to its declared empty alternate. The whole actual original vector is acquired, then the existing guard faults before consumption. Selection is not silently restored or substituted; both real original receipts stay diagnostic.
- An outage introduced after the first reserve similarly faults after whole acquisition rather than leaking a healthy retry with orphan promises.
- A provider throws after its actual first reserve. That original exception is retained; the first claim remains and the second was never acquired. No rollback or resumable prefix is fabricated.
- A provider performs the actual full first-material removal but returns a short quantity. The original conversion check faults; actual spent stock is not refunded, the unvisited second stock/receipt remains, and no verified success is published.

All terminal cases preserve the first fault and stopped sole game clock, clear pending success events, leave detached prior history unchanged, reject live history/save/hash/replay Finish and leave another loaded session unchanged. Positive completion/retry replays use native deserialized representations, not serialized test hooks. The setup's loaded-delivery replay plus two actual completion replays makes THREE new actual verifications; unfinished fault recordings are not counted.

| Selected final literal case | Checks | Suite/observer ms | PID/exit | Actual replays |
| --- | ---: | --- | --- | ---: |
| Work effect bindings: opaque stores availability and acquired claim faults preserve original ownership | 550/0 | 1396/1551 | 5840/0 | 3 |
| Work effect ports: native and independent whole bills wait for physical supply and saved timed completion | 5896/0 | 6935/7103 | 4296/0 | 24 |
| Current native protocol: authored expectations and physical selected-role checkpoints | 92/0 | 2598/2747 | 28476/0 | 2 |

Three selected scopes **6538/0, twenty-nine actual replays**, all on the final build4 image. Four targeted builds retained:6.90s clean;7.71s failed from the test helper using unsupported ReservationOwner `!=`;6.51s clean after using Equals;6.76s clean after strengthening original receipt-prefix/earned-work assertions. Five bounded runtimes retained: original healthy wrapper443/1 (PID8424/exit1/suite1965/observer2177ms), expanded corrected534/0 (PID7216/exit0/1523/1679ms), final550/0, existing5896/0, protocol92/0. No hidden failure, weakened rule, stock/time/bound change or rerun of unrelated green cases.

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Each runtime is the exact table literal on Release/net8.0 tests DLL with `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Framework profile enrollment is metadata only. NO full/default/profile/family/soak or consumer execution.

[Every attempt](../../artifacts/optional-module-durability/13792268b56a412fae7a2d86036feea2/attempts.json), [raw logs](../../artifacts/optional-module-durability/13792268b56a412fae7a2d86036feea2/), [frozen current inputs](../../artifacts/optional-module-durability/13792268b56a412fae7a2d86036feea2/final-inputs.json), [original221 source reconciliation](../../artifacts/optional-module-durability/13792268b56a412fae7a2d86036feea2/original-gates-current.json), [structural checks](../../artifacts/optional-module-durability/13792268b56a412fae7a2d86036feea2/structural-checks.json). Final native SHA75BFEE4612E242906F880107BF7AB7F5A77DF0EBAAE59C652DD9B2BC891FDD24 / proof51F44769CE07C022E51B3EBFD4D0913E1DEC2C44B331556F52AF9952B987E54E / tests0C770734D6216DDD163C3E51A0064C7E45FA2076C9870BE72B6800C02F894F62. These final hashes are not invented for the uncaptured earlier images. Exactly three native sources changed since M56: ResourceEndpoints contract comment, InventoryWorkEffects logical check and SaveGame build identity. No save161/replay2 payload change.

The initial structural link checker failed because Split-Path on root-relative README/AGENTS produced an empty parent. Its [original diagnostic result](../../artifacts/optional-module-durability/13792268b56a412fae7a2d86036feea2/structural-initial.json) is retained, not treated as missing repository targets. The corrected checker uses each actual file's DirectoryName and checks the same selected document set; no target/source/test changed and no runtime rerun. Link existence is not semantic or anchor qualification.

Self-review: one logical endpoint/book owner, original atomic acquisition/conversion authority, pure typed refusal, explicit authored retry, truthful terminal partial prefixes, physical-only earning, isolated sessions and no false healthy audit/notification/replay result. Existing first-fault/events/history/views remain the reporting owner; [wider operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) are not newly certified. This is self-review, not independent review.

Updated the existing D2 design/evidence, P12 current contract text, master/current P6 checkpoint, original audit navigation and active phase banners in place; earlier build-bound evidence retains explicit anchors. No duplicate fix document or changed original requirement words/checkboxes. Original221/210 source-recorded checked/11 open remain, no original closure/full completion. Next qualify named-port incident response/cancellation and spill/cargo/topology accounting through existing authored contracts and owners. Broader provider/packing/reentry/lockstep/stress and every-module gates remain OPEN. SimEngine only; no Oil/business/platform/Draw/adapters, staging/commit/push.

<a id="current-d2c-m56-explicit-work-effect-ports"></a>

## Earlier D2c/M56: explicit work-effect ports

Core2.716.0/save161/replay2; master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty tree. Extend the EXISTING IInventoryWorkEffectDefinition and InventoryWorkEffectDeclaration with required Ports, exactly one named capability per quantity resource. Native InventoryEffectDef and independent IndependentInventoryWorkEffectDef delegate the same immutable route schema and execution contracts. Local inventory is no longer required. Missing mappings reject composition; missing target ports return resource.port-definition-missing even when that target has local stock. No old JSON reader, inferred local binding or parallel effect implementation.

ResourcePortContracts is now the sole shared route owner: the original transformation route reader/resolver/coverage validator, target packing/capacity checks, current bindings, endpoint quantity grouping and outbound claim-vector grouping move there. Their former private transformation implementations are removed and native batch/continuous/operating callers migrate directly. Transformation-specific rate/WIP helpers remain with their existing owner. New typed metadata blockers distinguish missing/incompatible definition, packing and complete requirement capacity. No duplicate route parser, resolver, stock, transaction, claim book, work coordinator or clock.

InventoryWorkEffectContracts.CheckIntent validates actual target/company and port metadata without demanding future stock. CheckStart/CheckSubject evaluate the entire selected outbound claim vector without mutation. Earned CommitSubject acquires it atomically through IResourceTransaction, re-resolves the SAME current bindings and verifies original endpoint/store/company/policy identities, then delegates ONE ResourceConversionIntent with the authored consumed/destroyed/lost kind. Missing second material cannot reserve/consume the first. Changed selection/provider violations after acquisition throw into existing first-fault boundaries rather than consuming an alternative, returning healthy orphaned promises or fabricating rollback. The former effect-specific ResourceInventoryIntent/local EvaluateInventory/CommitInventory path is deleted; those APIs retain their distinct initialization/other lifecycle responsibilities.

Seventeen existing native/direct effect definitions in eleven engine-owned JSON documents gain explicit port maps. Thirty affected inventory prototypes across the selected proof documents gain an explicit disposition.input alias to their original stock, never a loader default or another inventory. All existing quantities/timing remain; the fault-site probe changes partial_remove to partial_take so it violates the actual new receipt-based mutation boundary. New explicitly embedded work-effect-ports.json is loaded through the existing master/reference-module composition and authors resources, two scoped stores, both effect providers, handling and four-minute activities. No Oil/consumer or Tycoon document/source migration in this stage.

The new public-player case runs twelve local/scoped × native/independent declaration × consumed/destroyed/lost contexts. Scoped targets have NO local inventory. Original JSON supplies finite20 supply/10 spares; the driver submits home, persistent plan and named-port haul commands, calls ordinary Update and reads immutable views only. Plans remain pending with NoStock/Order0; a real loaded two-unit first delivery saves/replays and returns home physically. While the second resource is absent, the supplied first stock stays unpromised and unconsumed. A real second haul supplies one spare, the original board activates the same plan, the worker travels and earns partial four-minute work. Save that exact job/progress and replay to one complete effect plus later no-duplication updates. Every update conserves native/cargo/scoped stock plus committed disposition; exact resource/category/cause quantities, terminal books/saves, private plans and independent sessions are checked. Two actual replays per context, twenty-four total. Six malformed maps reject before play; missing feed refuses preview/inbox despite stocked local inventory and remains healthy/loadable.

| Selected literal case | Checks | Suite/observer ms | PID/exit | Actual replays | Image |
| --- | ---: | --- | --- | ---: | --- |
| Work effect ports: native and independent whole bills wait for physical supply and saved timed completion | 5896/0 | 5683/5829 | 2108/0 | 24 | 3 |
| Resource disposition: named episodes, authored native effects and saved physical response | 93/0 | 978/1119 | 21972/0 | 0 | 4 |
| Resource conversion: timed transformer completion and provider fault boundaries | 100/0 | 380/524 | 14324/0 | 0 | 3 |
| Current native protocol: authored expectations and physical selected-role checkpoints | 92/0 | 2413/2564 | 26220/0 | 2 | 3 |

Four selected scopes **6181/0, twenty-six actual replays**. Images3/4 share native/proof binaries; tests differ only for the affected old incident assertions/JSON mutation helper. Already-green physical port/conversion/protocol scopes are not rerun after that test-only correction. Six targeted builds: clean17.91s (live43018 polled terminal0), test compile failure8.06s (JsonValue.Null is a method, fixed with explicit parsed null), clean6.00/6.28/6.27/5.35s. Seven runtimes retained: new799/3 (new spare missing explicit handling; rejected setup then bounded wait),936/1 (idle truck had no home and blocked the physical approach),5896/0; old incident83/10, native100/0, protocol92/0; corrected incident93/0. The initial handling cause is source-inferred from the supply-only original profile; the old failed driver omitted its refusal detail. Final driver fails immediately with command/refusal and includes physical positions in timeout diagnostics.

The fixture repair adds a JSON handling rule for the new spare with the existing one-minute load/two-minute unload, and ordinary SetHome plus actual return waiting. No engine time, dock/collision/compatibility guard or180-update stage limit is weakened. The old incident's six provenance assertions now require a real spending token whose endpoint equals the lifecycle endpoint, no receiving token and IsTransfer=false. Its four whitespace-sensitive invalid-content edits now structurally mutate the same produced-kind/empty/fraction/unknown-resource cases; bound-source and missing-effect cases remain. No invalid test is removed or made less strict. The actual partial-take failure still faults timed execution and blocks healthy save/hash.

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Every runtime uses the literal table name on the Release/net8.0 DLL with `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Profile enrollment is metadata only. No full/default/profile/family/soak, consumer runtime/build, unrelated green rerun or increased limit.

[All attempts](../../artifacts/optional-module-durability/25bb5b6eef7f47b5bffaa6a0d5f6d253/attempts.json), [raw logs/images](../../artifacts/optional-module-durability/25bb5b6eef7f47b5bffaa6a0d5f6d253/), [frozen inputs](../../artifacts/optional-module-durability/25bb5b6eef7f47b5bffaa6a0d5f6d253/final-inputs.json), [original221/source map](../../artifacts/optional-module-durability/25bb5b6eef7f47b5bffaa6a0d5f6d253/original-gates-current.json), [structural checks](../../artifacts/optional-module-durability/25bb5b6eef7f47b5bffaa6a0d5f6d253/structural-checks.json). Native SHAD97B0B54A8F11E4A9280B2ABD2CB1C59E457124D8C493D8057C72F8CABBD3DA0; proof94E04AD6678C2B4508D7A70C4E64B6C71774AFC3462646763DD34474817987C0; final testsC2852A728045B633AAA2D8653AB21CF17FA56492A6E1D03840D808BCFF746BC5. Build identifies direct contract/schema migration; save161/replay2 layouts and original clock stay unchanged.

Self-review covers one shared port rule owner, pure whole-bill readiness, real acquired receipts and classified history, physical-only earning, no inventory fallback, privacy/isolation and original terminal fault/save/notification owners. The post-acquisition selection recheck is source-reviewed but a hostile redirect/unavailable binding after acquisition is NOT yet separately runtime-qualified. Arbitrary alias/packing/provider/connected-source/fault/reentry/lockstep/stress and broader incident/cargo/WIP/topology accounting remain open; [wider operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) unchanged. Existing P12 and owned-reservation guidance is reconciled, original historical results/headers retained. Original221/210 source-recorded checked/11 open unchanged, no original gate closure/full completion. Next exercise unavailable/redirected binding and acquired-claim failure through existing provider hooks, not a second resolver/stock model or weakened refusal. Only SimEngine; no Oil/business/platform/Draw/adapters, header rewrite, staging/commit/push.

<a id="current-d2b-m55-explicit-scoped-disposition"></a>

## Earlier D2b/M55: explicit scoped disposition

Core2.715.0/save161/replay2, master HEAD `cb61483ab81404c11944931aedb4703ba694edb5` plus preserved dirty tree. Source review found that ResourceConversionInput had no accounting kind, receipt-based ResourceFlowFact always emitted Consumed, and live journal/attribution/restore accepted receipt metadata only for Consumed. Destruction/loss could not use the same scoped resource-port transaction correctly. This is a transaction prerequisite for generalized authored work effects, not proof that their local-inventory restriction is already removed.

ResourceConversionInput now requires explicit Kind; the old constructor is removed, without overload/default/compatibility path. IsDispositionKind is the sole Consumed/Destroyed/Lost classifier, reused by transaction admission, existing inventory-effect validation, live/saved journal validation and ResourceFlowAttribution. All previous draw/catalyst/flow/probe callers pass Consumed directly. Their quantities, physics and business semantics are unchanged. The existing ResourceTransactions owner validates the entire original receipt vector before mutation and keys each draw fact's declared kind by its original token for both coupled and separate endpoint commits. Partial draw, release, output capacity, units, stock/book/version checks and deferred whole-success fact publication keep their original owners. There is no new transaction, stock, registry, ledger, claim model or clock.

The receipt-based fact constructor also requires kind; plain ConsumeClaims passes Consumed explicitly. The original conversion probe's input codec carries each kind byte rather than silently reconstructing it as Consumed. Journal fields already stored kind, endpoint and source token, so persistent layout/save161/replay2 are unchanged; live validation and reconstruction now accept correctly attributed destroyed/lost receipts, still rejecting financial/creation/unknown kinds. Engine identity2.715 strictly distinguishes this semantic/API change; no old-save/replay header rewriting. No Tycoon source/rules or consumer content changed.

New exact case in existing ResourceConversionTests runs eight native/scoped × coupled/separate-output × minute/half-minute contexts. Explicit existing JSON authors30 steel/five water, finite stock seeds actual scoped stores through the original movement authority. Real original receipts cover local steel destruction1, local water loss0.5, remote steel consumption1 and a release-only water row with zero draw. One real probe command commits that mixed vector and an authored steel product1. The result is29 steel/4.5 water, with exact category/cause/endpoint/source-token facts, not a fabricated universal mass ratio. A zero-draw lost-kind row releases authority without losing material or publishing another loss.

Save the actual reduced local/remote steel tokens and classified scoped journal. A second actual command spends the remaining steel1+1, preserves another owner's claim, and reaches27 steel with total destroyed2/consumed2/produced1; water loss remains0.5. Both commands replay from their actual saved checkpoints: sixteen real verifications across eight contexts. Public immutable history/private summaries and independent sessions remain checked. Invalid StockTransfer/Produced/Sold/255 input kinds refuse pure evaluation and fresh commit before any stock/book/version/history prefix. These are explicitly low-level receipt/transaction protocol probes, not a public-player gameplay acceptance scenario.

| Selected exact case | Checks | Suite/observer ms | PID/exit | Actual replays | Image |
| --- | ---: | --- | --- | ---: | --- |
| Resource disposition conversion: explicit mixed receipt kinds preserve partial claims scoped journals and replay | 400/0 | 322/463 | 19044/0 | 16 | 5 |
| Resource conversion: whole admission, scoped outputs and canonical saved facts | 114/0 | 229/380 | 22656/0 | 0 | 4 |
| Resource conversion: timed transformer completion and provider fault boundaries | 100/0 | 370/518 | 476/0 | 0 | 5 |
| Coupled conversion providers: original mixed endpoint receipts facts and terminal fault boundaries | 626/0 | 298/435 | 26032/0 | 0 | 5 |
| Current native protocol: authored expectations and physical selected-role checkpoints | 92/0 | 2499/2660 | 28868/0 | 2 | 4 |

Five focused scopes **1332/0, eighteen actual replays**, images4/5 share identical final native/proof binaries; only the new test's release-only strengthening changes the tests DLL. Already-green whole-conversion/protocol cases are not rerun after that test-only edit. Five targeted builds9.62/6.78/7.66/5.17/5.74s clean; initial live session24482 was polled terminal0, not restarted. Nine runtimes retained: new24/1 (faulted recording masked first fact-validation error),23/1 (explicit invalid committed resource-flow fact from remaining attribution restriction),360/16 (only the oracle incorrectly assumed20 steel while original JSON authors30), corrected392/0, then existing114/0 and92/0, strengthened400/0, existing100/0 and626/0. No authored stock amount or runtime rule changes to fix the oracle. New final startup assertions explicitly verify30 steel/five water.

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`. Every runtime uses its literal table name on the Release/net8.0 DLL with `--bounded-exact --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`. Profile enrollment is metadata only; no full/default/profile/family/soak or consumer run, unrelated green rerun or bound increase.

[All attempts](../../artifacts/optional-module-durability/4778921f573b4dceba38febebb38424d/attempts.json), [raw logs/images](../../artifacts/optional-module-durability/4778921f573b4dceba38febebb38424d/), [frozen selected inputs](../../artifacts/optional-module-durability/4778921f573b4dceba38febebb38424d/final-inputs.json), [original221/current-source map](../../artifacts/optional-module-durability/4778921f573b4dceba38febebb38424d/original-gates-current.json), [structural checks](../../artifacts/optional-module-durability/4778921f573b4dceba38febebb38424d/structural-checks.json). Native SHA29C6DF24837480D5AA707E7B86266842EBC9902B1C47A2993794DAB6E64FC0BE; proofD04F543508B4DDDFEC0F494C13B5802B9AE4A2330FFAECB65AC5C729810C4886; final testsB9F6FAFCA0CEC55BFD92A0A53080035FDAAD9947F2D117DBF290C1F4D3E30626.

Self-review covers mandatory explicit accounting, exact token-to-kind mapping, rejection before mutation, unchanged native draw/output/remainder protocols, shared attribution/live/restore rules, command serialization, native timed output and independent first-fault/callback behavior. Original first-fault/notifications/checkpoint owners remain; [wider operational/assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage) remain open. No proof of generalized named-port inventory work effects yet, broad incident/cargo/WIP/topology/stress, arbitrary provider/CLR isolation or every-module durability. Original221/210 source-recorded checked/11 open unchanged, no original gate closes; full goal ACTIVE. Next extend the EXISTING effect declaration/contracts to explicit per-resource port bindings using this same transaction, never a missing-port fallback or parallel disposition service. No Oil/business/platform/Draw/adapters, staging/commit/push.

## Historical initial D2 outcome

Engine remains **2.615.0 / save159 / replay1**. No production source, protocol, financial rule, host application, renderer or platform edits. The 442 core C# path/content SHA256 remains `222fa20283f1b8954caac0cc73e78283d90c4318d918c447ac2d9f43daa5af72`.

Four selected literal finals: **114 + 100 + 322 + 322 = 858 passed, zero failed**. This is not a full/profile/family regression or closure of original P7.017. Original bookkeeping remains221 /200 recorded checked /21 tracked open, not a fresh global audit.

## Original ownership inspected

- `ResourceConversions.cs`: sole `ResourceTransactions` conversion prepare/commit path. Validate whole receipt records and native quantities, capture all actual input/output endpoints before callbacks, sort outputs deterministically, distinguish same-endpoint `IResourceConversionEndpoint` from uncoupled primitive calls, revalidate every expected stock/receipt change, stage facts until the complete commit verifies. No second conversion balance, codec, scheduler or compensating ledger.
- `ResourceTransactions.cs`: original ephemeral `ClaimEndpoint` checkpoints own expected stock/book/version deltas, not actual inventory. Original endpoint owners keep live stock and receipts. `Reduce` changes expected outstanding amounts on the original token; it never acquires substitute authority.
- `Transformations.cs`: completion delegates to `EvaluateConversion` / `CommitConversion`. `TransformerFlow.cs` uses the same conversion owner for flow production. No recipe execution is moved into tests.
- `SpatialResources.cs`: physical deposition/withdrawal delegates to original movement admission/commit. Spill stock is the original quantity layer/cell endpoint; cleanup cannot erase it. Treatment uses original transformer WIP and conversion.

This is source/runtime evidence for the selected paths, not proof that arbitrary undeclared CLR state or every mod callback is sandboxed.

## Changes and new assertions

### Partial receipt conversion

The existing resource-contract literal now includes an isolated real native receipt scenario. This is low-level transaction instrumentation, not public-player acceptance.

1. Hold3.5 native water and a separate owner's0.5 water on the original five-unit source.
2. Pure whole-record admission consumes1.25 and produces1.25 on an independent scoped endpoint, retaining the same token with2.25 outstanding. Actual source stock becomes3.75; unrelated0.5 remains.
3. Reusing the stale original3.5 request yields `InvalidReceipt` at admission and fresh commit without stock/book/hash/history change.
4. Save/load preserves the exact reduced record and original token. Submit the same native command to original and restored worlds.
5. Consume1, produce0.5 on the same endpoint, release the remaining1.25. Source becomes3.25; only the other owner's original0.5 receipt remains.
6. Original facts total2.25 consumed /1.75 produced. Releasing authority produces no refund, loss or destruction. Settled state, facts and unrelated receipt restore identically.

There is no universal requirement that authored resource conversions conserve equal numerical units. The test checks the exact declared input/output quantities and actual receipts rather than inventing universal yield rules.

### Timed conversion and hostile callbacks

The test recipe adds two reusable supply tools. It now explicitly binds them to the already authored `tool.input` port; strict content validation remains unchanged. Native completion still retains real three-unit WIP and two tool units while output capacity is promised, saves that wait, completes once after actual capacity release, repeats under the next execution identity, and cancels without fabricating output/refund.

Fault assertions no longer query healthy history after the engine has latched a terminal fault. They require live-history refusal, retained last-healthy detached facts without conversion success, no queued success prefix, and save/hash refusal.

The first repaired runtime revealed that the old fault probe exposes `IResourceOutlet`, not `IResourceConversionEndpoint`. Same-endpoint conversion therefore correctly returned healthy `Incompatible` before most injected callbacks ran. The test now explicitly preserves/proves this structural refusal. Hostile primitive cases use separate input/output endpoints and arm the endpoint whose actual callback is under test. This does not add a conversion interface to the probe or change the engine to execute incompatible providers.

Nine existing hostile modes exercise short take, ignored release, short receive, unrelated stock/receipt/endpoint mutation, mutating pure cross-endpoint check, silent mutation version, and newly acquired inlet refusal. Output receive/inlet probes target the real output endpoint; other modes target the actual input. Each reaches the original transaction/fault boundary, with no advertised resumable success.

### Physical spill accounting

Both native and independent spatial-provider cases continue ordinary engine commands and immutable views through delayed finite deposition, incident recovery, timed withdrawal, real loading/travel/unloading/departure, protected treatment WIP and full conversion. Existing saved twins, pause, per-update five-unit balance, session isolation and canonical replay remain.

New final checks independently identify one raw-consumption/treated-production fact pair, exact3-to3 quantities, one fact per disposition, the original recipe cause, actual WIP consumption endpoint, actual receiver inventory, and no hidden loss/destruction. Existing transfer history identifies the real spill cell for deposit/withdrawal.

The shared physical wait helper retains its240-update ceiling and adds a20-second elapsed deadline. Timeout diagnostics show original clock age, update count, elapsed time, spill quantity, actual job IDs and process phase/blocker/progress. No duration, stock, collision, route or completion fallback was changed.

Other callers of the shared helper compile but are not runtime verified here: spill environment/history/disposal variants retain separate gates.

## Every observed build/runtime attempt

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

Literal runner uses timeout60000 /termination5000 /output5000. All returned outputs below are untruncated.

| Attempt | Case/result | Process evidence | Interpretation |
|---|---|---|---|
| Baseline | Resource contracts93/0,381ms |pid33720,exit0,outer809ms|Existing complete scoped admission/facts pass.|
| Baseline | Timed conversion0/1,90ms |pid28984,exit1,outer604ms|Missing explicit tool port; refused before play.|
| Baseline | Independent physical spill315/0,6068ms |pid34532,exit0,outer6211ms|Existing actual spill/hauling/treatment works.|
| Baseline | Native physical spill315/0,16643ms |live handle20091 polled to terminal;pid20980,exit0,outer16786ms|Not restarted at observer yield.|
| Build1 |Clean5.06s,zero warnings/errors|exit0|Tool binding, partial input and initial fault assertion edits compile. No runtime at this intermediate build.|
| Build2 |Clean5.01s,zero warnings/errors|exit0|Generic retained-fact assertion and spill deadline/fact assertions compile.|
| Resource final |**Contracts114/0**,329ms|pid7420,exit0,outer504ms|New partial receipt/refusal/saved same-endpoint completion passes.|
| Timed intermediate |61/32,473ms|pid21308,exit1,outer658ms|Eight probes never reach primitive callbacks because coupled endpoint lacks required conversion interface; healthy typed refusal is correct.|
| Build3 |Clean4.70s,zero warnings/errors|exit0|Explicit capability-refusal proof and actual uncoupled hostile callback selection compile.|
| Timed final |**100/0**,400ms|pid33096,exit0,outer550ms|Native timing/WIP/tools and all actual hostile cases pass.|
| Independent final |**322/0**,6015ms|pid8944,exit0,outer6164ms|Added deadline and exact original conversion attribution pass.|
| Native final |**322/0**,16102ms|live handle92400 polled to terminal;pid8020,exit0,outer16262ms|Added deadline and exact original conversion attribution pass.|

No executable edits after these final results. The Contracts body/factory/helpers were unchanged after its final run; the last build changed only the separate timed `Faults` method. No full/profile/family runtime or Git publication occurred.

## Remaining gates

Original P7.017 remains open: entity-output conversion/prepared creation, broader coupled provider mutation/auxiliary-state observation, remaining disposition consumers, full topology accounting and stage stress/fault breadth need their own evidence. The four cases do not prove other spill environment/history/disposal variants, arbitrary mod isolation, global performance or public-player acceptance. Optional quality/lot remains separately unselected. No staging/commit/push before full goal and plan completion.
