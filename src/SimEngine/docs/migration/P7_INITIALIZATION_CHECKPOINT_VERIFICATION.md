# P7-D11 complete initialization before checkpointing — verification

## Outcome

Engine **2.622.0 / save159 / replay1**. One transient World-owned capability-initialization phase now prevents premature world checkpoint traversal, including after nested child initialization returns. No persistent entity flag, alternate lifecycle, allocator, clock, business rule or state layout was added.

Three exact final selections: **3,288 initialization checkpoint +612 original allocation +88 timed entity conversion =3,988 passed, zero failures**. No full/profile/family runtime. Original P7.017 and full plan remain open. Existing221 original requirements /200 recorded checked /21 tracked open are bookkeeping, not a fresh global completion audit.

442 core C# path/content SHA256: `a883201d73250364ca2b4be5572c0e2f1b24a3e0198b45028f8db7564c82dba1`.

## Actual reproduced boundary defect

EntityStore.Create correctly ends its local `_creating` phase after identity registration. World.Spawn then runs capability initialization, during which valid children may be created. The old entity-store guard therefore cannot cover this later phase.

An initializer can explicitly call `World.Settle()` and then Save, Hash, HashBreakdown or direct World.Sync hashing before its later parent initializer executes. The old code accepted these premature checkpoints, including after a completed child initializer returned. The initial192-context proof reproduced1,664 passed /928 failed.

The probe records actual incomplete parent initialization before each attempt. A real authoritative Entity.Touch occurs afterwards on successful completion, so premature serialized/hash state omits a later version change. The proof does not create a second simulation loop or pretend its callback counters are persisted gameplay state.

## One existing lifecycle owner

World.Spawn now increments `_entityInitializationDepth` around its existing capability initializer loop and decrements in finally. Child creation remains legal and independently uses the original allocator/local callbacks. The child finally reduces nesting without releasing the still-active parent phase.

One private `RequireCompletedEntityInitialization` guard serves World.Sync, Hash and HashBreakdown. The hash entry points check before their implicit Settle. Sync checks before any codec or read/write traversal. Component-scoped observation codecs remain available; arbitrary initializer mutation and legitimate random initialization were not prohibited.

The guard is a pure refusal, not a new fault owner. An initializer that catches it may finish normally. If it propagates, the existing World.Spawn catch records `entity.initialize` and preserves the exact thrown exception; later initializers stop. The phase unwinds in finally without making a faulted partial world resumable. No rollback of registered parents or previously initialized children is fabricated.

Only engine behavior identity changed. Existing save159/replay1 layouts remain unchanged; the nesting phase is not serialized.

## Final240-context proof

The matrix spans fresh/restored sessions, direct/prepared parent creation, direct/prepared child creation, parent-before-child/child/parent-after-child stages, five checkpoint entry points and caught/propagated refusals. There are120 healthy handled refusals and120 propagated failure contexts.

The fifth entry point, added after the original reproduction, is direct loading into the same active World using a valid original saved-state body reader. The guard refuses before the reader Position advances. This is a protocol check against reentrant restore, not an alternative load implementation or destructive replay of saved state into an unfinished world.

Healthy handled cases prove:

- No save, whole hash, breakdown, direct hash or load completion escapes mid-initialization.
- Original sequential parent/child identities and exactly-once initializer execution remain valid.
- After the outer initializer completes, save/load, authoritative final parent version, hash/breakdown and twin continuation work normally.
- Independent saved sessions remain unchanged and advance.

Propagated cases prove exact original error identity, original initialization fault phase, no later parent initializer/success events, unavailable resumable Save/Hash, no clock advancement and first-fault retention. Existing behavior of fault-diagnostic HashBreakdown outside an active initialization phase was not redefined by this stage.

The existing allocation literal independently retains pre-registration reentrancy/checkpoint refusals and legitimate post-registration child creation. The timed entity-conversion literal exercises normal physical result completion, saved waits and provider fault boundaries through the real engine owner.

## Every build and runtime attempt

| Attempt | Result | Process evidence |
|---|---|---|
| Initial proof build |clean,0 warnings/errors,5.06s | exit0 |
|192-context pre-repair reproduction |1664/928 | child14480 exit1;1092ms suite/1235ms process; output capped/truncated |
| Production guard build |clean,0 warnings/errors,6.72s | exit0 |
|192-context post-repair proof |2592/0 | child3360 exit0;1079ms suite/1215ms process; output untruncated |
| Existing creation allocation final |612/0 | child33876 exit0;457ms suite/596ms process; output untruncated |
| Timed entity conversion final |88/0 | child17100 exit0;738ms suite/887ms process; output untruncated |
| Expanded valid-reader proof build |clean,0 warnings/errors,5.20s | exit0 |
| Final240-context initialization proof |3288/0 | child31932 exit0;1349ms suite/1488ms process; output untruncated |

Allocation/timed processes ran in parallel. The final expansion changes tests only; both were already tested against the same final production source. Every runtime call is literal `--bounded-exact` with60s timeout/5s termination. No aborted suite, restarted process or live handle remains. Affected production/protocol/proof `git diff --check` passes.

```
Creation initialization: nested capability phases refuse premature checkpoints and release only after completion
Creation allocation: local reentrancy and checkpoints refuse while initialized children retain distinct identities
Entity conversion: timed results, saved waits and provider fault boundaries
```

## Review and limits

World owns capability initialization and whole-world persistence admission; EntityStore still exclusively owns local identity creation/replacement. The transient depth represents nesting, not current game time, a second lifecycle scheduler, or a gameplay size limit. Existing JSON/master/prototype contracts are reused without compatibility aliases or fabricated content defaults.

CreationTailProbe receives only a nonserialized per-instance test callback, matching the existing CreationProbe instrumentation pattern. The external proof assembly continues using public contracts only. Tests are explicit fault/lifecycle protocol instrumentation, not public-player acceptance.

This does not prove all constructors/initializers preserve arbitrary external state, initializer purity, every nested lifecycle action, all replacement/provider boundaries or broad stress/performance. Those remaining original requirements are still open. No business/Tycoon, host/demo gameplay, Godot/Unity/Draw, full/profile/family runtime, staging, commit or push. The lifecycle skill guided stage/tracker organization under the user's focused-only validation rule.
