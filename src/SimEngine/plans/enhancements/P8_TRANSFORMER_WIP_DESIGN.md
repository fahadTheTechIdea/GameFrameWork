# P8-A2b — Real recoverable batch work-in-progress

Current M88, unchanged native2.728/save161/replay2: the original physical accounting
driver records from initial unsupplied session through automatic hauling, controls,
blocked cancellation, physical pickup/return and original30-minute production.
Keep the recorded primary; load only independent twins at actual saved checkpoints.
ONE exact1022/0, one complete0→314 replay/7inputs/314checkpoints/final live hash
47C6DA990C1D6D22; all450 native sources/949proof inputs and binaries unchanged.
[Current full selected accounting evidence and remaining scope](../../docs/migration/P8_TRANSFORMER_WIP_VERIFICATION.md#current-m88-complete-physical-manufacturing-replay--2026-10-22).
M70/M87 recordings were blocked-return suffixes, not full initial-session recordings.
Earlier scoped implementation records below retain their actual historical images;
they do not imply that every port/probe case physically hauls its input.

## A2c: contract-wide processor retirement protection

Implemented and focused-verified at core2.714.0/save161/replay2, M54. Before A2c, native Transformer guarded active work but ITransformer did not require independent processors to protect queued/earned execution. The new exact negative lifecycle case reproduced actual independent removal with live work. Setup/cancellation use ordinary player commands; saved copies isolate forbidden direct despawn/replacement probes.

ITransformer now requires the existing IEntityRemovalGuard contract. Native Transformer and independent PulseTransformer delegate pure removal admission to ONE TransformationContracts.CheckRemoval rule, reading the original validated provider snapshot. A nonempty queue refuses with the existing blocker.active-transformation; normal cancellation/material return must complete before removal/replacement is allowed. The existing reader validates actual WIP stock/receipts/tools/execution; no duplicate queue, balance, guard registry or kernel dependency on Standard. Direct migration, no default method/legacy bridge. Build identity alone changes; saved payload/replay schema unchanged. Three selected exact scopes299/0, six actual replays; [current evidence and all failed attempts](../../docs/migration/P8_TRANSFORMER_WIP_VERIFICATION.md#current-a2c-m54-contract-wide-retirement-protection). Broader P8/disposition/provider gates and full goal stay open. No full/profile/family run or consumer changes.

Historical A2b status: native reversible raw-input scope focused-verified on2.77.0/save47/transformer4/system2/view3:
212 checks/two exact suites; clean build, zero failures/aborts.
[Evidence](../../docs/migration/P8_TRANSFORMER_WIP_VERIFICATION.md). Parent [P8](P8_TRANSFORMATIONS_DESIGN.md); resource prerequisite
[P7-C5a](P7_RESOURCE_TRANSFER_BATCH_DESIGN.md). The full [master](MASTER_TRACKER.md) stays partial.

## Problem and success criteria

Before A2b, Standard/Transformations.cs consumed startup inputs and forgot them on cancellation.
Replace that accounting, not supplement it with a refund, duplicated resource balance or old mode.
Success means startup moves real inputs into a protected WIP endpoint once, progress retains them,
completion consumes those actual inputs, and cancellation returns or waits with that same stock.
Save/load/hash and private immutable views must agree at supplying/running/output-wait/returning.

## One-owner design and API

- Extend IResourceTransaction with EvaluateMovement/CommitMovement over ResourceMovementIntent:
  owner, company, source/destination endpoint, native Bill and explicit source receipt references.
  Empty references explicitly request new outbound claims; existing references spend actual WIP
  claims. This is one directed native Bill, not a new stock/transaction authority.
- The same ResourceTransactions/ClaimEndpoint owner preflights the complete source set, whole
  destination capacity, native compatibility/quantum and actual claim ownership before acquisition.
  New source and destination claims use the existing endpoint books. After exact acquisition,
  CommitTransfers alone moves stock and publishes verified whole-set facts. No caller acquisition
  loop, release-then-retry fiction or compensation. Provider violations fault; ordinary refusal
  changes no stock/books/versions/sequences. This does not finish arbitrary inbound claim graphs.
- ITransformerWip exposes its actual IResourceOutlet and current reservation owner to engine-side
  coordination. TransformerManager also aliases IResourceEndpointProvider over those provider-owned
  buffers, not another endpoint registry/store. Stable scoped positive64-bit identities encode the
  existing entity/component identity; never fabricate a marker entity or impose a map limit.
- Native Transformer embeds ResourceInventoryState. This is a real separate process buffer: input
  movement reduces the source stock by the same amount. Its own version/book/stock use the native
  embedded codec inside Transformer.Sync, never another service codec or mirrored WIP balance.
- TransformerDef requires explicit wip_capacity referencing existing authored capacity policy;
  composition validates selected policy aliases, native quanta and each supported raw-input envelope. TransformationDef requires explicit
  cancel_policy: return_inputs for this native reversible-input workflow. Unknown/missing policies
  refuse; no old consumed-at-start mode or inferred capacity/policy remains.
- Each actual start allocates a checked provider-local positive64-bit execution identity. Reservation
  scope includes the stable WIP endpoint identity, avoiding collisions between companies/machines/
  named providers. IDs/progress remain in the sole native Transformer codec.
- TransformationSnapshot includes detached WIP stock/endpoint/execution facts. Providers without
  WIP explicitly report no WIP. Shared contract guards compare facts to actual books/stock/current
  head/phase, and validate orphan engine-owned claims before checkpoint and after full restore.

## Lifecycle and cancellation

1. Supplying: existing local supply/logistics/utility/money gates must pass; no WIP or paid progress.
2. Startup: acquire/move the whole raw-input Bill into actual WIP, protect it with actual outbound
   receipts, charge authored startup cost once, allocate execution identity and begin timed work.
3. Working/output wait: disabled/gated/full-output states preserve progress, raw WIP and its native
   claims. Completion preflights actual WIP consumption plus existing legal resource/entity output
   admission. Consume real WIP only when results can proceed; never consume it again on retry.
4. Cancel: unstarted/tail entries remove normally. Active head enters Returning and retains its
   execution/progress/WIP until ordinary native movement can return all inputs to the same physical
   inventory. Return bypasses production enabled/technology/utility gates, not native capacity/access.
   A full inventory blocks visibly; cancellation never spills/discards/resurrects inputs. On success
   remove that execution without output or repeat requeue. Spent money is not magically refunded.
5. Active queues/WIP refuse entity removal/replacement. Entity.Update remains the only state commit;
   operation reads derive returning/blocked facts without another loop.

WIP endpoint versions are independent of the entity inventory's version during verified two-ended
transactions. Native execution/control changes touch the entity at the enclosing engine operation;
generic diagnostics inspect the actual WIP version. Unsolicited/foreign/orphan buffer stock/claims
are corrupt, not accepted as extra inputs or a hidden stash.

## Files, identities and migration

Revise Core/ResourceTransactions.cs, Standard/ResourceTransactions.cs and its shared batch file;
Standard/Transformations.cs/TransformationContracts.cs/TransformationModule.cs and existing view.
Keep WIP code in another file of that same Transformer/TransformerManager owner if useful.
Migrate master-listed proof JSON and all active standalone callers directly. Bump engine/save,
native transformer component/system/view identities together; no reader/overload/legacy shim.

## Exact verification gate (two suite names only)

- `Transformations WIP: authored buffers, pure movement and private checkpoint contracts`
- `Transformations WIP: timed accounting, blocked cancellation and saved continuation`

Cover real stock conservation/no consumed-start flow, whole source/inlet admission, protected native
other-owner claims, explicit content failures, WIP/output waits, timed completion/disabled return/full
return, repeat execution identities, removal guards, saved phases/interleaved twins, private detached
facts, corrupt/orphan checkpoints and provider faults. Build standalone solution. Older suites
compile only; no family/full run. Update parent/master/evidence/README/AGENTS/help together.

## Still required after this stage

Distinct process ports/catalysts/tools, authored scrap/partial transformations/disposition providers,
general inbound/outbound claim graphs, entity-result transactional provider breadth, continuous
remainder/utility/scientific-result semantics, broader automatic provider demand identities and full
P1–P12 replay/stress/accounting acceptance. No Oil/Tycoon/old sibling, host gameplay, platform or Draw.
