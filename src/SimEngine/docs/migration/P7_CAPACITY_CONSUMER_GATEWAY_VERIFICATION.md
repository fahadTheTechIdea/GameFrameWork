# P7-CG1 — Capacity consumer gateway completion evidence

Status: VERIFIED-SCOPED; full goal/plan open.

[Stage](../../plans/enhancements/P7_CAPACITY_CONSUMER_GATEWAY_DESIGN.md). Core beep-simengine-2.492.0/save154/replay1. Original221/187checked/34open unchanged; additional221. No original gate closes.

## Actual source changes

63 interface dispatches on58 lines across27 existing production files now call CapacityPolicyContracts.Check/Quantum/Offer. Receivers/arguments/instances/control flow and normal refusals remain unchanged. This covers resource rates, specialized native declaration callbacks, construction/recovery/line admission, inventory signals/load policy, logistics demands/stock bands/routes/handling, network metadata/flow, conversions/claims/transfers/batches/spatial resources, transformations/WIP/ports, utility consumption and upgrades. No new arithmetic/model/stock/book/graph/clock/evaluator/compatibility path.

Before/after nondispatch canonical SHA256 values for all27 files are identical. Exact substitutions were applied to both inventories before comparing; actual file hashes changed. Residual dispatch review leaves the existing gateway's3 raw interface dispatches,3 exact InstancedCapacityPolicy forwards and native _declaration arithmetic entrypoints; LinePlacementPolicies.Check is a different placement-policy contract. Named WIP/utility capacity aliases were included, not excluded by a spelling-only match. This is explicit source/compile conformance coverage, not every-provider behavior proof.

## Exact build and runtime

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Capacity consumer gateway: shared physical workflow and malformed native hauling admission" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 24000
```

ONE solution compile6.70s/exit0/0 warnings/0 errors. ONE NEW exact FIRST/FINAL168 passed/0 failed/0 aborted in1368ms; PID31684 Exited0/process1534ms, no truncated output or observer error. Watchdog60,000ms/termination5,000ms/output24,000 chars. No older/family/full runtime and no executable edits after FINAL.

Four minute/half real independently authored native-storage hauling/batch/tools/completion/cancellation flows reuse the original public player workflow with actual quantities/timing/saved twins/private immutable views/pause/replay/interleaved isolation. Six zero/undefined/non-successful-with-positive-raw quantum admissions now reject both command and management hauling preview with explicit canonical contract error, not normal no-capacity. Pure rejection retains hash/time/no job/receipt acquisition and a healthy isolated session. These pure malformed-result errors do not claim terminal-fault semantics for every public service.

## Per-file nondispatch identity

| Production source | Identical canonical SHA256 before/after |
|---|---|
| src/Beep.SimEngine/Content/CapacityPolicies.cs | d1ac60df4634349094899620737fd60ae44c979d4060a170a9a7640a16617644 |
| src/Beep.SimEngine/Core/ResourceRates.cs | 278e937e1649f7e91159fb205caa7e4e5a3630b586fc0d7ab0df5c28597f2483 |
| src/Beep.SimEngine/Standard/Construction.cs | 5827c7d0c003c1a0693ab073fcfa27df726e1606aedd21230b0454aff984c992 |
| src/Beep.SimEngine/Standard/ConstructionRecovery.cs | ced850ff855e2ad10ad27c3705cd8e6fdcac44c055409a306be19c5e872dba9a |
| src/Beep.SimEngine/Standard/HaulLoadPolicies.cs | ddd11a9c244a3c07c7c5a8cc5ef266e4d6894b97441a149c07649bd82893afab |
| src/Beep.SimEngine/Standard/InventorySignalBindings.cs | 5a6c09fa257d00c3f867434c91df4f07f5698696cc54f73ac82774419d308cf4 |
| src/Beep.SimEngine/Standard/InventorySignals.cs | 666918e623271eb7c072e3c8134cf057e8f3440f93e5edc0943ddec4e07bd8f8 |
| src/Beep.SimEngine/Standard/LineConstructionService.cs | 1ad5281205bca28257ac726a012f2bdc5df5297994bf00d547de20d5a98288d4 |
| src/Beep.SimEngine/Standard/LineDefinitions.cs | a3c63f351d0d81e632106dd4c4197ce1391c44363a6e65dbbb1064beb4cdec46 |
| src/Beep.SimEngine/Standard/Logistics.cs | 194b21232ead5f42d8edb3d9f9f237fb3c256742fd7090d814d69cc23a7b28a6 |
| src/Beep.SimEngine/Standard/LogisticsDemands.cs | 1b411f274653007722fd33eb600e471ccd0d98fc368e57d952612dc9795562fa |
| src/Beep.SimEngine/Standard/LogisticsPolicies.cs | d3d9661b533e59dfe3d74785b1003475823093b5cbc87232b3e9f4ecdd7d4d52 |
| src/Beep.SimEngine/Standard/LogisticsScheduling.cs | ec7c67e86a36e3d84a2bd21a5708d5c994a92b18b2d8647e1545504a251784d8 |
| src/Beep.SimEngine/Standard/LogisticsStockPolicies.cs | 3f58b2bd7b08d00f6a609cde9d3f39c866b8cce5b17486b7984658d03d38b589 |
| src/Beep.SimEngine/Standard/NetworkDefinitions.cs | a46cc9818696569c99daa359f7751caf9d707726bfd5a40f9145232dd52ffa37 |
| src/Beep.SimEngine/Standard/NetworkFlow.cs | 5525cc3f6f9428cce3fed87f5d75d95e9873a8cd15e2d8e78dfa6194ad6d5d46 |
| src/Beep.SimEngine/Standard/ResourceConversions.cs | aab598fbf8da609a4a1a1a556e1c4d9b4761586bc7456e5ee15c9c5903e4679e |
| src/Beep.SimEngine/Standard/ResourceQuantitySignals.cs | acec1e3bf0a57b8d29a7cd0342eaa78de54f58cb96ca56ed0a7da26661af34d4 |
| src/Beep.SimEngine/Standard/ResourceRecoveryDeclaration.cs | 626baa25e7035f2cf956ad613ce6572ebbcf2d6db365d22270bf2c1535c4ae4e |
| src/Beep.SimEngine/Standard/ResourceTransactions.cs | 12b5bd588eb7e3a890b84957e0ab4939c4d97eda89aee5ea7283311504fbfad4 |
| src/Beep.SimEngine/Standard/ResourceTransferBatches.cs | 7459fb3ba4708f0219bcd19869c572973a996cbfe1487be383ebecf248e65991 |
| src/Beep.SimEngine/Standard/SpatialResources.cs | 2feaa50b4bbdacdeecd7983a7ea9b27d71785f87724feae1a2b13f014f230cb0 |
| src/Beep.SimEngine/Standard/TransformationContracts.cs | b49c9fa49a64c8733b3dd3cf511419d1b7b89724473d4cfdb22ceee0f1a7f11a |
| src/Beep.SimEngine/Standard/Transformations.cs | fdc1b36f776775a50578d74b7af09c70610cab070aeed8fe580cd472ca538373 |
| src/Beep.SimEngine/Standard/Upgrades.cs | b95bb3efb4a4e1935e22f57dfc5043ad17c2fa2ee340858aeaae8eef3f067a28 |
| src/Beep.SimEngine/Standard/UtilityConsumers.cs | bb62b8a39c35a8b42380e758c84fc3e5fbb1a50ade124c2085086407563f0715 |
| src/Beep.SimEngine/Standard/Work.cs | 6bbf4aa6c4a2cef2257c83f012aed729f12a8557889172726e53a7c06146611a |

## Remaining original gate audit

All34 literal gates remain OPEN. P3.001 and P5.001–009 belong to Oil-specific consumer/constraint validation; no Oil files or runs were changed or attempted here. Framework gates below still require the listed whole-scope evidence. Original text/checkboxes are not rewritten or narrowed.

| Original gate | Current completion evidence still required |
|---|---|
| P3.acceptance.001 | Oil-specific Minute-based oil sessions retain deterministic physical timing with explicit authored configuration. |
| P3.acceptance.008 | Whole snapshot/spatial authorization and immutable provider matrix; source and scoped proofs do not prove arbitrary providers. |
| P4.acceptance.014 | Every operation preview/command/blocker evaluator and private projection parity, not only capacity admission. |
| P5.acceptance.001 | Oil-specific Oil solution contains one extension and references one external framework runtime. |
| P5.acceptance.002 | Oil-specific No oil module needs friend access, mutable host-world exposure or concrete framework internals. |
| P5.acceptance.003 | Oil-specific All oil content resolves through its explicit master manifest with no duplicate generic definitions. |
| P5.acceptance.004 | Oil-specific Unsurveyed drilling completes physically; seismic/logging remain optional. |
| P5.acceptance.005 | Oil-specific Existing production/construction/hauling/recovery/research scenarios preserve timed behavior. |
| P5.acceptance.006 | Oil-specific All operation/placement/work/route facts used by clients originate in engine views/previews. |
| P5.acceptance.007 | Oil-specific Rivals and players have command/preview parity, affordability and private knowledge enforcement. |
| P5.acceptance.008 | Oil-specific Oil saves, lockstep and twin-run hashes pass under migrated contracts/time/content. |
| P5.acceptance.009 | Oil-specific No Tycoon, old sibling engine, Unity/Godot, renderer or adapter changes occur. |
| P6.acceptance.006 | Original extraction assertion-to-owner inventory and registration preservation across both repositories; protocol expectation cleanup alone is insufficient. |
| P6.acceptance.007 | Explicit framework plus Oil profiles, zero failures/aborts and declared exclusions; final full run remains deferred. |
| P6.acceptance.008 | Complete nonspatial/spatial/external-mod proof profiles without Oil or internals, not an aggregate of historical scoped passes. |
| P6.acceptance.009 | Current complete save/load/continue/replay/lockstep/isolation profile. |
| P6.acceptance.010 | Ownership/manifest/thin-client executable audit across complete runtime boundary. |
| P6.acceptance.011 | Every client/harness/host source writer/formula/completion-rule audit; public new drivers are not whole-client closure. |
| P6.acceptance.012 | Deliberate stall/corruption bounded diagnostics across complete scenario/profile set. |
| P7.acceptance.004 | Actual stress and defined allocation-budget proof; new defensive copies have no budget evidence yet. |
| P7.acceptance.009 | Complete tool/dock/graph/inbound/physical-batch participation and transaction matrix. |
| P7.acceptance.017 | Full incident/cargo/entity rebinding/conversion/spill/topology disposition and fault/stress matrix. |
| P7.acceptance.019 | Conditional selected quality/lot fixture must use one buffer and saved twins; no selected production quality/lot implementation found in current source search. |
| P7.acceptance.020 | Whole duplicate catalog/balance/hidden-limit/obsolete-field/load-path audit. Raw capacity consumers were an identified implementation gap now migrated, not this entire gate. |
| P8.acceptance.013 | Every transformation phase cancellation/save/load with exact WIP/output/cost accounting. |
| P8.acceptance.015 | Complete immutable preview input/yield/throughput/ETA/blocker and RNG/state purity matrix. |
| P8.acceptance.017 | Whole broader WIP/disposition/ports/continuous/results/provider/full accounting matrix. |
| P10.acceptance.001 | All topology mutations with live reservations and exact resource disposition; pooled-stock/scoped topology examples alone are insufficient. |
| P10.acceptance.003 | Provider-neutral corridor and generic utility blocker breadth. |
| P10.acceptance.022 | Complete control-provider/fault/replay/stress and phase acceptance. |
| P11.acceptance.026 | Whole remote/non-tile utility/sensor/condition/occlusion/freshness/exploration/sharing/forgetting/protected-cell matrix. |
| P11.acceptance.029 | Pressure/incident/objective/controller strategy and corruption/fault/replay/lockstep/soak matrix. |
| P11.acceptance.033 | Fog/knowledge metadata/content-menu/delta private-activity leak audit and executable matrix. |
| P11.acceptance.035 | Every selected optional module interleaving/save/load/shuffled-input/twin identity matrix. |

## Limits and authority

Broad endpoint/receipt/topology/privacy/provider/metadata/global side effects/durability/stress/allocation/full gates remain open. Existing stock/books/state schemas/sole clock remain. Probe fixture exposes data-only factory selection; old default behavior/registration/assertions unchanged, two new explicit quantum faults only in engine-side negative instrumentation. All Tycoon/Oil/platform/Draw/clock implementation sources unchanged. Final full regression remains authorized only after implementation finishes; no staging/commit/push.

## Source freeze and metadata closure

Freeze2026-10-12 11:52:03 UTC:1,755 source anchors/985 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2,941 files/1,049 Markdown documents/8,717 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original187 checked/34 open unchanged, additional221.31 previously selected executable inputs changed; previously existing ResourceQuantitySignals.cs plus one new driver added to freeze.28 existing production sources changed (27 consumer files/build identity), no new production source or schema. Sole clock/all12 Tycoon source unchanged. git diff --check exit0 with34 pre-existing warning lines;2,929 dirty entries/0 staged. No executable edits after FIRST/FINAL; no old/family/full runtime or staging/commit/push.
