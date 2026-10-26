# P3-S1c — Single game-clock ownership

Status: native ownership change focused-verified on build2.132.0/save71, 2026-10-06.
User correction: one game clock, not a world timer plus a pacing clock. Full P3/P1-P12 remains open.

Current source is2.136.0/save73 after [P10-B7b](P10_OUTLET_RESERVE_ACTIONS_DESIGN.md), with clock source
unchanged. Its682 automation checks include sole-clock pause and physical saved hauling, not a rerun
of this preceding85-check dedicated clock gate. A simulation step is a coordinate on the one clock,
not an independent timer or module-owned update loop.

## 0. Problem

`World.Tick` currently stores simulation age; `SimGame` constructs a separate pacing-only
`GameClock`. Although both cooperate in the same loop, current time does not belong to the clock.

## 1. Goals

1. Exactly one session-owned `GameClock` stores current game time and controls pause/speed/pacing.
2. World, lifecycle, systems and entities read that clock; no separate stored world time remains.
3. Publish exact total game duration and update duration without floating point or narrow age limits.
4. Save/load/hash current time through that clock's sole codec, with staged invalid-time rejection.
5. Retain deterministic physical update order, calendar projections and session isolation.

## 2. Design

World constructs its clock once from the explicitly selected JSON time definition. The clock owns
its immutable `GameTime` conversion and full-width `StepIndex`. Fixed-step timestamps remain exact
ordering/time units, not clocks. World.Tick/Time/Date are read-only projections, not stored copies.
SimGame uses the existing world clock; it never creates another clock when wrapping a restored world.

`IGameClock` exposes read-only time, duration, calendar and pause/speed facts. EntityUpdateContext
exposes that same contract. World.Step preflights the next index before gameplay, runs existing
systems/events/entity updates, then asks the clock to commit exactly that completed step. A failed
step never advances time. GameUpdate remains a detached report and includes actual game duration.

GameClock.Sync embeds current time at the existing world codec position. Pacing backlog/interpolation
and local speed/pause controls retain their existing non-authoritative save/hash semantics: platform
frame pacing must not change hashes at identical simulated state. They do not represent another time.
Exact duration construction uses wide integer intermediates and the existing GameDuration type.

## 3. API

Read-only IGameClock: StepIndex, Time, TotalGameTime, StepDuration, Date, Paused, Speed.
SimGame.Clock retains normal clock controls. IGameCycle.Clock and EntityUpdateContext.Clock expose
read-only facts. GameUpdate.ElapsedGameTime reports game duration, distinct from input platform delta.
Clock construction and time advancement are engine-internal; no unattached public clock constructor.

## 4. Files

Core/GameClockContracts.cs, Core/GameTime.cs, Hosting/GameClock.cs, Hosting/SimGame.cs,
Hosting/GameCycle.cs, Kernel/World.cs, Kernel/EntityLifecycle.cs, Kernel/SaveGame.cs;
focused lifecycle/save tests and test-only boundary instrumentation; tracker and engine documentation.

## 5. Errors

Negative decoded time rejects before publication. Step exhaustion faults before commands/systems.
Incorrect completion index rejects, never wraps. Existing lifecycle reentry/fault guards remain.

## 6. Compatibility

No old timer authority, compatibility facade, duplicate codec or save reader. Build identity advances;
save payload position and layout stay unchanged, so format71 and existing owner schemas remain.

## 7. Verification

Clean build, then only these two exact complete suites, extended with clock ownership proof:

- Lifecycle: initialize, update, snapshots and events
- Framework: save, load, describe

No third/family/full run. Existing unrelated suites compile only. Full framework roadmap remains open.

## 8. Risks

Boundary tests reflect into the clock, never obsolete World.Tick storage. Wide durations use exact
rational metadata. No inference of calendar, second/minute quantum or paused-time credit.

## 9. Out of scope

No rendering, platforms, oil/tycoon, hosts, separate update loops or broad regression. No change to
the existing explicit diagnostic RunTicks override or lockstep pacing protocol in this stage.

## 10. Order

- [x] Move authoritative state and codec into existing clock; wire all existing lifecycle readers.
- [x] Add read-only contract and exact total/update duration projections.
- [x] Update boundary instrumentation/build assertions without compatibility paths.
- [x] Build and run the two exact suites; record scoped evidence and update master.

## Recorded gate

Clean build: zero warnings/errors. Lifecycle55 and save/load30 checks pass; total85, exactly two
complete suites, zero failures/aborts. [Evidence](../../docs/migration/P3_SINGLE_CLOCK_VERIFICATION.md).
All62 earlier build assertions migrate mechanically; unrelated suites compile only. No save schema
changes. Public extension assembly compiles without engine friendship. The lifecycle skill guided
the staged spec/owner change/verification/docs sequence. Scope review: read-only module contract,
one concrete clock/codec, converter has no current time, detached lifecycle reports have no advancement.
Broader physical/replay/lockstep/fuzz/soak/allocation/full-roadmap gates remain unproven, not complete.
