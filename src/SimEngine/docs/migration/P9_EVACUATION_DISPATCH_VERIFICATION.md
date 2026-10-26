# P9-C3d authored shared evacuation — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.197.0/save105/board36/work35/logistics15/logistics maintenance2/logistics view26.
Work view69 and recurring provider schemas remain unchanged. Existing resource/job payloads
remain unchanged; LogisticsService now embeds evacuation intent chronology in its sole Sync.
[Design](../../plans/enhancements/P9_EVACUATION_DISPATCH_DESIGN.md).

## Engine implementation

PlanEvacuations, its direct PostHaul wrapper, unused FindDestination wrapper and
Tuning.EvacuationPriority are removed. LogisticsMaintenanceSystem only removes empty evacuation
containers after their native live haul/departure references are gone. It assigns no machines.
Cleanup also checks World.CheckDespawn, the canonical multi-provider removal admission, before
despawning. An empty container with another owner's inlet promise remains physically present;
cleanup cannot bypass resource/module removal guards merely because no native haul references it.

EvacuationPolicyModule is explicitly selected optional engine content. Its scenario settings
must name an actual EvacuationProfileDef. Native JSON requires enabled, nonnegative priority and
positive attempts_per_pass. IEvacuationPolicy and the abstract definition permit public-only
extensions. No selected module means no automatic stock evacuation; there is no legacy priority
or silent default. Games wanting automatic evacuation must author this module/profile selection.

LogisticsService.PrepareDispatch refreshes source/resource intent chronology. These records
store company, original registration and last-dispatch game-clock coordinates only: no quantities,
destinations, carrier assignments or mirrored claims. Current actual inventory/ports/policies
remain authoritative. Promised stock does not reset original intent age. Neutral/non-seat stock
does not fabricate a company assignment. Expired rows await normal preparation without mutating
pure views or saves.

The existing BuildQueue, CopyCandidates, TryAcquire, PreviewDestination and Acquire paths now
carry evacuation alongside demands/routes. WorkSystem's one assignment comparison applies
ordinary eligibility, emergency/priority/age/alerts/starvation/cost/identity rules. Current
acquisition recomputes real selected carrier, compatible destination, docks, handling, stock
floors, capacity, trip/service windows and native endpoint promises. Recording evacuation never
resets unrelated demand/route dispatch history. LogisticsDispatchOffer.Kind/EvacuationSource
identify the real origin; absent destination remains absent rather than a fabricated fallback.

One embedded SyncEvacuations stores chronology and selected profile identity, rejecting invalid
company/resource/source ranges, duplicate source/resource pairs and impossible chronology. It
does not advance time or duplicate inventory/job save ownership. Direct save/schema revision;
no old reader or compatibility facade.

## Focused proof

Two exact new suites author master-listed evacuations.json and evacuations-proof.json. The latter
uses a public-only independent profile implementation with the same engine contract. A native
evacuating inventory transfers into a public point-space stock endpoint. Tests author content,
submit ordinary player commands, read immutable private views, run bounded Game.Update and save;
no host gameplay, stock grants or repair/transfer fallback.

Missing/unknown profile and missing/negative/zero fields refuse before normal updates. No selected
capability with a real enabled machine leaves all stock in place. Disabled policy publishes typed
unready facts. Native/public policies alias actual immutable content and use the existing single
logistics dispatch source. Private reads preserve hashes and hide foreign facts. Original age
survives pending saves. Fresh acquisition claims one actual destination/carrier; retained offers
cannot double-book that carrier or create additional stock/promise state.

Full output publishes the actual origin and no selected destination. Periodic blocked preparation
retains original age, all source stock and no carrier. A saved blocked twin continues identically.

Authored low/high evacuation priority competes with a player's ordinary inspection in the same
pass: low yields, high wins. Distinct native/public profile cases actually load, travel, deliver
six units and leave the dock. Loaded saves preserve real cargo/receipts. Empty source removal
waits for physical departure; all six units arrive, cargo is empty, no phantom intent remains,
and saved twin hashes agree. This is scoped contention/conservation proof, not arbitrary fleet
fairness or a complete failure/congestion guarantee.

A provider-protocol fixture creates a real inbound receipt on an initially empty authored
evacuation container. Normal updates preserve that container and receipt without fault. After
the actual provider releases its receipt, canonical removal admission succeeds and normal
maintenance removes the container. This fixture instruments the existing port protocol; no
host-side cleanup rule or manufactured balance is introduced.

## Final focused gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Evacuation dispatch: authored policy, private chronology and shared ranking"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Evacuation dispatch: physical conservation, cleanup and saved contention"
```

Clean build: zero warnings/errors.42 admission +31 physical =73 passed, zero final failures or
aborted suites. Only these same two exact suites ran. Other suites compile only; preceding service,
recovery and other historical gates were not rerun. No full/family/third/platform/Draw gate.
Physical waits are bounded at360 updates with a job/state/blocker diagnostic on timeout.

The first build caught foreach over the non-enumerable ComponentList; indexed iteration fixed it.
No tests ran against that failed build. All final evidence follows a fresh successful build.
88 current build/save/logistics schema literal paths migrate directly; SaveGame is functional
save identity,87 other paths are mechanical metadata and compile only. Current fixtures that
previously relied on implicit automatic evacuation must explicitly select authored policy; no
broad consumer migration or regression closure is inferred from these focused suites.

## One clock, boundary and remaining work

GameClock.cs is unchanged, SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World constructs it once; SimGame reuses it. Intent ages/last-dispatch coordinates derive from
that clock, never another timer/current-time field or update loop.

The lifecycle skill kept the stage, implementation, focused proof and tracker aligned.
[Current fingerprint manifest](P9_EVACUATION_DISPATCH_SOURCE_MANIFEST.json); older artifacts remain
historical. The dirty worktree is preserved. No Oil/Tycoon/host/demo/platform/Draw changes.

P9-C3d is focused verified, not full P9/roadmap completion. Remaining work includes cargo-return
arbitration and its current constant policy, broader consumer content migration, editable recovery
preferences/preferred routes, failure/provider stress, congestion/global purity, allocations/CPU,
replay and final acceptance. The full implementation goal remains active.
