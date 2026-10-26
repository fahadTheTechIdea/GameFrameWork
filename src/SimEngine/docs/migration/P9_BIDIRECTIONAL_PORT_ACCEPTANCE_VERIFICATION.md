# P9-C11 — bidirectional port acceptance verification

Engine2.417.0/save154/replay1; no engine gameplay or payload schema changed. Existing contract inheritance universally guarantees IResourceOutlet is IResourceInlet and IOutletPort is IInletPort. IInventory and transporter cargo inherit the same invariant. Existing StorageDeclaration/Storage, ResourcePortContracts, WorkBoard/WorkSystem, ResourceTransactions, motion/dock/handling and GameClock remain the only authorities.

[Stage](../../plans/enhancements/P9_BIDIRECTIONAL_PORT_ACCEPTANCE_DESIGN.md). Original P9.acceptance.053 is verified; other compound P9/provider/lifecycle/stress and full P1–P12 remain OPEN.

## Authored input and actual public proof

One explicit ten-module JSON master reuses original Planet X documents. One add-on defines a liquid resource, a second cargo vehicle, explicit ore-only/solid cargo compatibility, service/wait/departure geometry and startup profile. Existing factory has input-only authoring options; proof module only registers existing independent storage metadata and checks actual catalog/definition/capability aliases before Initialize. No positive gameplay implementation in the driver or new proof module.

One new exact case: `Bidirectional ports: universal inlet contracts explicit compatibility and timed structure cargo return trips`.

Both native and independently registered storage declarations resolve the original same Storage runtime. Compiled contract inheritance covers every exported implementation. Actual public prototype views, capacity/receipts and two named aliases verify one real stock owner across structures and transporters. Explicit incompatible phase/resource and missing named port refusals preserve hash and actual stock; foreign reads are refused. No local fallback or instant command is selected.

The player holds the second transporter through an ordinary command, then the first physically loads/moves/unloads ore source→receiver→source and source→cargo→source. Service geometry allows loading beside the parked vehicle without collision/overlap. Per-update conservation checks all six real stores for ore, blanks, tools and fluid. Saved pending, partially loaded, partially unloaded and completed directions continue identically; replay and interleaved sessions match. Immutable partial snapshots stay detached, and final receipts settle. Public CopyJobs exposes live jobs only; settlement is absence of the previously observed job together with full delivered amount and empty carrier, not fabricated access to terminal internals. Arrival is checked during partial unloading; native home return after settlement is not mistaken for failed delivery.

## Every build and exact runtime attempt

| Attempt | Result |
| --- | --- |
| Solution compile | Exit0,14.10s,0 warnings/0 errors |
| Runtime1 | 67 passed/1 failed,1072ms; PID42440 exit1, observed1216ms. Parked cargo at30 blocked the authored one-dimensional route; bounded256-update diagnostics stopped the case. |
| Repair compile2 | Exit0,9.14s,0/0; explicit cargo90 and existing dock slots authored, all five dependencies built |
| Runtime2 | 67/1,496ms; PID35628 exit1,646ms observed. New driver incorrectly expected terminal jobs in public live-only CopyJobs. |
| Repair compile3 | Exit0,4.30s,0/0, all five dependencies |
| Runtime3 | 218/4,1294ms; PID31164 exit1,1435ms observed. New driver expected carrier to remain at endpoint after original home-return update. |
| Diagnostic compile4 | Exit0,4.46s,0/0, all five dependencies |
| Runtime4 | 218/4,1255ms; PID16864 exit1,1395ms observed. Actual settled carrier55 vs receiver60 and81 vs cargo90 confirmed physical home return, not stock loss. |
| Final compile5 | Exit0,4.11s,0/0, all five dependencies |
| FINAL runtime5 | 238 passed/0 failed/0 aborted,1345ms; PID32076 exit0,1493ms observed; Exited, no output truncation/observer error |

Only this ONE NEW exact case and its same-case repairs ran. Outer timeout30000ms, owned termination5000ms, captured output30000chars, each physical stage256Updates. No historical/family/full/Oil/Tycoon/platform runtime. No engine defect was fabricated to repair fixture assumptions. Final executable inventory is captured after the pass; later work is documentation/hashes only.

## Post-pass source and metadata freeze

UTC 2026-10-10 02:28:42 UTC:573 selected executable inputs,7 changed/new versus prior569,4 new executable files. Original221 requirement words unchanged; only P9.acceptance.053 changes unchecked→checked,204 original line anchors refreshed.124 additional scoped deliverables. No executable drift after FINAL.

Read-only metadata:1232 source anchors and13 quickstart anchors,0 mismatches;2507 selected paths,862 Markdown documents,7218 local links,0 broken;18 five-column master rows,0 malformed; git diff --check exit0 (34 pre-existing output lines); new driver boundary scan0 matches. Actual git status --porcelain=v1 --untracked-files=all:2497 dirty files,0 staged. Clock and all three protected G19 source hashes match the preceding baseline. No old/family/full runtime used for metadata refresh.

## Scope and publication

Original221 gates now158 recorded checked/63 open. Recorded historical checks do not certify full current aggregate closure. Broad regression and full plan remain OPEN. Protected G19 scope is still pending; its three Tycoon files and the sole clock are unchanged. No staging/commit/push until the full goal AND plan are complete and verified.

Lifecycle guidance kept the original owners and requirement-matched proof; no duplicate buffers, catalogs, schedulers, timer or compatibility path. No OilFieldDays, business behavior, host/demo gameplay, adapters, Draw, Unity/Godot or renderer changes.
