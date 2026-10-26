# P5-A20 verification — Canonical storage policies and physical cargo

## Current evidence

Core2.544.0/save154/replay1 unchanged. FINAL new exact3048/0/0 in2086ms, observer2262ms,
PID17984, exit0, stdout/stderr untruncated. Only this new affected case ran; no old/family/full suites.
No executable input edits after FINAL. [Design](../../plans/enhancements/P5_NATIVE_STORAGE_AUTHORING_DESIGN.md).

All26 stores (22 structures/four vehicles) now select sixteen shared policies in canonical inventory.json.
The master lists that file once. Original numeric capacities, public flags and accepted resource lists
are preserved; fluid intent becomes explicit liquid/gas versus solid phase filters. All18 resource
capacity measures use the already-authored normalized storage-slot. This is not invented physical density.
All26 stores expose exactly one oil.stock binding; eight retained and eighteen added.
One StorageDeclaration/ResourceInventoryState/CapacityPolicyDeclaration/transaction/logistics owner.
Every IOutletPort inherits IInletPort; named ports resolve the existing inventory, not copied stock.

## Focused builds and all runtime attempts

Three clean affected test-project compiles:10.89s,6.16s,6.22s, zero warnings/errors.
No Oil source compile required this stage: only its canonical JSON changed. Public preflights use the
preceding successful Oil assembly with the current disk content, not an unbuilt new source assembly.

1. 5passed/1failed,180ms/outer363ms,PID28352,exit1: fixture authored a zero-quantity initial bill.
   Two generic negative probes also passed on that wrong earlier refusal; they did not prove their
   intended cases. Fixed zero stock to an empty bill and required the actual refusal message.
2. 3037passed/2failed,2458ms/outer2653ms,PID38924,exit1: after delivery the carrier was still returning;
   original preview correctly prioritized NoCarrier before capacity. Driver must wait for actual engine
   idle-worker facts, not infer readiness from empty cargo.
3. FINAL3048passed/0failed,2086ms/outer2262ms,PID17984,exit0: all intended cases pass.

No bounded timeout or stdout/stderr truncation. No engine gameplay algorithm changed to satisfy the
driver. Several initial diagnostic inspections used incorrect paths/workdirs and changed nothing;
actual paths were subsequently discovered. One combined raw-JSON capture exceeded its output budget
and could not be parsed; files were read individually afterward. Some diagnostic source/history output
was truncated; no runtime or acceptance outcome inferred from it. No failed apply_patch validations.

## Focused command

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Oil storage: canonical capacity filters paired ports and physical cargo" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 100000
```

Canonical storage/port components from all26 entities are admitted in an explicit native projection;
other canonical entity components are excluded. All26 by18 resource compatibility combinations are
checked through InventoryView, repeated for the three physical runs. The current original role/motion/
handling fixture contracts remain responsible, with explicit one-minute-per-unit handling.

Cement dispatch clips to the actual40-unit truck cargo; crude/gas clip to five units of destination
room. Real loading/travel/unloading and home return consume game time, source+cargo+destination
conserve the finite authored stock on every observed update. Cargo is saved while loaded and before
delivery; saved twins match through delivery and return. Full liquid/gas destinations then return
NoCapacity using an actually idle carrier. All runs omit Tycoon; no budget or market is invented.
Missing policy on an unused projected store and a phase-incompatible accepted resource refuse with
their intended ContentException messages. Driver uses authoring, public commands/previews/views,
Update, save/load and hashing; no mutable-world writes or local inventory/gameplay rules.

## Static authoring parity

Current canonical stores26/policies16/named bindings26/master references1.
Captured before/after semantic comparisons show all capacities/public flags/accepted lists retained.
All entity and document data outside storage/inventory_port is exactly unchanged in structures and
vehicles, including other previously unmigrated components. The master changes only the inventory
document registration (formatting normalized). Scalar capacity/fluid fields no longer occur in those
two entity documents. Eight existing WIP capacity policies remain separate work-buffer declarations.

## Actual Oil initialization boundary

Three public current-content preflights refuse company_outpost construction: missing site in
oil/structures.json. The prior company_base storage-capacity blocker is gone.
No actual Oil world initialized or updated. Whole Oil vehicle/mover/worker/handling/construction,
network paths, authored physical founding/drilling/manufacture/repair/research/sales/private/rival/save/
lockstep acceptance is not proved by the storage projection.

## Remaining work and publication

Continue native construction graphs/site buffers, mover/worker/handling definitions, sales/research
financial ownership and the older paid-supply fixture. All broad provider/purity/congestion/fault/
performance/industrial/complete business and full Oil gates remain open.
Original221 texts/checkboxes/line anchors retain193recorded checked/28open. Additional284 is scoped
progress, not original closure. Full goal/plan unfinished; no staging/commit/push or full regression.

## Final metadata

2058 source anchors,13 quickstart anchors,1198 selected executable inputs: zero hash drift.
8680 local links: zero broken;18 master rows valid; thin-driver boundary search empty.
All221 original criterion texts/checkboxes/line anchors match; no acceptance change.
Git diff --check exit0 with34 existing diagnostic/warning lines;3161 dirty paths, zero staged.
Protected GameClock/Tycoon account/value/supplier hashes match the preceding stage.
Canonical parity confirmed26 stores/sixteen policies/26 bindings/master reference1; all other
structure/vehicle data unchanged. Only documentation/audit edited after FINAL, no executable drift.
