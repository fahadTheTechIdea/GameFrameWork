# P10-RP1 — Rule provider evaluation and composition verification

## Outcome

Engine **2.627.0/save159/replay1** observes actual bound entity codecs around rule
evaluation and result validation before writing rule state. Original dependency,
action-policy and rule-metadata composition intervals now preserve provider errors
instead of verifying in finally. Five selected current-core cases pass **3108/0**.
No full/family/profile or broad automation bundle was run.

[Design](../../plans/enhancements/P10_RULE_PROVIDER_OBSERVATION_DESIGN.md).
442 core C# path/content SHA256:
`9f92bdbb0e1b78a9bd68a858e0d254f2098dd84a3e753096ccc478fcb3778c50`.
Save159, replay1 and automation schema5 remain unchanged. No new persistent state,
clock, provider executor or queue.

## Reproduced defects

The new exact case on2.626 produced **286 passed/44 failed**. An evaluator could
change an actual bound source/target's saved level without Touch and complete the
update as healthy. Throwing evaluator callbacks were attributed only by the outer
system, and clock mutation plus throw lost the original cause to finally verification.
Direct original composition-validator calls likewise masked throwing metadata
callbacks. One dependency accessor was unnecessarily invoked twice in one interval.

The test uses actual external BandControlRuleDef/ProbeControlActionPolicyDef with
transient explicit hooks, not another evaluator or fabricated state. Eighteen live
evaluation contexts cover fresh/restored sessions, target/input/shared binding and
silent payload/payload-plus-throw/pacing-plus-throw. Eighteen composition contexts
cover fresh/restored dependency-node/action-validation/rule-duration intervals and
throw/pacing/pacing-plus-throw. Twelve healthy contexts preserve ordinary saved
rule continuation and pure composition behavior.

Negative evaluator evidence includes unchanged actual entity version despite changed
original payload, original fault/message, one callback cutoff, unfinished game step,
retained healthy snapshot facts, original rule-codec equality/no intent prefix,
no success events/save/hash and a separate saved rule session that actually advances.
Per-content negative hooks are disarmed before the independent session executes
the same definition; they are test instrumentation, not persistent/session gameplay.

Composition negatives call the same actual validator explicitly as a protocol test
after normal session creation so they can arm real per-content hooks. They are
not a claim that an invalid game was admitted before initialization. The positive
factory/independent-contract scenarios also exercise real startup composition.

## Existing owners, no duplicate observation or binding path

AutomationSettings.ResolveParticipants is internal binding resolution only: resolve
the actual entity target and all ordered input bindings through EntityStore. Action
dispatch, recovery and rule evaluation use it; none maintains its own participant
lookup/state list. Company controls do not invent an entity target.

EntityObservation.ReadParticipants remains the sole deduplication/original-codec
owner. Evaluation calls it with the actual IControlRule, resolved participants and
selected space. Both rule.Evaluate and ValidateEvaluation run inside the callback;
Record.State and candidate/pending transitions occur only after successful
observation. No finally verification can replace an original callback exception.

Composition's existing dependency-node/action/rule validation intervals now use
the original ReadProvider with selected space. The same strict metadata checks,
dependency compiler and content ownership remain; node construction reads recovery
and action references once into local variables. The old four finally-guard paths
in AutomationSystem are gone, not kept as compatibility alternatives.

## All executed builds and runtime attempts

Targeted compilation:

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Three clean builds, zero warnings/errors:6.32s reproduction,6.76s production repair,
4.55s canonical protocol assertion migration. Compiling dependencies did not run
their suites. No compile failure or live handle occurred.

All runtime commands used exact literal registrations with60s timeout,5s termination
and4500-character output limit:

| Attempt | Result | Terminal process evidence |
|---------|--------|---------------------------|
| New rule/composition baseline on2.626 |286/44 |16552 exit1;431ms suite/569ms process; bounded stdout truncated |
| Final rule/composition exact case |348/0 |24612 exit0;525/689ms |
| Recovery-provider adjacent case |164/0 |24860 exit0;373/536ms |
| Action-participant adjacent case |2496/0 |20544 exit0;1360/1525ms |
| Independent deferred normal-action case |13/0 |5604 exit0;286/449ms |
| Independent contract/physical response case, first attempt |86/1 |27452 exit1;834/996ms; sole failure hardcoded save154 vs actual159 |
| Same independent contract case after assertion migration |87/0 |34416 exit0;713/854ms |

The five selected post-repair processes ran independently in parallel. Only the
one failed contract case was rerun after its positive protocol assertion changed
to StateCodecTests.ExpectedSaveFormat, the existing JSON-backed canonical owner.
Its old label implying unchanged154 layout was corrected. No production/save
schema was changed to satisfy the stale assertion. All other proof bodies and
production inputs remained unchanged after their final runs. Output was untruncated
except the failing baseline explicitly noted above. All processes terminated.

Exact registrations:

```text
Rule providers: original evaluation participants and composition errors remain inside one observation owner
Input recovery providers: bound saved state and original failures remain inside native observation
Control action participants: original codecs reject silent mutation and preserve callback failures across pure dispatch
Control actions: independent normal commands deferred timing and saved continuation
Control content contracts: independent JSON policies drive deferred alerts, physical response and saved sensor recovery
```

The independent-contract case proves actual non-native IControlRule/action/recovery
aliases over JSON content, deferred alert control, physical incident response,
saved unavailable-input recovery and malformed declarations. It is evidence for
this unchanged contract surface, not all mods or every possible provider behavior.

## Scope remaining

This observes bound participants and selected providers/space, not arbitrary
unrelated external state or a CLR sandbox. Metadata accesses outside the named
intervals, normal-handler commit failure behavior, larger control-provider/fault/
replay/lockstep/interleaving/soak/allocation matrices remain unclosed. Broad
P10.acceptance.022 and full goal stay open. No original checkbox closes;221
original/204 recorded checked/17 open, confirmed by exact ID/literal mapping of
all221 original requirements with no missing match.

Lifecycle guidance kept the original-owner migration and evidence in one stage.
No Oil/Tycoon/host/game-platform/Draw/adapter logic, duplicate scheduler/state model,
compatibility shim, memory edit, Git staging/commit/push or full-suite run.

Final metadata: new stage/report local links resolve; git diff --check exits zero
with35 existing line-ending warnings and zero staged paths. No executable input
changed after its applicable final focused run.
