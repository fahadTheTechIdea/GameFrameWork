# P7-C3b2b5b1 — Response-safe resource disposition and mixed cargo return

## Current M62: public mixed cargo

2026-10-22; **unchanged native2.717.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved existing work.
The [active C3 obligation matrix](../../plans/enhancements/P7_OWNED_RESERVATIONS_DESIGN.md#current-c3-audit-m62)
identifies a concrete missing public acceptance path: the original mixed-cargo test below manually
spawns entities, changes Held, acquires offers and injects third-party claims. Those remain explicit
protocol probes, not evidence that a player can drive the whole mixed workflow.

The new partial of the SAME [ResourceDispositionTests owner](../../tests/Beep.SimEngine.Tests/ResourceDispositionTests.MixedCargo.cs)
contains no World/provider/job/receipt/stock writes. One explicitly embedded, master-listed
[startup document](../../tests/ExtensionProof/content/mixed-cargo-startup.json) selects the existing
loaded_truck and water_store prototypes. Native Initialize owns their real starting inventory:
three steel plus5.5 water on the carrier, an empty water-only receiver. No business module is active.
PhysicalRecoveryTestContent and Colony.Factory remain the sole shared authoring/composition helpers.

Ordinary SetWorkerHeldCommand holds/releases the machine. Existing logistics skips the blocked first
steel line and chooses the later water line, acquires actual cargo-out/receiver-in receipts and starts
real travel. Save/load preserves exact tokens, owners and quantities. A normal hold cancels that haul,
releases only its original authority and leaves both resources physically aboard without a transfer or
loss fact. The interrupted state waits/saves/pauses on the sole game clock. Release lets the existing
engine replan a genuinely new loaded return. Partial timed unload saves exact residual receipts; twin
continuation finishes real unloading and departure, preserving the blocked steel and all5.5 water.
Every observed Update checks finite material and twin hashes. Detached cargo snapshots, denied foreign
receipt/load views and an untouched isolated session remain correct. Actual flow facts retain source,
destination and before/after provenance: six productive legs at one minute (five whole plus0.5), eleven
half-unit legs at half a minute. Both actual command streams replay to the same final game hash.

### Actual focused execution and self-review

Only this new literal ran:

~~~powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet
dotnet tests/Beep.SimEngine.Tests/bin/Release/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Cargo player continuation: authored mixed stock saved interruption and exact fractional delivery" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
~~~

Two clean targeted builds8.40s/7.23s. Initial run931/1,1203ms,PID5384/exit1,observer1383ms:
the new assertion incorrectly expected `moving` rather than the actual authored `state.moving`.
The physical delivery completed within the original fixed bound; no engine defect or timeout increase
was inferred. Corrected that test oracle and added actual entity state to its bounded failure diagnostic.
Final **585 passed/0 failed**,1185ms,PID1976/exit0,observer1342ms; two clock contexts/two actual
replays. No executable edit after that final pass. No old green cases, full/default/profile/family/soak,
consumer/solution-wide execution or final-regression workaround ran.

[All attempts](../../artifacts/optional-module-durability/61e0795b031a49d6a0effb2939943029/attempts.json),
[raw logs](../../artifacts/optional-module-durability/61e0795b031a49d6a0effb2939943029/),
[baseline identities](../../artifacts/optional-module-durability/61e0795b031a49d6a0effb2939943029/baseline-binaries.json),
[frozen inputs](../../artifacts/optional-module-durability/61e0795b031a49d6a0effb2939943029/final-inputs.json),
[original requirement map](../../artifacts/optional-module-durability/61e0795b031a49d6a0effb2939943029/original-gates-current.json),
[structural checks](../../artifacts/optional-module-durability/61e0795b031a49d6a0effb2939943029/structural-checks.json).
Native DLL75BFEE4612E242906F880107BF7AB7F5A77DF0EBAAE59C652DD9B2BC891FDD24 unchanged;
proof356B68843861B18C827136D17384E9C357C4993ABD5885D4E29B5265575DE9BA changes only for
embedded startup JSON; testsB21BC7D0433483913D72C8D39AD13C1B76AAEF9FC0AB4DC8C1345349E253B95D.

The SDLC procedure required updating the existing plan owner, not adding a parallel feature tracker.
P7's obsolete native-inheritance requirement, transaction signature copy, graph/tool pending status and
deleted Production.cs migration instruction were replaced with current contract/source owners. Initial
stage versions remain explicitly historical evidence. Current permission is focused literal checks only,
including final validation. Self-review only, not independent review; reuse the existing
[operational/assurance mapping](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage).
No native/business/Oil/platform/Draw code or schema change, staging, commit or push. Original221/210
source-recorded checked/11open unchanged; no original gate is closed by this narrow positive proof.
Next: actual creation/transaction caller/fault matrix, typed reference-owner evidence, then relevant
bounded stage stress. Broader accounting/provider/stress and conditional quality/lot gates stay open.

## Historical effect and initial protocol evidence

Current effect API/behavior: [D2f/M59](P7_CONVERSION_ACCOUNTING_REVALIDATION_VERIFICATION.md#current-d2f-m59-spill-cargo-interruption) requires explicit resource-port maps, logical endpoint identity and one whole-bill classified claim/conversion commit, with authored waiting and terminal acquired-provider failures. The original historical disposition case was requalified at D2c/M56, not rerun in M57/M58/M59. M59 adds a separate public recovered-cargo interruption/return workflow. M58 separately qualifies public named-port incident phase cancellation and repeated episodes. The old2.41 record below retains its actual result/implementation scope, not current local-inventory guidance.

2026-10-05; working-tree **beep-simengine-2.41.0 / save 18**. Partial prerequisite work,
not P7/P9/P12 or the framework roadmap completion. Only two exact named suites ran.
The development-lifecycle skill guided the existing-owner execution contract, bounded proof and
master tracker update; the user's exact-test policy takes precedence over a full regression gate.

## Existing responsibilities, no parallel gameplay path

WorkBoard remains the only response order/job authority. Its existing ResponseSource/ResponseEpisode
now identify a response by target and named provider, not target/shared plan. ActiveResponseOn and
ResponseWork.CheckActiveOrder are reused by posting, work/supply advancement, jobs, completion effects
and incident views. Stale episode/plan/provider/ownership refuses before the next work mutation.
Renewal cancels/reposts the affected provider only. Already spent materials stay spent; no fabricated refund.

Optional sim.incidents owns one deterministic posting system at order 390, before WorkSystem 400.
The proof-specific posting system is deleted. Provider contracts supply their authored response priority,
plan and facts; providers retain their existing incident state/Sync. No second incident queue, scheduler
or host update loop exists. JSON response_effects explicitly select either a named source or
bound_response, never both or neither. Bound completion selects the current order's named provider.

WorkModule registers inventory_effects under the existing WorkEffectDef content authority. JSON requires
a nonempty native Bill and an explicit consumed/destroyed/lost kind. BillSpec validates resource references,
positive quantities and native discreteness. InventoryEffectDef uses the existing IResourceTransaction
whole-Bill evaluator/commit. Existing outbound promises are protected. Actual negative stock deltas publish
the existing ResourceFlowFact with explicit cause, endpoint and native unit. There is no loss balance,
fake transfer, compensating production, raw removal bypass or instant player disposal command.

IWorkEffectPreview is optional. WorkView.PreviewStepEffect checks the actual order/company/source,
then delegates to that provider; unsupported preview is typed work.effect-no-preview. Queries do not
execute an effect or manufacture success. This reports effect readiness, not full DAG stage eligibility.

LogisticsService remains the sole destination/haul owner. IHaulExecution.PreviewReturnCargo shares the
same selector/acquisition rules as physical return. PostReturnCargo scans actual canonical stock for the
first deliverable line rather than giving up on Stock[0]. Return uses actual carrier/cargo-out/destination-in
commitments and the existing timed movement/unload transaction. Cancellation releases only that job's
receipts; stock and unrelated claims remain with their owners. No cargo copy/loss fallback is introduced.
LogisticsView.CopyCargoReturns exposes detached company-private stock/availability/readiness per line;
NoDestination, NoCarrier, NoCapacity and other existing blockers are computed in the engine.

## Composition and durability

The existing Colony.Factory lists the additional JSON document in its master manifest. Embedded
resource-disposition.json supplies incident plans, native effects, receiving filters and opening cargo.
There is no new factory/loader. Independently typed named incident components share one generic
definition/behavior implementation because each entity component type has one owner.
ExtensionProof remains a normal non-friend assembly using public engine contracts.

Build identity changes. Runtime resource/order payloads remain save 18; WorkBoard schema 6 and the
incident providers' schema 2 remain unchanged. View schemas: work 5, logistics 3, incidents 2.
The new posting system has schema 1 and participates in composition/state identity. No old save reader,
API overload or compatibility dispatcher is added; this is not a cross-build compatibility claim.

## Final scoped verification

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Resource disposition: named episodes, authored native effects and saved physical response'
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Cargo disposition: mixed stock, private diagnostics and interrupted saved return'
```

Final build: **zero warnings/errors**, 1.49 seconds. Final exact suites:

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Named episodes, authored native effects and saved physical response | 92 | 0 | 245 ms |
| Mixed stock, private diagnostics and interrupted saved return | 52 | 0 | 199 ms |

**144 checks / two exact suites**, zero aborted suites. Earlier iterations used these same two names;
no broad filter/full suite or unrelated suite ran. Preceding replacement evidence (190) is historical,
not added to the current count. Existing response test call sites were source-migrated and compiled,
not executed as another suite.

The response proof covers shared-plan named identities, authored priority, idempotent posting, early
stale-effect refusal, provider renewal, real repair inputs, timed consumed/destroyed/lost effects,
whole-Bill promise refusal, typed unsupported/private previews, native private history, in-flight and
completed save/load/twin identity, invalid JSON/references and a partial removal provider fault inside
the actual engine tick. Faulted disposition cannot save/hash or falsely complete recovery.

The cargo proof covers an unreturnable first stock line, a later fractional returnable line, held/busy
carrier and promised-capacity blockers, pure private/detached diagnostics, actual owned receipts,
movement over game time, pre-acquisition/moving/interrupted saves, deterministic interruption/replanning,
preservation of another owner's cargo receipt, exactly one real unload fact, no silent steel loss and
saved blocked remainder. Wait loops have fixed failure bounds and report the engine tick/fault.
Core tests perform explicit provider/lifecycle probes; they do not implement game scheduling or effects.

## Still open

- P7-C3b2b5b2: authored cargo-loss/recovery/disposal policy for broken/removed/stranded carriers,
  spill consequences, route/fuel failure matrices and full native-resource accounting.
- New-identity home/operation/order reference retention/rebinding policies.
- P9 area/fleet/work/stock policies, starvation/retry diagnostics, amount-scaled docks/load/unload,
  congestion and stress/no-deadlock gates.
- P12 persistent detection/isolation/stabilization/repair/cleanup/restart graphs, propagation,
  preparedness, maintenance, shutdown/restart policies and environmental metrics/objectives.
- Adversarial same-tick provider changes, every interrupted stage and all extension shapes are not
  claimed proven by these two suites. Providers remain trusted engine modules, not a CLR security sandbox.
- Full regression and oil integration were not run. Oil consumer interface migration, where needed,
  belongs its existing P5 gate; no oil, Tycoon, host, demo, Unity/Godot or Draw files changed.

The engine's existing Initialize/Update/World.Step lifecycle and entity-owned state settlement remain
authoritative. The proof map's authored dimensions are scenario content, not new framework/platform limits.
