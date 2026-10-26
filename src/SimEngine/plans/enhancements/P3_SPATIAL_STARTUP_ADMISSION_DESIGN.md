# P3-S30: Spatial startup admission — Design Document

## 0. Problem Statement

World.FinishComposition compares selected space identity outside the scoped observer, while ValidateSpatialLayers observes the codec but not the required ObservationVersion. A selected space with no active consumers can reach Initialize without that contract being checked. Original P3.acceptance.004 requires missing required spatial providers to fail composition with no default-map fallback.

## 1. Goals

1. Admit actual selected space identity and pure nonnegative original observation revision before every Initialize, including empty spatial sessions.
2. Preserve intentional nonspatial sessions; never select a default geometry provider.
3. Exercise declared spatial capabilities via original component metadata admission, plus missing generic service bindings via original Services.Require/Freeze.
4. Verify actual native/point physical time, saves/replay/isolation using public player inputs, not host gameplay.

## 2. Design

Replace World's unobserved identity comparison with the original EntityObservation.ReadProvider over the actual selected space and its shared spatial scope. Run this inside original FinishComposition after registry freeze, before layer/component/provider validation and Initialize. No new validator owner, lifecycle stage, cache or clock.

Extend existing unavailable-space negative instrumentation with revision getter modes. Existing generic composition-requirement proof accepts typed spatial capabilities through its own authored metadata; it delegates absence decisions to the original engine pass. Declare no fallback providers.

## 3. API surface

Existing public factories/commands/views unchanged. ISpatialSpace already requires ObservationVersion; this phase admits it centrally. Additional proof factory inputs are pure JSON authoring only.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | src/Beep.SimEngine/Kernel/World.cs | 12 | medium |
| Modify | tests/ExtensionProof/SpatialPresenceCompositionProof.cs | 20 | low |
| Modify | tests/ExtensionProof/ComponentCompositionProof.cs | 35 | low |
| Modify | tests/Beep.SimEngine.Tests/SpatialPresenceCompositionTests.cs, ComponentCompositionTests.cs | 6 | low |
| New | tests/ExtensionProof/content/spatial-startup-admission.json | 25 | low |
| New | tests/Beep.SimEngine.Tests/SpatialStartupAdmissionTests.cs | 200 | medium |
| Modify | engine build/protocol, Program.cs, ExtensionProof.csproj | 4 | low |

## 5. Error handling matrix

| Condition | Required outcome |
|---|---|
| Selected definition binds no provider/wrong identity | Original composition refusal, no Initialize or default map |
| Negative revision/getter mutation/throw | No healthy startup; original callback first error retained |
| Unused component declares missing spatial/service capability | Original metadata or service freeze refuses before Initialize |
| No space selected and no declared spatial demand | Normal mapless simulation, no invented geometry |
| Valid native/point declaration | Original authored provider, physical execution and durability |

## 6. Backward compatibility

Dev only; replace admission in place. No legacy, second validation catalog, default revision/map or compatibility path.

## 7. Verification

One NEW exact bounded case: FINAL169/0/0 in9710ms, process9931ms/limit60000ms; all three attempts retained. One clean solution18.61s, two necessary focused fixture compiles7.77s/6.21s, no solution rerun. Twelve empty-session revision failures, fourteen missing capability cases, five absent generic services, four native refusals, valid empty/mapless continuation and four physical minute/half flows pass. Declaration/fallback source audit proves exact original P3.acceptance.004; only its checkbox closes, literal unchanged. No historical/family/full suite or executable edit after FINAL; wider gates stay open.

## 8. Risks

All declared requirements must remain owned by existing module/component contracts. Negative probes must not become normal providers. Observer revision is transient, not another clock or save field. No aggregate/full completion inferred.

## 9. Out of scope

Oil/platform/Draw/adapters/Tycoon/G19, broad fault/private CLR/fog/claims/control/stress/performance/full gates or publication.

## 10. Sub-task execution order

Read actual composition ordering and requirement owners; record stage; replace selected-space admission; extend existing negative probes and authored capability matrix; compile/run one exact public-player case; retain attempts, audit exact original gate coverage, update tracker/source identities.

Metadata closure: 1,634 source anchors, 880 selected executable inputs and 13 quickstart anchors match frozen SHA-256 values. All 221 original gate literals and line anchors are retained; exactly P3.acceptance.004 changes its checkbox (185 checked, 36 open). Additional193 is scoped evidence, not full completion. All 8,622 local links resolve; 18 master rows, the new public-driver boundary scan and git diff --check are clean. Ten existing executable inputs changed (two production), two executable JSON/test inputs added, no new production owner. Sole clock and protected Tycoon hashes unchanged; no executable change after FINAL and nothing staged.
