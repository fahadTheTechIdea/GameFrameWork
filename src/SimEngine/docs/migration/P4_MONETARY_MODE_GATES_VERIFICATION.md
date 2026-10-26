# P4-I3 monetary gates — focused evidence

Date: 2026-10-07. Runtime `beep-simengine-2.209.0`; save113; WorkBoard40/work system40;
work view73/transformation view9. Account3/business view3 unchanged.
Stage: [I3](../../plans/enhancements/P4_GAME_MODE_RULES_DESIGN.md).
Status: native work-cost classification/gating and recipe startup gating verified; full I3/I4 open.

## Implemented owners

- Existing BusinessModeRules owns one EvaluateCost entry point shared by work, recipes and views.
  It consumes IBusinessModeRules when present, otherwise the same immutable typed GameModeConfiguration;
  no JSON reads per Update, genre branches, copied flag table, account/store, system or clock.
  The disabled path needs no selected BusinessModule, IBusinessModeRules, IEconomy or valuation.
- MonetaryCostCategory/MonetaryCostDecision are detached value contracts. Decision preserves authored
  amount and distinguishes NotAuthored, DisabledByMode, Enabled and UnavailableCapability.
  Disabled is not zero price/payment/reservation: EffectiveAmount is absent, not fabricated cash.
- IWorkCostRequirement now declares CostCategory. Native work_cost_requirements require explicit
  category construction/manufacturing/running. Missing/unknown categories refuse. Ten existing
  native JSON/inline-authoring files migrate directly; no old reader or unclassified fallback.
- Existing WorkBoard validates applicable capabilities before initialization, scales the same authored
  amount, publishes diagnostics, and creates existing WorkCostBindings only for enabled obligations.
  Disabled positive costs never bind/spend. The same existing actual resource claims, work progress,
  requirements, startup boundaries and completion own physical execution.
- Existing CheckCostSet/AdvanceWork enforce enabled affordability and commit once. Cost category is
  embedded in the existing SyncCosts payload and checked against the authored selected occurrence;
  disabled saved obligations refuse. There is no new cost/budget queue or balance in work.
- Source audit clarified credit:true: existing debt-funded spending calls Charge, not Credit. That
  meaning is preserved under the category gate; no currency reward or production-income inference.
  This slice compiles/source-reviews that branch, but does not newly runtime-test debt permission.
- Existing Transformer supplies/manufactures over real game time. Its authored Cash is explicitly a
  manufacturing startup cost. Composition and actual affordability/payment use the same decision;
  disabled cash creates no account access, while physical recipe input/WIP/output remains unchanged.
  Enabled shortage waits before acquiring WIP. Existing queue admission still allows future work;
  queue acceptance is not a cash reservation or promise of readiness.
- WorkRequirementResult.Costs carries immutable authored decisions, including disabled ones.
  Actual node Costs contains only real applicable obligation/payment facts. Existing private
  TransformationManagementView.ReadMonetaryCost exposes the same decision without host-side rules.
- Existing resource-only fixture helper authors master-listed explicit profiles/accounts/costs using
  canonical property definitions; the new test sends ordinary commands and observes engine results.
  No test-owned money/work/manufacturing implementation or account refill shortcut was introduced.
- Direct build/save/board/work/view identity migration:91 existing metadata files migrate current
  build/save literals; one existing board expectation migrates to40. Ten native cost-authoring files
  change as above. Overall mechanical inventory:102 files/162 line edits (categories may overlap
  metadata files). Older scenarios compile only, not rerun; global profile migration remains open.

Exactly one unchanged GameClock construction remains in Kernel/World.cs. Hosting/GameClock.cs SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
No host/demo gameplay, OilFieldDays, Unity/Godot, renderer, Draw, extra loop or time authority changed.

## Executed gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Game modes: survival physical work and independent money rules"
```

One clean build:0 warnings/errors,4.87s reported. One exact new bounded scenario:
**105 passed,0 failed, one suite,364ms**. No aborted suite or repair/rerun.
No older/full/family/platform/Draw gate; previous88/84/99/27/72 results are historical, not rerun.

Coverage: all three work categories with positive authored costs disabled and business absent;
pure detached diagnostics; real material consumption and elapsed progress; no disabled cost bindings;
saved working continuations; explicit enabled category with shortage, unchanged stock/progress/money,
saved blocked continuations, actual one-time payment and saved paid work; accounts-only work does
not automatically charge; machine manufacturing with positive costs off/on, real input WIP, elapsed
duration, no instant entity output, conserved input/output and saved twins; enabled recipe shortage
without WIP/output; disabled-money material shortage; missing/unknown native classification refusal.
Every time horizon is fixed/bounded; no long acceptance wait or full-suite selection occurs.

## Explicitly open

- Global owned-scenario explicit-profile migration. Priced content now requires authored relevant
  Boolean properties/profile even for disabled costs; unmigrated fixtures are not runtime proof.
  No inferred survival or account-presence-based legacy interpretation is allowed.
- Public replacement cost-provider/decision matrix, disabled-binding corruption injection, enabled
  debt/fault/cancellation/preemption tests and broader alternative/resource/worker interactions.
  Native timer-only local work plus actual manufacturing machine is not full mobile construction.
- Running-category work is an authored startup obligation. It does not implement ongoing money
  rates, reserved budgets or maintenance interval charges. Physical running inputs remain independent.
- Priced supplier/trading-off selection/pickup and explicit trading commands. Never strip a price
  to obtain free vendor goods. Unpriced hauling/mobile construction acceptance remains separate.
- H2 actual eligible production settlement, budget reservation/cancellation and H3 advanced providers.
  Enabled income/trading/Tycoon continue to refuse until actual integrations exist.
- Executable mode/cost mod proof, replay/lockstep matrix, wider physical/providers/fault/soak/allocation
  and final I4/framework acceptance. Full goal remains active; no broad completion claim.

The development-lifecycle skill kept source, stage status and evidence aligned with the user's
focused-testing boundary. The adjacent manifest is a scoped source inventory, not full-repo proof.
