# P9-B11.3c1 successful-completion service — verification

Date: 2026-10-07. Dirty working-tree checkpoint, not clean HEAD attribution or full regression.
Build2.191.0/save104/board36/work33/work view66/native recurring8/public recurring5,5,6.
Movement6/logistics11/motion view9/logistics view21/trip1 remain unchanged.
[Design](../../plans/enhancements/P9_COMPLETION_SERVICE_CADENCE_DESIGN.md).

## Ownership and implementation

Master-listed native/public recurring JSON must explicitly select submission or successful_completion.
The existing source Sync embeds only LastCompletedPlan, a stable actual cycle head ID. WorkBoard
alone resolves that source's frozen admitted cohort, requires every member Done, and derives the
latest actual EndedTick. Existing explicit/automatic history admission protects the successful
reference until superseded. No copied completion timestamp, service age, successful-work ledger,
new scheduler or clock is saved. Failed/cancelled attempts retain prior success; submission time
only rate-limits retry attempts. Existing command controls do not rebase successful history.

WorkSystem acknowledges complete cycles before its existing recurring admission pass. A pure
view between terminal board settlement and the next acknowledgement reports CompletionPending
and the actual completion-based deadline; it cannot claim readiness or mutate source history.
Exact provider verification rejects ignored, forged or settings-changing acknowledgement.
Faulted normal Update cannot advance the sole game clock or publish a resumable save.

ConditionChangeKind.Restore uses the existing ConditionContracts.ValueAfter calculator to restore
at least an authored bounded target, never damage healthy condition or over-repair. The existing
worker condition effect still requires positive-time machine work; native requirements consume
spares and hold/release tools. Machine travel, bay assignment, physical exit and later DAG stages
must finish before success. The same independent public-contract providers demonstrate extension.

## Focused final gate

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: authored cadence, private admission and retained success"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Completion service: timed work, materials, restoration and saved continuation"
```

Final build: zero warnings/errors. Admission92 + physical116 =208 passed, zero final failures
or aborted suites. Only these two exact service suites ran. The first calls a focused cohort
proof using the existing template fixture; no third suite or broad family filter was run.
All separate suites compile only, including previously verified trip/home/worker suites.

Evidence covers explicit/missing/unknown cadence; pure/private views; bounded restoration;
real selected native/public machines, positive work and supply consumption; actual bay departure
and subsequent DAG work; pause through the sole GameClock; mid-service and pending-success saves;
healthy periodic inspection still consuming authored spares; successful history retention/release;
cancellation preserving previous success; grouped member pause/resume, latest-member anchoring,
mixed cancelled/successful cohorts; invalid provider acknowledgement and saved twin identity.
Physical service waits are bounded at400 normal updates; grouped construction waits at480.

## Corrections and limits

Added cohort tests initially needed the existing ordinary work-policy command to enable the
fixture's disabled worker, and explicit Content.Json qualification because another test helper
imports a same-named symbol. Both test setup issues were corrected; no engine admission, stock,
physics or provider guard was weakened. The terminal-before-acknowledgement preview gap was fixed
in the authoritative recurring protocol and is covered by actual save/load/continue tests.

Existing authored submission fixtures migrate explicitly; no default cadence/legacy reader is
introduced. Current build/save and source/view/provider schemas migrate directly. Other suites
with historical owner-schema assertions are not claimed green. Existing dirty source is preserved.
The preceding reports/manifests remain historical checkpoint evidence, not current fingerprints.

GameClock alone owns current time. World constructs it once, SimGame reuses it, and World.Tick
is read-only Clock.StepIndex. GameClock.cs SHA256 remains unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.

The lifecycle skill kept implementation, per-stage evidence and tracker aligned. Current source
and document fingerprints appear in [the manifest](P9_COMPLETION_SERVICE_CADENCE_SOURCE_MANIFEST.json).

C1 is focused verified, not completion of B11.3c or the full roadmap. Integrated early-condition
overrides and operation/fleet/trip readiness, broader queue/shortage/provider/failed-order stress,
arbitrary provider/global purity, allocations/performance, replay and full framework acceptance
remain open. No Oil/Tycoon/host/demo/platform/Draw changes or full/family/third gate.
