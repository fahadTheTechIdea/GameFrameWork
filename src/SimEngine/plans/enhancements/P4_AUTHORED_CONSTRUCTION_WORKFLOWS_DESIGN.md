# P4-G4a — Authored construction and removal workflows

## 0. Problem

ConstructibleDef.ReadCore independently parses construction/removal node arrays, including a stale
mandatory hours check incompatible with the canonical rational-duration node reader. Each prototype
recompiles a private graph instead of referencing the existing authored activity catalog. G4 requires
bound workflows to use file-authored contracts, not another parser, scheduler or progress owner.

## 1. Goals

1. Construction JSON explicitly references a selected ActivityDef through activity. Removal, when
   authored, references an activity under demolish alongside its existing explicit salvage bill.
2. ActivityDef is the only owner of authored node parsing/compilation. Bound plans alias its immutable
   Graph, supply policy and removal priority. No cloned graph/node list or old inline reader remains.
3. Binding profiles are deliberately non-player-orderable, unrestricted by target tags, and have no
   standalone on_complete. Native construction/removal finalization remains the existing component
   execution contract; node effects stay authored. Reject unused conflicting policy rather than ignore it.
4. Optional removal is genuinely absent, not an empty fabricated graph. Existing views/handlers refuse
   it with NotConstructible. The existing board/site/material/recovery/entity owners remain unchanged.
5. Migrate canonical authored JSON directly, then prove native staged construction/removal and shared
   graph aliases through one bounded player case; no harness rules, full suite or platform changes.

## 2. Design and API

Standard/Construction.cs removes private authored arrays/read loops/compiler and resolves activity
references from the same immutable selected catalog. ConstructibleDef.Graph/AutomaticSupply delegate
to the selected IActivityDefinition. Private DemolishPlan becomes binding-only, delegating graph/supply/
priority to its selected profile. ActivityDefinitions omits absent removal. CheckDemolition is null-safe.
No new public scheduler/store/codec. Required explicit activity field rejects old nodes/automatic_supply
components; canonical ActivityDef still owns executor/duration/requirements/dependencies/effects.

Profile references validate before Initialize: known actual selected ActivityDef, non-orderable, no
target tags or standalone final effect. Construction and authored removal must contain positive physical
machine work. SupplyCapacity derives from that same graph once, preserving actual native admission.
No implicit graph, compatibility conversion, fabricated defaults or runtime-generated workflow.

## 3. Files

Construction.cs/SaveGame.cs; affected JSON/inline fixture sources; one native proof/content files,
Program/resources; stage/master/P4 umbrella/AGENTS/evidence/scoped manifest. Work/clock/save byte owners
unchanged. Build identity advances; save139 and component/board/layout versions unchanged.

## 4. Verification

Build only after complete source migration. ONE exact distinct NEW bounded native player case:
master/startup data, real stationary machine timing, reverse authored dependency order, no early
completion, same profile/graph alias, construction plan/site checkpoints, real removal timing,
private immutable progress, missing/invalid profile refusal and unsupported removal semantics.
Existing fixture migrations are source/compile-only; no old/family/full/platform/stale gates.
Record every build/run, repair only observed failures, freeze code/tests/JSON after final green,
then docs-only inventory/tracker handoff. Full goal remains active; no per-slice commit/push.

## 5. Remaining scope

Other generated infrastructure/process/team/haul workflows and all G3/G4/G5 broader purity/fault/
replay/stress/CPU/full requirements remain open. Oil/platform/hosts/demos/Draw/business out of scope.

- [x] Resolve selected authored profiles and delete old construction/removal parsing paths.
- [x] Direct JSON/source migration and one native bounded player proof.
- [x] Focused verification/evidence/tracker/hash-link handoff.

Verified2026-10-07: ONE clean build11.23s (original68272 resumed), ONE exact NEW bounded case
61 checks0 failures/aborts499ms first attempt.107 older identity assertions and four JSON/nine C#
fixture migrations source/compile ONLY. Source frozen after green.
[Evidence](../../docs/migration/P4_AUTHORED_CONSTRUCTION_WORKFLOWS_VERIFICATION.md). Full G4/goal open.
