# P11-C10: controller feasibility and shared observations verification

Current engine **2.452.0 /save154 /replay1**.
[Stage](../../plans/enhancements/P11_CONTROLLER_FEASIBILITY_DESIGN.md).
Full goal/plan active; no staging/commit/push.

## Matching original requirement

Original `P11.acceptance.034`, P11 primary design line1598, unchanged literal:

> Unaffordable/broken-route recovery is a typed wait; decisions remain bounded per tick.

Current combined proof and source inspection qualify only this checkbox.
Original221 gates:184checked/37open; additional178. Arbitrary hostile CLR CPU
preemption, every recovery strategy, broader pressure/private/provider/stress/
full-profile and full P1-P12 completion remain OPEN.

## Sole ownership and implementation

Original PlanRecoveryContracts.Read, PlanSiteContracts.Read and the two provider
validation calls in PlanPlaybookDef.ValidateBinding now use EntityObservation.
ReadProvider. Successful reads verify their actual scopes; thrown callbacks
latch the original first fault without finally verification masking it.
Original content/private checks, pending-only cancellation, site selectors,
controller cadence/command budget, plans/work/stock/spatial/clock owners remain.

No public API, serialized payload or schema change. Build identity advances;
save154/replay1 unchanged. No compatibility path or second observation/strategy/
execution store. Existing ControllerParityBoundaryTests pure public helper
visibility changes only; historical body/assertions are not rerun.

Two new explicit master-listed JSON files author physical timed generation and
separate optional economics. Resource-only scenarios do NOT load Tycoon.
Enabled-budget variants reuse existing sole native accounts/budgets/fixed values/
primary generation with money/construction/value/income on and trading/advanced
Tycoon off. No buyer, purchases, supplier behavior or protected G19/clock edits.
All production economic source files remain unchanged.

The new driver authors JSON, sends ordinary company commands, reads immutable
private facts/results, calls Game.Update/sole clock, saves and verifies replay.
It never reads World/TestKit/RunTicks or writes resources/work/progress/money.

## Actual compile and runtime evidence

Only one compile and ONE NEW exact runtime; no repair/older/family/full runtime.

`dotnet build Beep.SimEngine.slnx --no-restore --nologo`

Terminal exit0,0warnings/0errors,21.86s. Live compile97541 was resumed on its same
handle until terminal clean result before runtime.

`dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Controller feasibility: typed route and optional budget waits retain physical recovery and bounded decisions" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`

FIRST/FINAL **4997passed/0failed/0aborted in33452ms**.
PID12136 Exited0/process34311ms; no stdout/stderr truncation or observer error.
Live session22771 was resumed on the same handle; no observation timeout restart.
No executable edits after FINAL.

## Requirement mapping

Eight resource-only/optional-budget × minute/half-minute × native/independent
policy flows, two authored quota flows and one actual provider failure:

|Stage|Authoritative evidence|
|---|---|
|Initial ordinary recovery|Human claims30, rival retains actual blocked intent; authored delay and normal cancellation answer precede alternate42. One cancelled intent and one active real plan only.|
|Physical route failure|Actual assigned worker remains above42 before closure. Normal point-space access command closes route; original prepare node reports blocker.unreachable. Zero earned progress/resources/payment, no cancellation/replan duplication, stable actual link and one recovery attempt.|
|Durable route wait|Eight ordinary twin Updates, save/load, paused20-second platform input with no sole-clock advancement; original access reopen retries physical travel/work.|
|Budget off|No Tycoon module/account/income dependency; rival physically constructs using finite20→12 supplies.|
|Budget on|Actual build node and controller diagnostics both report blocker.cash. Saved twins wait eight Updates with zero earned build/money/consumption. No debt, grant or active-plan cancellation fallback.|
|Actual funding|Ordinary transformation enqueue is not instant income. Three authored game-minutes generate20 actual supply; native selected5/unit credits100 once without buyer. Original construction pays80 and consumes8 once; final cash20/reserved0/generation settlements1 and physical stock32.|
|Once-only outcome|Exactly one rival tower, exactly two historical intents/one recovery attempt. Retained blocked facts detached; terminal save, replay final hash and untouched interleaved session agree.|
|Decision cadence|Published facts checked after every ordinary Update in physical/quota runs: decision count delta0/1 and at most one awaiting native command. Three simultaneously ready quota steps produce only one initial proposal and exactly three separately accepted native placement answers over original cadence. Two saved/replayed clock variants agree.|
|Observation failure|Actual independent recovery-provider throw preserves proof.recovery-fault, cached diagnostics reject/clear outputs, save refuses, terminal Update0 and original fault stable.|

Actual phase indices (native and independent policies agree):

|Clock/mode|Cancel|Alternate/assignment|Unreachable|Cash wait|Income|Rival complete|
|---|---|---|---|---|---|---|
|minute/resource|7|9|10|not enabled|not enabled|189|
|minute/budget|7|9|10|182|193|199|
|half-minute/resource|11|15|16|not enabled|not enabled|375|
|half-minute/budget|11|15|16|362|376|388|

Quota completion indices4/7. These are actual sole-clock facts, not driver timing
formulas. Each stage bounds512Updates and retains last12 actual private summaries.
The fixture uses separate depots0/70 and real sites30/42, preserving collision
rules. Multiple inactive intent planning is not fabricated physical completion.

The native owner iterates each authored binding at most once per simulation
step, checks declared cadence, and invokes one bounded command context. Native
playbook stops submissions at its declared CommandsPerDecision. This is not an
arbitrary external CLR runtime/CPU sandbox or a hidden world-size cap.

Pure site/recovery binding error preservation is source-reviewed; this case's
negative runtime is a real recovery Observe failure, not a manufactured mutable
world callback. Every broader provider/fault/pressure/knowledge/lockstep/soak gate
stays open. Protected G19 and sole clock unchanged; no Oil/platform/Draw work.

## Metadata closure

Current read-only integrity:1518 source anchors/790 frozen selected executable
inputs/13 quickstart anchors match current bytes; zero post-FINAL drift.964
Markdown documents/8584 local links have zero broken links;18 master rows valid.
All221 original literal gates validate. Actual preceding-baseline comparison:
only P11.034 checkbox changes; no original requirement text/document/line change.

Five prior selected inputs changed;20 added:17 actual previously existing files
and three new driver/physical/economics inputs.22 existing source anchors refreshed
(14 checkpoint documents/eight executable files);14 existing files explicitly
admitted (12 untouched economic source files/BusinessView/mode-properties JSON)
alongside five new driver/two JSON/stage/evidence source anchors. Original engine
production changes are only three shared callback owners and build identity;
economic sources and protected four files were not edited in this slice.

Freeze2026-10-11 18:59:04 UTC. git diff --check exit0 (34 pre-existing
line-ending warning lines); actual dirty inventory2728/staged0. Four protected
clock/G19 hashes unchanged. Full goal/plan and publication remain OPEN. These
integrity checks are read-only metadata audits, not a second gameplay runtime.

