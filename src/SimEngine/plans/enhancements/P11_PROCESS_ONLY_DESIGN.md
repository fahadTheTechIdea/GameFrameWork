# P11-F1 — Process-only game composition

## 0. Problem statement

Original P11.acceptance.036 requires a process-only fixture without progression, controller, economy or knowledge modules. The shared PointGame.Factory registers CompanyKnowledgeModule, so it cannot provide evidence of that complete absence. TransformationModule already requires only access/inventory/operations; WorkModule requires access/inventory/motion. Do not add a replacement runtime to prove optionality.

## 1. Goals

1. A strictly master-listed resource-only scenario registers and loads only physical process dependencies and one existing contract spatial provider.
2. Ordinary player commands haul actual inputs, retain reusable tools, manufacture resource outputs over time and execute authored machine work without money/research/objectives/AI/knowledge.
3. Queued, supplying, productive, paused, cancelled and completed saves, replay and interleaved sessions preserve the original owners' state.

## 2. Design

Use existing GameDefinition/ContentSet.Load with an explicit module array and process-only-master.json; no shared PointGame.Factory that injects knowledge. Reuse PointModule only as the existing external geometry/motion contract provider. Author process-only-world.json, process-only-work.json and process-only-recipes.json, with the original WorldSetup profile, inventory ports, Transformer, WorkBoard, LogisticsService and one clock. No new host, gameplay provider, content loader, scheduler, balance, clock or state store.

No positive cost is authored: work/manufacturing genuinely do not need a business property or account. Missing optional data is not fabricated as a money balance. Existing access/retry documents remain explicitly master-listed inputs, not loader defaults or scans. Recipe batch inputs3, reusable tools1 and outputs2 are explicit count-based data.

## 3. API surface

Existing game initialization, seat commands/previews, Game.Update/Clock, immutable snapshots, work/transformation views, saves and replay only. No new public engine API or protocol change unless actual composition exposes a defect.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| New | ExtensionProof/content/process-only-{master,world,work,recipes}.json | low |
| Modify | ExtensionProof.csproj, test Program.cs | low |
| New | ProcessOnlyGameTests.cs | medium |
| Modify | P11 design, MASTER_TRACKER, completion audit and current headers | low |

## 5. Error handling

Missing required selected physical module/document refuses composition. Wrong company command/view remains private. Disabled operation and worker keep their actual blockers/progress. Cancellation returns owned unconsumed WIP, never manufactures a result or refunds spent input.

## 6. Compatibility

Development only; no legacy/shim. Existing runtime/protocol/codecs remain unless a real repair is needed.

## 7. Verification

One NEW exact bounded process-only case, same-case repairs only; compile the completed slice as needed. Do not execute older/family/full/Oil/Tycoon/platform suites. Close only original P11.acceptance.036 when current authoritative absence and real physical execution are both proven; broad gates remain open.

## 8. Risks

Do not confuse an unselected optional module with a module absent from the registered definition/manifest. Do not make the driver compute gameplay eligibility, state, resource conversion or timing. Reusable tool quantities and cancelled WIP remain in the existing buffers.

## 9. Out of scope

Tycoon behavior/protected G19 references, new business defaults, renderer/platform/adapter work, all optional-module matrices, full regression, performance/soak and publication.

## 10. Execution order

1. Inspect existing process/work/module contracts and specify the actual minimal module closure.
2. Author explicit master/documents and thin normal-player workflow proof.
3. Build and run only that bounded case; repair the same case when necessary.
4. Record actual source/evidence and update the original tracker gate without aggregate completion inference.
