# P10-AP1 — Control-action participant observation verification

Current-source reconciliation,2026-10-22: the two specific findings recorded below were resolved
in the existing [P10-IR1 owner](P10_INPUT_RECOVERY_VERIFICATION.md), which separated actual execution
decisions from pure validation and selected the resource-only physical network fixture. The source
still contains those repairs and direct exact registrations. No repeated engine repair or runtime
was performed for this reconciliation. The old174/2 process-bundle result remains historical; its
unexecuted remainder and broad P10 gates are not claimed green on current2.726.

## Earlier AP1 implementation — 2.625.0

## Outcome and limits

Engine **2.625.0/save159/replay1** observes the original saved state of bound
control-action participants across pure binding, command construction, copying,
checking and fingerprinting. The old version-only tuple check is removed, not
retained as an alternate path. The two selected final action cases pass
**2509 assertions/zero failures**. A larger existing automation process case
encountered **174 passed/two failures** at this checkpoint; see the later IR1 resolution above.
This is not a green P10/full-regression claim.

[Design](../../plans/enhancements/P10_ACTION_PARTICIPANT_OBSERVATION_DESIGN.md).
442 core C# path/content SHA256:
`758530929f93105b9e2c9cec07b1dbb1d1026703cab3d8a524ef42755dd03d42`.
Save payload159, replay1 and control codec schema5 are unchanged. No Oil,
Tycoon, host, platform, Draw, content-default or module registration code changed.

## Reproduction and existing-owner repair

ControlActionDispatch.Pure previously captured only scalar facts and participant
identity/version/owner tuples. It did not read actual entity.Sync state. An action
policy or ordinary command callback could change a bound source/target's saved
level without Touch and still return a healthy preview. The helper's finally
verification could replace a clock-mutating callback's original exception.

The new exact case reproduced **2160 passed/216 failed** on the2.624 core.
Failures included successful silent participant mutation, repeated later binding
callbacks after that mutation, and masked original clock/callback exceptions.

The same Pure helper now uses EntityObservation.ReadProvider with the actual
callback owner and selected spatial provider. It captures existing entity scopes
for identity/owner/version plus actual whole-entity provider scopes over entity.Sync.
Actual bound entity IDs are deduplicated; a shared source/target is one participant.
Only successful callbacks proceed to verification. ReadProvider owns original
errors and first-fault latching. Callback throws are never followed by a finally
verification that can mask them. Existing command copying/fingerprinting, sender
stamps, typed refusals and action ordering are unchanged.

Missing references still use the original CheckReferences admission. Company
targets do not invent an entity; their bound input entities remain observed.
These scopes are transient use of existing observers, not another codec,
resource model, clock, queue, control executor or participant snapshot schema.

## Actual focused proof

The new participant case has 12 healthy ready/refused saved-continuation contexts
and180 negative contexts: fresh/restored × ready/refused command admission ×
target/input/shared participant × binding/create/copy/check/fingerprint phase ×
silent saved-payload mutation/payload-plus-original-throw/pacing-plus-original-throw.

Mutation uses the existing external AutomationProbe's real saved level field
through its existing negative signal instrumentation; version stays unchanged.
The proof observes actual codec/rule-intent state, actual sole clock, previous
published snapshot, callback count, exact original exception, first observation
fault, no event prefix, no healthy save/hash, and a separate restored session
that remains identical then actually advances. Faults do not pretend to roll back.

The existing ControlActionPolicyTests.Execution body now has one direct exact
registration instead of an embedded call inside AutomationTests.Processes. The
body is unchanged and no proof was deleted or duplicated. This isolates the
actual affected action behavior: authored dwell, deferred normal commands,
retained-command detachment, pause, saved twins, low/high transitions, cooldown,
normal command refusal and cancellation. The larger process registration remains
and its observed failures are not hidden by this separation.

## Every executed build and runtime attempt

Targeted build, never a test-suite invocation:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Three builds were clean, zero warnings/errors:4.78s (new reproduction),7.55s
(core repair),5.72s (direct action-proof registration). No failed compile or live
build session remains. Existing dependency projects compiled; their suites did not run.

All runtime commands used exact literal names with60s timeout/5s termination and
4500-character output bounds:

| Attempt | Result | Process evidence |
|---------|--------|------------------|
| New participant case on old core |2160/216 |12840 exit1;1168ms suite/1308ms process; bounded stdout truncated |
| Participant case after repair |2496/0 |28388 exit0;1146/1288ms; untruncated |
| Existing Automation process case |174/2 |33036 exit1;541/684ms; untruncated; unresolved findings below |
| Final participant exact case |2496/0 |31440 exit0;1278/1428ms; untruncated |
| Final independent action execution exact case |13/0 |5940 exit0;258/415ms; untruncated |

Final exact names:

```text
Control action participants: original codecs reject silent mutation and preserve callback failures across pure dispatch
Control actions: independent normal commands deferred timing and saved continuation
```

The two final processes ran independently in parallel and exited normally.
No family/profile/full/Oil/demo-horizon runtime was requested or run.

## Findings at the AP1 checkpoint — later resolved individually by IR1

`Automation: threshold edits, recovery, deferred timing and saved processes`
failed on the AP1 core. It was not run on the prior core here, so
this stage does not label either issue a proven pre-existing regression:

1. InputRecoveryTests.Execution expects ConditionalInputRecoveryPolicyDef.Calls
   to equal1 after an offline due input, but observes5. The counter includes pure
   Decide invocations; AutomationSystem.ValidateRecord also validates waiting
   records through ReadRecovery during codec observations. Next work must separate
   actual update decisions from pure validation callbacks and prove no duplicate
   retry/commit, not merely change the expected count to5.
2. PhysicalNetwork reaches NetworkSignalTests.Factory, whose resource-recovery
   base still declares a supplier without selecting sim.tycoon. Composition refuses
   before its physical network proof. Next work should reuse the existing shared
   physical fixture and explicit actual network/startup authoring; do not activate
   business globally or delete the scenario to obtain a pass.

That larger test stopped before all of its nested helpers, which are not claimed
as run. No unrelated fixture source or expected count was changed in this slice.

Original P10.acceptance.022 stays open, along with remaining policy/provider,
first-fault, replay, optional-module interleaving, stress/allocation and full-profile
requirements. Bound participant observation is not a CLR sandbox or proof about
arbitrary unrelated external state. No new original checkbox closes:221 original/
204 recorded checked/17 open, confirmed by exact ID/literal mapping of all221
original requirements without missing matches.

Development lifecycle guidance organized the same-owner repair, design and tracker.
No compatibility shim or fallback, full-suite run, memory edit, Git staging,
commit or push. The full goal remains active.

Final metadata: new stage/report links resolve; git diff --check exits zero with
35 existing line-ending warning lines and zero staged paths. No executable edit
followed the final two focused action runs.
