# P10-B7a — control action-policy verification

Current M146/native2.771: operation/company action JSON uses shared declarations over
IControlActionPolicy; redundant pure abstract base deleted, substantive native resource
sharing retained. [Focused368/0, typed-recipient refusal and limits](P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m146-shared-json-actions-and-input-recovery--2026-10-23).
Historical B7a results below are not new-runtime reruns or all-action qualification.

## Historical B7a evidence

2026-10-06. Build `beep-simengine-2.135.0`; save72/automation3; operations view6.
Scoped implementation proof, not P10/P1-P12 completion.

Current follow-on [B7b reserve-action evidence](P10_OUTLET_RESERVE_ACTIONS_VERIFICATION.md) uses2.136.0/save73,
682 checks/two exact suites including this preceding545 coverage, not additive. B7a source fingerprints
below record that gate, not current modified action contracts/target discovery.

## Final authoritative gate

```text
dotnet build Beep.SimEngine.slnx --no-restore
Build succeeded: 0 warnings, 0 errors. MSBuild elapsed 2.27 seconds.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold and recovery contracts, bindings and private controls"
312 passed, 0 failed; 1 complete suite; 501 ms.

dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Automation: threshold edits, recovery, deferred timing and saved processes"
233 passed, 0 failed; 1 complete suite; 699 ms.
```

**545 checks / exactly two complete suites**, zero final failures/aborts. Build completed before tests;
no source changed after this gate, only staged documentation. These same two suites include preceding
recovery, thresholds, native physical production/pool controls and complete profile/source discovery.
Historical479 is not an additional current count. No third/family/full suite ran. Unrelated suites
compile only. Clock source unchanged; historical85-check clock gate not rerun.

## Owners and breaking changes

- Required `IControlRule.ActionPolicy` resolves an actual `ControlActionPolicyDef` through JSON
  `action_policy`. Native/public rule implementations migrate without a default selector.
- Open `IControlActionPolicy` declares the actual command ID/owner, composition validation, pure target/
  action interpretation and command construction from readonly `ControlActionRequest` values. Request
  StepIndex is a captured timestamp from GameClock, not mutable time or a new clock.
- Native `OperationControlActionPolicyDef` selects the normal ConfigureOperationCommand registration
  and validates IControllableOperation/unit semantics. Existing handler retains safety/bounds/commit.
- Stateless `ControlActionDispatch` verifies actual catalog/registration/owner/type, physical target
  ownership and participating target/input versions, membership, ownership, current clock and next
  entity identity. Construction has no World handle. Returned command must be unstamped, of the exact
  declared type and expose the actual entity via `IEntityControlCommand`. Redirects cannot substitute
  another target. Existing CommandRegistry.Copy snapshots the payload; engine stamps sender/time.
  Normal handler Check cannot alter payload, header or target without faulting.
- AutomationSystem no longer imports a hardcoded operation registration/target-unit path. Existing
  CheckReferences delegates target semantics; configure preflights both actions; existing order290
  rechecks and applies the selected ordinary handler only for an earned old pending decision.
- Existing records/settings/pending booleans/Sync/phase290/GameClock remain the only runtime owners.
  No action queue, alternate command executor, controller state, capability registry or timer.
- `ControlBinding` replaces OperationBinding everywhere; no alias/compatibility overload. Its codec
  is unchanged. Existing normal command marker getters add no payload fields. Save72/automation3 and
  persistent owner layouts remain unchanged; operations view5→6/build134→135. Strict build identities
  still refuse old builds.62 build assertion files migrate; no save-format assertion migration needed.
- Four existing explicitly manifest-listed JSON rule documents declare native operation policies and
  required action_policy references. The public proof document declares its own action-policy type
  and normal command. No automatic registration, folder scanning, content fallback or implicit action.

## Focused proof

Seven composition faults reject missing/unknown/wrong-kind policies, missing/foreign command ownership,
wrong native command type and a command without the target contract. Profile metadata exposes only
detached policy/command/owner strings, not registration or evaluator handles. Actual existing knowledge
visibility now withholds hidden action policies as well as rule/signal/recovery dependencies; ordinary
configure shares the refusal. Observation recording remains test admission instrumentation, not a new
physical knowledge activity.

Independent public-only ProbeControlActionPolicyDef targets an actual signal source using unit.level
and selects the normal `proof.set-automation-level` command instead of operation controls/unit.work-rate.
Normal actor ownership and unit refusal parity, pure previews, provider-retained command detachment,
engine stamps, deferred actual target-state changes, unchanged operation controls, dwell/cooldown, pause,
cancelled pending decisions, actual handler refusal and save/load/interleaved twins pass.

Eight fault modes cover binding mutation, null command, foreign command type, redirected target,
forged timing stamp (friend-test instrumentation), participating-source construction mutation, command
codec mutation and normal-handler payload mutation. Invalid previews publish no rule prefix and never
advance the game clock. Participating-owner guards are not a claim of global rollback: a malicious
callback may already have touched an instrumented object before the engine faults.

## Repairs

First source build failed on a missing proof import and an attempted write to engine-owned IssuedTick
from the non-friend public extension assembly. Added the import and kept the setter internal; timestamp
fault injection runs through a public diagnostic callback assigned by friend tests. No public mutation
escape was added. Clean rebuild preceded all executed tests.

First extended contract run had311 passes/1 failed hash assertion; execution233 passed. The assertion
compared against a hash captured before an unrelated refused player command advanced the existing clock/
journal. Moved the capture to immediately before pure preparation; no engine semantics weakened.
Final rebuild plus the same two suites yielded312+233. No third suite or stale-binary test run.

## SHA256 after final source gate

| File | SHA256 |
| --- | --- |
| Standard/ControlActionPolicies.cs | 1718A36332DD607BD115C60F9A1D8DFFB011D8AA44DD46F172A8FA388E95D264 |
| Standard/AutomationContracts.cs | F9CB134AB668BDA319BF642628C671E0B3F00079F4E1D67CB3B99A920D283DB6 |
| Standard/AutomationSystem.cs | A321AE137BA57B612A867ED1077FA468FA8B1E7AEC4AAD0041EB751387C7349E |
| Standard/AutomationProfileSnapshot.cs | 953DFF3376D995F2C7E55DAB98430BDCFF21E95D1111918EBF486D904F1391D1 |
| ExtensionProof/AutomationProof.cs | 036DCA7FF51DE0A2DEBD6FFA24AFC3E542A6A76BDA736C6375A3340527FCBECF |
| Tests/ControlActionPolicyTests.cs | F19997FC3CFF9C049F4FA5D86BE9B5F455B50621B96856797EE123842449D141 |

## Remaining scope

Native reserve/stock-policy actions and runtime action/duration editing remain next P10 work. Utility
allocation/physics, generic graph providers, whole provider/purity/global rollback, replay/lockstep/
fuzz/soak/allocations and final cross-game acceptance remain open. Scalar enabled/target/unit action
does not claim arbitrary multi-command transactions. No Oil/Tycoon/host/demo/platform/Draw work.

[Stage](../../plans/enhancements/P10_CONTROL_ACTION_POLICIES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md),
[parent](../../plans/enhancements/P10_NETWORKS_AUTOMATION_DESIGN.md).
