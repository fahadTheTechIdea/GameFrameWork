# P12-E1 — Persistent terrain cleanup through physical response work

Date2026-10-14. Engine beep-simengine-2.593.0/save157/replay1. Existing spatial/work/incident/resource payloads remain unchanged. No Oil/Tycoon/platform/Draw/host gameplay or Git writes.

## Mechanism and ownership

Optional `sim.tile-terrain-work` registers `tile_terrain_work_effects` only; it introduces no Update system, terrain store, clock, job or progress ledger. `ITileTerrainWorkEffectDefinition` binds actual layer metadata and an exact target value. One strict declaration authors this content; one shared contract rule validates selected frozen aliases, layer/sample capability and fixed worked footprint. Native registration implements contracts directly on Def, not a required effect family base.

Existing PhysicalWorkEffectContracts owns positive-time machine ancestry and earned order/node completion. Existing ITileSampleMutationSpace owns whole-footprint publication. Actual target ownership/placement and private-layer ownership remain required. Resource-quantity channels are rejected both by file resolution and shared runtime admission: resetting terrain cannot delete actual stored/reserved material. Byte/int/fixed native value compatibility comes from the selected original sample layer, not a copied conversion rule.

One explicit master-listed proof document authors the persistent contamination channel, terrain effect, cleanup requirement, complete repair -> cleanup response graph, phase mapping and initial scenario terrain. Source/target episode components select that file-authored workflow. Original incident response and propagation, DAG, transporter, stock and clock owners execute it. The old response definition remains separate authored content, not a compatibility/runtime path or parallel execution owner.

## Focused physical proof

Separate exact cases run native TileMap and external public-only SensorTileSpace. Both use their original durable layer owner and original native machinery. Four cells initially carry authored contamination value12; this is terrain state, not conserved material stock or scientific contaminant mass. Incident enrollment does not clear it. Normal delayed propagation starts a target episode.

Each response receives two units by actual player-requested hauling. Repair consumes one unit over six authored minutes; dependent cleanup consumes another over four minutes. The new physical terrain effect publishes zero only when cleanup earns completion. Partial cleanup leaves both cells dirty. Saved twins, one paused clock and disabled worker retain actual progress and dirty samples; resumed work clears only the completed target footprint. Source cleanup leaves the target untouched. Two repairs/two cleanups consume four of the original12 material units, leaving8. Clean terrain remains durable after completion. Existing episode history, final saves, independent session and ordinary command replay match.

Missing effect-layer reference, fractional byte result and resource-quantity reset definitions refuse before play on each provider. No driver mutates terrain, stock, incident episodes or work progress. Public commands and immutable work/incident/inventory facts drive the workflow; a read-only friend-test assertion inspects actual sample contracts to verify terrain values. It is not a public host terrain-query acceptance claim.

## All attempts

1. Targeted test/content compile clean6.71s, zero warnings/errors.
2. Initial independent exact case183/0,2756ms; PID20856 exit0/2898ms.
3. Initial native exact case183/0,7407ms; PID25300 exit0/7549ms.
4. Move complete response graph/phase authoring into the master-listed JSON add-on. Composition only selects its workflow IDs; no graph nodes are built by C# driver code. Production mechanism unchanged.
5. Final targeted compile clean7.24s, zero warnings/errors.
6. FINAL independent exact case183/0,2780ms; PID5808 exit0/2930ms.
7. FINAL native exact case183/0,7416ms; PID13124 exit0/7570ms.

Final distinct successful checks366, no failed attempt. Two literal case names only; no full/profile/family runs. All results untruncated and terminal. No executable edits after final runs. Earlier incident/spatial cases compile only and were not rerun.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Persistent terrain cleanup: independent tile repair supply and earned footprint publication" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Persistent terrain cleanup: native tile repair supply and earned footprint publication" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
```

## Review and remaining requirements

One responsibility per owner: metadata declaration, work-effect admission, original earned work, original atomic spatial data and original incident lifecycle. No native TileMap dependency inside the mechanism, extra scheduler, stock mirror or compatibility facade. Every authored effect is validated even when unused; private/movable target checks are source-reviewed, not new multi-owner physical acceptance tests. Mutation batches retain existing collection/allocation constraints, not a new world-size policy.

Automatic spill deposition/accumulation, weather/network/terrain chemistry and transport, capture/disposal of contaminant resources, contamination objectives/metrics, all private/mod/host-query surfaces and global fault/concurrency/lockstep/fuzz/soak/allocation matrices remain open. This stage closes no broad P12 checkbox or full engine requirement. Frozen original counts remain historical; whole plan/goal and final publication are incomplete.
