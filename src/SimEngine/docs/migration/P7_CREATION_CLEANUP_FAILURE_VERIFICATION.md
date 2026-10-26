# P7-D5 creation cleanup primary-failure verification

## Outcome

Engine **2.618.0 / save159 / replay1**. Public signatures and production codecs unchanged. The original World/EntityStore lifecycle, first-fault owner, allocator and spatial cleanup remain authoritative.

Three selected literal finals: **780 +218 +88 =1086 passed, zero failed**. Original P7.017 and the full goal remain open. Bookkeeping stays221 original requirements /200 recorded checked /21 tracked open, not a fresh global audit.

442 core C# path/content SHA256: `ac4b2a8396878c17e1b422478ba9cb51b02e10fed6bc53d5cd35965e1e6b6f8d`. Earlier checkpoints are historical unless explicitly rerun here. No financial/host/platform/Draw change, full/profile/family runtime or Git publication.

## Reproduced defect

The original EntityStore catch called cleanup before setting candidate Alive=false or notifying World of the primary creation failure. A throwing OnDespawn replaced the original thrown exception and left the unregistered candidate Alive. A cleanup callback that attempted a child creation could allocate that child and latch its initialization failure before World.Spawn finally saw the original error—even when cleanup caught the secondary exception itself.

New proof completed the full matrix on2.617 with **600 passed /180 failed**. No production behavior was changed to obtain that reproduction.

## Production repair

`World.FaultCreation` forwards to the original `LatchFault("entity.create",error)`, the same World fault owner as Spawn's phase-aware catch. Internal `EntityStore.Create` receives that required callback from its sole original caller, without an old overload or alternate path.

The existing catch now:

1. Notifies the existing fault owner of the original exception **before** cleanup.
2. Attempts the original selected space's OnDespawn exactly once.
3. Sends a cleanup exception to that same first-fault-wins owner, without replacing/rethrowing it over the primary.
4. Marks the actual candidate dead in finally, regardless of cleanup behavior.
5. Uses bare rethrow for the original exception object and stack.

The immutable WorldFault remains the first failure, not a new aggregate/secondary history. There is no second fault flag, observer, candidate registry, codec or compensating state. The outer World.Spawn catch remains necessary for constructor/registration and initializer failures and preserves this first record.

Cleanup is best-effort after terminal failure. It is not a rollback promise. If the selected provider refuses further operations because the primary fault is already latched, that secondary refusal cannot replace the original diagnostic or enable a healthy continuation.

## Focused proof

Literal: `Creation cleanup: primary exception identity candidate death and first fault survive secondary failures`.

New master-listed `creation-cleanup.json` is supplied through existing PointGame explicit extra-module/document composition. It declares the real point result and a child with the existing throwing initialization probe. The existing endpoint proof module owns creation/tail declarations; native point geometry, stock, clock, save/load and module composition are reused. Installed module selection remains explicit; no business mode is introduced.

Existing PointSpace `GeometryObservationFault` is now also invoked at configure/spawn/despawn boundaries. Instrumentation filters the exact cleanup.result prototype. Existing other geometry probes filter their selected phase, so these additional hook labels do not add normal behavior. Per-instance hooks/counters are test diagnostics, not authored game rules, saved progress or static mutable state.

Matrix: **48 negative contexts** = direct Spawn/CommitCreation × new/restored × configure/OnSpawn/OnCreate primary failure × four cleanup behaviors:

- successful cleanup;
- cleanup throws a distinct exception;
- cleanup attempts a real failing child creation and catches its exception;
- cleanup attempts that child creation without catching its exception.

Each proves exact original exception reference, withheld result, actual candidate, one primary and one cleanup invocation, original immutable fault visible before cleanup, same fault identity afterward, original entity.create phase/message, candidate Alive=false, no registered candidate, unchanged next ID (no child after first fault), no later OnCreate/Initialize or success events, retained publication, save/hash refusal and actual progress in another restored session.

Four healthy direct/restored × Spawn/CommitCreation checks preserve normal callback counts, real geometry/identity/hash and load without rerunning component creation/grant callbacks. Spatial OnLoaded may legitimately rebuild its own registration; the proof does not claim spatial restore callbacks never execute.

The proof uses direct provider/World instrumentation. It is not a public-player acceptance scenario, universal CLR isolation guarantee or proof of otherwise healthy commit purity.

## Existing selected regressions

- `Creation lifecycle: direct failures and swallowed faults stop later callbacks` **218/0** retains original phase/fault precedence, later callback cutoff, real pending prefixes and exactly-once healthy/load behavior.
- `Entity conversion: timed results, saved waits and provider fault boundaries` **88/0** retains native WIP/tools, actual capacity wait/repeat/cancel, sequential/independent entities, initialization failures, point/tile rollout and saved placement waits.

No broader lifecycle family was run.

## Every observed attempt

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

All runtime calls use one `--bounded-exact` literal per child, timeout60000 /termination5000. Baseline output6000; finals4500.

| Attempt | Result | Evidence |
|---|---|---|
| First proof build |Failed5.89s,3 errors|New test omitted Content namespace for EntityDef; no runtime against this failed build.|
| Test import repair build on2.617 |Clean5.10s|zero warnings/errors,exit0; original production cleanup unchanged.|
| New proof reproduction |600/180,403ms|pid22628,exit1,outer540ms;stdout truncated6000,stderr untruncated. Whole matrix/final counts returned; individual failure transcript is not claimed complete.|
| Production cleanup repair/build2.618 |Clean6.57s|zero warnings/errors,exit0; save159/replay1 unchanged.|
| New cleanup final |**780/0**,512ms|pid34152,exit0,outer691ms,untruncated.|
| Lifecycle cutoff final |**218/0**,221ms|pid8072,exit0,outer415ms,untruncated.|
| Timed result final |**88/0**,820ms|pid24516,exit0,outer1012ms,untruncated.|

No executable edits after these finals. All process results terminal; no live handle restart or broader/full run. Scoped whitespace checks passed, apart from informational Git line-ending warnings.

## Remaining boundaries

Original P7.017 remains open: otherwise healthy creation/initialization source and map purity, successful reentrant creation, coupled conversion callback/auxiliary observation, remaining disposition consumers, full topology/accounting and stress/performance need their own evidence. This stage does not add multi-error diagnostic retention. Other original P1–P12 gates remain tracked. No staging/commit/push until full goal and plan completion.
