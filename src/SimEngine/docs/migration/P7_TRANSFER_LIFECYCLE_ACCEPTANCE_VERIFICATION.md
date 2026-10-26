# P7-C5: physical transfer lifecycle acceptance verification

Current engine **2.450.0 /save154 /replay1**, unchanged in this slice.
[Stage](../../plans/enhancements/P7_TRANSFER_LIFECYCLE_ACCEPTANCE_DESIGN.md).
No gameplay/protocol/clock changes; no staging/commit/push before full goal/plan.

## Matching original requirement

Original `P7.acceptance.018`, P7 primary design line1102, unchanged literal:

> Transfer/cancel/despawn/save during pickup and delivery preserve accounting and physical position.

The current combined eight-flow proof qualifies only that checkbox. Original221
gates now182checked/39open; additional176. Other P7.009/.017/.020/provider/
quality-lot/stress/global and full P1-P12 acceptance remain open.

## Actual implementation ownership

The new driver reuses the existing authored retirement master/JSON factory and
ordinary player activity helper. Those two helpers became internal; their
bodies/historical assertions and production engine sources remain unchanged.
The original runner registers exactly one new case. WorkBoard/jobs, native or
bound actual inventory, owned receipts, spatial motion/docks, resource recovery,
guarded entity removal and sole game clock remain the only gameplay owners.

The driver never uses mutable World, TestKit/RunTicks, stock/progress/state writes,
instant removal or duplicate recovery rules. Its balance/promise/position checks
are assertions over public private engine facts, not execution algorithms.

## All compile attempts

| Attempt | Command | Terminal result |
|---|---|---|
|1|`dotnet build Beep.SimEngine.slnx --no-restore --nologo`|exit0,0warnings/0errors,6.27s|
|2|`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore --nologo`|exit0,0warnings/0errors,7.12s|
|3|same targeted command|exit0,0warnings/0errors,6.42s|

Each runtime followed a confirmed clean terminal compile. First compile's shell
call also contained a read-only Fix.One lookup with one nonexistent-path warning;
that is not a compiler failure. No executable edit after FINAL.

## All runtime attempts

ONE NEW exact case only, same-case repairs; no older/family/full runtime:

`dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Transfer lifecycle acceptance: saved pickup delivery cancellation and physical removal conserve stock and position" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`

|Attempt|Result|Process evidence|
|---|---|---|
|FIRST|23637passed/28failed/0aborted,34105ms|PID44596 Exited1,34292ms; no truncation/observer error|
|SECOND|TimedOut; check counts unavailable, not a pass|PID10784 terminal timeout/exit-1,60089ms; no truncation/observer error|
|FINAL|**23773passed/0failed/0aborted,34606ms**|PID5160 Exited0,34810ms; no truncation/observer error|

FIRST incorrectly expected source/destination centres from another fixture,
instead of actual JSON service slots1/11 and their engine-selected slot IDs.
It also assumed unrelated earned packing could not commit after cancellation
released its blocking promise. Fixture correction uses ordinary plan pause to
isolate cancellation's own stock/position effects, then ordinary resume executes
the already-earned packing. No engine rule was changed to satisfy assertions.

SECOND exhausted its declared wall bound before counts/phase diagnostics. Its
cause is not inferred as engine deadlock or successful gameplay. FINAL adds
limited public stage/wait diagnostics and completes under the same60s bound;
all attempts remain recorded. Live sessions66965/31315/73568 were resumed via
their same handles to authoritative terminal output, never restarted merely
because observation yielded.

## Eight-flow requirement mapping

Native/bound cargo × minute/half-minute × partial pickup/partial delivery:

|Stage|Actual evidence|
|---|---|
|Normal transfer|4 finite supplies; saved actual pickup and delivery; reaches authored service slots1/11; exact source16/destination4/cargo0; old promises released|
|Second partial transfer|8-material intent; actual positive partial cargo/handling; endpoint/actual inventory/carrier IDs/placement remain distinct; separate cargo stays at75 without worker stock copy|
|Removal with claims|Physical actor travels and earns packing preparation; actual ActivePromises blocks stock disposition/removal; retains carrier cargo/position/handling|
|Saved cancellation|Ordinary removal-plan pause, pending held command, saved twin and sole paused clock; resume accepts real answer, clears old job/receipts only, exact committed source/destination/cargo/position retained|
|Endpoint removal|Ordinary plan resume; timed packing/removal; residual endpoint stock accounted in actual containers at exact old inventory placement; old stable endpoint absent, carrier/bound owner unchanged|
|Loaded carrier guard|Ordinary retirement waits UndisposedStock; saved actual cargo retained; cancel that intention, actor physically clears route to home70|
|Residual return|Authored receiver demand; ordinary work policy/unhold; same actual carrier and inventory source endpoint; saved return travels/handles, exact real residual delivered once, receipts released|
|Empty carrier retirement|Actual service actor reaches carrier+1; positive timed prefix saved; final original removal once; separate empty inventory persists at original75 when bound|
|Durability/boundaries|Balance20/nonnegative stock/promises checked every observed Update; retained partial facts detached; foreign history withheld; terminal saves, public-command replay and untouched interleaved session hash agree|

Every physical wait bounds512Updates with last12 actual private diagnostic
snapshots; completed phases and128-update wait markers are diagnostic only.
FINAL last retirement indices: minute278/279/285/286, half536/538/548/550 in
native pickup/native delivery/bound pickup/bound delivery order. These are sole
clock facts, not driver-computed durations or instant fallback.

No selected lot/quality model, arbitrary-provider purity/security, universal
deadlock freedom, allocation/stress, topology/conversion/spill or full-profile
claim. Protected clock/G19, Oil, Tycoon, Draw/adapters are untouched. Historical
cases compile only and are not treated as current runtime evidence.

## Metadata closure

Current read-only integrity:1495 source anchors/761 frozen selected executable
inputs/13 quickstart anchors match current bytes; zero post-FINAL drift.960
Markdown documents/8576 local links have zero broken links;18 master rows valid.
All221 original literal gates validate. Actual preceding-baseline comparison:
only P7.acceptance.018 checkbox changes, zero requirement text/document/line
changes. Counts182checked/39open, additional176.

New driver boundary matches0. `git diff --check` exits0 with34 existing
line-ending warning lines. Inventory2720dirty/staged0 preserved; four protected
clock/G19 SHA256 hashes unchanged. Two previously selected executable inputs
changed (original test factory/helper visibility and runner registration), one
new driver added to the same freeze; no existing source newly admitted and zero
production source changes. Program quickstart registration hash updated in
the existing13-anchor set. No executable edit/compile/gameplay rerun in closure.
