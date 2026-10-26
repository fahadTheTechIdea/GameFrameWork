# P9-B11.1 — Operating horizons and remaining-route funding verification

2026-10-07 dirty working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.186.0; save101; work view64; motion view7. Saved owner payloads,
board35/work system32/movement system6 and the single GameClock remain unchanged.
The full framework implementation goal remains active.

## Implemented ownership

OperatingInputsContracts.Read now accepts an explicit nonnegative signed64-bit exposure
horizon. OperatingInputEvaluation carries Intervals and RequiredInputs; the former
evaluation.NextInputs member is removed, not retained as an alias. UtilityDemandSnapshot's
NextInputs still means the actual next accepted update. The existing native/public
IOperatingInputs and IOperatingInputsDefinition contracts remain authoritative.

One DrawIntervals helper delegates to existing ResourceRates.Between, using actual accepted
phase exposure, the sole clock's immutable authored time conversion, native capacity
quantization and cumulative ceiling rounding. Existing CalculateDemand and one-interval
physical Commit use this same calculation. Longer budgets therefore retain already-paid
quantum credit instead of multiplying rounded next-update demand. No extra rounding balance,
fuel ledger, time owner, rate copy or future exposure is saved.

Read aggregates the entire selected provider vector before the existing IResourceTransaction
inventory admission. Existing outbound promises remain unavailable. Arithmetic overflow
returns typed OperatingInputCodes.Range and no incomplete RequiredInputs prefix. Zero
duration yields no future input; negative public intent returns InvalidArgument. Provider
metadata, exposure and utility observation guards remain unchanged.

Existing private MotionView/WorkView expose the same evaluation. MotionArrivalView attaches
OperatingBudget only for an actual known remaining duration, including zero after arrival.
Unknown, unsupported, blocked, pending and idle routes have no invented funding budget.
RemainingTicks/ArrivalTick retain conditional current-route ETA semantics; the separate
budget reports current-stock shortage. Neither ETA nor funding is a reservation, assignment
permission or guarantee. Actual movement still admits/consumes one update at a time.

Existing explicit master-listed phase/rate JSON is reused. No new loader/default policy,
host/demo gameplay, Draw, Unity/Godot implementation or arbitrary horizon/platform size cap.
Public-only ExtensionProof providers still compile without engine internals.

## Focused verification

Final build: `dotnet build Beep.SimEngine.slnx --no-restore` — success, zero warnings/errors,
4.72 seconds reported by the build.

Only these same two existing suites ran for this stage:

- `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Operating inputs: phases, public contracts and atomic resource admission"`
  —156 assertions passed, zero failed;596ms.
- `dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Operating inputs: physical work, standby resupply and saved continuation"`
  —167 assertions passed, zero failed;1,229ms.

Total323 final assertions, two suites, zero failures/aborts. Other suite identity assertions
were revised mechanically to the current build and compiled only, not executed. Work/motion
view identities change directly; no old API reader or compatibility path is introduced.

New horizon callbacks prove zero/negative/wide/overflow durations, whole-vector native/public
aggregation, actual reserved stock, fresh release, phase selection, immutable/private views
and pure reads. An explicit sufficiently slow rational authored rate supports long.MaxValue
intervals without an arbitrary engine cap; unrepresentable ordinary-rate budgets refuse.

Physical callbacks submit ordinary SetHomeCommand, inspect actual routes, pause the sole
clock, save/load and continue paired sessions. Funded native and overlapping public routes
consume exactly the previously read remaining cumulative budget. Retained views do not follow
later exposure. Underfunded travel moves while its next interval is payable, then waits without
free distance, fuel or exposure; no last-known estimate substitutes for current blockage.
Existing callbacks retain movement, standby, resupply and provider-contract-fault coverage.

Initial new physical fixture failed: its machine also declared ITransporter, so existing
cargo return reserved its opening supply stock and posted job1 before home travel. Diagnostic
snapshot showed Idle, operating.inputs, stock6/job1. The route-budget fixture now explicitly
authors a non-hauling worker; production cargo/fuel rules were not bypassed or changed.
The original null dereference was replaced with a bounded status/blocker/stock/job diagnostic.

## Single clock and remaining work

GameClock alone owns current time. World constructs it once; SimGame reuses it; World.Tick
is a read-only StepIndex projection. Horizon intervals measure durations against that same
clock, not independent timers. GameClock.cs SHA256 is unchanged:
`339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.

B11.1 is focused verified, not full fleet capability. B11.2 prospective whole-itinerary
provider contracts, B11.3 authored reserve/service policy and native assignment/replanning
admission, B11.4 broader congestion/stranding/provider/replay/performance/full acceptance
remain open. No full/family/third suite, platform gate or Oil/Tycoon/host/demo change ran.

Source/dependency fingerprints are recorded in
[the source manifest](P9_OPERATING_HORIZONS_SOURCE_MANIFEST.json). They identify the current
dirty-worktree evidence, not a clean commit or proof that all dependency suites passed.

Final static checks:111 source/dependency fingerprints and8 document fingerprints match,
with zero duplicate paths, trailing-whitespace lines or missing documentation/help links.
Exactly one runtime GameClock construction remains; old current build references in C# are
zero. Scoped git diff whitespace check passed (line-ending normalization warnings only).
These checks are not runtime regression or allocation/performance evidence.
