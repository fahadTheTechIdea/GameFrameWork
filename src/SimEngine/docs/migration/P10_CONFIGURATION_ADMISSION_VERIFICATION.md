# P10-CA1 — Complete configuration admission verification

## Result

Engine **2.630.0 / save159 / replay1**. Original AutomationSystem now observes the
entire configuration validation and initial latch/state capture before publishing a
new/rebound Record. It reuses EntityObservation.ReadParticipants and the immutable
settings' original participant resolver; no new validator, scheduler, rule-state,
clock, queue, content catalog or persistent codec was introduced.

Original P10.acceptance.022/full goal remains **open**. Original requirement mapping
stays221/208 recorded checked/13 open; historical checks were not globally rerun.

## Demonstrated defect and production repair

Before repair, nested action observations started after early disclosure/shape
metadata reads. A custom rule getter could silently change a bound entity's private
payload without Touch, and configuration could still succeed. InitialLatch and
InitialState were also read outside provider observation during new/rebound Record
creation. Throwing getters bypassed provider fault ownership; silent getters could
publish an invalid rule and allow the next queued command to execute.

CheckConfigure now rejects unseated actors, null settings and missing profiles by
their original typed codes before requiring an actual provider. For a real rule,
its complete original validation body, including refusal branches, runs inside the
same ReadParticipants mechanism. Existing nested action checks remain in place.
The initial Record is prepared through that mechanism before dictionary publication;
same-binding edits still retain original applied/latch/state history.

The expanded refusal proof caught an intermediate repair regression: observing a
null provider threw instead of preserving the typed null/unknown refusal. Explicit
early conditions fix it; no dummy provider or relaxed observer contract was added.

The same proof then exposed the existing null-settings codec path: it wrote a partial
settings header and dereferenced current.Inputs. Required settings have no valid
absent wire representation. AutomationSettings.Sync now throws ArgumentNullException
before any write for null write input. Preview still returns InvalidSettings; a raw
Submit bypassing preview is rejected before command-copy bytes, sequence allocation
or enqueue. CommandInbox already copies before allocating; its owner was unchanged.
No default settings, wire sentinel, compatibility encoding or schema change was added.

Engine build and independent expected protocol JSON advance2.629.0→2.630.0 for changed
simulation behavior. Save159/replay1/automation codec5 and all view/content schemas
remain unchanged. Valid command/save bytes remain the existing format.

## Contract-based proof

The existing RuleProviderObservationTests gained one exact configuration case.
BandControlRuleDef, an existing independent/non-friend definition, gained two explicit
transient initial-state/latch test hooks, reusing its existing disclosure hook. No
authored gameplay or persisted state lives in those negative hooks.

252 negative contexts cover fresh/loaded sessions, empty/existing records, sensor/
target/shared bindings, public preview/direct contract/ordinary inbox entry points,
disclosure/initial-state/initial-latch reads and silent payload/payload+throw/clock+throw
failures. Preview deliberately does not seed a rule, so initial-state/latch preview
combinations are excluded rather than pretending the callback occurred.

Each actual negative mutation happens once through its original getter. Tests prove
private persisted payload change without Touch, provider fault/first cause, direct
exception identity, unchanged complete original rule codec, no completed time/healthy
publication/success event, no accepted or later queued command, refused healthy
save/hash, and an independently saved session actually continuing after hook removal.
The probe may be read more than once; it performs only one hostile mutation. This
does not claim immediate cutoff between every property read or rollback of hostile
payload/clock changes already performed.

12 healthy contexts prove pure previews, new/rebound configuration, saved twin
deferred actions and same-binding edits retaining history. Added refusal checks
prove null/bad ID/unknown profile/missing input/foreign target/foreign source handling,
unseated-before-null precedence, normal target conflict and strict absent-payload
rejection without queue/hash prefix. These are original engine APIs, not test-owned
validation logic.

## Every build and execution

Five targeted builds of tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj with
`--no-restore -v minimal`:6.04s,6.81s,4.23s,6.53s,6.20s; all exit0, zero warnings/errors.
No solution/profile build or full test run was substituted.

All runtime commands select one `--bounded-exact` literal, timeout60000ms,
termination-timeout5000ms, output6500 characters. All child handles terminal,
no observer errors/timeouts/restarts. Only the original failing baseline stdout is
truncated by that declared output budget; its result/exit and diagnostic tail remain.

| Attempt/case | Pass/fail | Suite/process ms | PID/exit |
|---|---:|---:|---|
| Original configuration baseline |2401/456 |1597/1733 |25564/1 |
| First interval repair: configuration |3121/0 |1683/1831 |20376/0 |
| Same build: dwell |270/0 |418/568 |17856/0 |
| Same build: rule providers |348/0 |502/654 |30800/0 |
| Same build: normal actions |13/0 |242/389 |25368/0 |
| Added typed refusals: null provider regression |3121/1 |1643/1783 |26908/1 |
| Early guards: existing null write bug exposed |3123/1 |1707/1858 |23296/1 |
| Early-guard build: dwell |270/0 |383/535 |35420/0 |
| Early-guard build: rule providers |348/0 |477/625 |34436/0 |
| Early-guard build: normal actions |13/0 |239/383 |27020/0 |
| Final configuration/refusals |3149/0 |1588/1736 |34024/0 |
| Final dwell |270/0 |390/537 |34436/0 |
| Final rule providers |348/0 |477/626 |31784/0 |
| Final normal actions |13/0 |245/394 |17216/0 |

Final selected total **3,780 passed / 0 failed**. Original negative expectations
were not weakened. Strict malformed-input submission is distinguished from a
well-formed command's typed execution refusal; no null wire fallback was fabricated.
The expanded source-only tests caused one additional targeted compile. Existing
three controls were rerun after actual production fixes, never broad family filters.

Exact literals:

```text
Rule configuration admission: disclosure and initial metadata preserve bound state and existing records
Rule dwell admission: bound metadata faults cannot publish candidate state or deferred intent
Rule providers: original evaluation participants and composition errors remain inside one observation owner
Control actions: independent normal commands deferred timing and saved continuation
```

## Identities and remaining scope

442 core C# sorted path/content SHA256:
`b44f0d275d9e93b2d20600a130e61541d3c75c5b7a785e4b00813155bc3e9e23`.
Debug framework:
`5217608E52D01772EE2EEC4E6079086312DBD7A09F8289244A04625E65F468E4`.
Framework test assembly:
`5A3EB894CF5B886786C159BC7A5625AD8B32060D65E653E956289D6F5C813EA2`.
RuleProviderObservationTests:
`C746A2795D212E18D6733851DA8D9F7B7C329A06A3C81A9F72AE8FACB87D4E9F`.
Independent AutomationProof:
`5D9C83DA2B5D9A86EE9E1DB05989B8DD7FA1E6AEE48D4B53F45D3BAA99D3A30E`.

No broad automation/Contracts bundle, full/family/profile, Oil/Tycoon/platform/Draw,
adapter or publication work. Persistence/replacement metadata entry points, arbitrary
reentrant handlers, unrelated/unbound provider effects, content-payload mutation,
replay/stress/performance and original full P10/P6 acceptance remain open. Prior Oil
evidence is historical, not a2.630 rebuild. No original acceptance checkbox changed.
