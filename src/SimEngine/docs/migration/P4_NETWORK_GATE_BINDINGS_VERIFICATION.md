# P4-G46 actual verification — 2026-10-09

Canonical repository Beep.SimEngine; core beep-simengine-2.389.0, save154, replay1. [Design](../../plans/enhancements/P4_NETWORK_GATE_BINDINGS_DESIGN.md). A focused reusable network-gate seam, not full P1–P12 completion.

## Actual commands and terminal evidence

```text
dotnet build Beep.SimEngine.slnx --no-restore
```

One build, exit0,14.93s, zero warnings/errors. Initially yielded session54311; the same handle was polled to terminal completion. No running build remains.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Network gate bindings: independent JSON reserve policy waits for physical resupply and resumes earned flow' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

ONE NEW exact case, ONE attempt: FIRST/FINAL191 passed,0 failed,0 aborted in924ms. Child PID30296 observed Exited/exit0 in1084ms. No execution/termination timeout, output truncation or observer error. No old/family/full/Oil/Tycoon/platform runtime. Older cases compile only; they are not rerun on2.389.0.

## Implementation and scoped proof

Native and direct ComponentDef<SignalNetworkGate> implementations share ISignalNetworkGateDefinition and sole SignalNetworkGateDeclaration; independent metadata never inherits SignalNetworkGateDef. Registered gate runtime/codec admission belongs to existing ExecutionGateContracts. Both gate and signal metadata use one NetworkConnectionContracts admission. Original SignalGateCondition still parses/interprets conditions and publishes detached gate facts. Raw source/blocker/unit identities are validated before Code construction, keeping malformed authored input inside typed content diagnostics.

InfrastructureModule admits every loaded original runtime definition, including unused entities, before Initialize. Networks and conditions must be nonempty, unique, ordered actual aliases with compatible actual source schemas. Live metadata is observed with original whole-entity/provider codec/clock guards; validated collections are consumed once. No second gate/network/inventory/state/clock owner.

Explicit master-listed JSON selects physical engine modules, never Tycoon. Existing authored world initialization creates the startup geometry/entities. Source reserve condition reads native InventorySignal availability, not a fake valve or test readiness field. Native/direct gates have equal actual chronology: first Update earns4 from source30, second earns4 more; at source22/pool8 the25-unit reserve threshold blocks further flow. Four saved idle Updates cannot accrue stock or catch-up credit.

An ordinary HaulResourcesCommand picks up and delivers5 from depot via the real truck over travel/loading/unloading time. Submission and inbound promise do not reopen the reserve. Actual receipts earn reopening; exactly one new four-unit injection leaves source23/pool12, balancing initial30 plus resupply5. Four later Updates remain blocked. View assertions only observe these outcomes; they do not implement policy or mutate gameplay.

Actual non-gated network.secondary connection reports a non-applicable gate with no conditions/blocker. Private gate reads refuse without a snapshot. Retained facts stay detached. Original clock pause, saves during wait and physical haul, interleaved saved twins, completed restoration, replay and isolated idle session are covered.

Twenty-six unused malformed authorings/implementations refuse before Initialize: absent/empty id; missing/empty/duplicate/unknown/unsupported networks; absent/empty/duplicate conditions; absent/empty source; unknown signal; empty blocker/unit; inverted range; extra field; metadata null/empty/duplicate networks or conditions; unbound original runtime and wrong registered codec. Four live Networks getter injections throw, pause the actual clock, Touch the actual entity or change its actual private sibling codec. Scoped first faults latch; subsequent gate views/save/hash refuse. No repair/rerun was needed after the first runtime.

## Source evidence and remaining scope

Selected343 executable inputs were hashed AFTER final green at2026-10-09 18:04:36 UTC. This is a post-run source inventory, not a pre-run freeze or complete transitive source/binary/environment closure. No subsequent executable edits; final metadata validation checks selected hashes, actual source anchors/links, original compound gate text/states and protected file hashes.

Original221 compound checkbox requirements retain exact text/states; offsets alone may be refreshed. Full P1–P12 remains OPEN. NetworkDef/policy dependencies, other gate migrations, Boolean/multiple conditions, dynamic topology, populated reservations, public/neutral breadth, hostile ports, recursion/private CLR/global rollback, performance, lockstep/fuzz/soak/full acceptance remain unproven. No physical corridor/incident acceptance claim. G19 authority unanswered; GameClock and protected Tycoon files unchanged. Nothing staged, committed or pushed; publication still requires actual whole-goal/plan completion.
