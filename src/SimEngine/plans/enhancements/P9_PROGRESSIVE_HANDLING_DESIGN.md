# P9-C4c — Progressive native cargo handling

Status: historical focused gate, 2026-10-05. **191 checks / two exact suites**, build2.64.0/save36,
work17/work-view19; zero warnings/errors/failures/aborts. No broad/full/third suite ran.
[Evidence](../../docs/migration/P9_PROGRESSIVE_HANDLING_VERIFICATION.md).
Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md); the full roadmap remains partial.
Uses [P7-C4b bounded receipts](P7_BOUNDED_RECEIPTS_DESIGN.md) and existing physical docks.
Follow-on [C5a authored retries](P9_HAUL_RETRY_DESIGN.md) now owns timed retained retry behavior;
this gate's count/layout is historical, not fresh regression evidence for the current source.

## 0. Problem

Before this stage, WorkSystem waited for complete handling then transferred an entire haul. WorkBoard
rejected partial Loaded and expected whole native receipts in every phase. A timer alone is not progressive physical
handling. Cargo arriving in chunks must stay protected without replacing tokens or adding a ledger.

## 1. Goals

1. Actual source/cargo/destination stock moves incrementally after each earned compatible native
   quantity. Existing JSON quantity/loading/unloading durations are the sole handling authoring.
2. Existing profile CheckHandling is pure, positive and monotone over positive compatible quantities.
   Shared HaulHandling finds the greatest completed native prefix through that same duration contract,
   including stepped mod profiles. No copied rate, percentage stock, chunk-size default or host timing.
3. Native receipt growth protects received cargo using its same outbound token. Add explicit
   IncreaseReservedOut and ResourceReservationBook.Increase; ResourceTransactions.ExtendClaim verifies
   existing whole-vector checkpoints. Initial protection uses existing AcquireClaims.
4. Job.Loaded remains actual held job cargo, not an invented original amount or second inventory.
   Source/CargoIn retain Amount-Loaded during pickup; CargoOut protects actual Loaded; destination
   retains the whole amount before delivery and actual remaining cargo during unloading.
5. WorkSystem advances sole HandlingProgress only while reached/eligible. LogisticsService computes
   earned quantities, commits through existing native transactions, owns job receipt/Loaded updates,
   and returns typed result. Payment/resource refusal retains actual partial stock/claims/progress for
   retry; invalid/missing participants end execution without refunding or deleting actual cargo.
6. WorkBoard validates native partial phases and earned progress on save/hash/restore. Views expose
   actual resource/amount/held/handled/remaining facts. Existing codec/view/build identities migrate,
   no old reader or compatibility mutation path. Dock waiting/pause earns no service; physical exit
   keeps the carrier until completion.
7. Cancellation/interruption releases this job's remaining receipts only; native partly loaded or
   partly unloaded stock stays in real endpoints for existing physical return/recovery. Saved twins,
   native conservation, provider failures and session isolation remain deterministic.

## 2. Design and ownership

- Core/ResourceReservations and ResourceEndpoints: explicit outbound growth retains native row/token.
  ResourceInventoryState and public-only ports guard stock/quantum/amount before mutation.
- ResourceTransactions.ExtendClaim uses existing PrepareClaimReceipts/ClaimEndpoint, preflighting
  current identity, available stock, native packing and representable growth; verifies exact new row,
  unrelated claims/stock and version. It changes no stock or resource-flow history.
  CheckClaimCapacity is the shared full outbound-vector admission for both new and growing holds.
- ResourceHandling: one pure cumulative prefix search over CheckHandling and actual leg quantum,
  checked native arithmetic, no hard game-size/search cap. Existing time converter owns rounding.
- LogisticsService.Load/Unload return ResourceTransferResult. Zero newly earned quantity is Ready,
  actual positive chunk is Committed, resource/payment refusal stays typed. Cargo protection grows
  only after verified physical arrival through current transaction/claim owner. Final source/inlet
  tokens clear only on exhaustion; original cargo token persists through growth and consumption.
- WorkSystem retains lifecycle/motion/eligibility/progress owner, advances once per reached service
  update, retries blocked earned transfer without earning extra work, advances toil only at actual
  full pickup/empty cargo. Job.Amount stays original intent; Job.Loaded is currently held quantity.
- WorkBoard derives exact remaining receipts from the same job/phase, validates handled <= earned,
  allows route reapproach without resetting service, and keeps native participant-loss cleanup.
- WorkJobView projects detached quantities through these engine owners; no host math or provider handle.

## 3. API

IResourceOutlet.IncreaseReservedOut(owner, token, amount); ResourceReservationBook.Increase;
IResourceTransaction.ExtendClaim(company, receipt, amount); IHaulExecution.Load/Unload now return
ResourceTransferResult. Existing profile interface gains a monotone/pure behavioral contract, not an
alternate registry or loader. Existing JSON has no new inferred fields or compatibility profile.
IWorkerDispatch.CheckJob exposes the existing sole remaining-stop/policy eligibility calculation;
direct physical execution also requires current assignment and positive effective work rate.

## 4. Files

Core endpoint/book/transaction contracts; Standard inventory/storage/transaction/handling/logistics/
work execution and sole checkpoint codec; WorkView/schema; current public-only provider wrappers;
exact progressive tests/registration; engine identity; parent/master/README/AGENTS/evidence.

## 5. Errors

Premature direct transfer, wrong job/carrier/phase/arrival/amount refuses before mutation. Unknown,
nonmonotone/impure duration facts or exact native provider violations fault normal Update and cannot
save/hash. Failed payment retains already paid/received chunks; no refund/instant delivery. Surviving
native claims release on explicit execution end; missing participants do not substitute endpoints.

## 6. Compatibility

Development only. Replace old whole-batch hauling semantics and result contracts. No shim/old reader.

## 7. Focused verification

Clean build, then only these two exact suites:

```powershell
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Progressive handling: native holds, prefix timing and partial checkpoints"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Progressive handling: physical chunks, interruptions and saved continuation"
```

Bound every physical stage with diagnostic job/toil/stock/progress/blocker snapshots. No third,
family or full suite; migrated older callers compile only.

## 8. Risks

Duration prefix queries must be monotone/pure; generic provider fault breadth remains a separate gate.
Native quantum can make a partly loaded cancelled quantity incompatible with a particular destination;
ordinary return admission selects only compatible actual cargo, never rounds away or duplicates it.
Full-vector transaction checks are allocation-heavy; soak/performance gates remain open.

## 9. Out of scope

New transport media, throughput/ETA/fleet policies, all congestion/collision/replanning, plans/areas,
starvation/preemption, full supplier/economy adversary matrix, replay/soak/full gates, Oil/Tycoon/hosts/
demos/platform/Draw. Full roadmap remains partial.

## 10. Execution

- [x] C4c.1 native same-token cargo hold growth through existing transaction checkpoints.
- [x] C4c.2 authored duration prefixes and actual incremental transfer/job transitions.
- [x] C4c.3 partial phase save/hash/view migration and bounded two-suite proof.
- [x] C4c.4 linked current evidence/tracker/instructions with broader gates open.
