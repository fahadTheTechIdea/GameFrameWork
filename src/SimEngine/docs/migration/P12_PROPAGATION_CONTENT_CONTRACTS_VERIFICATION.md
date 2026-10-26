# P12-B2 propagation content contracts — scoped verification

Date: 2026-10-09. [Stage](../../plans/enhancements/P12_PROPAGATION_CONTENT_CONTRACTS_DESIGN.md).
Core `beep-simengine-2.384.0`, save154/replay1. Full goal and P1–P12 plan remain OPEN.

## Implementation boundary

Direct `IIncidentPropagationRule` and `IIncidentPropagationDefinition` implementations resolve through
the existing content catalog and actual immutable named aliases. The required rule base and generic
emitter base are deleted, not preserved as compatibility paths. Sole policy/binding declarations
own strict JSON parsing/reference resolution. The native emitter selects the actual metadata contract;
its stateless codec1 and the original exposure system's records, cursor, captured scan membership,
pending expected episodes, due-time eligibility, bounded budget and codec1 remain authoritative.
Original incident episode2, response DAG, WorkBoard, resources, motion and GameClock retain ownership.

All loaded rules and bindings, including unused definitions, validate before Initialize. Original
EntityObservation scopes guard rule callbacks, actual source/target entity codecs, clock and original
exposure codec. Transient callback reentry flags add no persistent state. Invalid callbacks stop the
session; no substitute provider, rollback, repair grant or healthy checkpoint is fabricated.
Selected engine extension content supplies point geometry. The test driver only authors JSON and
uses public commands, clock control, immutable views, saves and replay.

## Actual build and runtime attempts

Four `dotnet build Beep.SimEngine.slnx --no-restore` invocations completed with exit0, zero warnings,
zero errors:14.69s,6.37s,4.02s,4.28s. The first build's session18102 was observed terminal.

Only this ONE NEW exact case was invoked:

`Propagation contracts: direct JSON rules, delayed physical recovery and private saved exposure`

| Attempt | Passed | Failed | Aborted | Case ms | PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 4 | 1 | 1 | 363 | 41756 | 1 | 528 |
| 2 | 67 | 1 | 1 | 977 | 34664 | 1 | 1125 |
| 3 | 129 | 1 | 1 | 1139 | 2824 | 1 | 1273 |
| FINAL | 174 | 0 | 0 | 1163 | 8940 | 0 | 1299 |

First attempt: proof read exposure before the first normal Update created its record. Second:
proof authored a zero line in a positive stock bill; the correct explicit empty bill is `{}`.
Third: proof attempted a guarded live incident read after the session correctly faulted. Final
asserts that this read, save and hash are refused. These were thin proof repairs; no engine default,
relaxed bill parser, read-time state creation or fault bypass was introduced.

All four children were observed Exited. Limits:30,000ms execution,5,000ms owned termination,
30,000 output characters. No timeout, truncation or observer error. No older incident/propagation/
environment, family/full/Oil/Tycoon/platform runtime ran. Old proof and one old fault expectation
were migrated for compilation only; compilation is not runtime proof for those historical cases.

## What this case proves

Native and independent emitter definitions both reuse the original emitter runtime and independently
authored point rule. They earn identical interval/delay/episode/recovery timing. Receipt happens after
six authored game minutes, never during the initiating command. One actual machine travels4→20→22,
consumes each facility's eight supply units and earns response work; both stocks20→12. Distant and
foreign targets are refused by the actual radius/company policy. Source completion clears its pending
cause without duplicate episodes or a second response owner.

Physical detection/isolation is earned at the source by that machine, then its phase signal suppresses
due delivery even across held-worker cancellation/save/load. A separately recovered receiver has a
new episode identity; an older queued receipt cannot redamage it or spend its stock again. Explicit
any-company policy propagates while normal company views hide foreign pending identities. The
explicit observer alone exposes global scan cursors and foreign pending facts.

Pending and partially scanned exposure resumes with equal saved-twin hashes under budget1; pause
freezes the sole clock and hash. Detached/immutable facts, ordinary replay, resolved saves and
interleaved fresh-session ownership are covered. Eighteen malformed unused content variants reject
before Initialize. Seven rule callback faults cover invalid default facts, throw, pause, Touch,
source/target private codec mutation without Touch, and recursive composition. The faulted session
refuses live incident views, saves and hashes; the proof does not claim healthy post-fault state.

## Evidence limitations and inventory

Selected303 executable source/proof/test/JSON inputs were hashed AFTER final green at
2026-10-09 16:53:07 UTC and recorded in the completion audit. This is a post-run snapshot, not
a pre-run freeze or complete transitive source closure. No executable edits followed.
Metadata hash/link/table checks are not gameplay regression proof.

Not proven: native tile runtime breadth, arbitrary geometries/weather/terrain/network science,
all emitter/live metadata callbacks, unrelated private CLR mutation, global rollback, source removal
and every interruption combination, throughput, stress, lockstep, fuzz/soak or full acceptance.
Original221 gate text/checkbox states remain unchanged. G19's excluded Tycoon type-reference authority
is unanswered. No Tycoon/Oil/host/platform/clock changes or staging/commit/push before full verified finish.
