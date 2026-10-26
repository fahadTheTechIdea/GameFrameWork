# P4-G70 transporter cargo binding/removal verification

Scoped verified on engine2.441.0/save154/replay1. Original221/178checked/43open
unchanged; additional165. [Stage](../../plans/enhancements/P4_TRANSPORTER_REMOVAL_CONTRACT_DESIGN.md).
Full goal/plan/publication remain OPEN.

## Actual builds and attempts

One solution compile exited0 in9.46s, zero warnings/errors. Three needed targeted
test-project compiles exited0 in4.89s/4.76s/5.62s, zero warnings/errors. Every
compile was terminal before runtime; no overlaps. No full solution/test runtime,
old case or case family was run. No executable edits after FINAL.

ONE NEW exact case: Transporter removal: native and bound cargo guard physical
retirement through partial transfers. All same-case attempts retained:

1. 1passed/1failed/0aborted in227ms; PID38720 Exited1/process402ms. Negative
   authored service offset was outside prototype space. Author prototype presence
   at4 as already selected in its startup placement; no spatial-rule relaxation.
2. 45passed/1failed/0aborted in3503ms; PID7972 Exited1/process3652ms. Physical
   packing reached512-update bound and emitted twelve live-job/stock diagnostics.
   Left-side dock crossed the held carrier in 1D; author positive adjacent service
   bays on the actual vehicle. No teleportation or motion bypass.
3. 473passed/4failed/0aborted in8498ms; PID46888 Exited1/process8659ms. Native
   retirement's own still-live work dock correctly refused active promises.
   Driver waits for actual retirement service to release its physical dock before
   expecting undisposed-stock. No guard/receipt rule change.
4. FINAL477passed/0failed/0aborted in15308ms; PID37760 Exited0/process15491ms.
   No truncation, observer error, timeout or aborted case.

Every outer run: timeout60000ms, termination5000ms, output30000characters.
Every physical wait: at most512 ordinary Updates, twelve diagnostic entries.
Terminal FINAL is the only current runtime pass; previous attempts are not passes.

## In-place implementation and ownership

ITransporter now requires IEntityRemovalGuard. Native Transporter, independent
ReserveTransporter and independent BoundCargo delegate to TransporterContracts.
The original private LogisticsService cargo binding reader is deleted; every
engine cargo binding query uses ReadCargo. One direct .Cargo getter remains,
inside that helper. Actual transporter provider/entity is observed, and cargo must
be actual/live/registered/same-company IInventory. No empty/native/foreign fallback.

Native attached inventory keeps the original inventory guard, including explicit
ConstructionService recovery resolving that exact guard. A separate cargo binding
checks actual inventory promises and stock under the original provider observation.
World guards remain sole removal authority; no duplicate cargo ledger, scheduler,
binding state, recovery owner, clock or parser. BoundCargoDef now exposes the real
ITransporterDefinition metadata and validates its declared provider/owner binding.

Original logistics dispatch/recovery/diagnostics, WorkBoard haul admission/handling,
haul policies and cargo views are migrated in place. Obsolete successful-null
view handling is removed. Existing saves/provider payloads remain unchanged.
Build identity2.441.0 identifies this contract change; save154/replay1 unchanged.

## Exact proof scope

Eight public-player flows: native versus independently bound actual cargo, minute
versus half-minute quantum, partial pickup versus partial delivery. Master-selected
JSON authors ordinary retirement and recovery graphs, separate inventory placement
and startup, and independent provider composition. Reused composition/view helpers
contain no gameplay or direct mutable-world access.

Normal commands enable work, submit hauling, pause policy, hold/cancel work, submit
and cancel retirement, and explicitly recover actual cargo. Actual live cargo
promises block worker removal; released commitments still leave undisposed-stock.
No handling prefix/stock/position is discarded. Explicit packing consumes physical
worker time. Attached stock packs at vehicle placement; separately bound inventory
packs at its own authored75 position, never fabricated vehicle attachment geometry.
Only empty bound cargo permits later worker retirement. Exact total remains20.

Saved twins cover actual haul, live promise waits, paused sole clock, released
commitments, physical packing and terminal removal. Immutable diagnostics identify
real native/separate inventory, remote cargo never copies into worker storage,
retained handling/cargo facts stay detached, private plan/stock views do not leak.
Public-command replay matches and an idle interleaved session remains unchanged.

This does NOT prove arbitrary provider purity/fault callbacks, moving remote cargo
attachment topology, all endpoint types, lot accounting, incidents, paid/business
effects, global stress/performance/full P4.010/P7.018/P1–P12 acceptance. Original
broader checkboxes remain unchanged. Protected G19 catalog/Tycoon authority is
still pending; clock/Tycoon protected bytes preserved. No Oil, renderer/platform,
adapter or advanced Tycoon work. Commit/push only after full goal AND plan finish.

## Metadata closure

Read-only closure on 2026-10-11 14:48:14 UTC: 1425source anchors,
13quickstart anchors and 712selected executable inputs; zero source/quick/input drift.
2677workspace paths, 938Markdown files, 8496local links: zero broken links.
18master rows have valid shape; zero driver-boundary matches; git diff --check
exit0 with 34existing line-ending warnings. All221 original literal requirements and
checkboxes unchanged (178checked/43open); 204line anchors shifted6. Additional165.
Four protected clock/Tycoon hashes unchanged; 2665dirty entries, 0staged.
Fourteen previously selected executable inputs changed; two newly authored inputs
and two previously existing relevant inputs added to freeze. Ten existing production
files changed. No executable drift after FINAL; no runtime repeated by metadata
checks. Goal complete=false; nothing staged, committed or pushed.
