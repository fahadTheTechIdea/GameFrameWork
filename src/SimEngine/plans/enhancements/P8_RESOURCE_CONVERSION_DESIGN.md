# P8-A2d — One verified resource-conversion protocol

Current shared protocol additionally supports explicit consumed/destroyed/lost input accounting through mandatory ResourceConversionInput.Kind; [D2b ownership, current scope and remaining work](P7_CONVERSION_ACCOUNTING_REVALIDATION_DESIGN.md#d2b-explicit-disposition-at-scoped-receipt-endpoints). Same transaction, books and original receipt authority, no alternate processor or legacy constructor. Initial-stage evidence below remains historical, not current full P8 qualification.

Status: focused-verified on2.79.0/save49/resource-flow5:175 checks/two exact suites (93 contracts;
82 execution/fault checks), clean build, zero failures/aborts.
[Evidence](../../docs/migration/P8_RESOURCE_CONVERSION_VERIFICATION.md). Full roadmap remains partial.

## Problem and outcomes

Before A2d, Transformations.cs Deliver called ConsumeClaims, CommitInventory and ReleaseTools separately.
Each validates/publishes its own success, so a later provider fault can leave earlier conversion facts
queued as if the complete batch succeeded. P8 requires whole-resource completion admission and history.

1. One IResourceTransaction operation admits actual consumed receipts, retained-tool release and every
   output endpoint/Bill before any mutation. Ordinary shortage/capacity/compatibility refusal commits nothing.
2. Commit reserves every actual output inlet, consumes inputs, releases only its own tools and produces
   all output quantities with shared complete stock/book/metadata/version verification after each mutation.
3. No conversion fact publishes before all participants verify. Provider violations fault Update and
   block save/hash; no rollback, compensation or second stock/promise authority conceals partial mutation.
4. Native timed transformer completion uses this same operation. Full output retains completed WIP/tools;
   saved continuation, repeat boundaries and cancellation still use their existing owners.
5. Multiple mixed-unit, scoped non-entity output endpoints work without a platform or fabricated entity.

## Design and API

Core/ResourceTransactions.cs adds immutable ResourceProductionIntent(Endpoint, Quantities) and
ResourceConversionIntent(Owner, Company, Consumed, Released, Outputs, Cause). Lists are detached copies;
Bills are the existing immutable resource catalog objects. EvaluateConversion/CommitConversion return
the existing ResourceInventoryResult status/count contract. Empty parts are explicit; an all-empty
conversion is invalid. All consumed/released records must match the request owner exactly, be current
outbound records and have distinct tokens across the two sets. Output endpoints are unique; each Bill
is nonempty. Ordering is canonical by endpoint/resource/token, independent of caller list order.

Standard/ResourceConversions.cs is another file of partial ResourceTransactions. Reuse PrepareClaimReceipts,
TransferEndpoint, ClaimEndpoint and CheckClaimEndpoints; no service, registry, queue, ledger, codec or timer.
Capture every participant before pure provider checks. Validate native units/quanta/compatibility and full
incoming capacity, then each exact CanReserveIn. Output space must exist now, not borrow hypothetical
input consumption. No overlapping output owner/resource inbound claim is admitted. Fresh commit repeats
admission. Actual inlet tokens protect the whole output set before irreversible input spending. Shared
ephemeral vectors track only expected deltas and verify actual owners, including unrelated stock/claims.

Consumed/released inputs use existing endpoint primitives; production uses ReserveIn/ReceiveReserved.
Defer the existing ResourceFlowFact list until all deltas verify; tools emit no consumption/production.
ResourceFlowFact production supports actual ResourceEndpointId without entity conversion. Migrate its
sole journal validation/reader in place to accept scoped Produced endpoints; do not permit generic
unowned scoped destruction/initialization. Existing fields/stock codecs stay unchanged. Build2.79.0/
save49/resource-flow5 identify the new protocol; no prior reader or overload.

Native Deliver builds the complete intent from actual WIP records, actual tool records and authored
outputs, evaluates it before legal entity rollout, then commits once. Entity creation remains on its
existing guarded path; arbitrary resource-plus-entity rollback/transaction semantics are not claimed.
No recipe schema additions or new manager. Public-only proof commands instrument the same API and
actual endpoint owners to exercise faults; tests do not implement conversions or time progression.

## Files, errors and sequence

Core/ResourceTransactions.cs; Standard/ResourceConversions.cs; Standard/Transformations.cs;
Standard/ResourceFlows.cs; flow registration/view/schema identities; Kernel/SaveGame.cs; existing
public ClaimProbe/EndpointProof and focused tests. Stage/master/parents/evidence/README/AGENTS/help
update together.

Invalid/null/empty/foreign owner/duplicate token/output endpoint/unknown native quantity -> typed refusal.
Missing endpoint -> MissingEndpoint. Stale actual receipt -> InvalidReceipt. Incompatible unit/filter/
quantum -> Incompatible. Whole destination pressure -> NoCapacity. Pure check mutation, fabricated
receipt, short consumption/production, ignored release, changed metadata/version or unrelated/peer
mutation -> actual Update fault, no conversion history or resumable checkpoint.

Sequence: authored stage -> protocol/flow migration -> native caller -> public-only fault proof ->
build -> exactly the two suites below (repair repeats allowed) -> evidence/docs/tracker.

## Focused gates and remaining work

- `Resource conversion: whole admission, scoped outputs and canonical saved facts`
- `Resource conversion: timed transformer completion and provider fault boundaries`

Verify actual mixed-unit inputs/tools/outputs, multi-endpoint pressure, stale preview/records, unrelated
owners, canonical shuffled lists, private immutable history, scoped save/read/continue, physical WIP
and blocked output/cancel/repeat with bounded waits, and fault detection across shared endpoints.
Other suites compile only. No third/family/full suite, Oil/Tycoon/host/demo/platform/Draw changes.

Remote process ports/catalyst wear/authored partial disposition, continuous/utilities, entity-result
atomicity, general inbound/paid supply graphs and broad provider/replay/fuzz/stress/full acceptance
remain required. Ordinary refusal is whole-set atomic; hostile arbitrary-provider mutation is a fault,
not recoverable rollback. Never promote this finite resource slice as full P8 or roadmap completion.
