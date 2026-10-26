# P12-E4 — Current spill backlog and environmental objectives

Date2026-10-15. Engine beep-simengine-2.596.0/save157/replay1; environmental view contract2. Persistent payload layouts remain unchanged. No Oil/Tycoon/platform/Draw/host gameplay changes, full/profile/family run or Git publication.

## Contract and ownership changes

IEnvironmentalMetricDefinition now requires explicit EnvironmentalMeasure (Flow/Stock), with nullable RateUnit. Existing native and both external flow definitions migrate directly to Flow and retain their authored rate/window. EnvironmentalObservation exposes the measure; unavailable observations withhold it as well as amounts, units and timestamps. There is no old-signature facade or inferred measurement default.

The original observation factory and EnvironmentalMetrics service validate both semantics. Flow retains exact journal coverage and elapsed game-time rates. Current Stock has FromTick==ThroughTick==the current game-clock step, no rate, and typed environment.stock-has-no-rate. The existing zero-amount/count invariant applies; for stock Facts counts nonzero contributing cells, not historical transfers. A zero backlog is real available zero, not unknown stock.

SpatialResourceEnvironmentalMetricDeclaration authors only an explicit native unit and nonempty distinct cell IDs. SpatialResourceEnvironmentalMetricContracts is the single actual-contract binding and observation algorithm. It checks frozen owned aliases, exact unit identity, selected actual cells, stock metadata, native amounts, content visibility and original endpoint company/public access. It reads real material from original endpoints/layers and sums raw quantities with BigInteger. It creates no inventory, pollution ledger, timer or cumulative cache. The existing environmental service also observes actual spatial state during provider callbacks.

Native SpatialResourceEnvironmentalMetricDef and independently registered IndependentSpatialEnvironmentalMetricDef both inherit directly from Def and implement the public spatial/environmental contracts. Both delegate to the same declaration and runtime algorithm. The extension module/document is master-listed, selected normally and owns no game state or custom metric evaluator. Native and independent quantity providers execute unchanged beneath these definitions. Broader arbitrary-provider fault/private closure is not inferred from this proof.

## Authored gameplay proof

independent-tile-spill-environment.json authors the actual-cell backlog, original Produced treatment-flow metric and three objective predicates/definitions: spill detected at3 -> backlog recovered to0 -> actual treated production at3. The existing objective system owns enrollment execution, prerequisites, progress, completion and saves. Fixture scenario JSON enrolls the definitions; tests only issue normal player commands, advance the existing Update lifecycle and observe facts. independent-spill-observation.json authors the independent definition over the same cell. Neither host nor test calculates objective progress.

Both tile-provider cases reuse the previous finite five-unit physical scenario. Initial zero stock cannot complete recovery before detection. Delayed real deposition creates3 units and completes detection; terrain response cleanup leaves backlog3. Six-minute machine recovery returns actual material and permits the recovery objective, but cannot complete treatment. Physical loading/travel/unloading and five-minute processing create one real3-unit Produced fact under cause sensor.treat-oil; the existing flow metric and objective system complete treatment on their normal update schedule. Current backlog is0 while treatment total is3, with explicit Stock versus Flow semantics. Native and direct external metrics agree at all sampled stages. Paused/disabled work, saved partial phases, exact material conservation, journal provenance, isolated sessions and command replay remain verified by the extended scenario.

The admission/privacy literal rejects absent/empty/duplicate cells, mismatched unit, and inappropriate rate/window stock authoring. Own initial zero is available with no rate; foreign/private reads withhold material, measure, unit and timestamps without mutating the engine. Saved zero-state objectives retain prerequisite locks. This is the authored one-company/private-cell case, not arbitrary mixed-owner aggregation or full host spatial privacy.

## Focused regression fixture repair

The existing flow contract case exposed stale setup unrelated to metric runtime. Its resource-only incident document loaded a Tycoon-only cost requirement despite not selecting Tycoon. Only this test profile removes that monetary annotation and its group entry, retaining physical materials/tools. Shared incident content and Tycoon rules are unchanged.

Its standalone mapless profile also referenced an unlisted colony game mode. An attempted inclusion of full colony modes revealed those modes reference a registered Tycoon capability absent from this deliberately minimal GameDefinition. The final profile instead explicitly master-lists environment-only-game-mode.json, whose resource-only mode has no properties or financial capability. No runtime fallback or Tycoon enrollment was introduced. The original no-map/no-worker/no-operation metric proof now passes using that authored mode.

## All observed attempts

1. Initial targeted compile clean12.42s, zero warnings/errors; session68818 terminal exit0.
2. Initial stock admission6 passed/1 failed,872ms; PID23008 exit1/1169ms. New manifest incorrectly declared an extra dependency beyond its module's exact Requires.
3. Initial existing flow contract0/1,161ms; PID25436 exit1/453ms. Resource-only fixture selected a Tycoon-only work-cost section.
4. Corrected manifest and resource-only fixture; targeted compile clean8.52s, zero warnings/errors.
5. Stock admission46/0,754ms; PID30388 exit0/932ms.
6. Existing flow contract79/1,1009ms; PID31444 exit1/1176ms. Standalone mapless fixture omitted its named game mode.
7. Initial independent physical objectives360/0,8730ms; PID2388 exit0/8897ms.
8. Initial native physical objectives360/0,22334ms; PID27708 exit0/22523ms, session40783 terminal.
9. Refined shared runtime contract over actual interface metadata and added public independent-definition proof; included attempted mapless mode declarations. Targeted compile clean11.73s, zero warnings/errors; session59994 terminal.
10. FINAL stock admission51/0,843ms; PID14912 exit0/1039ms.
11. Existing flow contract79/1,1037ms; PID30304 exit1/1184ms. Full colony-mode properties named unregistered Tycoon capability in minimal mapless definition.
12. Authored the minimal standalone resource-only mode. FINAL independent physical objectives365/0,7838ms; PID31512 exit0/8295ms.
13. FINAL native physical objectives365/0,19070ms; PID32448 exit0/19224ms, session20027 terminal.
14. Final test-content/fixture compile clean7.68s, zero warnings/errors. No production edits after step9.
15. FINAL existing flow contract84/0,897ms; PID31692 exit0/1050ms.

Four distinct final literals total865 successful checks, zero final failures/aborts. Earlier failing attempts remain recorded. The native/independent/stock finals precede only the last mapless test-fixture rebuild, not a later production change. All process handles reached terminal status; no timeout caused a duplicate/restart. No full/profile/family suite ran.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill environment: authored current stock privacy and malformed inputs" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill environment: independent physical backlog recovery and treatment objectives" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Spill environment: native physical backlog recovery and treatment objectives" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Environment accounting: authored metrics and durable journal admission" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7500
```

## Remaining scope

Current backlog is not cumulative spilled volume. Cumulative scoped spill totals, continuous/multi-cell leaks, mixed materials/qualities/units and mixed-owner aggregation policy, disposal/destruction, environmental science/transport/weather, hostile metric/cell/provider mutation/reentry and global lockstep/fuzz/soak/allocation remain open. The standard environmental module still selects its existing resource journal dependency; independent journal-free metric composition was not redesigned here. Existing broad response/objective/flow families compile only except the one named flow gate. Frozen consolidated audit counts/hashes remain historical, no original broad checkbox is closed, and the entire plan/goal plus final commit/push remain incomplete.
