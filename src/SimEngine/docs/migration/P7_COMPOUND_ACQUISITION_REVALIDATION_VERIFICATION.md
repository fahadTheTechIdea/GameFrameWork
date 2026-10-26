# P7-C11 compound participation revalidation

Unchanged engine2.611.0/save159/replay1/inventory-view9; no engine edits after N2.
Original P7.009 remains OPEN. This report distinguishes current supported physical
participant evidence from unresolved broader/optional-path evidence.

## Source and runtime matrix

| Participant/semantics | Actual owners and evidence |
| --- | --- |
| Workers and productive bays | WorkAcquisition.PrepareNativeAdmission validates complete actual jobs/workers/slots/timed nodes, then acquires resources before binding worker/job/dock relations. Native team115 and atomic hauling714. |
| Tools, materials and all/any DAG branches | WorkResources.ResourceIntents, AcquireResources and CommitResourceAcquisition use original ResourceTransactions receipts, canonical order/node identity and exact original native bindings. Parallel433 proves contention without prefix, saved branch/token identity, cancellation, release and subsequent work. |
| Nonmoving/shared tools | ResourceTransferBatches.PrepareMovements validates complete movement/participant sets. Protected151 proves shared feed/tool stock, preserved nonmoving tokens, original WIP, positive elapsed production, output, replay and peer-mutation faults. |
| Inbound source/cargo/destination claims | Logistics.Acquire freshly previews the actual carrier, then acquires all three native claims together for physical hauling. Atomic714 proves competing actors, finite capacity, native token cleanup, saved travel/handling, no crossing and conservation each update. |
| Multi-leg batches and canonical refusal | PrepareTransfers/EvaluateTransferLeg validate all structural/owner/native receipt/quantity/capacity participants before mutation. Native batch166 includes malformed later legs, shared/foreign/spent tokens, actual count/fluid quantities, aggregate capacity, shuffled identical commits and no refusal prefix. |
| Durability and failure | Batch93 proves pending saved commands, partial-token continuation, shared endpoints, single/set parity and ten hostile provider modes. CommitCapturedTransfers stages facts until all legs/purchases/checkpoints verify. Faults stop save/hash/live facts; no compensation is fabricated. |

Protocol-level tests deliberately use friend/public contract instrumentation to
probe impossible/stale sets. Native team physical paths use ordinary seat commands,
views, saves and replay. Do not claim a driver-owned scheduler or a CLR sandbox.

## Repairs to test authoring, not gameplay rules

1. Removed the intentionally invalid missing-dispatch profile from the normal shared
   company-alert JSON. CompanyAlertTests now explicitly injects that malformed
   profile for both unused and selected composition rejection. Existing validator
   remains strict; affected admission literal77/0.
2. Batch failure assertions now require live flow reads to throw and retained
   last-healthy pages to remain unchanged. Removed nested Paid calls from the batch
   literal; their independently registered `Partial receipts: paid sources, scoped
   endpoints and provider faults` case remains present, not deleted or counted green.
3. Corrected native batch refusal expectation to Incompatible for an ordinary stock
   endpoint masquerading as a supplier. EvaluateTransferLeg checks the actual
   ISupplier contract; the engine is unchanged.
4. Native team waits retain2000-update bounds and now20s elapsed bounds, with real
   job/worker/toil/slot/position/motion diagnostics. Actual stalled snapshots showed
   opposing traffic in a no-passing point space, not a missing resource transaction.
5. Authored an east-side home for the ordinary outer-bay machine. Its JSON fragment
   is embedded explicitly and merged into the existing master-selected point document
   by composition; the normal native setup/placement/SetHome contracts own execution.
6. Funded physical response gets real two-service/two-departure geometry, opposing-side
   starts and actual homes. Existing160balance/four40holds/payments remain unchanged.
   WorkBudgetTests gains an optional JSON-authoring hook, no financial implementation.
7. Emergency response uses real constrained bays, authored initial position and homes.
   After actual interruption/reacquisition assertions, normal player policy commands
   select EmergencyOnly. Completed responders cannot resume opposing ordinary chores
   through a still-active bay. This is explicit player policy, not an automatic
   congestion fix or evidence for deadlock freedom on arbitrary topology.

## Every attempt

All runtimes used literal `--bounded-exact`, timeout60000ms, termination5000ms,
output4000chars. All children terminal; stdout/stderr not truncated.

Native team attempts, in order:

| Pass/fail | Suite ms | PID / outer ms | Outcome |
| --- | --- | --- | --- |
| 2/1 | 212 | 10956 /346 | Normal fixture included invalid unused alert profile. |
| 53/1 | 4522 | 7048 /4657 | Ordinary opposite traffic stalled. |
| 53/1 | 4560 | 33316 /4700 | Diagnostic rerun established workers at12/11 and blocked bay. |
| 38/1 | 531 | 34636 /669 | New authored home omitted from explicit embedded resources. |
| 61/1 | 5950 | 16480 /6090 | Funded workers both targeted dockless point20. |
| 67/1 | 10979 | 34024 /11120 | Added bays still required impossible arrival/departure crossing. |
| 98/1 | 5071 | 33884 /5210 | Funded path passed; old emergency dockless path stalled. |
| 104/1 | 8350 | 5328 /8486 | Emergency acquired/completed first node, then resumed opposing ordinary traffic. |
| 115/0 | 8560 | 22484 /8701 | Final all native-team paths, including optional costs and physical construction, pass. |

Child34024 initially returned live exec session59270. Polled that SAME handle to
its terminal exit1; no process restart was inferred from a yield.

Other runtimes:

| Literal (abbreviated; full names below) | Pass/fail | Suite ms | PID / outer ms |
| --- | --- | --- | --- |
| Parallel | 433/0 | 6453 | 28860 /6597 |
| Protected | 151/0 | 450 | 34776 /589 |
| Batch durability baseline | 26/1 | 191 | 27544 /326 |
| Batch durability after fault-read repair | 93/1 | 399 | 5740 /534 |
| Batch durability final | 93/0 | 217 | 33156 /355 |
| Alert admission | 77/0 | 427 | 16948 /565 |
| Native batch baseline | 164/2 | 297 | 31064 /431 |
| Native batch final | 166/0 | 290 | 34240 /424 |
| Atomic hauling | 714/0 | 1230 | 19680 /1368 |

Batch baseline stopped at an invalid live history read after the correct first
fault. Next attempt reached the hidden paid-source call and failed because IEconomy
is not bound in its default physical-only colony composition. This known missing
fixture migration is NOT fixed, hidden as a pass, or included in final sums.

Nine affected-project builds (`--no-restore -v minimal`) all exit0, zero warnings/errors,
reported5.16s/4.38s/5.41s/6.00s/5.60s/5.68s/4.61s/4.18s/4.16s.
Runtime was started only after each actual build returned terminal success.
Final relevant cases1672/0; separate alert77/0. These scoped results are not a full,
profile or family run. Unchanged-case results are not presented as rerun after every
fixture edit. Final focused diff-whitespace check exit0.

Exact names:

- `Native team acquisition: complete commitments, refusal purity and physical saved continuation`
- `Parallel acquisition: authored all-any roles tools and materials reserve whole contended sets`
- `Protected movement: complete participants survive acquisition and shared-tool startup or fault peer mutation`
- `Resource transfer batches: saved continuation, shared endpoints and provider fault boundaries`
- `Resource transfer batches: whole-set admission, native ownership and canonical commits`
- `Atomic hauling: common quantities, all-participant refusal and exclusive carrier acquisition`
- `Company alerts: strict profiles, private commands and derived dispatch priority`

## Remaining requirements

P7.009 is only partially reconciled. Its participant matrix above is current, but
the known paid-source fixture still assumes a mandatory cash service and needs an
explicit selected optional module/account composition and contract API migration.
No monetary default or free-resource fallback was introduced to make it pass.
Broader disposition/incident/rebinding/topology/full-conversion/fault/stress evidence
remains under original P7.017; quality/lot remains optional and unselected. Arbitrary
provider/global purity, arbitrary-map deadlock freedom and other original gates
are not proven by this matrix. Original221/199recorded checked/22tracked open unchanged.

Core442-file hash stays N2:
`aad34e204352fac6fa9448c52cf9d18d18332566a0a67e659a173cd373dbcbcf`.
No Oil/platform/host/Draw or Tycoon business-rule changes, full/profile/family runtime,
staging, commit or push. Full goal remains active.
