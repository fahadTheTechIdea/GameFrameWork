# P9-C5c1 — Partial recovery delivery at non-docked endpoints

Status: historical C5c1 focused-verified,2026-10-07. Builds on [C5b](P9_LOADED_RECOVERY_REPLANNING_DESIGN.md).
Current follow-up: [C5c2 physical occupied-dock recovery departure](P9_DOCK_RECOVERY_EGRESS_DESIGN.md).
[Evidence](../../docs/migration/P9_PARTIAL_RECOVERY_REPLANNING_VERIFICATION.md) /
[current manifest](../../docs/migration/P9_PARTIAL_RECOVERY_REPLANNING_SOURCE_MANIFEST.json).

## 0. Problem and specification

`LogisticsRecovery.TryReplanDestination` only accepts fully loaded delivery travel.
`WorkBoard.EarnedHandling` measures unloading against the original entire shipment.
Retargeting a partially delivered shipment must preserve the original amount and completed
delivery prefix, but must not grant unfinished handling to a different output.

## 1. Goals

- Same job, worker, original amount, creation time and cargo-out promise after retargeting.
- Preserve actual delivered material; promise only the actual remaining cargo at the new output.
- New unloading earns time against the remaining leg, not the original shipment.
- Repeated retargeting, cancellation and saved continuation use the same owners and codecs.
- An occupied physical dock still refuses retargeting: departure is a separate unfinished stage.

## 2. Design and contracts

Extend existing `Job` with `UnloadingBaseline`: the committed prefix before the current delivery
leg. It is work progress, not a stock ledger. `HaulHandling` owns the stateless current-leg amount
calculation. `WorkBoard` uses it for duration and native prefix calculation, validates it and
embeds it once in its existing `Sync`. `WorkJobView` exposes the detached baseline.

Existing `IHaulExecution.TryReplanDestination(World, Job, Code)` remains the sole entry point.
Existing authored recovery blocker lists remain authoritative. Allow delivery GoTo/Unload with
positive remaining cargo, but require completed pickup and refuse occupied docks or partial dock
handling. Validate the actual job before committing new room. Reserve all remaining capacity
before releasing the old inlet promise. Then set baseline to original amount minus remaining
cargo, clear unfinished handling, restore the delivery GoTo and use actual motion/transfer rules.
Do not rewrite pickup intent, reduce original amount, create a new job or invent elapsed time.

## 3. API and files

No new commands, scheduler or store. Existing job execution contract is extended semantically.
Change Standard/{Work,ResourceHandling,LogisticsRecovery,Logistics,WorkModule}.cs,
Views/WorkView.cs and Kernel/SaveGame.cs. Extend the same two RecoveryDispatchTests suites.
Identity expectations migrate directly; no compatibility reader.

## 4. Error handling

| Condition | Result |
|---|---|
| Disabled/unlisted recovery, fixed haul or no complete alternative | No changes |
| Invalid actual job/progress/receipt | Explicit protocol error before replacement commitment |
| Occupied dock or partial dock handling | Refuse; retain physical bay and promises |
| Partial non-docked delivery | Preserve completed prefix; new leg starts with zero unfinished credit |
| Cancellation | Existing EndHaul releases only this job's promises and resets baseline |

## 5. Persistence and compatibility

One baseline field in current WorkBoard payload: build2.201.0/save108/board38/work38/work view71;
logistics16/maintenance2/logistics view27 and recurring schemas unchanged. Direct current identities;
no legacy reader. One unchanged GameClock; no new time owner or update loop.

## 6. Verification

Build, then only the two exact recovery suites. Use ordinary timed engine execution for pickup,
movement and unloading. Explicit recovery-failure protocol injection is labelled separately from
player commands; it is not host gameplay. Cover no-alternative purity, fixed target immunity,
partial dock refusal, native/public actual cargo, repeated retargeting, save/load during travel
and new service, cancellation and aggregate material conservation.

## 7. Risks and out of scope

Partial occupied-dock departure requires retained old physical claim plus next-leg planning;
do not fake it by clearing the bay under a vehicle. That stage, endpoint destruction, wider mixed
fleets, replay/performance and full acceptance remain open. No Oil/Tycoon/platform/Draw work.

## 8. Tasks

- [x] Current-leg prefix/duration, validation, codec and immutable view.
- [x] Existing owner retargeting and cleanup; unoccupied future dock may replan its retained prefix.
- [x] Clean build;123 admission+181 physical=304 assertions,0 failures/aborts; two exact suites only.
- [x] Evidence and master tracker reconciliation; no full completion claim.
- [ ] Occupied-dock physical egress was not closed here; [C5c2](P9_DOCK_RECOVERY_EGRESS_DESIGN.md) now owns its focused proof.
