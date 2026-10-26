# P3 wide authored durations and fractional work — 2026-10-04

Historical evidence for 2.16.0 only. The [2.17.0 session-time slice](P3_SESSION_TIME_VERIFICATION.md)
replaces static duration conversion with exact authored intent resolved per session. Old API names,
counts and remaining-work statements below describe the source that was verified then, not current HEAD.

## Scope and status

Only Beep.SimEngine runtime, standalone tests and local documentation changed. No oil, Tycoon, sibling
engine, host/demo gameplay, adapters, renderer or platform implementation changed or ran. Initialize →
Update remains the sole lifecycle. The full framework objective remains active; P3 is incomplete.

Build **beep-simengine-2.16.0**, save format **9**. This replaces elapsed work storage and affected
layouts, not a compatibility layer. The development-lifecycle skill keeps implementation/evidence
and the existing stage/master tracker aligned. The user's testing instruction overrides its full
regression guidance: only exact directly affected tests ran, not a broad family-filter batch.

## Authoritative changes

- Json owns one strict lexical number validator, shared by parsing and JsonValue.Number construction.
  Valid JSON numbers need not fit Q32.32. Typed Fix readers retain their representational check and
  return contextual ContentException, not an uncaught arithmetic/format failure.
- JsonReader.DurationHours returns long ticks. GameTime scales the exact decimal/exponent literal
  before a single ceiling. There is no pre-rounding through Fix: 0.05 hours is exactly 3 current ticks,
  and 0.1 is exactly 6. Positive sub-tick work rounds up, negative work and overflow refuse explicitly.
  Parsing is linear in authored digits with bounded carry; extreme exponents do not allocate a huge
  power of ten. Missing-field whole-hour fallbacks do not round through formatted fixed-point text.
- StepDef.Ticks, RecipeDef.Ticks, LineDef.TicksPerTile and generated demolition durations are long.
  Existing work plans/queues continue to own execution. WorkBoard refuses negative generated steps.
- WorkProgress replaces Fix elapsed work in Order.Progress and Producer.Progress. It holds nonnegative
  long whole ticks plus unsigned Q32 fractional ticks. Advance preserves fractional carry and clamps
  completion at the actual requirement without overflow on overshoot. No duplicate progress store
  or implicit narrowing/conversion remains. Rates/quantities still use their existing Fix contracts.
- StateSyncExtensions.Work is the single save/read/hash codec for progress. WorkBoard schema is 3,
  Producer component codec is 2, and save format is 9. Prior layouts/builds refuse; no old reader survives.
- Work/production views expose full progress and long requirements. Production's inspection gauge is
  now a normalized 0..1 fraction, computed inside the engine. Its elapsed-work scalar automation signal
  remains typed in work-ticks; when actual progress cannot fit Fix, it returns `signal.numeric-range`
  unavailable with no numeric value. It never substitutes zero, saturation or another unit.
- LineDef.DurationFor owns per-tile multiplication. LineService.Check returns `core.duration-range`
  on overflow, and Begin uses that same check before allocating a marker or order. Tile stamping uses
  complete whole per-tile work, without narrowing elapsed work into a Fix/int quantity.

## Verification actually run

From the standalone framework root:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Durations:'
```

Final build: exit **0**, **zero warnings/errors**, **1.59 s**.

| Exact selected suite | Passed | Failed | Time |
| --- | ---: | ---: | ---: |
| Durations: exact JSON scaling and full-width fractional progress | 78 | 0 | 15 ms |
| Durations: physical work, production and lines retain saved progress | 43 | 0 | 146 ms |
| **Total: only these two new duration suites** | **121** | **0** | **0.2 s runner** |

No full suite, broad filtered sweep, prior test-family reruns, quick-setup run or oil integration gate
was executed. Existing tests were migrated to the breaking public progress type and literal save 9;
they compile, but compilation is not a claim that their behavioral checks passed on this source.

The initial exact same two-suite run exposed three failures. One test expected FormatException where
Json.Parse intentionally wraps malformed JSON as contextual ContentException; its expected type was
corrected. A real typed-reader gap now rejects valid-but-unrepresentable Fix quantities with contextual
ContentException. The line test tried to finish after three fixed ticks even though its marker had
moved to the next tile; it now waits within a 60-tick bound for actual machine travel/completion.
No movement/arrival rule was bypassed or weakened. Only those same two suites were rerun.

## Coverage and limits

The numeric suite includes exact boundary decimals, 32 seeded comparisons against an independent
rational BigInteger oracle, extreme exponents, long.MaxValue success/refusal boundaries, strict JSON
grammar, former int-limit fractional carry, overshoot clamping, normalized wide fractions and shared
save/read/hash layout including negative stored-progress refusal.

The physical suite starts real jobs through public commands and waits for normal supply/dispatch/
arrival before framework-test-only boundary instrumentation positions elapsed progress near a wide
goal. It saves/restores construction, generated demolition, producer queues and partial/final lines,
then advances actual remaining work and compares twin hashes. Later construction materials travel
physically and are consumed by the ordinary execution path. One queued recipe creates exactly one
vehicle, and overflow line previews/commands refuse without partial markers/identity allocation.

This is bounded boundary proof, **not billions of ticks of soak**. Other scenario combinations,
full regressions, replay/lockstep compatibility at this new layout, allocation budgets and hostile
save-state fuzzing remain unproven here. Broad/full verification requires an explicit user request.

## Executable-source identity

All **117 authored src/tests files** from `rg --files src tests`, excluding bin/obj, sorted by path.
Each line is `slash-normalized-relative-path|SHA256`; LF joined, UTF-8 encoded, then SHA-256 hashed.

- Source identity: `286C094B91EDF6D9F9037CF62B36FC5FF93A774EC988443EF55C791472C345AF`
- Runtime DLL SHA-256: `62A62CF4FFAF08D733A4995799A80833CD7274A877B90EEC871743C6B08C54C1`
- DLL: `src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll`

## Still open in the original roadmap

Session-authored quantum, exact cross-quantum duration/schedule conversion, optional calendar/horizon,
minute/frame/calendar assumption removal, optional spatial providers, arbitrary platform-cap removal,
generalized state references/facets and the complete P3 acceptance gate remain open. Current authored
hours still resolve against the existing minute tick; there is no dormant time model or parallel clock.
P4/P7 and the subsequent framework/gameplay stages retain their full scope and dependency order.
