# P9-C4a — Timed cargo handling verification

Historical gate. Subsequent [C4b](P9_DOCK_QUEUES_VERIFICATION.md) uses build2.62.0/save34/work16/view18;
these suites were compiled, not rerun for that change. Counts below remain C4a's original evidence.

Date: 2026-10-05. Working source **2.61.0/save33/work15/work-view17/transporter2**;
logistics4/logistics-view8/flow4 and optional company-alerts1/view1 unchanged.
[Stage](../../plans/enhancements/P9_TIMED_HANDLING_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Exact focused gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Timed hauling: strict native handling profiles, exact scaled time and private progress"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Timed hauling: physical service stages, interrupted receipts and saved sessions"
```

Final build zero warnings/errors, **2.18 seconds**. Admission **53 passed / 330 ms**; physical
**47 passed / 278 ms**. **100 checks / two suites / zero failures / zero aborted suites**; summed
reported suite time608 ms. TestRunner substring selection with these complete unique names selected
exactly one suite per command, confirmed by output. No broad/full/third suite ran.

Initial build failed one CS0246 missing Kernel import for Entity; corrected clean build4.21s. The first
test build passed1.89s, then admission had47 passed/2 failed (checkpoint write lacked physical bounds
validation); physical had42 passed/5 failed (pause fixture lacked explicit work_policy). Fixed actual
WorkBoard checkpoint validation through the existing ValidateHaul and explicitly authored the fixture's
work policy. No runtime fallback or fake pause was added. Clean build2.14s preceded corrected49/47 suites.
Additional immutable-content/finer-quantum/bounds proof then had one CS0246 missing System import in
Capabilities; corrected before any tests. Final gated2.18s build and53/47 suites are the current evidence.
No stale-binary runs after either failed build are counted. Previous reports are historical, not rerun.

## Implementation and ownership

TransporterDef requires handling profile; ITransporter exposes that immutable contract and its existing
cargo. Empty old transporter JSON refuses, without a compatibility reader or instant default. Authored
ResourceHandlingProfileDef rows reference the same selected ResourceDef/native quantum with positive
basis quantity and positive rational durations for each phase. Unknown/duplicate resources, missing
phase, zero/non-native basis and undefined phase/amount refuse. Unsupported resource or duration beyond
signed64-bit ticks has typed refusal before any carrier/job/native receipt acquisition. Profiles are
read-once; resolved content base aliases admit independent public-only profile subclasses.

GameTime.Ticks(duration,numerator,denominator) scales exact authored duration before rounding to session
quantum through the existing converter. No second converter or fixed global minute clock. General zero
ratio stays zero; handling always requires positive duration/quantity. Checked long boundaries and both
minute/30-second session quantum are proven. Effective worker rate advances existing WorkProgress, so
fractional machine work remains exact without narrow ticks or a saved remainder ledger.

WorkSystem's existing Load/Unload path advances one Job.HandlingProgress only while currently reached
and eligible. Its ordinary entity state source reports loading; Entity.Update alone commits state.
LogisticsService's direct physical methods additionally guard completed service, preventing callers
from bypassing time. Existing batch ResourceTransaction performs exact stock/promise mutations only
after service completion. This is elapsed service, not gradual stock transfer: all cargo/source/destination
balances remain real throughout, and a completed batch commits once. Per-subquantity receipt semantics
and progressive inventory remain future native-protocol work, not a simulated copy.

The same job/WorkBoard.Sync saves current leg progress; no component timer/system/ETA/queue or new
inventory owner is introduced. Native claims persist through policy waits/physical reapproach; actual
leg transition/end clears elapsed service. Load/unload cancellation releases only native unspent claims
and retains loaded cargo for existing timed return handling. Checkpoints and complete restoration use
phase/profile bounds validation. WorkJobView exposes detached private progress/required ticks rather
than asking hosts to calculate completion. Save33/work15/view17/transporter2 explicitly migrate in place.

## Proof and scope

Admission proves explicit master JSON binding/base aliases, native amount scaling, separate load/unload
rates, fractional discrete rejection, unsupported/invalid amounts/phases, final-ratio rounding, wide
tick boundary/overflow/invalid denominator, pure private/observer progress facts, immutable retained
facts, premature pickup refusal and partial-service save/hash identity. Resolved content rejects re-read.
Out-of-range handling refuses allocation/promises without state change. Checkpoint write rejects elapsed
time above duration or a fractional overshoot; actual accepted restored continuation remains fault-free.

Public-only SlotHandlingDef is a normal master-listed content provider using a slot-based batch duration,
not the builtin linear rate. It owns no job, progress, stock, claims or update. Native WorkSystem honors
its tenth-update pickup after9 real partial updates and saves through the unchanged sole owner.

Physical proof uses source20/cargo capacity8/destination10 units away: three real load-service updates,
explicit policy pause without progress/claim loss, saved/interleaved twins and a separate half-quantity
session. Full loading removes8 actual source units once; cargo travels rather than teleporting. Partial
16-tick unload retains8 cargo and real destination receipt, rejects direct early commit, saves/continues
and delivers once. Interruption before pickup consumes nothing; interruption while unloading preserves
all actual cargo, and existing return cargo obeys timed service. Normal endpoint removal refuses promises;
explicit missing-participant fault injection (not player gameplay) cleans surviving claims deterministically
after save. Half-rate carrier saves Q32 fractional progress and finishes four required ticks on its
eighth update, not earlier. All waits bounded360 with current job/toil/progress/blocker diagnostics.

Point/quantum/colony/recovery/disposition authored fixtures migrate explicit profile bindings; public
proof assembly and all suites compile. CompanyAlertTests and SharedMachineDispatchTests current build/
save/work schema assertions migrated and compiled only. No older suite was rerun or counted; compilation
does not prove their timing assumptions/full regressions. OilFieldDays content was deliberately untouched;
it requires a later ordinary extension content/schema migration before this new engine build can run it.

Scoped checks passed: tracked-slice git diff --check (line-ending conversion notices only), explicit
trailing-whitespace scan across29 edited/new files, all six edited/new JSON documents parsed, and
current build/save/work/view/component declarations plus progress/commit gates inspected. Docs-only handoff causes no
additional build/test. Lifecycle skill kept stage-first design and evidence; user's focused-gate rule
overrides broad regression. Existing dirty/unrelated work preserved; no commits/reset/Git init or Oil/
Tycoon/host/demo/platform/Draw changes. Physical dock/waiting queues, incremental receipt/stock protocol,
congestion/fuel/fleet/areas, starvation/preemption, persistent plans/templates, adversarial external
provider/fault/replay/lockstep/stress/soak/allocation and whole-roadmap acceptance remain open.
