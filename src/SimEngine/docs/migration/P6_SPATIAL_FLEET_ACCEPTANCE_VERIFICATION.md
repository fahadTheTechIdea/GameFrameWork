# P6-I4 independent spatial fleet — focused evidence

Date2026-10-15. Engine2.605.0/save159/replay1 unchanged. Completes the composed
P6 Game B flow for this authored independent tile scenario, not the whole A/B/C,
industrial-colony goal or global logistics/replanning guarantees.

## Content, contracts and original owners

SpatialColonyProofModule is a separate-assembly content-only module. It declares
the existing IndependentWorkerDef under colony_worker and explicit dependencies
on Work, Construction, TileNavigation and the independent tile provider. That
definition reuses the original WorkerDeclaration and native Worker runtime/codec.
It adds no worker implementation, scheduler, saved work state or role fallback.

The new explicit spatial-colony-master.json lists14 actual modules, original
independent tile world/startup/navigation/layer conditions/work/construction files,
access/work retry, and new spatial-colony-fleet.json. Both new files are explicitly
embedded. Fleet JSON authors a30-second clock quantum, native hauler/builder/blocker,
independently declared fabricator, two ordinary empty stores, and three impassable
layer rectangles leaving crossings at(10,10)/(10,20). It selects the actual external
tile provider; the32x32 dimensions belong to that scenario, not an engine limit.

The original construction document adds sensor.fabricator as an alternative role
in its existing build node; sensor.inspect remains valid. Original depot12 steel,
8-material typed requirement, four-minute work, construction/site types and finish
effect remain unchanged. No copied construction model or extra stock.

Original NavService/Mover compute and execute routes. Existing WorkBoard, Logistics,
inventory/transactions, stock policy, worker control, construction and sole game
clock retain all decisions and physical state. The test driver composes authored
content, submits commands, reads public views and verifies invariants. No mutable
World access, direct placement/assignment/stock/terrain writes or host gameplay.

## Complete executed flow

1. Verify original total12 steel and no business. Hold actual hauler/native builder
   via SetWorkerHeldCommand. Create and cancel an unstarted project through normal
   commands; site disappears, no completed structure and all12 material remain.
2. Configure low-priority10 and high-priority80 stock policies, each requesting8,
   while carrier is held. Release carrier. High request wins despite low being
   posted first. Observe a real partial load, checkpoint its actual handling/receipts,
   then continue saved twins until all8 cargo has physically started the shorter
   crossing route. Both stores still empty: assignment/loading is not delivery.
3. Move the ordinary blocker from(12,10) into(10,10) using MoveVehicleCommand.
   Save with actual changed occupancy and loaded native route. Pause both clocks
   and prove no simulation-age change, then resume. The same assigned haul, carrying
   stock and with zero observed failures, moves down to the other crossing near
   y20. No crossing/overlap/teleport or replacement haul is used to pass the proof.
4. Continue saved twins through both physical deliveries and carrier home return.
   High store8, low store4, all12 material conserved. Before the original haul
   disappears from the intentionally compacted live-job view, actual high delivery
   must already be8; early disappearance/failed visible states fail with diagnostics.
5. Hold carrier, disable the high reserve by normal policy command, and order a
   priority100 project. Low reserve remains protected. Release carrier; original
   automatic supply physically delivers8 available high-store material. Original
   work selects the independently declared fabricator and sensor.fabricator role,
   not the held native sensor.inspect builder.
6. Observe incomplete productive work, exact8 required half-minute updates (original
   four-minute duration), native working entity state and exact8-material debit.
   Save/load partial work and complete physically. Exactly one facility remains,
   temporary site is retired and total4 material remains.
7. Finished save/load and canonical player replay hashes match. A separate untouched
   session retains its original topology/resources. Retained opening spatial views
   remain detached. Every observed update checks nonoverlapping physical tile
   occupancy and total material12 or4, never an intermediate fabricated amount.

No delays/bounds/reach were extended to make this flow pass. Waits are bounded480
updates/20 diagnostic wall-clock seconds; outer case timeout60000ms.

## All build/runtime attempts

Targeted build command:

`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`

1. Build failed2.96s: new test omitted Maps namespace for TileSpatialSnapshot.
2. Build failed4.42s: new test referenced nonexistent FrameworkProofContent.OpenAccess;
   corrected to original FrameworkProofContent.Access. No runtime after failed builds.
3. Build clean4.53s, exit0 before first runtime.
4. New exact spatial case79/0,5907ms; PID29828 exit0/6043ms.
5. Added partial-loading checkpoint and stronger same-haul continuity observation;
   targeted build clean4.39s, exit0.
6. Same exact spatial case45/1,2888ms; PID17340 exit1/3021ms. The added final Single
   lookup assumed completed automatic jobs stay in the live-job view. Original
   owner intentionally compacts them. Corrected test to observe live ID/state/
   failure continuity and require actual delivery before disappearance; no engine
   history retention or duplicate store was added.
7. Separate exact compiled module audit10/0,120ms; PID16936 exit0/253ms. Audited
   ExtensionProof6006 bodies/30130 member references and QuickGame.Module5/15,
   zero boundary violations. Audit preceded only the test-observation correction;
   external module code did not change afterward. Not a security sandbox/global audit.
8. Targeted correction build clean5.48s, exit0, zero warnings/errors.
9. Final same new spatial literal81/0,5772ms; PID18000 exit0/5907ms. No output
   truncation/abort/timeout. No executable edits after final.

Exact runtime commands use the built test DLL, `--bounded-exact`,60000ms timeout,
5000ms termination timeout and4500 output chars:

- `Independent spatial colony: scarce stock priorities, loaded route detour and alternative-role construction`
- `Framework independence: compiled external modules use public engine contracts only`

Only the new spatial literal and relevant compiled audit ran. No full/profile/family
runtime and no historical independent construction cases rerun. Shared alternative
role metadata therefore does not prove every older test's assumptions were updated.

## Remaining original scope

Game B's composed requirements are covered in this specific independent-provider
scenario. Native geometry permutations, blocked-all-routes, endpoint loss, fuel/
breakdown/recovery/queue contention, cancellation after consumption/loaded cargo,
custom hostile providers and general deadlock/stress guarantees remain unproven here.
Alternative metadata reuses Worker intentionally; this is not a second independent
worker runtime/codec proof. Untouched-session isolation here is not the separate
all-optional-modules genuinely advancing interleaving requirement.

The complete original Game C assembly extension (worker/project/endpoint/signal/
incident response together), broader A/B/C/global gates and full industrial plan
remain open. Original broad checkbox/count metadata221/195 recorded checked/26open
is unchanged, not a new audit. No engine implementation, Oil/Tycoon, host/demo,
platform/Draw integration, compatibility path or Git publication in this slice.
