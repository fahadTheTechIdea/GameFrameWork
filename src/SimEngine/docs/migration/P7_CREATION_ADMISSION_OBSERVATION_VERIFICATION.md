# P7-D3 creation placement admission verification

## Outcome

Engine **2.616.0 / save159 / replay1**. Public signatures and original state codecs unchanged; build identity advances for stricter admission/fault semantics. No compatibility reader, financial rule, host application, platform or Draw change.

Three literal finals: **1072 + 76 + 88 = 1236 passed, zero failures**. Original P7.017 remains open. Bookkeeping remains221 requirements /200 recorded checked /21 tracked open, not a fresh audit of every recorded completion.

442 core C# path/content SHA256: `cf9ed077028e0292756b29266df0544cb01f8db7448dd1b309365337b4c6ead5`. Previous2.615 evidence is historical unless explicitly rerun here.

## Production repair

`World.CheckCreation` previously called `ISpatialPlacement.CheckPlacement` directly and compared only source Version/Owner/identity and next allocated ID. The new proof showed actual hidden auxiliary payload, clock pause, pacing, map state and random changes could escape as healthy admission/refusal. Direct callback throws did not latch an observation fault.

The version-only block was deleted. The sole placement invocation now uses existing `EntityObservation.ReadProvider` with the actual selected provider, actual optional source, original whole-source Sync and selected Space. Existing observation captures clock/age/pacing/random state and invalidation/allocator/source facts, plus the selected space's original explicit codec/revision. Both successful and typed refusing callback results are withheld until scopes verify; unexpected callback throws preserve the original exception and latch first fault.

All earlier structural refusals stay with `CheckCreation`: forged/cross-session/stale intent, invalid prototype alias/owner/source/placement and exhausted identity. Healthy placement refusals remain ordinary codes. `PrepareCreation` still initializes its out handle to null and publishes only after successful admission. `CommitCreation` and actual allocation/registration/initialization remain with the existing Spawn/EntityStore lifecycle. No second validator, state model, allocator, transaction coordinator or observer was added.

This phase protects the pure placement callback, not all possible initialization/commit effects or arbitrary undeclared CLR state.

## New direct-admission proof

Literal: `Creation admission: original source clock and space observation before ready or refused results`.

64 actual contexts: direct Prepare/Check × new/restored × normally ready/blocked ×8 modes. The existing point fixture authors one structure as an occupied placement, without a fake candidate/live result. Check intent is originally prepared while valid; moving that existing blocker is explicit provider-test setup, not player gameplay or a new ID allocation.

Negative callbacks use the existing PointSpace `GeometryObservationFault` hook and existing `SingleResourcePort.FaultServicePayload` method; no new runtime fields or codecs were added. Modes: hidden source payload, pause, speed, space access, random draw, random draw with state restored (invalidation still changes), payload+throw and plain throw.

Each context verifies original structural ready/blocked behavior before arming; pure preparation hash; one callback; actual terminal fault; no leaked prepared handle; unchanged next ID; no new result; no queued success events; retained last healthy snapshot; save/hash refusal; and no later provider re-entry. Payload probes verify the real hidden field changed while Entity.Version stayed unchanged—there is no fake rollback. Throw cases retain `proof.creation-original-error`. A separate restored healthy session actually advances after each failure.

The pre-repair run completed the whole matrix with **692 passed /252 failed**. After replacing only the incomplete production boundary, the same literal passed1072/0. The check count increases because its two postfault cutoff assertions run only when an actual fault exists.

## Existing result-conversion evidence

Prepared contracts76/0 cover pure prospective identity, cross-session/forged/stale requests, invalid aliases/ownership, uncounted initial stock refusal, entity-only/mixed resource results, saved pending commands, original allocator and private facts.

Timed execution88/0 covers actual WIP/tools, output-capacity wait, saved identical continuation, real result registration, repeat with fresh execution, cancellation without extra result, sequential entity-only batches, independent producer, native resource failure before creation, initialization failures without advertised healthy completion, native point/tile rollout and saved placement waits.

Test repairs preserve strict production behavior:

- Added tools explicitly bind `supply` to existing `tool.input`. Entity-only recipes explicitly clear input/output bills **and** routes. Point recipe explicitly declares inventory ports and every route/empty tool-consumption/operating map.
- Terminal fault assertions require live-history/save/hash refusal and no queued success prefix; only detached prior facts remain usable.
- Independent negative operations use separate worlds, so a faulted world is not silently continued.
- The old `MutatePlacementSource` hook touched `Entities.All[0]`, which is not guaranteed to be the declared source. The test now uses the existing geometry hook to Touch the actual source. Production observation was not widened to arbitrary unrelated entities to satisfy incorrect instrumentation.

These existing tests also perform direct receipt, inventory, world and provider setup. They are protocol/provider verification, not proof that the entire public-player acceptance scenario is complete.

## Every observed attempt

Build command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

Runtime: one `--bounded-exact` literal per child, timeout60000 /termination5000. Output4500, except new-proof baseline6000. No full/profile/family run.

| Attempt | Result | Process evidence / scope |
|---|---|---|
| Original prepared baseline |76/0,223ms|pid9140,exit0,outer362ms,untruncated.|
| Original timed baseline |0/1,81ms|pid7700,exit1,outer216ms,untruncated; missing explicit tool port before play.|
| Test authoring/proof build |Clean4.68s|zero warnings/errors,exit0. Production guard still old2.615.0.|
| New proof reproduction |692/252,423ms|pid33544,exit1,outer560ms;stdout truncated at6000,stderr untruncated. Final counts and full matrix completion returned; no claim to retain every failure line.|
| Production guard/build identity build |Clean6.97s|zero warnings/errors,exit0;2.616.0/save159.|
| New admission final |**1072/0**,429ms|pid5344,exit0,outer573ms,untruncated.|
| Prepared contracts final |**76/0**,220ms|pid31056,exit0,outer363ms,untruncated.|
| Timed intermediate |87/1,703ms|pid3428,exit1,outer847ms,untruncated; old probe touches first entity instead of actual source.|
| Actual-source test repair build |Clean4.19s|zero warnings/errors,exit0. No further production repair.|
| Timed final |**88/0**,657ms|pid34036,exit0,outer791ms,untruncated.|

No executable changes after the final timed result. New admission and prepared-contract bodies, their factory/helpers and production sources were unchanged after their finals; the last edit only corrected the separate Spatial negative test callback. All processes terminal; no yielded handle was restarted.

## Remaining work

Original P7.017 remains open for broader creation initialization/commit observation, coupled conversion callback/auxiliary-state breadth, remaining disposition consumers, full topology accounting and stress/fault gates. Other P1–P12 requirements remain individually tracked. No global purity, renderer/platform support, performance or full-regression claim. No staging/commit/push before full goal and plan completion.
