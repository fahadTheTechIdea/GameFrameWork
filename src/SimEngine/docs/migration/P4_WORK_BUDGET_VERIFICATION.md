# P4-H2 — Account-owned physical work budgets

Date:2026-10-07. Build `beep-simengine-2.214.0`, save118.
Account4, board41, work system41, work view74, business view5. Supplier4 and transformation view9 unchanged.

## Final result and complete invocation record

ONE distinct new bounded scenario:
`Tycoon budgets: competing physical work, release and saved payment`.
Final64 passed,0 failed,0 aborted suites,494ms. Final build clean0 warnings/errors2.33s.

Three build invocations:

1. Clean0 warnings/errors4.15s; initial slice compiled.
2. One new contract-probe compile failure: pricing is not IEntityCapability; use the actual
   ISupplier capability's IResourceSupplyPrice contract. No core/provider compile error.
3. Final clean0 warnings/errors2.33s after that probe repair.

Three invocations of that SAME exact scenario:

1.19 passed, one fixture exception470ms. Cancellation correctly released funds; queued ordinary
   manufacturing then spent90 before the next construction assignment, following system order.
   The test incorrectly expected the construction intent to outrank that declared processing step.
   Repair: player explicitly cancels the queued manufacturing request before cancellation/reassignment.
2.19 passed, same fixture exception411ms. This was an accidental stale-binary invocation after
   build2 failed, NOT proof of revised source. It is recorded and not omitted from invocation counts.
3. Final64 passed,0 failed/aborted494ms after successful build3.

No full/family/older/platform/OilFieldDays suites run.93 older metadata files migrate151 exact build/
save assertion lines; two older fault-probe WorkCostBinding constructors retain their receipt under
the current signature. These compile only and were not runtime-verified. No gameplay edits after green.

## Ownership and timing

IMonetaryBudgets, IEconomy, IBusinessAccounts and purchasing alias the SAME native optional Tycoon
account object. One ordered hold collection is embedded in its existing persistence owner. Balance
remains sole actual money. Reserved totals and available raw money derive from live receipt records;
BigInteger diagnostics preserve honest available-money readings even during explicit debt.

The existing WorkBoard owns selected occurrence references. Whole monetary sets are admitted before
physical acquisition; stable receipts include actual company/order/node/requirement occurrence.
Existing machine assignment/travel/toils/progress/material claims remain unchanged authorities.
First earned productive progress spends its exact held set once; existing cancellation/resource
release removes unspent holds. A paid binding retains provenance, not a live promise. No refund of
paid work, copied balance, synthetic income, second scheduler, purchase authority or clock.

IEconomy affordability/ordinary charges and supplier aggregate settlement consult unreserved money
from this same owner. Prepared purchase commits reject changed budget revisions even with unchanged
cash. Explicit debt-authorized Charge remains explicit and can underfund holds, not silently become
guaranteed financing. No fake account or zero-money fallback for physical games.

Existing Sync saves hold IDs/revision/owners/amounts; WorkBoard checks live/spent receipt parity and
work-scoped orphan ownership on restored composition. Existing BusinessView protects company-private
budget lists; WorkView exposes detached exact binding provenance. Schema changes are direct, not shims.

## Proven physical behavior

- Two40 cost leaves acquire atomically for one actual assigned activity with opening100. Cash stays100,
  reserved80/available20; real machine must travel before consuming materials or paying.
- A competing activity cannot promise those held funds or create a partial cost set. Ordinary90-cost
  manufacturing waits; actual logistics purchase preview40 refuses the same typed payment blocker.
- Saved traveling account holds and node references restore identically. Cancelling unpaid work releases
  money without crediting it; another actual assignment acquires fresh, never-reused receipt IDs.
- First real productive construction spends80 once and consumes2 physical materials. Held total becomes0.
  Later cancellation does not refund paid money or consumed material, and saved twins remain identical.
- Opening160 permits two independent physical assignments/whole holds. The earlier-arriving machine
  pays80 while the second still travels with80 held. A saved partial-payment checkpoint finishes both,
  ending cash0/holds0 and each site's physical stock6 from8; later updates never repeat spending.
- Opening79 refuses the whole two-leaf80 set: no partial receipt, money, resource consumption or progress.
  Cash-blocked intent saves and continues with identical hashes.
- Authored money-off costs are dormant: no account/budget provider/binding, both real machines travel,
  consume physical materials and finish over game time. Native monetary definitions still declare their
  module owner; selection alone activates no money.
- Separately labeled contract probes use actual assigned holds: stale transient purchase commit,
  mismatched amount release, duplicate spend and nonexistent-company spend all refuse with identical
  hashes/money/holds/stock. These probes do not simulate purchases, player payments or generation.

Tests only submit player commands, observe authoritative views/providers, save/load and drive bounded
Update; each physical stage is bounded120 updates (survival completion80). No test-owned allocation,
reservation, loading, payment, resource generation or gameplay scheduler. Two new explicitly listed
JSON files author requirements, physical machines/sites and independent modes through current contracts.

## Commands executed

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Tycoon budgets: competing physical work, release and saved payment"
```

## Source checks and limitations

[Scoped inventory](P4_WORK_BUDGET_SOURCE_MANIFEST.json) records current file hashes; prior inventories
are historical and unchanged.188 files,0 duplicate paths,0 mismatched hashes.15 changed documents
contain1144 local links,0 broken targets. Read-only git diff --check exits0. These are source checks,
not extra runtime suites. One GameClock constructor remains in Kernel/World.cs. Clock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
No Draw, adapters, host/demo gameplay, platform/OilFieldDays edits or extra timing owner.

Custom budget-provider purity/commit violations, native debt/range exhaustion, deliberate orphan-save
corruption, interruption/worker/endpoint removal, actual purchased-load interleaving with held funds,
future haul-budget policies, wider income/trading/recovery/replay/lockstep/session stress, advanced
Tycoon and full H2/H3/I4/framework acceptance remain open. Earlier suites only compile, not pass by
inference. Source validation is not a full runtime/regression proof.
