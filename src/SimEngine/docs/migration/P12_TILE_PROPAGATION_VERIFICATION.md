# P12-B5 — Layered tile incident eligibility: scoped verification

Verified 2026-10-10. Core `beep-simengine-2.404.0`, save154/replay1/incident-propagation-view2. Exposure system1 and episode/work/map/resource state codecs are unchanged.

## Engine implementation

TileIncidentPropagationDeclaration is the sole native/direct tile-rule authoring and eligibility algorithm. Existing propagation policy supplies timing, scope and signal gates. Explicit source_tiles/target_tiles objects select all/any footprint coverage through existing ITileConditionDefinition aliases and TileConditionReferences. The new TileFootprintConditionDeclaration interprets the quantifier; all requires every cell to satisfy all predicates, any requires one cell to satisfy their whole conjunction. Explicit all+[] is unconditional; any+[] refuses. Existing nonempty construction/navigation references retain their semantics.

All reference resolution finishes before resolved-content validation checks footprint spaces/aliases. The original composition owner then requires the actual selected map, frozen conditions and exact timing. Missing/duplicate/unknown/foreign-space/invalid authoring refuses before Initialize, unused included. Original source/target presence, distance, signal, company and episode checks remain. New SourceTerrain/TargetTerrain blockers are engine-calculated.

Existing IIncidentPropagationState/IncidentPropagationView.ReadEligibility reports detached current source episode/rule/clock/nominal delay and typed per-target eligibility through that same Observe path. It never advances scans, posts responses or reserves resources. Ordinary queries require both entities to belong to the viewer; refused/private requests publish null facts. Faulted queries refuse even an otherwise missing target. Explicit observer access remains engine diagnostics, not a player-privacy bypass.

Observation scope verification now runs only after a successful callback. Exception cleanup no longer replaces the original callback error with secondary verification against a faulted session. Existing whole source/target/exposure codecs and tile-condition map/clock observations remain. No copied layer samples, exposure records, incident state, stock, scheduler or clock.

## Exact runtime

Only this ONE NEW exact case was run (three attempts of that same case):

```text
Tile incidents: authored footprint conditions reobserve physical changes and supplied response
```

| Attempt | Passed | Failed | Aborted | Suite ms | Child PID | Exit | Observed ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1 | 2 | 1 | 1 | 255 | 7092 | 1 | 413 |
| 2 | 50 | 1 | 1 | 1938 | 10688 | 1 | 2092 |
| FINAL 3 | 272 | 0 | 0 | 4654 | 36092 | 0 | 4803 |

All children authoritatively Exited, stdout/stderr untruncated, observer errors empty. Limits: outer30000ms, owned termination5000ms, output30000 characters. Physical stages bounded256 Updates with actual layer/exposure/eligibility/response diagnostics. Aborted values record suite exceptions, not observation timeouts.

Attempt1 found a proof Initialize order collision with the existing network-signal negative installer at304; the proof now uses its distinct12345 order. Attempt2 completed native physical work but exposed premature space validation while the independent predicate registration was still unresolved. Validation moved from Resolve into ValidateResolvedCore for native/direct registrations. No arbitrary registration-order dependence, fallback alias or weakened geometry/refusal was introduced.

## Build evidence

`dotnet build Beep.SimEngine.slnx --no-restore`:

| Build | Exit | Elapsed seconds | Warnings | Errors |
|---|---:|---:|---:|---:|
| 1 | 0 | 15.52 | 0 | 0 |
| 2 | 0 | 6.63 | 0 | 0 |
| 3 | 0 | 5.12 | 0 | 0 |
| FINAL 4 | 0 | 5.74 | 0 | 0 |

The historical incident-propagation.json gains explicit unconditional footprint selectors; its original cases compile only, not run. No old construction/network/incident/family/full/Oil/Tycoon/platform runtime.

## Narrow acceptance evidence

Four native/direct physical workflows prove identical chronology within each all/any mode. Master-listed JSON initializes the two-cell source and partial receiver mask. Ordinary commands raise the source, queue exactly one target with60-minute delay, physically travel/cut one actual source layer cell while retaining the other, and re-observe the changed footprint at delivery. All blocks the stale receipt; any remains eligible. All then requires actual hauled steel and earned reconstruction before a fresh scan earns a new full60-minute delay. Original source-plus-pool supply8 is preserved; reconstruction consumes exactly one depot steel where performed.

Source and target repair each needs one real hauled water unit and eight-minute machine work through the original response DAG. Depot water10 becomes8 with zero duplicated delivered/cargo stock. Unmasked and distant targets remain undamaged. Partial cut/pending and completed saves, saved twins, one clock pause, original episode guards, detached eligibility facts, company privacy, interleaved isolation and ordinary command replay pass narrowly.

Separate current eligibility checks prove all-target failure on a partly selected two-cell target, any's same-cell conjunction (different predicates on different cells do not combine), and explicit empty-all behavior. Twenty-two malformed loaded-content variants (unused native/direct rule clones and an invalid shared predicate-space binding) refuse before Initialize. Four armed predicate faults throw, pause, write a layer or touch the source; first fault latches and healthy facts/save/hash refuse.

Selected executable inventory:461 inputs, observed2026-10-09 22:20:43 UTC after FINAL green;14 changed/new compared with previous456. Exact hashes are attached to the completion audit; no executable edits after this pass.

## Remaining scope

This is not complete P4/P10/P12 or P1-P12 acceptance. Climate/weather generation and scientific propagation, private layers, non-tile geometry, intervening corridor sampling, arbitrary custom-provider/private purity/reentry, cross-company behavior, all signal/material/fuel/queue combinations, global/performance/fuzz/soak/lockstep and full regression remain open. Layer interpretation is authored classification, not an inferred weather/chemical model.

Protected resource-catalog/Tycoon authority remains pending. No protected clock/Tycoon file, Oil behavior, budget/default-money behavior, renderer/platform, staging, commit or push changes. Publication waits for the FULL goal AND plan to genuinely finish and be verified.
