# P4-G43 inventory signal bindings — scoped verification

Current M101/native2.737/save161/replay2: original inventory case262/0/0 after actual
256/6 baseline. Fourteen fresh/restored faults preserve first causes; native/direct
complete0→32/2input/32checkpoint replayEEFEFF058CBB334D. Duplicate finally guard removed,
same inventory/signal/work owners. [Every shared attempt, exact scope and handoff](P10_SIGNAL_CONTENT_CONTRACTS_VERIFICATION.md#current-m101-shared-inventory-network-and-utility-metadata-observation--2026-10-22).

## Earlier G43 evidence — 2026-10-09

Date: 2026-10-09. [Stage](../../plans/enhancements/P4_INVENTORY_SIGNAL_BINDINGS_DESIGN.md).
Core `beep-simengine-2.386.0`, save154/replay1. Full goal and P1–P12 remain OPEN.

## Implementation boundary

Original stateless InventorySignal now selects IInventorySignalDefinition, not native InventorySignalDef.
Native and independent ComponentDef registrations delegate one strict InventorySignalDeclaration.
Required inventory identity and explicit null/nonempty native threshold bill are JSON-authored.
Original inventory, promises, capacity, ResourceQuantitySignals, work/incident, entity Update and
GameClock owners remain authoritative. No copied stock, threshold cache, state store, compatibility
base, scheduler, new signal algorithm or runtime timer. Codec1 and signal schema3 are distinct and
unchanged. The existing metadata-only module validates all loaded reusable bindings before Initialize,
including unselected entities. Actual frozen component/owner/inventory/policy and typed metric aliases
are required. Live observations reuse actual whole-entity/provider codec and clock guards. The proof
installer Update is empty; it only installs negative metadata getters.

## Actual build and runtime

Two `dotnet build Beep.SimEngine.slnx --no-restore` invocations, both exit0, zero warnings/errors:

| Build | Seconds | Note |
|---|---:|---|
| 1 | 14.40 | Terminal result observed on original session39398; not restarted |
| FINAL | 6.11 | Removed an unsupported id field from the reused private negative probe's JSON |

The proof-only authoring repair was found by reading its strict empty declaration before runtime;
no engine behavior changed between builds. ONE NEW exact case ran once:

`Inventory signal bindings: independent JSON facts follow physical stock and saved recovery`

Result:176 passed,0 failed,0 aborted in978ms. Child PID14452 observed Exited, exit0, elapsed1129ms.
Execution limit30,000ms, owned termination limit5,000ms, output limit30,000 characters. No timeout,
stdout/stderr truncation or observer error. No older, family, full, Oil, Tycoon or platform suite ran.
Historical cases compiled only and are not current runtime evidence.

## Proven scope

Native/direct metadata have equal actual physical recovery timing. One real machine travels4→20
before a repair consumes eight actual supply units: stock20→12. All five numeric facts (stored,
available, inbound, outbound, receiving-room) and three readiness facts use the original inventory
algorithm; thresholds16 reflect the real shortage after that physical spend. Explicit numeric-only
sources report the existing no-threshold blocker, never invented readiness. Repeated observations
are pure and returned scalar snapshots remain detached. Foreign company inspection is refused.
The sole clock pauses work/hash; saved twins resume the same physical work and completed stock.
Normal commands replay; interleaved fresh sessions retain separate stock/incident ownership.

Eleven malformed unused binding variants reject before Initialize: absent inventory/quantities,
empty bill, unknown resource, negative quantity, over-capacity bill, null inventory, empty runtime bill,
wrong signal schema, null runtime metric bindings and original runtime without focused metadata.
Four live getters throw, pause, Touch or corrupt a sibling private codec without Touch. Actual first
fault phase is checked; faulted signal reads, saves and hashes refuse rather than publish healthy facts.

## Evidence limitations

Selected320 executable inputs were hashed AFTER green at2026-10-09 17:22:51 UTC. This is a post-run
snapshot, not a pre-run freeze or full transitive closure. No executable edits followed. Hash, links,
table and anchor checks only establish metadata integrity, not runtime regression.

Not proven: populated inbound/outbound promises, fractional/fluid/scaled units, alternative capacity
providers, native tile scenarios, all getters/recursion/global CLR mutation, performance/allocation,
lockstep/fuzz/soak and full acceptance. Scoped guards are not global rollback or a CLR sandbox.
Original221 compound gate requirements/checkboxes are preserved. G19 authority remains unanswered;
protected Tycoon files, GameClock, Oil and hosts/platforms are untouched. No staging/commit/push.
