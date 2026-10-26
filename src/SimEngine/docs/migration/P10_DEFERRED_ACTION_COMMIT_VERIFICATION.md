# P10-AC1 — Deferred action commit verification

## Outcome

Engine **2.628.0/save159/replay1** admits cooldown metadata before the normal
command and requires the original world to remain healthy before recording rule
success. Four focused current-core cases pass **502/0**. Broad P10/full-profile
requirements remain open; no full/family/profile or broad bundle ran.

[Design](../../plans/enhancements/P10_DEFERRED_ACTION_COMMIT_DESIGN.md).
442 core C# path/content SHA256:
`7b1efd6284bb8fe661c8da42bc8be67faad0ae66921b41fdf4150b6cdd096d7c`.
Save159/replay1/automation schema5 remain unchanged. No clock, state field,
transaction executor or rule queue was added.

## Reproduction and authoritative ordering

The new exact case on2.627 produced **106 passed/22 failed**. A normal command
handler could perform its first real target change, invoke a native operation
that latches a fault, catch its exception and return. Automation then incorrectly
cleared the pending intent and began recording applied/latch/cooldown success.
The resulting rule state could not even satisfy its original codec invariants.

The same reproduction showed cooldown metadata being read only after the actual
handler had touched the target and applied-state fields had begun changing. Silent
metadata payload mutations could complete the update as healthy; throwing metadata
ran after physical work rather than being admitted as pure prerequisites.

The original due loop now captures its existing pending decision, resolves the
checked cooldown deadline through the existing ReadParticipants using the actual
rule/bound entities/space, then invokes the normal handler. RequireHealthyUpdate
runs immediately after Apply, before applied/latch/cooldown/pending fields change.
All published record fields are then assigned from already-admitted values.
No provider callback remains between those success assignments.

The existing handler, fault record, clock and rule owner remain authoritative.
An already executed command leg is not rolled back: the proof reads the actual
changed first target after failure. Its later target and rule-success state do not
advance. Save/hash refuse the faulted continuation and snapshots retain the last
healthy facts. Original native first-fault phase/cause is preserved, including
when the handler caught its original exception.

## Proof scope

Twelve negative contexts cover fresh/restored pending state with caught native
creation failure, propagated native failure, direct command throw, and cooldown
silent-payload/throw/pacing-plus-throw. Two healthy contexts verify two simultaneous
due rules over distinct actual targets, both applied through the ordinary registry
and continued as saved twins.

Normal handler fault injection is explicit external engine-side protocol
instrumentation: the existing SetAutomationProbeHandler performs its real SetLevel,
then an armed hook invokes the actual World.Spawn lifecycle with a throwing local
callback. Native creation owns that fault and consumes no successful entity ID.
The hook does not implement gameplay or a second fault owner. Direct throwing and
propagating/catching the native error are tested separately. World-owned command
handlers keep the independent saved session's hooks isolated.

Cooldown fault instrumentation is the existing BandControlRuleDef's transient
metadata hook, not authored runtime policy. Whole participant observation catches
actual silent saved-state changes; original target version remains unchanged,
proving its normal control handler has not run. This does not claim the malicious
metadata mutation itself was rolled back.

## Every build and runtime attempt

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Two clean targeted builds, zero warnings/errors:6.30s reproduction and7.67s repair.
No failed compilation or live build handle. Dependencies compiled, not tested.

All exact commands used60s timeout/5s termination and4500-character output bound:

| Attempt | Result | Terminal evidence |
|---------|--------|-------------------|
| New commit proof on2.627 |106/22 |14668 exit1;320ms suite/458ms process |
| Final commit proof |130/0 |20384 exit0;349/510ms |
| Existing independent normal deferred actions |13/0 |21092 exit0;265/423ms |
| Existing evaluator/composition provider proof |348/0 |17100 exit0;525/680ms |
| Existing resource-only physical valve recovery |11/0 |11488 exit0;699/853ms |

All output was untruncated. The four finals were independent parallel exact
processes and terminated normally; no aborted, timed-out, restarted or live handle.
Literal registrations:

```text
Deferred action commits: metadata admission and swallowed native faults cannot publish applied rule state
Control actions: independent normal commands deferred timing and saved continuation
Rule providers: original evaluation participants and composition errors remain inside one observation owner
Input recovery: resource-only physical network wait withdrawal and saved valve restart
```

## Remaining scope

No broad gate closes:221 original/204 recorded checked/17 open, confirmed by exact
mapping of every original ID/literal with no missing matches. Original P10.acceptance.022/full goal remain open.
This does not prove arbitrary handler side effects outside declared participants,
all reentrant automation edits, full-width absolute deadline overflow, every
metadata access, complete replay/lockstep/interleaving/stress/allocation matrices,
or the unexecuted large automation process bundle. No CLR sandbox or rollback claim.

Development lifecycle guidance kept this existing-owner repair in one tracked
stage. No Oil/Tycoon/host/platform/Draw/adapter changes, compatibility/default
fallback, memory edit, Git staging/commit/push, full/profile/family run.

Final metadata: new stage/report links resolve; git diff --check exits zero with
35 existing line-ending warnings and zero staged paths. No executable input
changed after the four final focused runs.
