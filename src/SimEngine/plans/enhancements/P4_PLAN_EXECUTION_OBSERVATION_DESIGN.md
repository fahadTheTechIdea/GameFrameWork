# P4-R4: Pure execution binding and native order identity

Status: implemented/focused-verified on2.609.0/save159/replay1/work-view108.
Five literal finals1560/0. [Every attempt and limits](../../docs/migration/P4_PLAN_EXECUTION_OBSERVATION_VERIFICATION.md).

## Problem

WorkBoard resolves execution bindings and verifies execution identities through raw
IPlanDefinition callbacks. Binding must not change its actual target/space/clock.
ExecutionIdentity also receives a live native Order; changing priority AFTER the
promotion priority check can escape validation. Existing argument observation does
not cover these execution callbacks.

## Design and acceptance

Extend the EXISTING PlanObservation with observed binding and identity methods.
Binding observes actual provider/whole target/selected space. ExecutionIdentity
also observes its actual native order through the SAME codec WorkBoard.Sync uses,
and checks its derived Plan reference is unchanged. Extract the existing order
codec body into one WorkBoard.SyncOrder method, retaining exact field order and
validations. No new format, duplicate serializer, state book or whole-board hash.
Migrate execution resolution, promotion and saved-plan validation in place.

Extend existing immutable negative activity probes with binding/execution stages;
reuse one test factory. Public persistent-plan input activates callbacks during
the engine update. Inject target/clock/space/exception faults and direct order
priority mutation. Verify terminal observation fault, own retained publication,
no subsequent time advancement/save/hash and independent-session isolation.
Existing paused argument probes remain unchanged semantically.

Engine2.609.0/work-view108; save159/replay1/state schemas unchanged because extraction
must preserve the original codec. No compatibility shim. Add explicit purity
documentation to the existing execution-binding/identity contracts.

## Verification

Record a pre-fix reproduction in the existing work-observation literal. Clean
targeted build; only exact work-observation, graph-disclosure, process-staffing and
current-native-protocol cases, plus the actual infrastructure corridor case for
dynamic line-bound graphs. Inspect mechanical codec identity before/after and
record every attempt. No full/profile/family runtime, Oil/Tycoon/host/platform/Draw
changes or Git publication. P4.014/23tracked gates remain open pending full matrix.
Reference-discovery mutations to unknown entities and arbitrary CLR purity remain
separate limitations; this stage does not claim a general module sandbox.

## Stages

- [x] R4.1 Reproduce raw binding/identity mutation through actual plan promotion.
- [x] R4.2 Single native codec extraction and guarded callback migration.
- [x] R4.3 Focused durability/physical checks and current tracker evidence.
