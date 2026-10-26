# P2-F2 event-listener cutoff verification — 2026-10-06

Source build `beep-simengine-2.147.0`, save76; existing codec/view/event manifests unchanged.
The64 current source/test identity files migrate in place via apply_patch, not a compatibility reader.
No host/demo/Oil/Tycoon/platform/Draw change or second clock/dispatcher/state owner.

## Final gates

`dotnet build Beep.SimEngine.slnx --no-restore`: succeeded;0 warnings,0 errors, MSBuild2.26s.

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Event ownership: handler failure clears every pending queue | 84 | 0 | 86ms |
| Event ownership: nested same-type publications drain in order | 27 | 0 | 91ms |

Total111 checks, two suites, zero failures/aborts. Each runs with
`dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "<exact suite name>"` after the build.
An earlier clean build/run passed86 checks before adding healthy saved continuation; the same two
named suites were rerun. No third/family/full test gate. Other suites compile only. Preceding F1's190,
A9's242 and other historical gates are not rerun/additive or proof of current broad completion.

## Coverage

- EventBus has no public constructor; World constructs it exactly once and passes its own authority.
- A real engine GameSystem publishes multiple payloads during normal Update. Direct listener failure
  identifies events.probe.first; actual escaping/wrapped/swallowed entity initialization faults retain
  entity.initialize and their original cause. Existing WorldFault remains the sole immutable record.
- Later same-payload subscriptions, other queued payloads, nested publications and CompleteUpdate do
  not execute after failure. Existing finally clears all touched queues; later Publish/Dispatch and
  normal/diagnostic Update cannot resume the terminal event owner.
- Failure earns no completed game time, clears pending pacing budget and publishes faulted facts before
  Updated. Earlier healthy snapshot is retained; saves/hashes refuse partial terminal state.
- Healthy same-type recursive dispatch order and queue reuse remain correct. A public-only external
  module proves subscription order/global publication order/nested payloads via normal engine Update.
- Ordinary listener service Sync persists facts. Restored dispatcher is session-owned, not shared;
  five bounded interleaved normal updates retain exact facts and hashes. Paused one-clock Update
  changes neither listeners nor time; an unrelated session stays healthy and unchanged.

## SHA256 fingerprints

| File | SHA256 |
| --- | --- |
| Kernel/Events.cs | 04423D5E03C1E6E180F52F1EE3BDD7D84C702C105887ACDFE17B2892EB196E3D |
| Kernel/World.cs | 41B243BC07C56D20344244A4D00B7F6950FABEBB68C2AE0B730B8920A1EE1ACE |
| Tests/EventFaultBoundaryTests.cs | 0A56A769A36B1E8F023692D0C0FF1D86A094920C5AC763164F1A0404E1BAF972 |
| Tests/EventOwnershipTests.cs | 38736B43049C8E8750D5207C51D918F68442568E9A524C5D58FF517508AF6A05 |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |

Current inventory: one new GameClock at World.cs84, one new EventBus at85, one WorldFault constructor
in World's existing LatchFault. Tick is the sole clock's StepIndex coordinate, never another timer.

## Limits

Callbacks cannot be forcibly preempted; their internal code before returning remains provider-owned.
This is not arbitrary callback hardening, attached-player/host Updated error policy, full composition/
content freeze/protocol validation, startup/load lifecycle acceptance, replay/stress/allocations or full
P2/P3/P1–P12 proof. Developer fault strings are not safe player-localized messages. No instant recovery,
rollback, fault reset or retry fallback was introduced. Full regression remains unrun as requested.
