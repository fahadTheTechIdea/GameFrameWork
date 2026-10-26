# P11-A3 — Contract-owned utility-dependent physical research

## 0. Problem statement

P11 acceptance030 still lacks utility research. Existing UtilityWorkRequirements.cs uses an
inheritance-only leaf reader and UtilityContracts.Read observes entity scalars rather than the
actual consumer's persisted codec. Private provider mutation can therefore escape that boundary.
This stage replaces the obsolete utility leaf base, not the existing work or utility execution.

## 1. Goals

1. One immutable UtilityWorkRequirementDeclaration authors explicit named consumer identity.
2. Native target/worker leaves and independent direct IWorkRequirement JSON definitions reuse it.
3. UtilityContracts.Read guards actual provider metadata, codec, clock, identity and optional map.
4. Ordinary staffed research consumes materials and advances only while actual utility is ready.
5. Shutdown and shortage retain progress; normal physical hauling and restart permit completion.
6. Interrupted saves, private views, pause, replay and independent sessions use original owners.

## 2. Design

The declaration selects an explicitly supplied target/worker and reads the actual named consumer
through UtilityContracts.Read. No claim, stock, utility burn, worker selection or progress exists
in the declaration. UtilityWorkRequirementDef is deleted; native leaves extend WorkRequirementDef.
Independent definitions implement IWorkRequirement/IDependencyNode directly and delegate to the
same declaration. The existing WorkBoard all/any evaluator and WorkSystem execute without type cases.

UtilityContracts becomes public so independent consumers can reuse the guarded original reader.
ReadProvider captures the actual Component.Sync before metadata/callback reads and validates facts
against existing native ports, units, capacities and reservations. Actual provider/slot is rechecked.
The optional TileMap scope is the existing observation scope, not another world state snapshot.

Native Transformer burns a JSON-authored four-minute supply rate over time in its original processor.
Research targets that facility and uses the existing eight-material requirement plus utility
readiness. A separately authored carrier refuels through HaulResourcesCommand, never stock writes.
An independent utility capability forwards the original guarded processor facts, with its own
identity only; it has no copied burn/progress ledger. Its fault counter instruments its actual codec.

## 3. API surface

UtilityWorkRequirementDeclaration.Read(JsonReader,string), Consumer and
Evaluate(WorkRequirementContext,Entity). Public UtilityContracts.Read(World,IUtilityConsumer).
Native section names and consumer JSON field remain; no compatibility base or reader.

## 4. Files to change

Standard/UtilityWorkRequirements.cs, UtilityConsumers.cs; compile-only historical utility test
reference; new public-only utility proof and master-listed JSON; one thin player case, Program,
build/protocol identity; this stage, verification, master tracker and completion audit.

## 5. Error handling

Malformed/missing consumer key fails content loading. Missing consumer is typed pending;
foreign owner is blocked; inactive/shortage retains native blocker and actual progress.
Provider exceptions or observed private codec, clock, identity or map mutation fault before a
result is returned, and faulted sessions cannot advance, hash or save. No invented readiness.

## 6. Compatibility

Development only; delete obsolete base and migrate callers. Save154/replay1 and existing native
work, learned, processor, resource and clock payloads remain unchanged. New optional proof
component owns only its own declared codec; no old reader or default content.

## 7. Verification

ONE NEW exact bounded case: Utility research contracts: physical supply governs earned knowledge.
Build only for compilation. Selected executable inputs freeze before the final attempt.
No previous, family, full, Oil, Tycoon or platform runtime. Optional map guarding is source-only
unless specifically exercised; do not infer it from the non-tile player scenario.

## 8. Risks

Readiness is current observation, not future supply reservation. Guard scopes are not a CLR sandbox
or whole-world rollback. Wider providers/effects and original P11 acceptance030 remain open.

## 9. Out of scope

Tycoon/G19 scope authority, Oil/game hosts, Draw/platforms, new scheduler/clock/catalog/stock owner.
Commit/push deferred until the entire original goal and plan are genuinely complete and verified.

## 10. Sub-task execution order

Inspect existing owners; replace shared declaration/base and guard; author independent module/JSON;
prove physical research and interruption; compile/freeze/run the one new case; update scoped
evidence/tracker/audit without changing original completion gates.

## 11. Status

Implemented and scoped-verified on2.374.0/save154/replay1. FINAL144/0/0 in2479ms;203 selected
inputs frozen2026-10-09 13:25:20 UTC before the final attempt. Native/independent utility research,
shutdown/shortage, real carrier refuel using two bays and separate authored operating areas, saves,
sole-clock pause, private facts, replay/isolation and actual provider codec faults pass.
[Evidence](../../docs/migration/P11_UTILITY_RESEARCH_VERIFICATION.md).
Original P11.acceptance030 remains unchecked for broader independent provider/effect breadth;
full P1-P12 remains OPEN. No commit/push.
