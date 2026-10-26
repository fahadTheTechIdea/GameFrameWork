# P7-C3b2b5b2a — Authored physical cargo recovery and effect admission

2026-10-05; working-tree **beep-simengine-2.42.0 / save 18**. This is a prerequisite inside
the open P7/P9/P12 scope, not full cargo-loss/incident policy or framework completion.
The lifecycle skill guided the existing stage's execution contract, scoped proof and tracker update.
Only the two exact names below were executed; no full/broad regression was run.

## One owner per responsibility

WorkBoard remains the only plan admission/order/job authority. Optional IWorkEffectPlanValidator
lets an authored effect declare capability and physical-plan requirements inside ValidatePlan,
both before system initialization and on normal Post. There is no second validator or registry.
WorkModule registers resource_recovery_effects through the same master-listed content path.
Selected recovery work without IResourceRecovery fails explicitly before gameplay, rather than
substituting ConstructionService, a tile map, a fabricated service or an instant fallback.

PhysicalWorkEffectDef is the common admission/preview/completion owner for InventoryEffectDef and
ResourceRecoveryEffectDef. Readiness checks require the actual board order, target, company,
authored effect invocation and response binding. Resource checks are pure and target-version guarded.
Apply rechecks current readiness and requires the actual completed stage, or completed whole order,
before delegating to its existing resource coordinator. An unfinished/mismatched invocation returns
work.effect-not-complete without mutation. This is normal engine execution admission, not a CLR
security sandbox; trusted modules still have lower-level resource execution contracts.

Physical effect admission requires positive-time machine work in the stage's dependency ancestry.
An unrelated parallel machine is insufficient. Explicit zero-time finalization after a real machine
dependency remains valid; final effects require machine work in the completed plan. The check uses
existing resolved AfterIndex and temporary admission facts, not another persisted DAG or work ledger.
Timer-only/zero-work recovery and negative-effect plans fail instead of becoming instant disposal.

ResourceRecoveryEffectDef depends only on IResourceRecovery. Moving targets and unrelated current
source jobs yield typed blockers; the same completing job may recover its own source. Active stock
promises, incompatible packing and other selected recovery-provider guards remain authoritative.
Completion delegates to the existing evacuation owner. ConstructionService's selected tile recovery
packs current live stock locally into real receiving endpoints using owned receipts and verified
StockTransfer facts. It does not manufacture structural yield, grant opening stock again, duplicate
cargo, publish fake loss/production or delete/release the held source machine.

Crew travel, authored preparation input and work time execute through normal player commands and
WorkSystem. Recovered endpoints then use normal physical hauling to compatible stores. Cancellation
retains real cargo and already spent inputs; a new explicit order can retry after a real blocker clears.
Recovery is not automatically forced on every broken/fuel-starved carrier: those selected game policies,
phase graphs and condition providers remain planned.

## Content, persistence and extension boundary

The existing resource-disposition.json is extended with ordinary recovery plans: a timed work effect,
a zero-time dependency finalizer, a whole-order finalizer, and native loss work. The existing extra
document/master/Colony.Factory remains the loading path. Existing fixture helpers are shared by the
new tests; no second game factory, scheduler, disposal store or host gameplay loop is added.
ExtensionProof remains a normal public-contract assembly without friend access.

Runtime stock/order payloads and schemas are unchanged; save format remains 18 while the build identity
changes. No old reader or compatibility path is added. New effects use existing endpoint/history codecs;
work/logistics/incidents views remain schemas 5/3/2. Initialize/Update/World.Step and entity-owned state
settlement remain unchanged. Draw, Unity/Godot, demos, Tycoon and oil files were not changed or tested.

## Final focused verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Cargo recovery: timed evacuation, native receipts and saved physical salvage'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Cargo recovery: unsafe sources, owned claims and authored completion admission'
```

Final build: **zero warnings/errors**, 1.48 seconds. Final exact suites:

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Timed evacuation, native receipts and saved physical salvage | 90 | 0 | 257 ms |
| Unsafe sources, owned claims and authored completion admission | 47 | 0 | 234 ms |

**137 checks / two exact suites**, zero aborted suites. Earlier iterations used the same two names.
Strengthening the missing-provider assertion exposed an unrelated malformed proof order; the fixture's
required automatic_supply setting was supplied, and the final assertion now verifies the actual
missing IResourceRecovery refusal. This is not inferred from any generic ContentException.
The preceding 2.41.0 disposition suites (144) compile but were not rerun; they are not added to this count.

The physical suite proves normal player holding/posting, another machine's actual travel/material/work,
no premature recovery, partial-work save/load/twins, exact mixed native source/receiving receipts,
no loss/production compensation, private history, local endpoint creation, saved downstream physical
pickup/unload pairs, dependency and whole-order finalization, and real timed consumed/destroyed/lost
effects through the common execution owner. Waits have fixed bounds and engine tick/fault diagnostics.

The guard suite proves other-owner promises survive, early recovery/destruction cannot spend ready stock,
moving/busy sources report current blockers, normal cancellation retains cargo/spent materials, saved
last-work-tick claims refuse without transfers, explicit retry after release, rejection of instant/timer-only
and unrelated-machine workflows, and the exact optional-service admission failure.

## Open gates

- Remaining b2: authored automatic breakdown/fuel/stranding/loss/spill policy, retention/restart behavior,
  persistent incident/environment phases, complete failure matrices and full native accounting.
- P4 activity-DAG/provider migration, any-provider/tool claims and generic construction; P9 areas/fleet/
  stock/work priorities, amount-scaled docks/loading, congestion, retry/fairness and stress gates.
- Non-tile IResourceRecovery implementation/phase proof and arbitrary hostile-provider effects are not
  claimed by the selected tile coordinator proof. Required-provider failure in a point game is proven;
  successful point recovery is not.
- Entity-reference rebinding, full regression, oil consumer integration and allocation/soak budgets remain
  open. Tests are scoped evidence, not complete engine acceptance or scientific/environmental modeling.
