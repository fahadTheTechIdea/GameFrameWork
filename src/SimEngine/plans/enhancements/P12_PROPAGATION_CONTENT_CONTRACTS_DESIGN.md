# P12-B2 — Contract-owned propagation content and reusable emitter bindings

Status: scoped verified174 checks on core2.384.0/save154/replay1. Full P1–P12 remains open.

## 0. Problem Statement

Before this stage, `Standard/IncidentPropagationContracts.cs` resolved `IncidentPropagationRuleDef` despite its public
`IIncidentPropagationRule`. Its generic emitter definition also cast back to that native base.
`IncidentPropagationSystem.ValidateComposition` enumerated only the native base, so direct content
implementations bypassed admission. The native emitter runtime required its concrete definition type.
Previous observation checks omitted provider codecs and the game clock. These seams are replaced below.

## 1. Goals

1. Native/independent JSON rules and emitter metadata bind through actual immutable contracts only.
2. One shared propagation policy declaration and one emitter declaration own strict parsing and
   reference resolution. Delete obsolete required bases; migrate the existing proof implementation.
3. The original exposure system validates every loaded rule/emitter before Initialize and retains
   its one backlog/cursor/episode codec, timing, bounded budget and next-update wave admission.
4. One new public-player case proves delayed delivery, physical recovery, phase-based isolation,
   stale episode refusal, company privacy, saved pending/scan continuation and ordinary replay.

## 2. Design

Add pure `ValidateSource(EntityDef)` to `IIncidentPropagationRule` and `ValidateBinding(World)` to
emitter metadata. `IncidentPropagationPolicyDeclaration` shares explicit interval/delay/target/scope/
signal-condition metadata, exact time admission and condition evaluation. Rules can implement their
own geometry/provider eligibility without deriving native classes or modifying the scheduler.
`IncidentPropagationBindingDeclaration` resolves `IIncidentPropagationRule` and validates exact
named phased sources. Native emitter derives nongeneric `Component` with the actual contract alias.

Existing `IncidentPropagationSystem` validates all aliases/callbacks, including unused definitions.
Existing `EntityObservation` scopes protect actual rule/source/target codecs, clock and exposure
owner state during eligibility reads. Callback failures fault the session; no compensation or fallback.
Original records, captured membership, round-robin, pending expected episodes, due-time rechecks,
delayed Raise and codec1 remain authoritative. Transient recursion flags are not persistent state.

## 3. API surface

Content contract/declaration additions only; ordinary incident/work/automation commands and private
propagation views remain unchanged. Module/master explicitly lists independent rule JSON and emitter
kind/schema. Timing and geometry units/quantities remain authored, not hard-coded game defaults.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---:|---|
| Modify | src/Beep.SimEngine/Standard/IncidentPropagationContracts.cs | 150 | medium |
| New | src/Beep.SimEngine/Standard/IncidentPropagationContent.cs | 200 | medium |
| Modify | src/Beep.SimEngine/Standard/IncidentPropagationSystem.cs | 65 | high |
| Modify | tests/ExtensionProof/IncidentPropagationProof.cs | 40 | low |
| New | tests/ExtensionProof/PropagationContentProof.cs | 95 | low |
| New | tests/ExtensionProof/content/propagation-contracts.json | 25 | low |
| New | tests/Beep.SimEngine.Tests/PropagationContentContractTests.cs | 200 | medium |
| Modify | tests/ExtensionProof/ExtensionProof.csproj | 1 | low |
| Modify | tests/Beep.SimEngine.Tests/Program.cs | 1 | low |

## 5. Error handling matrix

| Failure | Result |
|---|---|
| Missing/wrong actual rule/source/conditions/runtime pair | Composition refusal before Initialize |
| Invalid zero/inexact/overflow duration | Strict authoring/session-time refusal |
| Provider throws or changes scoped clock/entity/private codec/exposure facts | Existing observation fault cutoff; save/hash refused |
| Source resolved, target changed episode or no longer eligible at delivery | Original typed blocker; no duplicate damage or substitute target |

## 6. Backward compatibility

Development only. No old reader, native-base adapter or compatibility wrapper.

## 7. Verification

Build after the slice is ready. ONE NEW exact bounded case; repairs may rerun only that same case.
Old incident/propagation/environment/Oil/Tycoon/platform/family/full runtimes are not authorized.
Preserve terminal outcomes and attempt evidence; leave broad gates open.

## 8. Risks

Observation scopes are not a CLR sandbox/global rollback guarantee. New wave timing must retain
captured initial source membership. Callback checks must not mutate the authoritative exposure book.

## 9. Out of scope

Oil formulas, scientific contamination/weather models, native tile breadth, throughput, stress,
lockstep/fuzz/soak/full verification and deferred G19 authority. No platform/Draw/host/business changes.

## 10. Sub-task execution order

1. Replace required native bases with shared declarations/actual contracts.
2. Integrate all-loaded validation and actual callback observation in the existing system.
3. Author direct JSON content/reusable emitter proof and one thin physical player case.
4. Build/run the new bounded case; reconcile tracker/audit/evidence without closing original gates.

## 11. Current evidence

Actual direct rule/emitter aliases now use the sole shared declarations and original exposure state.
One exact bounded case finished174/0/0 in1163ms, PID8940 exit0, observed1299ms. Four clean builds
and four attempts of that SAME new case are disclosed in the [verification record](../../docs/migration/P12_PROPAGATION_CONTENT_CONTRACTS_VERIFICATION.md).
Proof repairs use ordinary Update before reading created exposure, an explicit empty stock bill,
and refusal of live reads after fault cutoff. No engine defaults or fault bypass were introduced.
Native/independent emitter timing, delayed receipt, real source/target travel and resource-consuming
recovery, physical isolation, stale episodes, company-private facts, pending/bounded-scan saves,
pause, replay, session isolation,18 early authoring refusals and7 scoped live faults are narrowly proven.
Selected303 executable inputs were hashed AFTER final green at2026-10-09 16:53:07 UTC; this is not
a pre-run freeze or complete transitive closure. No executable edits followed. Historical cases were
compiled only, not run; full incident/geometry/weather/network/global/performance/full gates stay OPEN.
