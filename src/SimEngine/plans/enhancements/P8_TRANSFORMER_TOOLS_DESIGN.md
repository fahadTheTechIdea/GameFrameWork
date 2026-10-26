# P8-A2c — Actual reusable batch tool commitments

Status: native reusable-tool scope focused-verified on2.78.0/save48/view4:175 checks/two exact suites,
clean build, zero failures/aborts. [Evidence](../../docs/migration/P8_TRANSFORMER_TOOLS_VERIFICATION.md).
Extends [P8](P8_TRANSFORMATIONS_DESIGN.md) and the verified
[native WIP stage](P8_TRANSFORMER_WIP_DESIGN.md). Full [master](MASTER_TRACKER.md) remains partial.

## Problem and success criteria

Before A2c, TransformationDef authored only consumed inputs and outputs. Native work could start without
protecting its reusable tools. Add explicit JSON tool Bills and actual endpoint-owned holds, not
another scheduler, tool balance or consumed-and-refunded stock. Work must not start or charge unless
the complete input/tool set and WIP destination are currently admissible. Holds survive work, pauses,
full output and blocked cancellation; completion/return releases only its own real tool receipts.

## Single-owner design

1. ITransformationDefinition/TransformationDef adds Tools and immutable resolved StartResources,
   the checked canonical union of Inputs + Tools. Every transformation authors tools:{} or a positive
   native Bill. Existing resource/unit/capacity catalogs resolve references; no fabricated defaults.
2. ITransformerTools extends ITransformerWip with the actual ToolStore outlet (therefore inlet).
   Native ToolStore is its existing IInventory. The same execution ReservationOwner protects tools;
   distinct machine/capability scopes already prevent collision. No durable tool array/codec/balance.
3. Native startup preflights complete StartResources, then whole WIP movement before any acquisition.
   One AcquireClaims holds the combined actual source Bill. CommitMovement spends only the raw input
   quantities, retaining same-token reusable remainders, including a resource used as both input/tool.
   Any lawful refusal is before all claims/stock/cash changes. A provider violation faults; no release/
   compensation fiction masks partial mutation. Protect received raw WIP through the existing owner.
4. Generalize CommitMovement's existing-receipt mode in place: supplied references must be exact current
   native records and match every bill resource once; moved amount is explicitly bounded by each record.
   Inlet claims protect only moved quantities. The same CommitTransfers mutates and retains remainders.
   Canonical ordering and full endpoint vector checks stay authoritative. No alternate partial API.
5. Existing source books alone hold tools. Native reads derive current execution-owned records directly
   from those books. Completed output admission preflights actual tool receipts before result mutation;
   successful completion releases them once. Returning keeps tools until the complete raw return succeeds,
   then releases them. Disabled work/access gates do not discard tools. No tool movement or consumption
   fact is invented merely for a hold/release; catalysts/wear and remote ports are later authored contracts.
6. TransformationSnapshot includes detached tool endpoint/version/held native Bill. Providers without
   tools explicitly report no tools. Shared contract validation checks expected head/phase, actual complete
   native records/stock/company/current metadata. Orphan execution-owned claims may exist only at that
   execution's actual raw WIP or declared tool store and must match their validated quantities.
7. Before Initialize, tool-positive content requires the runtime tool capability; selected metadata and
   native composition remain the authority. Normal private management reads use the same validation.
   Existing queue, progress, source/WIP books and Sync save/load/hash all state; no new codec fields.

## Files and breaking identities

Revise TransformationContracts.cs, Transformations.cs, TransformationsWip.cs and the shared native
ResourceTransferBatches.cs. A separate TransformationsTools.cs may be another partial file of those
same owners, not a service. Migrate master-listed standalone/proof JSON and both snapshot callers
directly; no overload, reader or old tool-free authoring mode. Build2.78.0/save48/view4 identify changed
semantics. Native transformer4/manager2/resource endpoint codecs retain their existing layouts because
the actual inventory books already save tool promises.

## Errors and verification

Missing/unknown/invalid/fractional discrete tools and overflow reject content. A lawful unavailable
tool produces a typed blocker before raw input movement. Existing receipt mismatch/foreign/spent/
overspent movement refuses purely; hostile mutation/short transfer faults normal Update. Corrupt/missing/
extra/orphan tool records cannot checkpoint or progress. Other owners' claims are never released.

Only these two suite names may run this stage (repair repeats are allowed):

- `Transformation tools: authored contracts, bounded receipts and private ownership`
- `Transformation tools: physical batch holds, interruption and saved continuation`

Build standalone solution; test native overlap/remainders, mixed units and shuffled receipt facts,
provider faults, no-start shortage, actual holds/no consumption, completion/blocked return/disable,
repeats/two machines/other owners, explicit content errors, private detached views and saved stages.
Older suites compile only. Update evidence, stage, master, parent, README, AGENTS and help together.

## Remaining scope

Remote/distinct process ports, catalyst wear/partial disposition, general input graphs and aggregate paid supply,
provider-wide atomic conversion/entity results, continuous/scientific/utilities, broader automatic demand
identity, replay/stress/fuzz/performance and full P8/P1–P12 acceptance. No Oil/Tycoon/old sibling edits,
hosts/demos/harness gameplay, platform SDK, Draw or compatibility implementation.
