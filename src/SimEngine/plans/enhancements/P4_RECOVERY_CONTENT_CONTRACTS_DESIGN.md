# P4-G13 — Open recovery dispatch and destination content

Status: scoped verified,100 checks; full P1–P12 open. Publication remains conditional on full goal and plan completion and verification.

[Current evidence](../../docs/migration/P4_RECOVERY_CONTENT_CONTRACTS_VERIFICATION.md).

## Problem and goals

Before this slice, LogisticsRecoveryPolicies.cs declared ILogisticsRecoveryPolicy but selected LogisticsRecoveryProfileDef and resolved RecoveryDestinationPolicyDef. LogisticsRecovery.cs validated those native bases; Logistics.cs resolved the same base for destination evaluation. Independent JSON providers could not participate through their actual existing contracts alone.

1. Both existing contracts extend IContentDefinition; all catalog/service/restore consumers admit actual owned identities.
2. LogisticsRecoveryProfileDeclaration owns the original explicit two-kind JSON settings and reference validation. RecoveryDestinationDeclaration owns the original mode reader and quantized demand decision. Native and independent implementations delegate to those same declarations.
3. Existing guarded observation reads selected profile settings and evaluates destination decisions. Missing references, wrong kinds and unused invalid profiles fail before Initialize. No default policy, duplicate queue/ledger or compatibility reader.
4. One NEW exact bounded thin-player case authors independent profiles/destination modes, observes disabled worker queues, enables normal work, physically returns cargo then evacuates stock, pauses/saves/replays and conserves real resources. Invalid authoring variants fail early.

## Design and API

- Standard/LogisticsRecoveryPolicies.cs: ILogisticsRecoveryPolicy identity, shared declaration Read/Resolve/ReadSettings; one internal selected-settings observation helper used by native recovery and WorkBoard restoration validation.
- Standard/RecoveryDestinationPolicies.cs: IRecoveryDestinationPolicy identity, shared stateless declaration Read/Evaluate; existing typed blocker identities unchanged.
- Standard/LogisticsRecovery.cs, Logistics.cs and Work.cs: actual aliases and guarded callbacks; same intent chronology, jobs, physical receipts and resource owners.
- Existing public extension profile delegates to the shared declaration. Native base classes remain implementations, not an inheritance requirement.
- ExtensionProof registers JSON-only direct Def implementations and diagnostic Initialize; no host gameplay or additional scheduler.
- Engine build identity changes; save154/replay1 and existing component/service/view codecs remain unchanged unless evidence requires otherwise. Sole GameClock unchanged.

## Files and errors

Modify the five named engine sources, Kernel/SaveGame.cs, current-engine-protocol.json, existing extension recovery proof and test registration/project resources. Add one public-only proof module, master-listed JSON content, one thin-player case and scoped verification evidence.

Explicit evacuation/cargo_return children, booleans, priority, positive attempts_per_pass, destination identity and unique blocker array remain mandatory. Destination mode accepts only nearest_available/demand_first/demand_only. Missing/wrong-kind selected or referenced content refuses before initialization; provider faults latch the original session. Native endpoint/worker/capacity/quantum/dock readiness remains authoritative. Development only: no backward compatibility or second reader.

## Verification and execution order

1. Replace base-gated consumption and share original declarations; review every actual consumer.
2. Author independent JSON content and ONE NEW exact bounded case. Build only as needed; repair/rerun that same new case only.
3. Source review, executable freeze, actual runtime/build evidence and tracker/hash/link reconciliation.
4. Keep all broad/family/historical/Oil/Tycoon/platform runtime and full-phase gates open absent renewed authorization.

## Risks and limits

Custom arbitrary provider mutation/recursion, named endpoint loss/replanning breadth, comparative contention, complete resource accounting, fuel/breakdown/stress/lockstep/full acceptance require additional evidence. Narrow verification does not close those requirements.

## Out of scope

OilFieldDays, optional business/Tycoon, mandatory income/budget, Draw/platforms/adapters, host gameplay, a second recovery queue, copied stock or clock, fabricated compensating refunds and instant recovery.
