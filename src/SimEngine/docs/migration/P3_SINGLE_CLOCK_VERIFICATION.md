# P3-S1c single game clock — scoped verification

2026-10-06. Source build `beep-simengine-2.132.0`; save format71. No payload/schema changes.
This proves the native time-ownership slice, not the full framework implementation plan.

## Ownership

World constructs exactly one GameClock from its selected authored SimulationTimeDef. SimGame reuses
that same object, including restored worlds. GameClock.StepIndex is the only stored current simulation
time. World.Tick/Time/Date, entity contexts and game lifecycle project that owner. Immutable GameTime
converts units only. TotalGameTime and GameUpdate.ElapsedGameTime are exact existing GameDuration
values; wide integer products preserve the full signed64 age even with large authored quanta.

World.Step preflights through Clock.NextStep before gameplay and invokes CompleteStep only after
systems/events/entities/final phases succeed. The clock's single embedded Sync owns current-time
read/write/hash; its existing eight-byte world-codec position is unchanged. Negative/truncated time
rejects before publication. No world time setter/backing field or unattached public clock constructor.
IGameClock is read-only; module/entity users cannot advance time through that contract.

Local pause/speed/frame backlog retain their preceding pacing semantics. Saves/hashes record actual
game time, not earned platform input or a renderer's cadence. RunTicks remains an explicit diagnostic
override using the same clock and actual update path; it is not a second time owner.

## Fresh gate

Working directory: `C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine`.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Lifecycle: initialize, update, snapshots and events"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Framework: save, load, describe"
```

Final build: zero warnings/errors, MSBuild2.08s. Exact complete lifecycle suite:55 passed/0failed,
141ms. Exact complete save/load suite:30 passed/0failed,127ms. Total85 checks, exactly two suites,
no aborted suite. One earlier compile error in the obsolete test-only context constructor was
migrated before this final clean build; no stale binaries or runtime-failure retries.

Proof includes shared clock identity/converter, immutable read-only access, exact update durations,
fractional platform budget versus actual time, pause/speed, immutable past reports, snapshot/calendar
alignment, actual entity-phase observation, fault/no advancement, full-width exhaustion, queued-command
save continuation, independent sessions, native codec width/no write mutation, invalid/truncated decode,
invalid completion identity, exact thirds of a second and saved fractional continuation.

62 existing build assertions migrate; all unrelated suites compile only, not rerun. Existing test-only
wide-age/exhaustion reflection now instruments the actual clock, not removed World.Tick storage.
No third/family/full test run, no platform/Draw/host/demo/oil/tycoon mutation. Broader physical
work/logistics/replay/lockstep/provider/global rollback/fuzz/soak/allocations/full P1-P12 remain open.

## SHA256 after the verified source gate

| File | SHA256 |
| --- | --- |
| Hosting/GameClock.cs | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
| Kernel/World.cs | 8F007FD48437A4559072F16C2303A4A6DF97B66D85843653EA4874469CD49097 |
| Core/GameClockContracts.cs | C2CBBC3B8FDAE4374F77E9034D311B3CAC8CEC3D4B63E9B636A86958CF714B53 |
| Core/GameTime.cs | FA3841AA40BAD1DAE13BBE201C33EFA21D65F1D8D1D19956F60AB145F9D123A9 |
| Hosting/GameCycle.cs | 237D3F71CBF4F51A0371B6DA288E2A435F40B57B8C46345B418F4D653739B34A |
| Hosting/SimGame.cs | D4EED6E28BDE80029ECA969F26EDB862A01022100D2804D0C1B8AACE9C4EF3CD |
| Kernel/EntityLifecycle.cs | 9BA7DABA1FED02FE5362501BD44B7B41B476D96C08041C08097EC1CC7D96A75D |
| Tests/SingleClockTests.cs | 9FA81133B88F22590A5D37EEE0DDF6F234C90E0CDE74175681E2EBBE783F7889 |
