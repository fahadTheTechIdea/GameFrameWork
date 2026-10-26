# P4-H2 — Account-owned work budgets

## 0. Problem statement

`Standard/WorkStart.cs` currently checks available cash and records unpaid cost bindings
without reserving funds. Competing assigned activities can promise the same account balance.
Implement the existing H2 budget task in the sole optional Tycoon account owner. There are
not two purchase authorities to preserve: logistics admission and physical settlement use
the same owner. No legacy reader, compatibility path, second balance or scheduler is added.

## 1. Goals

1. Acquire the whole selected ordinary monetary cost set atomically with stable owner receipts.
2. Assignment holds money without paying; first actual productive work pays once.
3. Cancellation/interruption releases unpaid holds; paid work never receives a fabricated refund.
4. Ordinary manufacturing and supplier purchases cannot spend another activity's held funds.
5. Save/hash preserve exact holds and binding references; orphan or mismatched work holds refuse load.
6. Disabled monetary costs create no receipt/provider requirement; physical survival stays independent.

## 2. Design

Add `IMonetaryBudgets` and detached request/receipt/status contracts in Core. The existing
`BusinessAccounts` implements them and owns one ordered receipt collection inside its existing
codec. Held totals and available money are derived from receipts and the existing balance,
not separately stored balances. Available raw money uses BigInteger for honest debt diagnostics.
The same owner implements IEconomy, income, purchasing and budgets; module aliases are identical.

Existing selected `WorkCostBinding` records carry exact budget receipts, keyed by order/node/
requirement occurrence. Credits/debt-authorized costs retain their explicit existing semantics
and do not create ordinary cash holds. Whole-set acquisition is preflighted before resource
mutation; provider violations fault rather than become fake compensation. First productive
progress validates and spends the held set before existing material consumption. Cancellation,
reopening and interruption use the existing ReleaseResources lifecycle to release unspent holds.
No enqueue hold for unassigned work, no monetary replacement for physical materials/time.

IEconomy affordability/TryCharge and complete supplier purchase admission derive unreserved
money from the same receipt owner. Explicit Charge remains debt-authorized; it may underfund
holds, which then block productive payment until actual funding. Purchase commits also guard
the reservation revision, so a stale transient commit cannot bypass a newly acquired hold.

BusinessView exposes detached held/available diagnostics and company-private receipt lists.
WorkView's existing cost bindings expose exact provenance; no additional view/store is created.
Loaded WorkBoard validates exact matching live receipts and rejects work-scoped orphans.

## 3. API surface

IMonetaryBudgets: whole-set evaluation/reservation, receipt evaluation/spend/release, and detached
inspection. Budget refusal is typed; mismatched or repeated receipts never debit/release another owner.
No new player instant-money command, purchase system, host loop or module.

## 4. Files

Core monetary contracts and work costs; Standard WorkStart/WorkResources/WorkBoard;
Standard/Tycoon BusinessAccounts/BusinessPurchasing/TycoonModule; BusinessView;
current runtime/save/schema identities; one master-listed JSON physical work scenario and runner;
master tracker and scoped verification report.

## 5. Errors

Insufficient unreserved money: Pending cash blocker, no receipt or material mutation.
Invalid receipt/owner/duplicate: typed refusal, no payment or release.
Arithmetic/ID exhaustion: typed range refusal before mutation.
Saved mismatch/orphan: CorruptStateException; no reconstructed or substituted receipt.
Custom provider violation after committed acquisition: deterministic engine fault.

## 6. Compatibility

Direct current schemas only. No compatibility overload, old save reader or unreserved fallback.

## 7. Verification

One solution build and ONE exact new bounded scenario, covering real assignment/travel, competing
activities, held funding versus ordinary manufacturing/purchasing, cancellation/reassignment,
actual first progress, saved interleaved continuations, private diagnostics and money-off work.
Tests only submit commands, observe authoritative state and drive time. Named direct receipt
contract probes are labeled separately. Older/full/family suites remain unrun.

## 8. Risks

Do not mistake held money for spent money. Paid receipt provenance may remain in an existing node
binding but is no longer a live account hold. Running debt is explicit, not silently forbidden.
Assignment and resource acquisition remain the existing WorkBoard responsibility. Shared custom
provider fault/stress proof and hauling-specific future holds are not implied by this slice.

## 9. Out of scope

Advanced Tycoon/markets, seller revenue, renderer/platforms/demos, OilFieldDays changes,
new haul-budget policy, global provider/replay/stress/full acceptance.

## 10. Execution

- [x] Implement sole account receipt contract, derived availability and purchase guard.
- [x] Integrate existing work acquisition/progress/release/Sync and loaded commitment validation.
- [x] Author physical JSON scenario and focused bounded player-command acceptance.
- [x] Completed slice gate: final build clean;64 checks in the ONE exact scenario.
  Three builds/three invocations are recorded, including one new compile repair, fixture ordering
  repair and accidental stale-binary repeat. No older/full/family gate. See
  [verification](../../docs/migration/P4_WORK_BUDGET_VERIFICATION.md).

This completes the scoped native work-budget implementation, not full H2 or the framework roadmap.
Custom provider/purity/debt/overflow/orphan-corruption/interruption matrix, future haul holds,
and global replay/stress/advanced acceptance remain open in the master tracker.
