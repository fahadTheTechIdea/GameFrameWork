# P12-D4 operating policy — scoped verification

Date: 2026-10-06. Runtime: Beep.SimEngine only. Build **beep-simengine-2.102.0**, save **64**.
[Stage](../../plans/enhancements/P12_OPERATION_POLICIES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Implemented ownership

IOperationPolicyDefinition binds actual named IControllableOperation and IConditionProvider with the
existing catalog's exact unit. IOperationPolicy owns current settings, shutdown latch and latch-change
tick only. Actual enabled/target, condition, work, resources and travel stay with their existing owners.
Native operation_policy is an optional component in EquipmentConditionModule, codec1. There is no second
control/condition ledger, service scheduler, repair queue or host rule. Missing bands/identities/unit/
restart mode fail explicitly; 0 <= shutdown <= maintenance < restart <= actual maximum is unit-quantized.
Criticality has no arbitrary genre ceiling; actual base+criticality overflow refuses admission.

The existing system370 retains its captured wear pass, then captures every policy observation before
committing safety controls. Its schema becomes3; existing condition codec2 is unchanged. The shared
operation Check/Commit now owns both normal commands and internal policy actions. Safety stop keeps
target/WIP. Auto restart requires actual recovery and its own latch. Player disable clears ownership;
manual restart waits for normal command admission. Enabled safe rate changes are not subjected to the
disabled operation's restart band. The normal registered player/automation control route checks policy
safety. End-to-end AutomationSystem integration is not separately exercised by these two suites.

One OperationPolicySettings parser/serializer serves authoring, native staged codec, normal command and
public proof. Policy configuration/latch commits verify exact state, actual session identity/time/version
and unchanged bound condition/control facts. Native decode stages all fields before replacement. Current
chronology and latched-enabled contradictions validate at checkpoint/after full restoration. These are
scoped participant guards, not global transactional rollback or arbitrary cross-service purity.

Recurring native JSON now requires operation_policy:null or an actual named policy. Optional metadata
IRecurringOperationPolicyDefinition permits other providers. Current linked condition readiness ANDs
with the existing trigger; effective priority derives checked base + criticality. WorkBoard alone still
owns admission, duplicate detection, native plans/jobs/progress/stock/tool claims and history. Editing
policy/source settings never rewrites admitted plan priority. Source codec becomes2, work view31;
WorkBoard22/work system2 payloads are unchanged. Condition view3 exposes private immutable policy facts
and matching ConfigureOperationPolicyCommand previews. Unknown/foreign facts never fall back or leak.

## Final commands and results

```
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Operation policies: authored thresholds, controls and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Operation policies: shutdown, physical maintenance and saved restart"
```

- Final build: **0 warnings, 0 errors**, 3.20 s.
- Authored policies/controls/checkpoints: **108 passed, 0 failed**, 485 ms, one exact suite.
- Physical maintenance/restart: **21 passed, 0 failed**, 507 ms, one exact suite.
- **129 checks, 0 failures, 0 aborted suites**. Only those same two full names ran during repairs.
- Initial runs exposed uninitialized codec locals on load. The fix is staged serialization scratch with
  one shared settings serializer, not a gameplay default or OnCreate replay. Later focused additions
  prove bound-control corruption detection and alternative discrete-unit policy behavior.
- 36 preceding engine identity assertion files migrate/compile only, including one already-stale save
  assertion migrated to actual64. No earlier/third/family/full acceptance or full-regression run.
- Native source fixture authoring directly adds required operation_policy:null; no compatibility reader.
- Scoped tracked-file whitespace check and explicit new-file trailing-whitespace scan passed; Git
  reported only LF/CRLF notices. The checkout's unrelated existing changes were preserved.

Contract evidence covers explicit bands/units/IDs/restart mode, missing bindings, duplicate operation
control ownership, representational priority boundaries, private detached facts, command/preview parity,
pending-command save/continue, native staged malformed decode, future and contradictory checkpoints,
manual disable, safe rate change, partial recovery, exact restart boundary and zero shutdown threshold.
Independent public-only policy and unrelated operation implementations use the same coordinator; an
independent condition provider uses an alternative discrete authored unit. Five fault modes cover pure
read mutation, missing settings commit, missing latch commit, future latch time and a settings provider
silently changing actual controls. Faulted sessions cannot checkpoint. No friend access is added.

Physical evidence uses ordinary player commands: enqueue real process WIP, enroll maintenance, perform
timed machine stress, trigger safety stop, and submit actual maintenance at base70 + criticality20 =90.
Crane travel and positive service work consume three actual spares and retain two tools; two prior WIP
inputs remain protected. Current policy edits leave admitted priority unchanged. Native plan pause freezes
progress without repair or restart. Resume completes actual repair, releases held tools and restarts
only in the subsequent policy observation; process work then resumes preserved progress. Saved worlds
at signed64 age1<<40 retain identical hashes, settings, latch, source references, claims and work. Foreign
company controls/stock remain unchanged. Every wait is bounded at700 ticks with policy diagnostics.

## Source fingerprints (SHA-256, relative to framework root)

```
c846593ac487fad616a38428b9872af5886c446067395ef222e0f0f73be4b6e5 src/Beep.SimEngine/Standard/OperationPolicies.cs
2150802d3ff1013fc1565cfabeeb05bfce0c7f6b49e61c11e5f9ced1265e7bb9 src/Beep.SimEngine/Standard/Operations.cs
461b59000fd828575203a5a735c94f2e0528f973aab5eec850a63bc296cc6571 src/Beep.SimEngine/Standard/EquipmentCondition.cs
29e467c2feaa0b8fd29c2896406757900a555e31ec5865d25172aa7e97a2c9c4 src/Beep.SimEngine/Standard/EquipmentConditionModule.cs
3beb2ae338a7fce90518dc7e08abb2a5d7e913cdc0975f8cb5f5ddb5438f36ff src/Beep.SimEngine/Standard/RecurringPlans.cs
725cb659a65be294036f4cdc7d73d8cadf008a20e6bd12e43919646526dc9177 src/Beep.SimEngine/Standard/WorkRecurringPlans.cs
cb05ce4da96aa9ced47cd3f71de7d29d076f3583d6ab251b340fc66c8894d264 src/Beep.SimEngine/Standard/WorkModule.cs
e68fb1b085d6c22f024bca08a776174a3cdb0411c6ffbdd92c65b9149e86b064 src/Beep.SimEngine/Commands/OperationPolicyCommands.cs
cef4d9b7648a7bc76899ec1d0f38911fd2de98cc38a10cef4b22a1d97f10b597 src/Beep.SimEngine/Views/EquipmentConditionView.cs
685638bd32f53a69e5e3f279136f5df59c43e60d8464c0c825d2d92ff221ccc0 src/Beep.SimEngine/Kernel/SaveGame.cs
7a56e71ff541961a9c94ec3dc38ef60a1ab6d8de5110bad722a547f712cefc6a tests/Beep.SimEngine.Tests/OperationPolicyTests.cs
aa2ad4df25de879c22c33651e8a7d9225254438329cd296f40a3a2f7a4c81c3b tests/ExtensionProof/OperationPolicyProof.cs
fc233cb57eb539ba4637805d4262a94140695da67c00c31f6f5c940018b16dd8 tests/ExtensionProof/content/operation-policies.json
52d7f8d11c3bfc5c4e145eec29ea355b6313ab4c122ff7ef3336fbe3979d1c51 tests/ExtensionProof/content/recurring-maintenance.json
```

## Remaining scope

Fleet/preparedness reserves, incident policies/arbitrary execution gates, inspection science, contamination/
native formulas, provider replacement, global purity/rollback, full AutomationSystem integration, broader
providers/failure recovery, replay/lockstep/shuffled arrival/fuzz/stress/soak/allocations and full P12/P1–P12
acceptance remain open. No OilFieldDays/Tycoon/host/demo/harness/platform/Unity/Godot/Draw implementation.
Development-lifecycle guidance kept stage, evidence, tracker and help aligned. Full framework goal active,
not complete.
