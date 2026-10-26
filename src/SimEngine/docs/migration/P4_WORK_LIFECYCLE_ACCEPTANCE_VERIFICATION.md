# P4-G73: Work lifecycle acceptance verification

Current scoped identity:2.449.0/save154/replay1.
[Design](../../plans/enhancements/P4_WORK_LIFECYCLE_ACCEPTANCE_DESIGN.md).

## Requirement qualified

Original P4.acceptance.010, unchanged text:
"Pause/loss/cancel/reselect retains progress/cargo; effects commit once."

The new current physical case qualifies every named condition below. Only this
original gate is checked; no full P4/P1-P12, transaction, topology, arbitrary
hostile-provider or whole-suite acceptance is inferred.

## Actual engine repair

Original LogisticsService.PreviewRecovery incorrectly supplied the carrier entity
as the source of a cargo return. A separately bound ITransporter inventory therefore
failed the existing source-ownership guard as InvalidRequest. It now reads the
actual already-validated IInventory entity; carrier, intent and endpoint remain
their own identities. No carrier-local shadow stock or new recovery coordinator.

AddRecoveries also previously used the successful haul source as RecoveryOrigin.
For cargo return this must remain the actual transporter, not its distinct inventory
entity. The original dispatcher/acquisition/reservation/handling/motion/clock owners
now receive the correct pair. Evacuation alias behavior remains unchanged.

Existing pure JSON factory accepts optional lifecycle authoring; the new case reuses
actual selected workflow/permanent-removal/bound-cargo documents and registrations.
No extension-only recovery rule or gameplay in a host/test driver.

## Compile attempts (all retained)

1. Solution `dotnet build Beep.SimEngine.slnx --nologo`: failed6.18s,
   zero warnings/eight CS0121 diagnostics from two imported Step helpers.
   Production compiled. Removed duplicate static import.
2. Targeted test project/dependencies --no-restore: clean5.96s.
3. Same targeted compile for stronger live diagnostics: clean4.40s.
4. Original-owner source repair targeted compile: failed2.00s, zero warnings/one
   CS1061. IResourceOutlet is not an entity capability; cargo is actual IInventory.
5. Same targeted compile after contract-type repair: clean6.21s.
6. Same targeted compile after dispatch-origin repair: clean5.97s.
   All successful targeted builds had zero warnings/errors.

No clean solution aggregate claim. All final runtime inputs compiled before FINAL.

## ONE NEW exact case (all five attempts retained)

`Work lifecycle acceptance: permanent loss pause cancel reselection and partial cargo commit once`

Command: `dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Work lifecycle acceptance: permanent loss pause cancel reselection and partial cargo commit once" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`

1. FIRST337passed/1failed in4592ms; PID40840 Exited1, process4743ms.
   Independent cargo recovery stalled within bounded512 Updates; no stock lost.
2. SECOND337passed/1failed in4848ms; PID21508 Exited1, process4993ms.
   Rich public diagnostics identified actual bound cargo1 / InvalidRequest.
3. STALE337passed/1failed in4932ms; PID21512 Exited1, process5082ms.
   Inadvertently invoked unchanged2.448.0 binaries after the failed fourth compile.
   Retained as an execution mistake, NOT current-source proof or repair verification.
4. CURRENT335passed/1failed in3220ms; PID22488 Exited1, process3385ms.
   Correct inventory source revealed second original defect: dispatch treated that
   inventory owner as a transporter. Original first fault stopped sim.work.
5. FINAL1017passed/0failed/0aborted in10584ms; PID5156 Exited0,
   process10726ms. No output truncation/observer error on any attempt.
   No executable edit after FINAL.

## Matching physical acceptance matrix

Eight flows: native and independently implemented workers/transporter cargo,
minute and half-minute quanta, complete versus cancelled construction after loss.
Each uses the same contract-authored graph, materials, actual endpoint receipts,
motion, operation, work policies, jobs, cleanup and single clock.

- Pause: physically haul/consume eight of original twenty materials; first actor
  travels and earns assembly, with parallel cure/dependency timing. Paused plan
  and paused clock retain exact prefix, actor pose and spent material through saves.
- Loss: a second real machine travels to authored adjacent retirement dock and
  earns timed removal. The original worker remains alive before completion, then
  is actually absent. Same original graph/job and earned progress survive.
- Cancellation: saved post-loss cancel command retires incomplete site; no facility
  or completion fact is fabricated, and spent material is neither refunded nor repeated.
- Reselection: compatible backup physically travels from dock to actual site and
  earns only remaining work on the same job; actual facility/history commits once.
  Later updates preserve exactly one facility, or zero in cancelled flows.
- Cargo: both partial pickup and partial delivery have positive actual stock/handling.
  Policy pause retains handling/pose/cargo. Saved held input cannot execute under
  paused clock; ordinary cancellation releases only promises, preserving committed
  stock and actual position. Current engine recovery selects the carrier with the
  correct native/separate inventory source, reserves only residual cargo and delivers
  prefix plus residual once. Unpicked cancelled promise is never loaded or recreated.
- Durability/visibility: saves during intent/productive/retiring/post-loss/reselection/
  cancelled/partial-handling/pending-held/recovered stages; twin continuation, private
  foreign plan withholding, immutable retained removed-worker/cargo facts, replay,
  terminal save identity and untouched interleaved session.
- Driver uses only actual master/JSON authoring, public commands/previews/snapshots/
  work/logistics views and Game.Update. No mutable world, direct progress/stock,
  completion injection, teleport or fabricated budget.

## Remaining limits

No historical/family/full runtime was rerun. P4.014 shared evaluator/private
projection breadth, broader P7/P8/topology/control/provider/stress and full P1-P12
acceptance remain open. Original221 gates now181checked/40open; unchanged texts/
line anchors except the one matching checkbox. Additional174. Protected clock/G19
bytes unchanged. No Oil, advanced Tycoon, SDKs, compatibility, parallel engine
owner, staging, commit or push. Publication waits for full goal AND plan verification.

## Metadata closure

Current integrity audit:1488 source anchors,758 frozen selected inputs and13
quickstart anchors match current bytes; zero post-FINAL executable drift.
956 Markdown documents/8566 local links have zero broken links;18 master rows
are structurally valid. All221 literal gates validate; baseline comparison finds
zero requirement text/document/line changes and exactly one checkbox change,
P4.acceptance.010. Counts181checked/40open. Driver boundary matches0;
git diff --check exits0 with34 existing line-ending warning lines. Inventory
2714dirty/staged0 preserved; four protected clock/G19 hashes unchanged.
Program quickstart registration hash refreshed in place. Five existing selected
inputs changed, one new driver added to the same authoritative freeze; two
existing production sources changed (original recovery and build identity).
No executable edits, compile or gameplay rerun during metadata closure.

