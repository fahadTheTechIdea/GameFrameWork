# P4-WO1: Operational signal work and earned manufacturing dispatch — Design Document

## 0. Problem Statement

`SignalWorkRequirementDef.Evaluate` refuses physical work when a signal definition becomes hidden. `TransformationDispatchContracts` reuses player enqueue admission at start/completion, so an earned workflow may stop solely because recipe metadata is forgotten. Dispatch dependency declarations also omit the recipe's internal resources and result prototype.

## 1. Goals

1. Keep signal visibility in ordinary intent/projection admission; evaluate accepted work using real typed signal values, availability and authored conditions.
2. Give the existing TransformerManager an operational queue capability sharing its original evaluator, append protocol and access-policy owner.
3. Require earned dispatch to obey actual ownership, compatibility, capacity and operation restrictions; never manufacture outputs instantly.
4. Use one public recipe-reference derivation for recipe projections and native/direct dispatch content dependencies.
5. Prove hidden signal work waits for genuine condition changes, resumes earned progress, and hidden earned dispatch leads to timed physical production with real material accounting and durable continuation.

## 2. Design

Remove only the metadata predicate from signal physical evaluation. Add `ITransformationOperationQueue` bound to the same original queue owner. Player and operation admission call one private evaluator with explicit intent mode; only player mode requires entity/recipe metadata visibility. Both use existing `CheckAccess` and the same exact provider checks. Both append through the original verified queue mutation path.

Native/direct dispatch CheckStart/CheckSubject and Commit use the operational capability; CheckIntent remains player-facing. Existing physical-effect phase/identity/earned-time guards remain authoritative. Publish the existing partial TransformationContracts type and extract its current recipe-reference loop into one public stateless method, not another registry. Dispatch declarations retain the original resolved catalog alias and expose this exact recipe closure.

## 3. API surface

Small required engine-module capability `ITransformationOperationQueue.CheckEnqueueOperation/EnqueueOperation`. No new player command, host escape, state owner or queue. Public `TransformationContracts.ContentReferences` derives immutable actual references. Build changes; save/replay/view schemas unchanged.

## 4. Files to change

Original signal requirements, transformation contracts/declaration/disclosure/queue/module; direct dispatch proof; engine identity/protocol. Explicit JSON and one thin public-player case plus registration/profile. Existing recipe fixture gains JSON-only world edit support. Tracker/stage evidence and P5 permission wording.

## 5. Error handling matrix

Hidden new intent still returns UnknownDef. Unavailable or unsatisfied signals retain typed authored blockers. Real operation restrictions continue to block earned dispatch; queue checks never bypass policy. Refused append changes no queue/resources. Provider protocol violations still fault the session. No instant result, fallback or rollback fiction.

## 6. Backward compatibility

Development only; native and direct declarations migrate in place. No compatibility shim or alternate scheduler.

## 7. Verification

Build completed implementation, then run only the new bounded exact case. Preserve every failed build/runtime attempt. Profile-info is metadata. One authorized full regression remains deferred until implementation is complete.

## 8. Risks

Operational admission must omit only metadata restrictions, not ownership/access/real provider eligibility. Dispatch metadata must use the same resource/result closure as ordinary recipe projections. Worker/target signal conditions must continue to stop actual work when their real values are outside bounds.

## 9. Out of scope

No Tycoon behavior change, Oil code, rendering/platform/Draw or clock change. Broader provider faults/lockstep/stress/global gates remain open. P5.acceptance.009 wording is amended only to match the user's explicit mechanical-API permission; its checkbox remains open and every other original criterion is unchanged.

## 10. Sub-task execution order

1. Separate physical signal/dispatch eligibility from player metadata admission in original owners.
2. Share recipe dependency derivation with existing projections and native/direct effects.
3. Author target/worker and earned-dispatch physical proofs with normal policy commands and saves.
4. Record scoped evidence and permission amendment without inferring aggregate completion.

## Stage outcome

Scoped operational slice delivered: FINAL849passed/0failed/0aborted, original221/193checked/28open counts retained/additional255. [Evidence](../../docs/migration/P4_WORKFLOW_OPERATION_ACCESS_VERIFICATION.md). Initial metadata:1912source anchors/1090selected inputs/13quickstart anchors,0hash mismatch/drift/broken links. All221current criterion/line checks pass; only P5.009 wording reflects explicit human permission, no checkbox/line change or aggregate closure. All12Tycoon sources and sole clock unchanged. One final full regression remains deferred until implementation finishes.
