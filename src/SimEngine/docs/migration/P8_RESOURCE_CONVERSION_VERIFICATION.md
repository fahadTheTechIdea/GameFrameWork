# P8-A2d — Whole resource-conversion verification

Date: 2026-10-05. Standalone Beep.SimEngine only. This resource protocol is focused-verified;
P8 and the complete P1–P12 goal remain active/partial.

## Current source and owners

Build **beep-simengine-2.79.0/save49/resource-flow5**. Native transformer4/TransformerManager2/
transformation-view4 and actual inventory/endpoint codecs retain their existing layouts. The flow
event/journal/view migrate directly to schema5 for scoped Produced endpoints; no old reader/overload.

IResourceTransaction alone owns EvaluateConversion/CommitConversion. ResourceConversions.cs is
another partial file of ResourceTransactions, using its existing resolver, receipt checks and complete
ephemeral endpoint checkpoints. ResourceConversionIntent owns detached request facts only, never
stock or promises. Inputs, released tools and output Bills use existing native catalogs and actual
endpoint books. Every participant resolves/captures before pure receipt/capacity callbacks; canonical
ordering is independent of caller arrays. Whole current output capacity is required, without borrowing
hypothetical input consumption. Commit protects all output inlets, validates their actual receipts,
consumes exact raw inputs, releases only requested tools, and produces exact output lines. Verify all
stock/books/identity/company/version deltas after every mutation. Publish the existing flow facts only
after the whole resource set verifies. No compensating rollback masks a provider's partial mutation.

Native Transformer Deliver replaces separate ConsumeClaims/CommitInventory/ReleaseTools completion
calls with one complete conversion intent. Cancellation still owns physical return and then release;
ordinary timed queues, progress, repeat, WIP, actual tools, company views and codecs remain sole owners.
Material-free entity results skip an empty resource conversion; no fake stock is created to call it.
Entity spawning remains its existing guarded path, not proven arbitrary entity-plus-resource atomicity.

The engine-side public-only ConversionProbeCommand uses the ordinary command handler and the same
resource service; its own Sync contains detached requests for pending-command durability. Existing
EndpointPools/ClaimFaultOutlet inject failures into real native owners, not a duplicate conversion.

## Executed gates

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

`dotnet build Beep.SimEngine.slnx`: **zero warnings/errors**, final source build **3.26s**.

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Resource conversion: whole admission, scoped outputs and canonical saved facts"`

**93 passed**, zero failures/aborts, one suite, **162ms**.

`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Resource conversion: timed transformer completion and provider fault boundaries"`

**82 passed**, zero failures/aborts, one suite, **234ms**.

Total **175 checks/exactly two suite names**. Repeats used only these names. Other suite current build/
save expectations migrated mechanically and compiled, not executed. No third, family, full, Oil/
Tycoon/old sibling/host/demo/platform/Draw gate ran. Lifecycle guidance kept design/owners/evidence aligned.

## Direct proof

- Real mixed count/fluid inputs, local held tool stock and two product endpoints (including scoped
  identity5000000000) admit/commit together without invented entities or unit totals. Tools remain
  unchanged, raw input and output amounts match authored quantities, and unrelated inbound/outbound
  claims survive. All temporary output/tool/input receipts retire correctly.
- Pure whole admission changes no stock/book/version/token sequence/hash/history. Invalid/default/
  all-empty/null-release/cause/duplicate/foreign/unknown/fractional discrete requests refuse before
  mutations. Aggregate multi-line output capacity includes actual unrelated incoming promises.
  New pressure after a retained ready preview refuses the fresh commit without a prefix.
- Canonical shuffled input/output requests produce identical inlet tokens, versions, history and
  hashes. Saved live claims, pending ordinary conversion commands and scoped Produced facts load and
  continue identically. Caller mutation cannot change detached request arrays; old private history
  remains immutable, and another company cannot read those conversion facts.
- Explicit release-only requests create no consumption/production. Creation-only requests require
  an explicit output/cause/owner and do not fabricate input claims.
- Native timed repeat transfers real raw stock to WIP and protects local tools. Full output retains
  completed work, WIP and tools without any consumption/output fact. Saved capacity release commits
  one conversion, consumes3/produces1/releases2 tools once, and repeat acquires a new execution.
  Ordinary cancellation returns real inputs/releases tools with no conversion refund or extra output.
- Nine normal-Update provider faults cover short input, ignored release, short output, unrelated stock/
  promise mutation, peer-endpoint mutation during spending/pure checks, silent version and refused
  acquired inlet receipt. Two native completion faults cover short ReceiveReserved and ignored tools.
  Every case faults actual Update and rejects save/hash. Crucially Events.HasPending is false: even
  queued consumption/production success prefixes are absent, not merely undelivered journal facts.

## Remaining required gates

Remote/distinct process ports, catalyst wear/authored partial disposition, worker/technology/utility
binding, continuous rates/remainders, general inbound graphs and aggregate paid supply, entity-result
transaction guarantees, broad provider purity/rollback policies, replay/fuzz/stress/allocation and full
P8/P1–P12 acceptance remain open. Production space must fit before consumption; this protocol does not
use future freed input space. Faulting arbitrary hostile providers is not recoverable rollback.
Focused resource success is not full process, platform or framework completion. Goal stays active.
