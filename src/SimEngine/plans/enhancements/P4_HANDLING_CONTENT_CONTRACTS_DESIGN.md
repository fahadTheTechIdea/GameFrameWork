# P4-G16 — Open physical cargo-handling content contracts

Status: scoped verified, 2026-10-09;275 checks/zero failed/zero aborted. Full P1–P12 remains open.

Build attempts:7.48s public proof clock API correction, clean8.11s/6.25s. ONE NEW exact case first275/0/0 in803ms; FINAL275/0/0 in612ms, childPID10256 exit0/observed759ms. Existing public transporter/slot/identity consumers migrated; older runtime branches compile only. Core2.348.0/save154/replay1; state/view schemas unchanged; freeze05:43:39UTC. [Evidence](../../docs/migration/P4_HANDLING_CONTENT_CONTRACTS_VERIFICATION.md).

## 0. Problem and specification

TransporterDef resolved ResourceHandlingProfileDef despite exposing IResourceHandlingProfile.
HaulHandling.Check watched only carrier.Version around callbacks, not the sole clock/provider scope.
Independent authored handling providers could not join the original physical path without inheritance.

## 1. Goals

IResourceHandlingProfile is an actual IContentDefinition. Native transporters resolve this contract.
One explicit ResourceHandlingDeclaration owns original linear resource rows/resolution/duration math.
Native and independent definitions use that same declaration; alternate slot handling owns only its
distinct duration semantics and directly implements the same contract.
Existing observation guards protect profile lookup/callback facts before preview, receipt acquisition,
prefix calculation, physical progress and checkpoint/view inspection.

## 2. Design

Standard/ResourceHandling.cs moves native row authoring/exact ratio calculation into the public shared
declaration. Original HaulHandling takes the actual World, aliases owned content and uses only world.Time.
ITransporter.Handling lookup and duration callback use EntityObservation.ReadProvider with the actual
carrier. Invalid default/throw/clock mutation cannot authorize a haul or save a faulted partial session.
Existing Earned monotone prefix search delegates the same Check; no copied rates or second clock.
Logistics.cs and Work.cs pass their existing world through all admission/prefix/restore/view calls.
Job progress/Loaded/receipts, native inventories, movement/docks and entity.Update remain authoritative.

## 3. API

IResourceHandlingProfile extends IContentDefinition; ResourceHandlingDeclaration.Read/Resolve/CheckHandling.
No mutable public scheduler, balance, progress or host rule. JSON field names remain original.

## 4. Files

Standard/ResourceHandling.cs, Capabilities.cs, Logistics.cs, Work.cs; build identity only in SaveGame.
Public proof module/JSON, direct slot definition; ONE new thin player case, current protocol fixture.

## 5. Errors

Missing/wrong-kind handling, malformed/duplicate/unknown resources, invalid quantities/durations refuse
content initialization. Unsupported resources and actual time overflow remain typed admission blockers;
large authored durations are not globally banned when a smaller compatible ratio may resolve.
Malformed callback facts/throws/clock mutation fault through the existing observation owner, never
compensate or fabricate cargo. Callback guards are not an arbitrary CLR security/CPU sandbox.

## 6. Compatibility

Development-only; no legacy reader, alternative field names, shim or parallel subsystem.

## 7. Verification

Build as needed; ONE NEW bounded case:
`Handling content contracts: independent JSON rates, physical prefixes and guarded callbacks`.
Normal player hauling, amount-based partial loading/unloading, real travel, policy/clock pause,
saved phase continuation, conservation, private views, replay/session isolation, early JSON refusals
and deliberately faulty providers. Only repair/rerun that same case; no historical/family/full runtime.

## 8. Risks

Broader resource/unit/quantum, endpoint loss/cancel/recovery, provider faults, lockstep/stress/soak/
allocation/performance and full P4/P7/P9/P1–P12 acceptance remain open.

## 9. Out of scope

Oil/domain changes, business expansion, mandatory money/budget, Draw/Unity/Godot/adapters, publishing.

## 10. Order

Read source/tracker/stages → shared declaration and contract → original consumers/guards →
master-listed public proof/thin player → focused build/runtime → evidence and tracker reconciliation.

[Parent](P9_PROGRESSIVE_HANDLING_DESIGN.md), [tracker](MASTER_TRACKER.md).
