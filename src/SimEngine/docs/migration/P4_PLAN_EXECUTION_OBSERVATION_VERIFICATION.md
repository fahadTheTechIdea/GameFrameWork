# P4-R4 execution binding and order identity evidence

2026-10-15. Engine2.609.0/save159/replay1/work-view108. Five literal finals1560/0.

## Actual defect

`ResolveExecutionPlan`, plan promotion and saved-plan validation called execution
binding/identity contracts directly. In promotion, priority equality was checked
BEFORE ExecutionIdentity received the actual mutable native Order. A custom
callback could increment priority and return the correct identity, with no fault.
Other callbacks could change target, space or clock; exceptions were caught only
at an outer update boundary, without the actual provider's observation context.

The corrected pre-fix reproduction960/9 proves seven mutation combinations escaped
without a fault and two original exceptions reached the wrong boundary. The test
posts ordinary persistent-plan input and lets the engine promote it. It performs
one actual warmup Update first: promotion runs before its own clock epoch commits,
so the immutable negative probe must not be armed by an initial StepIndex>0 check
at the very first update. No time field or live order is written by the driver.

## One codec and observation owner

The original76-line native order codec body moves verbatim, apart from indentation,
to `WorkBoard.SyncOrder`. The original board loop calls this method; original
reading, field order, validations, node resources/costs and order registration stay
in that SAME method. A mechanical in-memory comparison of the captured original
body against the extracted body returned true (76lines each). No separate order
serializer, mirror record, new payload field or schema. This is field-sequence
source evidence, not a claim of whole-save byte identity across different builds.

Existing `PlanObservation` gains Binding and ExecutionIdentity invocation. Binding
observes actual provider, whole target and selected space. Identity additionally
observes the actual native order through that sole codec and requires the derived
Plan reference to remain identical. No whole-board serialization, copied book or
second fault owner. Original World/EntityObservation owns faults and cutoff.

Execution resolution, promotion and saved checks migrate in place; remaining
native Construction/Networks/Infrastructure declaration forwarding is underlying
implementation, not an alternate raw consumer. Existing contract comments now
explicitly require pure binding/identity and read-only access to supplied orders.

The existing negative subtype extends the same independent declaration/parser.
Its immutable JSON stage selects binding or execution identity; faults select
target/clock/space/throw, plus direct native priority mutation for identity. One
existing fixture factory now authors all intent/admission/execution probes, replacing
repeated factory bodies. No mutable callback is installed in shared definitions.

Build2.608.0→2.609.0 and work-view107→108; authored protocol expectation updated.
Save159/replay1/state schemas unchanged; no compatibility reader or alias.

## All attempts, including test corrections

Build command throughout:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

All seven builds exited0, zero warnings/errors, reported durations5.80s,4.24s,
5.32s,4.32s,5.29s,16.40s,4.58s. The production build's session46747 was polled
to terminal before runtime; no process was restarted after a yielded observation.

Work-observation literal attempts, in order:

1.942/9,1007ms, PID30228 exit1/1142ms. Initial probe checked after only one Update
  and its first-epoch activation flag was not yet armed; not accepted as reproduction.
2.930/3,956ms, PID29876 exit1/1092ms. A live CopyPlans query used to detect activation
  itself invoked validation/codec callbacks; suite threw during the negative clock
  case. Removed that interfering query, not the fault assertions.
3.942/9,1106ms, PID5716 exit1/1242ms. Bounded Update-only driver still missed the
  initial pre-commit callback activation; no production conclusion taken.
4.942/9,1047ms, PID19224 exit1/1187ms. Added immutable result/clock diagnostics:
  command accepted and12epochs elapsed. Confirmed fixture arming, not refusal.
5.960/9,1112ms, PID12596 exit1/1266ms. One normal warmup epoch properly arms probes;
  seven mutation bypasses and two outer-boundary exceptions now demonstrated.
6.FINAL1025/0,1492ms, PID29268 exit0/1679ms after production repair.

Other exact cases:

- Graph FINAL246/0,974ms, PID8688 exit0/1113ms.
- Process staffing FINAL102/0,6416ms, PID26668 exit0/6553ms.
- Native protocol FINAL92/0,2559ms, PID32436 exit0/2696ms.
- Infrastructure94/1,3464ms, PID32404 exit1/3592ms: ONLY stale save154vs159
  assertion. Updated it to independently authored StateCodecTests.ExpectedSaveFormat;
  no physical assertion changed. Rebuilt tests only; runtime core source unchanged.
- Infrastructure FINAL95/0,3387ms, PID2008 exit0/3516ms.

Only five literal selectors ran, using `--bounded-exact`,60second timeout,
5second termination deadline and bounded output:

1. `Work observations: physical execution and terminal candidate plan query boundaries`
2. `Activity graph disclosure: nested requirement and node final effects stay private while earned work continues`
3. `Process staffing: automatic exact queue teams, retained duty, sibling contention and saved departure`
4. `Current native protocol: authored expectations and physical selected-role checkpoints`
5. `Infrastructure contracts: one owner earns physical corridor work and private flow facts`

Final sum1560/0 excludes all earlier failed attempts. Every child terminal; no
process timeout/abort. The earlier suite exception is explicitly retained above,
not reported as successful coverage. Negative execution waits are bounded by
12Updates/10seconds; no widened limit or instant gameplay fallback.

## What current assertions prove

Work case1025 retains prior physical work/candidate/haul/save/replay/private target
checks. New execution probes require observation-phase first fault, retained OWN
previous snapshot/entity versions/space with no deltas, no later time advancement,
no healthy save/hash, original exceptions and separate-session unchanged hash.
Direct priority edits are detected against the actual order, not a reconstructed
expected order. Other negative probes and pure malformed-argument refusals remain.

Graph246 covers physical discovery/earned work, metadata revocation/re-discovery,
saved twins and replay. Staffing102 covers native queue-bound automatic service,
retained teams, contention and saved departure. Native protocol92 validates authored
current identity and actual selected-role checkpoints/format refusal. Infrastructure95
adds actual dynamic line-bound graphs through physical supplied construction/cut,
saved unfinished work/flow cursor, topology stock conservation and native/direct
effects parity. No claim that a static graph case alone covers dynamic binding.

Core442 C# combined SHA256:
`8620234b8075f6de18233ad95880511ed828494f13f2173831d4198087d69d7a`.

## Limits

P4.acceptance.014 remains OPEN for its complete shared-evaluator/private projection
matrix. Unknown-reference discovery writes, undeclared global state, arbitrary CLR
isolation, all-modules concurrency and global allocation/soak remain outside this
scoped proof. Original bookkeeping221/198recorded checked/23tracked open unchanged.
No original checkbox changed. Earlier build evidence remains historical unless
rerun above. No full/profile/family tests, Oil/Tycoon/host/platform/Draw edits or
Git staging/commit/push. Full goal remains active.
