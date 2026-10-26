# P9-B11.3c2b1 authored service-first dispatch — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.193.0/save104/board36/work35/work view68/logistics view22/native recurring8/
public recurring5,5,6. Existing owner payloads are unchanged.
[Design](../../plans/enhancements/P9_SERVICE_DISPATCH_DESIGN.md).

## Ownership and implementation

Every native/public recurring definition explicitly reads dispatch_mode: compete or service_first.
Missing/unknown modes fail composition. service_first requires actual source-owned worker nodes
on a single activity; unbound/nonworker/template sources cannot invent machine reservations.
The immutable recurring view exposes the authored mode. There is no default or inferred policy.

WorkBoard factors its existing source/order/node/selected-worker provenance check. Self-service
reserve permission and service-first dispatch share that check; manual activities cannot gain
source authority by using matching node definitions. The service-first gate reads actual source
LastPlan and existing native cycle members. Live unpaused admitted work blocks fresh ordinary
machine assignments. Future unadmitted demand does not. No second plan, worker flag, fleet mirror,
reservation ledger, timer or scheduler is created. It does not recursively invoke plan admission.

The existing IWorkerDispatch idle and full assignment paths use the same typed
work.recurring-service-pending refusal. Authentic own service remains assignable. Eligible emergency
work bypasses only this reservation restriction, not physical/safety/policy rules. The existing
validated incumbent-continuation rule preserves loaded jobs, claims and cargo; service-first never
forces interruption, unloads cargo, refunds stock or teleports a machine. Pause, cancellation and
completion release fresh eligibility from actual plan state. Disabling future recurrence leaves
already admitted work intact. Actual shortages remain supplied by ordinary stock-band logistics.

The gate's assignment semantics revise WorkSystem to35, WorkView to68 and LogisticsView to22.
Board36, native recurring8, independent public recurring5,5,6, movement6, logistics11 and condition/
operation policy owner layouts remain unchanged. Save104 is unchanged. Current build identity
migrates directly; no old reader or compatibility path is introduced.

## Focused final gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: authored cadence, private admission and retained success"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: timed work, materials, restoration and saved continuation"
```

Final build: zero warnings/errors. Admission188 + physical220 =408 passed, zero failures or
aborted suites. Only these two exact suites ran; other suites compile only. Earlier completion/
trigger callbacks remain inside these suites, not a broad family filter. Physical waits are bounded
at400 normal updates; existing grouped service proofs at480. No full/family/third suite ran.

Native and independent public recurring/worker providers prove explicit/invalid content, real
worker binding, no speculative hold, shared idle/full assignment refusal, emergency exception,
actual private candidate presence, manual-clone refusal and unchanged hash after reads. Tests
exercise disabled future recurrence, pause/resume/cancel and explicit compete under the same
spares shortage. The candidate-presence assertion prevents an empty preview from falsely proving
provenance refusal.

Physical tests save an actual admitted shortage, supply it with another real carrier through normal
player stock-policy commands, consume actual spares, earn timed condition restoration, acknowledge
the full service cycle and release ordinary eligibility. Saved twins have identical hashes.

The loaded-shipment case first earns real cargo/outlet/inlet claims through normal stock requests.
Service is then admitted without changing those claims or the incumbent job. CheckJob permits its
actual continuation; save/load preserves it. Original delivery completes exactly once (four units
at the real destination), followed by whole physical service; saved twin hashes agree. Tests do not
implement gameplay: engine contracts, commands, updates and authoritative state supply the rules.

## Source boundary and limits

All previously authored sources migrate explicitly to compete; focused new content selects
service_first. Independent public definitions use the same mandatory parser. No host/demo/platform,
Oil/Tycoon or Draw gameplay was added or changed. The dirty tree is preserved.

An additional static JSON parse check found two malformed level fields in the existing
tests/ExtensionProof/content/stock-target-automation.json fixture. Both explicitly select desired
now, matching the existing stock action contract; no reserve policy or automation runtime rule
changed. The content parses, but its separate runtime suite was not run and is not claimed green.
All eleven recurring definitions in standalone authored JSON explicitly select dispatch_mode.

GameClock.cs remains unchanged, SHA256:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
World constructs it once and SimGame reuses it. World.Tick is only a read-only projection of
Clock.StepIndex, not a second time source. All durations, progress and service chronology use
this single game clock; the phrase update step denotes its fixed progression interval.

The lifecycle skill kept implementation, scoped evidence and tracker aligned. Literal source/doc
fingerprints appear in [the manifest](P9_SERVICE_DISPATCH_SOURCE_MANIFEST.json). Older manifests
remain historical checkpoints, not current fingerprints.

B11.3c2b1 is focused verified, not completion of B11.3c2b/B11.3c2/B11.3 or the full roadmap.
Multi-source service/queue fairness, failed-order/retry/provider stress, stranded propulsion rescue,
broader fleet/trip integration, global purity, CPU/allocations, replay and full acceptance remain open.
