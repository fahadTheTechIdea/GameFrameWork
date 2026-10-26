# P3 numeric and clock prerequisite — 2026-10-04

Historical evidence for 2.14.0/save 7. Current 2.15.0/save 8 replaces the int timestamps described here;
see [wide timestamp evidence](P3_WIDE_TIME_VERIFICATION.md). These counts do not verify newer source.

## Scope and verification policy

Only standalone framework source, tests, instructions and documentation were changed. No oil, Tycoon,
sibling engine, Unity/Godot, platform adapter or rendering implementation was changed or verified.
Initialize → Update remains the only engine lifecycle. The full framework goal remains active.

Build **beep-simengine-2.14.0**, save format **7**. Payload layout is unchanged; build identity deliberately
refuses older saves/peers. No compatibility shim, second clock, alternate numeric model or renderer was added.

As requested, tests were filtered to affected concerns, not run with --all. The latest full gate belongs
to 2.13.0 and is not presented as a full regression of this source. The lifecycle skill guided the scoped
task checks, source-specific evidence and sole-tracker update; AGENTS now records the user's test policy.

| Command/filter | Final result |
| --- | --- |
| `dotnet build Beep.SimEngine.slnx --no-restore` | Exit 0, zero warnings/errors, 1.72 s |
| `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- 'Time boundaries'` | 834 passed / 0 failed / 6 suites, 0.3 s |
| Filter `Lifecycle:` | 61 / 0 / 6 suites, 0.2 s |
| Filter `Standard:` | 49 / 0 / 8 suites, 0.2 s |
| Filter `Protocol boundary` | 70 / 0 / 4 suites, 0.1 s |
| Filter `Framework: math` | 10 / 0 / 1 suite |
| Filter `Framework: a scenario` | 12 / 0 / 1 suite, 0.1 s |
| Filter `Framework: determinism` | 6 / 0 / 1 suite, 0.1 s |

All filtered commands exit 0: **1,042 checks / 27 suites**, no failures. This is focused evidence only.
No current whole-suite, oil integration, allocation/soak or cross-game release claim is made.

## One numeric authority

The existing Fix owns checked addition/subtraction/negation, signed 128-bit limb multiplication and
96-bit scaled division. Multiplication preserves arithmetic-shift floor rounding; division is nearest
raw bit with ties away from zero. Representational overflow throws instead of wrapping a quantity,
duration, distance or rate into a different value. Full-width FromRatio inputs no longer need artificial
32-bit numerator/denominator conversion. Ceiling/rounding avoid overflowing raw intermediate sums.

Decimal/exponent parsing quantizes after applying the exponent, not before. The parser bounds its
intermediate to at most 42 decimal digits using Q32.32's 32 terminating decimal fractional places;
huge exponents cannot wrap. BCL BigInteger is used only in bounded content parsing and the independent
test oracle, not per-tick multiplication/division. No package or platform dependency was added.
Declared precision still truncates sub-raw-bit decimals, including very small values to zero; it does
not silently clamp an overflowing integer to a fabricated maximum.

The oracle checks 752 signed arithmetic results/refusals over extreme raw values and seeded full-width
inputs. Parsing/limits add 30 checks. An initial assertion incorrectly classified a just-below-integer-limit
decimal that validly truncates to the largest raw value as overflow; that assertion was corrected. Engine
range validation was not loosened. Duration parsing reports ContentException on actual overflow.

## One clock and game loop

GameClock retains earned whole ticks plus fractional input under its existing integer accumulator.
CatchUpCap is a per-normal-Update work budget for both elapsed-time and engine-time calls, not a limit
on supported game duration. PendingTicks diagnoses retained work; Update(0) drains without new time.
No input is clipped to one second or discarded merely because a budget is reached. Alpha uses only
the fractional remainder and always stays in [0,1), including stalled lockstep and long backlogs.

Pause earns nothing and hides Alpha but preserves work already earned. Resume, Speed and
TicksPerSecond changes do not reset that backlog; changes affect future input. A pause applied within
a pump ends further ticking in that pump. Terminal outcome/fault clears unapplied host backlog and
prevents later updates from accumulating time. Negative or unrepresentable elapsed inputs refuse
before ticking or replacing pending work. Checked arithmetic is the numeric safeguard, not an
arbitrary embedding-rate/frame-duration ceiling. Clock accumulation remains local pacing data, not
a duplicate authoritative simulation clock, saved ledger or source of gameplay decisions.

Tests prove 2.05 seconds at 10 ticks/second with budget 3 earns exactly 20 ticks plus one half tick;
draining matches a twin kernel batch. A 100,000-tick engine frame retains/drains its budget remainder
and matches a diagnostic twin. Pause/pace changes, stalled input, horizon cutoffs and invalid inputs
are covered. Physical construction, hauling, production, salvage and network work still pass their
focused suites with save continuation. The existing horizon fixture explicitly requests its large
per-Update budget instead of assuming the clock silently exceeds its work cap.

## Current representation and exhaustion

World preflights its current int tick increment before command/system execution. Exhaustion latches a
begin-phase fault without advancing gameplay. Entity ID/version increments fail before wrap; coordinate
differences use widened intermediates, and path/rectangle results check their declared current range.
StateReader validates ranges by subtraction, avoiding offset-plus-length overflow. The proof injects
limit values only through test-assembly instrumentation; no public state mutator or external module
reflection is introduced.

These checks are not the planned wide-time or optional-space implementation. Static minute/calendar
assumptions, int tick/duration fields, the 8..4096 tile-map side cap and dense spatial allocations remain
explicit P3 work. Remaining exact authored time definitions, saved schema/peer/view migration and
generalized state/space contracts must replace those paths, not supplement them.

## Executable-source identity

All **114 authored src/tests files** from `rg --files src tests`, excluding bin/obj, sorted by path.
Each line is `slash-normalized-relative-path|SHA256`; LF joined, UTF-8 encoded, then SHA-256 hashed.

- Source identity: `1ACAB082345176F5B589A73C88C4678895555BD74948C538181795882355651C`
- Runtime DLL SHA-256: `529152CD36D25DB3B7B70E920F07E1E46D75047E84B870C52D4EA663194B59EB`
- DLL: `src/Beep.SimEngine/bin/Debug/netstandard2.1/Beep.SimEngine.dll`

The single MASTER_TRACKER records T0a–T0c progress and leaves T1–T3/S1–S4 and the P3 stage gate open.
