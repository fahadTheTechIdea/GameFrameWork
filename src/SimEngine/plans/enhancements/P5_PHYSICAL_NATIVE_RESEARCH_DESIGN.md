# P5-A33: Physically supplied native research facilities

Current M150/native2.772: existing canonical physical case1418/0; two276/1
placement-fixture attempts retained. [Current evidence and handoff](../../docs/migration/P5_PHYSICAL_NATIVE_RESEARCH_VERIFICATION.md#current-m150-physical-canonical-research-continuation--2026-10-23).
No native/Oil gameplay/content change or original compound gate closure.

## 0. Problem

P5-A32 proved native research using authored HQ capacity. Canonical research_lab still needs evidence
that actual construction, supplies and power availability govern native research progress.

## 1. Goals

1. Use only public player commands/views and the actual Oil engine composition.
2. Build canonical lab through original6h clearing/30h erection and physically hauled materials.
3. An unfinished or unpowered facility contributes no research capacity.
4. Construct canonical grid power through its original4h clearing/16h erection.
5. Save productive work, shut down/restart the actual lab and continue identical native research twins.

## 2. Design

No new gameplay implementation is planned unless this focused flow exposes a real engine defect.
Original construction/work/logistics/resource/operation/knowledge/clock owners execute the scenario.
The test driver never writes stock, positions, work, progress, power or learned facts.

Proof JSON selects a24x24 sandbox, founding fleet of bulldozer/crane/two haul trucks/repair truck,
electrical stock80 and spares40. These are explicit scenario inputs, not runtime defaults or engine
limits. Canonical lab/grid prototypes, material bills, machine rates and construction durations stay.
The study retains canonical120-day nominal work:172800 units at original lab
rate12 require14400 productive minutes, ten actual game days. P5-RT1 removed the
short-study projection; no shortened companion remains. Grid placement requests
radius8 through PlanningView rather than requiring an invalid radius4 square.
Authored clearance and actual power satisfaction remain engine decisions. Typed
failure diagnostics query existing public views only.

## 3. Contracts/API

Existing FoundCompanyCommand, ConstructCommand, StartResearchCommand, ConfigureOperationCommand,
PlanningView, WorkView, OperationsView, InventoryView, KnowledgeView, snapshots, save/load and Game.Update.
No host World escape, adapter, Draw or parallel state model.

## 4. Files

- OilCo.PublicProof/PhysicalResearchTests.cs: new public physical acceptance case.
- OilCo.PublicProof/Program.cs: explicit registration.
- Framework staged plan/evidence/master/audit: actual results and limits only.
- Production files: change only if a diagnosed failure requires an in-scope fix.

## 5. Refusals and diagnostics

No lab or unpowered lab must retain native work at zero, not fall back to a timer. Normal placement/
command previews must agree. Each stage has420s and1000-frame failure bounds plus public diagnostics;
the current exact case has180s watchdog and5s termination wait. Full study retains
its existing180s/14400-minute stage bound. A live observation timeout is not terminal.

## 6. Compatibility

No legacy/compatibility path or instant site/power/research fallback.

## 7. Verification

Current Release builds4.35s/2.33s clean; SAME full-duration case FINAL1418/0 in
78555ms/observer78658ms. Lab2590/grid4030/final18495; two276/1 placement-fixture
failures retained. [Evidence](../../docs/migration/P5_PHYSICAL_NATIVE_RESEARCH_VERIFICATION.md).
No full/profile/family test or Git publication. Original221/210 source-recorded
checked/11open unchanged; compound P5.005/.008/.009 and full goal stay open.

## 8. Risks

Long physical work needs bounded stages; failures preserve diagnostics. Random equipment incidents
may require ordinary physical recovery rather than hidden repair. Power topology removal/rebuild,
gas fuel exhaustion/refill, all domain modifiers/rivals/fault providers
and lockstep/replay/soak are separate remaining proof obligations.

## 9. Non-goals

No Tycoon behavior/price change, business requirement, platform/Draw/host gameplay, whole-regression
claim, full gate closure or commit/push before full goal/plan finish.

## 10. Stages

- [x] Author focused public scenario retaining canonical facility construction/material/timing rules.
- [x] Prove real lab supply and construction with research held at zero.
- [x] Prove actual power arrival, controlled shutdown and productive saved continuation.
- [x] Record actual evidence, update master and retain broader gates open.

Use dev-lifecycle-standards for the stage/evidence/tracker workflow; focused-only user instructions
override its generic full-regression guidance.
