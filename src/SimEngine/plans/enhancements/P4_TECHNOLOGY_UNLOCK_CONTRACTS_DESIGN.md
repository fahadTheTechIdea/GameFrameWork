# P4-G36 — Contract-owned technology unlocks and physical manufacturing

## 0. Problem statement

Standard/Technology.cs retains TechnologyUnlockDef/DefinitionTechnologyUnlockDef inheritance.
Referenced unlocks are identity-checked, but unused declarations are not validated and Matches has no
explicit session context or actual-provider observation. P11 acceptance030 remains open for utility
research, independent effects and transformation-unlock execution; this stage implements its actual
recipe-unlock execution portion without pretending the other requirements are finished.

## 1. Goals

1. Replace both unlock bases with direct Def/ITechnologyUnlock and one reusable typed declaration.
2. Preserve exact entity/activity/recipe reference identity, never unlock homonyms by text alone.
3. Validate all loaded actual immutable unlock aliases before Initialize, including unused declarations.
4. Guard actual matcher callbacks through original observation/clock/map ownership.
5. Prove native and independent JSON research unlocks real timed production, with saved/replayed twins.

## 2. Design

TechnologyUnlockDeclaration<T> owns the sole target-key read/typed resolve/reference matching.
Native wrappers and independent declarations reuse it with explicit authored property keys.
ITechnologyUnlock.Matches(World,IContentDefinition) receives the actual session; no captured shared
world, global provider or definition-owned runtime state. TechnologyUnlockContracts validates frozen
actual aliases and rejects IPersistentService definitions. Its matcher uses EntityObservation.ReadProvider
with actual provider and optional actual TileMap. TechnologyService alone combines matches with the
existing learned ledger. WorkBoard and Transformer remain the research/manufacturing execution owners.
No new learned table, queue, content catalog, state codec, clock or scheduler.

## 3. API surface

Revised ITechnologyUnlock.Matches(World world,IContentDefinition definition).
New TechnologyUnlockDeclaration<T>.Read(JsonReader,string), Resolve(ContentResolver), Target and Matches.
New TechnologyUnlockContracts.Matches(ITechnologyUnlock,World,IContentDefinition).
Native Entity/Activity/TransformationTechnologyUnlockDef directly implement the existing contract.
Delete both abstract bases; migrate existing independent activity unlock to the shared declaration.

## 4. Files to change

Standard/Technology.cs; existing TechnologyContentContractsProof.cs; new public-contract proof/module
and thin player case; Program/build/protocol; stage/evidence/master and current-source audit.

## 5. Error handling

Unknown/wrong-kind target, malformed unused declarations and stateful/shared aliases fail before
Initialize. Locked recipes refuse through the same preview/command access rule without queue/output
allocation. Matcher exceptions and observed provider/map/clock/identity mutation fault before a result,
hash or save. No permissive fallback, instant research/production, grant or substituted target.

## 6. Compatibility

Development only. Remove obsolete bases/signature; no old reader/shim. Build identity advances.
Save154/replay1 and existing learned/work/processor/physical schemas remain unchanged.

## 7. Verification

ONE NEW exact bounded case: Technology unlock contracts: research earns physical manufacturing.
Native/independent entity/activity/recipe bindings and homonyms, real research and recipe supply/WIP/
time/output, private facts, partial saves/twins/pause/replay/isolation, JSON changes and early/fault cases.
No historical, family, full, Oil, Tycoon or platform execution. Build only as needed.

## 8. Risks

Scoped guards are not a whole-world/CLR sandbox. Utility research, broad independent effects/providers,
all workflows/stress/global accounting/full P11/P1-P12 acceptance remain unproved.

## 9. Out of scope

Tycoon and G19 authority, Draw/platforms, second research/production lifecycle or defaults.
No commit/push until the whole original goal and plan are finished and verified.

## 10. Sub-task order

Inspect original owners and open requirements; replace declaration/contract/bases; validate all actual
unlocks and guard matching; migrate existing proof; add physical research/manufacturing public proof;
compile; freeze selected inputs/run one bounded exact case; update honest staged evidence/master/audit.

## 11. Status

Implemented direct unlock definitions/shared declaration, actual session matching and all-loaded
immutable alias validation in original owners. Core2.373.0/save154/replay1.
ONE NEW exact case FINAL123/0/0 in1970ms;198 selected inputs frozen12:58:07UTC.
[Scoped evidence](../../docs/migration/P4_TECHNOLOGY_UNLOCK_CONTRACTS_VERIFICATION.md).
P11 utility research/independent effect breadth and full P1-P12 remain OPEN; no publication.
