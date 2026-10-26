# P4-H2 — Budget lifecycle and execution integrity

Date:2026-10-07. Build `beep-simengine-2.215.0`, save119.
Account4, board41, work system41, work view74, business view5, supplier4 and transformation view9
payloads unchanged. IMonetaryBudgets now exposes its existing owner revision; no compatibility path.

## Final result and complete invocation record

ONE distinct new bounded scenario:
`Tycoon budgets: route interruption, physical funding and provider integrity`.
Final51 passed,0 failed,0 aborted suites,571ms. Final build clean0 warnings/errors4.17s.

Three builds:

1. One new test compile defect: ReservationOwner requires explicit scope AND long identity.
   Two probe constructor calls repaired. Engine and public-only extension compiled.
2. Clean0 warnings/errors2.65s; first runtime scenario revealed a real retry lifecycle defect.
3. Final clean0 warnings/errors4.17s after the actual assignment transition repair.

Two invocations of the SAME scenario:

1.13 checks passed then CorruptStateException during native Hash after route recovery/payment,
   360ms. Reassignment retained an elapsed RetryAt; reaching the site reset failures to0, leaving
   a nonzero deadline with no failures. Saved timestamp validation correctly refused that state.
   Fix: actual assignment refuses a future retry deadline and consumes the elapsed deadline. It
   preserves failure history until actual site arrival; no serializer relaxation or test workaround.
2. Final51 passed,0 failures/aborts571ms.

No stale binary, older/family/full/platform/OilFieldDays suite run.93 older metadata files migrate151
exact build/save assertion lines; compile only, not runtime proof. No gameplay edits after green.

## Same owners, contract validation

The optional native Tycoon BusinessAccounts remains the sole actual balance, live hold, purchase and
income owner. Revision exposes its already-saved budget revision; no new persistent counter. Existing
WorkBoard remains the sole selected occurrence/progress/resource authority. New MonetaryBudgetExecution
is stateless protocol validation using transient detached balance/receipt vectors, not a second
ledger, budget store, economic policy, clock or execution model. Calls return no fabricated success.

Preflight checks must preserve actual cash, hold records and revision. Inspection must report one
valid detached receipt set with unique identities/company-owner pairs and stable cash/revision.
Reserve changes only its exact requested receipt set, not cash or unrelated holds. Spend removes
only exact selected receipts and debits their total once from that company. Release removes only
those receipts and changes no money. Mutations must advance the actual owner revision. NoFunds is
ordinary cash wait; RangeExceeded has a typed admission blocker. Missing/invalid active receipts
fault rather than become invented shortage. Resource acquisition preserves the actual typed blocker.

No rollback/compensation is claimed for malicious providers. The boundary observes exact violations;
the same native account implements policy. Broader provider fault/recovery and allocation gates remain
open. Normal gameplay below uses actual native providers, player commands and bounded engine time.

## Proven normal physical gameplay

- Opening100 reserves80 as two native receipts for assigned construction; the machine must travel.
- Public point-access closure causes native route failure/reopen to free the machine and release
  unpaid holds/bindings. Cash100, materials8 and progress0 remain. A blocked saved twin stays equal.
- Reopening access assigns the actual machine and acquires fresh receipt IDs. Reassigned travel/save
  reaches the site, consumes2 actual materials and pays80 once. Completion/idle twins remain equal.
- Explicit master-listed running-cost work charges150 once at earned work, yielding debt-50 while
  existing80 promises remain. AvailableRaw is-130, no productive construction/material consumption.
- Saved debt-held work retains exact receipts and no repeated upkeep charge. Public production intent
  runs an authored3-minute native recipe, producing40 actual support at5/unit and crediting200 once.
  Cash becomes150 with80 still held, inventory40 retained; no buyer, seller or fixture credit.
- Real funded construction travels/works to completion, consumes materials from8 to6, pays80 once,
  ends cash70/holds0, and has one generation settlement. Saved producing/working twins stay equal.

Activity duration uses existing numeric hours authoring,0.05 hours =3 native minute quanta. Funding
recipe uses existing rational duration authoring. Two new JSON documents are explicitly embedded and
listed by the master fixture, with independent authored mode flags and native policy selection.
Tests own no money, receipts, reservation, work progress, route decision or production behavior.
Every Reach stage is bounded120 updates and reports actual clock/balance/reserved facts on timeout.

## Separately labeled instrumentation

Named checkpoint corruption removes node bindings leaving actual account holds orphaned, substitutes
a nonexistent receipt with matching amount, or fabricates a spent binding with a still-live receipt.
All refuse actual restore. Restoring original probe facts returns identical native hash/valid save.
These are deliberately corrupted state probes, not player commands or normal lifecycle mutation.

Named forwarding proxies wrap the actual native service; they own no test account/book/policy. Direct
boundary calls reject a preflight Committed status, fabricated Reserve success without actual holds,
false Spend/Release success, omitted/multiple inspections, a check that releases real holds, and
spend/release commits with an incorrect cash change. Non-mutating false facts preserve native hash.
Mutating fault cases use fresh native games; they do NOT claim normal Update fault latching, recovery,
rollback, replay or a public-only custom provider mod proof. Native pure checks also preserve hash.

## Commands

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Tycoon budgets: route interruption, physical funding and provider integrity"
```

## Source checks and remaining work

[Scoped inventory](P4_WORK_BUDGET_LIFECYCLE_SOURCE_MANIFEST.json) records this checkpoint. Historical
inventories/reports remain unchanged.194 files,0 duplicate paths,0 mismatched hashes;22 inventoried
documents contain1161 local links,0 broken targets. Read-only git diff --check exits0. Source hash/link
checks are not additional runtime scenarios. The new manifest itself is excluded from its own hashes.
One GameClock constructor remains in Kernel/World.cs; unchanged clock SHA256:
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
No Draw, platforms/adapters, host/demo gameplay, OilFieldDays edits or extra clock/financial owner.

Open: wider invalid/foreign/unrelated/range/revision/exhaustion provider matrix, normal Update fault/
recovery, worker/endpoint/manual/policy interruption, paid reassignments, live purchased-load changes
with holds, future haul-budget policy, wider income/trading/replay/lockstep/session/stress/allocation,
advanced Tycoon and full H2/H3/I4/framework acceptance. Older suites only compile; no inferred pass.
The development-lifecycle workflow kept the existing staged plan and authoritative owners.

FULL goal/plan remain active. Per human instruction, no commit/push until full completion and
verification; unrelated dirty work is preserved and publication targets must be checked then.
