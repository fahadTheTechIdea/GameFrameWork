# P10-CU1 provider-neutral corridors and utility blockers — verification

## Outcome and original gate

**Original P10.acceptance.003 is now verified:** provider-neutral corridor breadth and generic utility blockers are proven through the existing supported contracts. Broader arbitrary-provider/control/stress acceptance remains open; no generic electrical/thermal/pressure physics completion is claimed.

Engine **2.623.0 / save159 / replay1**. A real unguarded command metadata read was repaired in original LayLineHandler; no public signature or state layout changed. Six exact final literals: **222+182+198+166+708+168 =1,644 passed, zero failures**. No full/profile/family selection.

Core442 C# path/content SHA256: `dce1df4d11b362ef4299f1d232b986f03296ffe73e32e7e9e2ca0f6bfccd2687`.

Closing this one original gate changes current source bookkeeping from221/201recorded checked/20open to221/202recorded checked/19open. The original ID/text mapping is refreshed against phase sources, not historical audit status. Other historical checked requirements are not reverified by this result; full goal remains open.

## Requirement evidence

| Obligation | Current source and executable evidence |
|---|---|
| Independent authored corridor definitions, not a required native concrete part | IndependentLineDef derives from Def and implements ILineDefinition; current line-definition literal executes the same physical DAG/material/template/plan behavior as native definitions and rejects malformed unused content before Initialize |
| Provider-owned path data and stable identity | Portable path literal verifies canonical tile/non-tile JSON identity, fractional point positions without tile substitution, ordered intent, read-only detached data, authored independent cursors, partial saved work and replay |
| One engine-owned physical executor across geometry | LineConstructionService binds ILineGeometry/ILineRemovalGeometry once; execution literal verifies native and point work/travel/material costs, earned prefixes, pausing/cancellation, private facts, save/replay and original owner aliasing |
| Physical removal and stock-safe interruption | Removal literal verifies actual native/point earned cuts and current geometry/stock refusal semantics, not client geometry edits |
| Generic utility demand/blocker contract | UtilityDemandSnapshot owns inactive/explicit-port-blocker/shortage precedence; UtilityWorkRequirementDeclaration queries named IUtilityConsumer through UtilityContracts without concrete Transformer/geometry switches |
| Real utility consequences for work/research | Utility owner literal covers native/independent capabilities and both time quanta: staffed progress, engine shutdown, finite shortage, no speculative progress/burn, physical haul/refuel and completion through saved twins/replay |
| Network supply and interruption in non-tile space | Point utility literal physically constructs the connection, consumes original finite stock, cuts it with real work, exhausts remaining supply, exposes the same shortage to processing and work, rebuilds through actual materials/work and resumes; actual journal totals conserve stock |
| Private detached facts and fault cutoff | Selected utility literals verify sibling/owner/port/clock and selected-space mutations in view/update callbacks; no healthy output/save/time continuation, original first fault, retained facts and independent healthy sessions |

## Production defect and single-owner repair

LayLineHandler previously accessed `definition.Layer.Space.Id` and `definition.CreateIntent(...)` before entering the plan evaluator's observation scope. A throwing metadata getter escaped without latching a world fault; a getter could pause the sole clock and then the later evaluator would capture already-mutated pacing. The original line-definition literal reproduced159 passed /5 failed after its stale save assertion was corrected.

The expanded protocol proof tests layer and CreateIntent callbacks, each throwing or pausing, through both ordinary seat Preview and the original registered handler Apply. Its complete pre-repair result was186 passed /36 failed. Positive physical cases still use ordinary player commands; direct handler Apply is explicitly negative command-protocol instrumentation, not host gameplay.

One private `LayLineHandler.Intent` helper now wraps both layer access and intent construction in existing LineDefinitionContracts.Read. Check and Apply share that helper. Actual PreviewPlan/SubmitPlan remain with the original board and mutation stays outside the pure observation boundary. Existing EntityObservation owns first-fault latching; no second validator, fault store, plan compiler or compatibility wrapper was introduced.

All eight negative callback/entry combinations now refuse with original observation.provider fault, retain original throw/scalar diagnostics, refuse healthy save/hash and stop time continuation. An independent healthy session remains unaffected. The production behavior identity advances from2.622 to2.623, with unchanged save/replay payloads.

## Proof maintenance and fixture issue

Six selected proofs still hard-coded historical save154. They now read existing StateCodecTests.ExpectedSaveFormat (159); no codec or schema was changed to satisfy a stale assertion.

An intermediate expanded negative test attempted to load a factory after deliberately attaching a world-bound throwing callback to its otherwise immutable content definition. Recomposition read that intentionally hostile getter before the negative command was exercised. The test now compares against a separately composed healthy control factory, as the other hostile-content proofs do. This is not a claim that arbitrary CLR mutations on shared immutable content can be sandboxed or saved; normal positive save/load/replay remains covered independently.

Existing finite per-stage update bounds and diagnostic snapshots remain. No wall-clock gameplay behavior or engine size limit was added.

## Every build and runtime attempt

All outputs below are untruncated; no aborted suite or live process remains. Exact selections use60s timeout/5s termination. Independent selected processes ran in parallel; no family/profile/full registry was executed.

| Attempt/selection | Result | Process evidence |
|---|---|---|
| Initial point utility baseline |167/1, only stale save154 | child34376 exit1;5890ms suite/6030ms process |
| Selected protocol expectations build |clean,0 warnings/errors,4.99s | exit0 |
| Initial line-definition defect |159/5 | child32116 exit1;2725ms suite/2883ms process |
| Pre-repair portable path |182/0 | child27144 exit0;6132ms suite/6299ms process |
| Pre-repair corridor execution |198/0 | child9140 exit0;6184ms suite/6354ms process |
| Pre-repair corridor removal |166/0 | child7492 exit0;4608ms suite/4759ms process |
| Pre-repair utility owner |708/0 | child30612 exit0;9791ms suite/9944ms process; session71962 polled terminal |
| Pre-repair point utility |168/0 | child33544 exit0;7267ms suite/7428ms process |
| Expanded metadata proof build |clean,0 warnings/errors,5.20s | exit0 |
| Hostile-factory control-session mistake |158/1 before actual command probe | child13216 exit1;2475ms suite/2609ms process |
| Independent healthy control build |clean,0 warnings/errors,4.79s | exit0 |
| Expanded complete defect reproduction |186/36 | child34088 exit1;2659ms suite/2786ms process |
| Original command-owner repair build |clean,0 warnings/errors,6.85s | exit0 |
| Final line definitions |222/0 | child10696 exit0;2549ms suite/2681ms process |
| Final portable paths |182/0 | child5220 exit0;5948ms suite/6105ms process |
| Final corridor execution |198/0 | child25516 exit0;6167ms suite/6329ms process |
| Final corridor removal |166/0 | child34720 exit0;4806ms suite/4960ms process |
| Final utility owner |708/0 | child5352 exit0;9836ms suite/9994ms process; session83117 polled terminal |
| Final point utility |168/0 | child8712 exit0;7092ms suite/7248ms process |

```
Line definitions: independent master JSON drives physical DAG corridors and plans
Portable corridor paths: provider JSON identity and durable physical cursor
Spatial corridor execution: one physical owner across tile and point geometry
Spatial corridor removal: shared earned execution with selected geometry and native conservation
Utility owner observation: timed supply and sibling getter faults stay engine owned
Spatial utility consumption: owned physical point supply blockers and selected-space observation
```

Affected production/protocol/proof `git diff --check` passes. Other callers compiled but were not broadly executed.

## Review and limits

LineConstructionService remains sole corridor work/marker owner; geometry is selected by registered contracts. UtilityConsumer remains a named observation/binding contract over actual stores and existing execution consumption, not a utility-only inventory or loop. The fix extends the original command owner and observation path rather than preserving an unsafe bypass.

This closes provider-neutral physical corridor and generic blocker integration for the supported contract surface; it does not exhaust arbitrary providers, all metadata callbacks, global resource/utility priorities, pressure/thermal/electrical physics, all control providers or stress/performance. Those original requirements stay open.

No business/Tycoon, Oil, host/demo gameplay, Godot/Unity/Draw, full/profile/family run, staging, commit or push. The lifecycle skill guided stage/evidence/requirement reconciliation under the user's focused-only rule.
