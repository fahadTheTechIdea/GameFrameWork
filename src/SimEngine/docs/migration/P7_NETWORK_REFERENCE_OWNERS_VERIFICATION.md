# P7-ER8 — Connected flow replacement reference verification

Date:2026-10-14. Canonical framework build2.565.0/save157/replay1; network service codec6 unchanged. [Design](../../plans/enhancements/P7_NETWORK_REFERENCE_OWNERS_DESIGN.md).

## Ownership and behavior

The existing NetworkService implements IEntityReplacementReferenceOwner. A responsibility-specific partial handles its existing FlowCursor bindings, not another service or ledger. Admission validates exact actual network/resource/direction, retained component key/runtime/codec, native actor endpoint and unchanged effective transfer quantum against the existing pool.

Commit validates actual destination ports, native endpoint/company/resource acceptance and quantum before changing any cursor. It rewrites only Entity and the entity-owned Source/Destination endpoint, then uses the original flow ordering. Elapsed, LastTick, Quantum and actual pool identities remain unchanged. FlowTurn is historical successful-service ordering and is deliberately not rewritten.

Normal Flow continues to evaluate gates, demand, current pools and readiness. Inactive/interrupted/changed-pool cursors still reset or expire normally; retained elapsed time is not banked resources or a promise of throughput. No clock, payload, registry, topology or resource-accounting duplicate.

## Public physical proof

network-replacement.json is explicitly embedded and master-listed by proof.references. Existing physical corridor construction consumes six of twenty real supply units. Eight success scenarios combine push/pull actors, minute/half-minute clock configuration and native/independently compiled port contracts.

An ordinary three-minute inspect upgrade earns replacement while a slow one-unit-per-seventeen-minutes network flow is in progress. The original elapsed phase and pool persist, actor/endpoint identities rebind, and the next real unit transfers at the retained deadline with no early transfer, restart or duplication. Material remains14 through all post-build upgrades/transfers.

Pending-upgrade and rebound-flow saves continue as twins; prior detached facts stay unchanged, private actor timing is withheld from the other company and an independent interleaved session remains unchanged. Six refusal variants, using native/direct ports, cover removed connection metadata, reversed direction and missing carried resource with preview/inbox parity and coherent saves.

## Affected fixture correction

NetworkRateTests used the old shared resource-recovery sample with an unused Tycoon supplier and navigation tied to a different space. Its authored copy now omits the supplier, binds the existing navigation and condition files to recovery.tiles/road and explicitly selects TileConditions. Resource amounts, map size, rates, physical flows and business rules are unchanged. This is content selection/authoring, not simulation in a driver. The original shared sample and Tycoon implementations are untouched.

## Commands and final results

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Network replacement references: slow connected push pull timing and saved physical upgrades" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Network rates: slow physical legs, blocked intervals and saved continuation" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 18000
~~~

Final solution6.74s/exit0/0 warnings/errors. New exact408/0 in3447ms, observer3615ms, PID25908/exit0. Affected slow-flow exact45/0 in5124ms, observer5283ms, PID29468/exit0. Total453/0 across two exact cases; normal termination, no watchdog truncation/observer error. No executable edits after finals.

Earlier observations, not final proof:

- Initial solution2.31s failed four compile errors from using the endpoint identity contract for capacity/acceptance. Corrected to the existing IResourceInlet contract; solution9.15s clean.
- Initial new308/0 in3695ms, PID40144/exit0/3889ms; before stronger real-transfer deadline assertions.
- Strengthened new408/0 in4122ms, PID24224/exit0/4350ms; solution8.28s clean, before affected fixture edits.
- Affected0/1 in163ms, PID44948/exit1/388ms: unused supplier requires inactive Tycoon. Fixture copy removed the supplier; solution5.55s clean.
- Affected0/1 in183ms, PID33648/exit1/360ms: navigation still selected colony.tiles. Authored navigation rebound; solution6.65s clean.
- Affected0/1 in236ms, PID31276/exit1/404ms: navigation needs its explicit condition provider. Authored conditions rebound and module selected; final solution6.74s clean.

## Remaining scope

Independent/shared actor endpoints, changed-quantum provider stress, arbitrary topology/gate/fault/replay/lockstep breadth, active work/shared reservations and other reference owners remain open. Utility and non-tile breadth beyond the tested point flow is not inferred. Historical fairness IDs remain historical; broad fairness stress is not claimed.

Original221 requirements/194 recorded checked/27 open unchanged; no original gate closes. Additional320. No Oil, Tycoon/business, sole clock, platform/rendering/adapter or host gameplay changes. No full regression, staging, commit or push; full goal and plan unfinished.

Metadata verification:2224 active source anchors/13 quickstart anchors/2009 selected executable inputs match current files;221 original requirement literals/checkboxes/line anchors verified without errors.8115 local links resolve and18 master rows are valid. New public driver has no direct World/resource/progress writes. Diff check exit0; sole clock and three protected Tycoon source hashes unchanged; staged files0 in both repositories. No executable drift after finals. Metadata checks are not additional game tests.
