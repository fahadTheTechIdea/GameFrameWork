# P9-R3: Retained endpoint access and physical dock queues

## 0. Problem

`LogisticsService.Preview` selects productive-only dock access whenever a continuing delivery retains its endpoint. After P9-R2, the same endpoint may be reached through a different physical site/port. Treating those different accesses as a restored old service position wrongly prevents joining the new dock's waiting queue while its bay is busy.

## 1. Goals

1. Preserve productive-only restoration for the same original site/port with earned handling.
2. Admit normal physical waiting/service selection at another site/port backed by that same store.
3. Retain actual old bay until physical departure, then move to the new queue with original job/cargo/capacity receipt.
4. Earn new-site unloading time, never copy service credit or teleport between bays.
5. Verify partial delivery, busy second carrier, egress/queue saves, pause, exclusive service, conservation, privacy, isolation and replay.

## 2. Design

Existing preview passes `productiveOnly` only if endpoint, destination entity and destination port all equal the continuing delivery's original physical binding. Existing WorkBoard dock selection/claims/queue/egress and P9-R2 reset of changed physical access stay authoritative.

One master-listed JSON add-on creates a distinct alternate facility with First and Second ports plus service/wait/departure slots. It contains no copied inventory. Existing scenario setup includes that authored facility; existing native work/handling/network-free stock modules are unchanged. Thin tests submit ordinary haul, stock-policy, work-policy, hold and port-selection commands. No dock rule or stock/claim writes in drivers.

## 3. API/state

No public API or persistent schema changes. Engine2.592.0/save157/replay1. The existing endpoint/job/dock fields already represent the selected physical access.

## 4. Files

`Standard/Logistics.cs`, protocol identity/expectation; existing public-only scoped composition; one JSON/embedding; existing NamedDockRecoveryTests adds a separate exact case and migrates its obsolete save-format assertion; tracker/evidence.

## 5. Errors

Busy/unreachable/incompatible queue slots remain normal blockers. Original restoration with nonzero handling cannot move earned credit into a waiting slot. No endpoint destruction, free capacity, double bay occupancy or instant movement.

## 6. Compatibility

Current contracts and original owners only; no fallback or alternative dock/job/claim state.

## 7. Verification

Targeted compile6.35s clean. New exact alternate-access case75/0 and existing exact native original-restoration case209/0:284 distinct successful checks. [All attempts and scope](../../docs/migration/P9_RETAINED_ENDPOINT_DOCK_QUEUE_VERIFICATION.md). No full/profile/family gate.

## 8. Risks

External point/independent tile providers, multiple mixed-resource dock slots, simultaneous endpoint loss, hostile callbacks and global performance/lockstep/soak remain broader gates.

## 9. Exclusions

Oil/Tycoon/platform/Draw/host gameplay, synthetic movement, duplicate inventory, full regression and Git publication.

## 10. Status

Implemented and scoped verified: original physical egress precedes alternate same-store waiting admission; actual busy service remains exclusive; saved queued cargo retains its original capacity receipt; timed service/departure and normal replay complete. Same original site/port restoration keeps earned handling. Broader spatial/provider/concurrency/global requirements remain open; whole goal active.
