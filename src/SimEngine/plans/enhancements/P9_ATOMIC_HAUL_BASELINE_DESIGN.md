# P9-L1: Current atomic-haul baseline and authored competition

## Current M66: compound operation ownership — 2026-10-22

Native **2.721.0/save161/replay2**. [Current implementation, all failures/images and scope](../../docs/migration/P9_ATOMIC_HAUL_BASELINE_VERIFICATION.md#current-m66-compound-logistics-operation-admission--2026-10-22).
The existing IHaulExecution contract now declares operation admission; LogisticsService owns Acquire,
Load, Unload, EndHaul and TryReplanDestination scopes, pure nonreentrant refusal and first-fault cutoff.
ResourceTransactions and logistics each own their own instance of the one Kernel SimulationOperation
nesting primitive. World observes the original declared owner contracts; no separate busy registry,
job store, stock/book, clock, save field, supplier adapter or legacy alias. Whole job assignment uses
existing ValidateHaul; supply release checks its real book before surviving-claim release completes.
Prices/charges/business rules and physical elapsed timing do not change.

Old2.720 callback hash reaches invalid half-built itinerary, not a successful escaped hash; the initial
inverted test oracle is retained separately. Final80 contexts plus two malformed providers1008/0,
actual carrier competition714/0, paid delivery7/0, physical haul16/0, spill interruption856/0/two public
replays, shared nested transaction2104/0:4705/0 across captured build5/build6 images, not full regression.
No original broad acceptance closes. Next existing spatial semantic accounting, typed reference-owner
evidence and focused stage stress. Focused only including final; no business/consumer/platform/Draw
work or publication. Original221/210 source-recorded checked/11open/full goal ACTIVE.

## Historical L1 authoring baseline

Status: repaired and focused-verified on unchanged engine2.601.0/save159; one literal
FINAL587/0. [All attempts and scope](../../docs/migration/P9_ATOMIC_HAUL_BASELINE_VERIFICATION.md).
No production engine workaround or original broad gate closure.

## 0. Problem

The original AtomicHaulTests.Acquisition regression fails before checking actual
atomic acquisition: it requests company1 in a one-seat scenario. It also assumes
save15 and bypasses worker controls and authored startup for its second carrier.
The industrial baseline requires actual current evidence, not historical passes.

## 1. Goals

1. Exercise legitimate owner/foreign views with an explicitly authored two-seat scenario.
2. Author both carriers in startup JSON using the existing machine prototype.
3. Use the original IWorkerControl owner for holds; no direct Held writes or play-time spawning.
4. Prove all-or-none source/cargo/destination/worker acquisition, competing requests,
   exact common quantities, saved receipts, bounded physical deliveries, real dock
   departures and distinct physical home returns with no overlap/crossing.

## 2. Design

Add haul-competition.json under the original proof module's master-listed documents.
Reuse the original machine/resource/time/space/capacity definitions. Two endpoint
content variants add explicit service/wait/departure geometry to the same original
inventory and point-dock providers; they are not new behavior or resource owners.
Author non-overlapping carrier startup positions and a two-seat scenario, with two
actual human seats requested by the session. Select it only for the contract proof.

The retained diagnostic failure at200 updates showed that the no-dock fixture's
first delivered carrier physically blocks its successor at the exact destination.
Keep that real occupancy rule; fix authored departure/queue geometry, not duration,
teleportation, overlap, completed-job shortcuts or a second movement coordinator.
Distinct forward parking/home intents allow completed carriers to clear the shared
departure position without an impossible pass in the one-dimensional space.

The bounded continuation checks finite stock conservation, strict carrier ordering
and saved-twin hash identity after every actual Update, then checks all participant
books empty and both actual workers idle at their original engine-owned homes.
Failure diagnostics retain stage/time/fault, detached jobs and actual inventory
vectors; they do not attempt new live queries after a terminal game fault.

The test is an engine-owner contract proof: calls IHaulExecution and IWorkerControl,
reads original stock/job facts and drives Update. It does not own scheduling,
selection, travel, transfer or reservation rules. Ordinary-player command acceptance
is a separate requirement, not inferred from direct-owner API assertions.

Replace the stale format literal with the independent current protocol expectation.
Retain fixed update bounds and external watchdog. Investigate any later failure at
the actual consuming owner before claiming production changes are needed.

## 3. API

No new production API or duplicate class. Existing worker/hauler/world setup contracts.

## 4. Files

- tests/ExtensionProof/content/haul-competition.json: additive scenario intent only.
- tests/ExtensionProof/ExtensionProof.csproj: explicit resource registration.
- tests/Beep.SimEngine.Tests/AtomicHaulTests.cs: current contract proof.
- Tracker/evidence/docs: exact outcomes, not broad original gate closure.

## 5. Errors

Private/invalid/no-carrier/no-stock/common-quantity refusals must leave state and ID
sequences unchanged. Competing actual commitments may never share the same carrier.
Stalls report stage, engine time and actual faults within fixed update bounds.

## 6. Compatibility

Development-only; no old save expectations or duplicate fallback behavior.

## 7. Verification

The literal atomic acquisition test only, with a60000ms external watchdog. It includes
its bounded two-delivery continuation. No paid/Tycoon nested suite, family/profile
or full run. Build affected test project and record all attempts.

## 8. Risks

This proves the authored direct-owner combination, not all providers, physical
manufacture of carriers, command/replay or dock/congestion breadth.

## 9. Out of scope

Tycoon, paid supply, Oil/platforms, new transport media, full regression/publication.

## 10. Order

1. Retain original failure evidence.
2. Author legitimate competition setup and migrate proof to existing controls.
3. Run literal bounded case; repair actual engine defects if found; update tracker.
