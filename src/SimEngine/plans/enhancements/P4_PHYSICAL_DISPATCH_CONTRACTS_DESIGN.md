# P4-G38 — Contract-owned physical manufacturing dispatch

## 0. Problem statement

Standard/TransformationDispatchEffect.cs still requires PhysicalWorkEffectDef inheritance to reuse
machine ancestry, actual-order admission, subject checks and completion. Its JSON declaration cannot
be reused by a direct independent Def/ITransformationDispatchEffect. WorkEffectContracts also allows
an Applied callback to change game-clock controls even though only the game owns time advancement.

## 1. Goals

1. One physical-effect contract/helper owns actual order, binding, physical ancestry and earned stage.
2. Native dispatch is a direct definition; an independent definition uses the same declaration/helper.
3. All loaded dispatch metadata, including unused content, validates before Initialize.
4. Final and node effects append ordinary production exactly once after physical preparation.
5. Manufacturing retains existing material/WIP/tool/queue owners and consumes time, without Tycoon.
6. Applied callbacks cannot change clock controls; legitimate target/identity commits remain allowed.

## 2. Design

Add IPhysicalWorkEffect with capability/subject/check/commit callbacks. PhysicalWorkEffectContracts
owns the original shared algorithm; existing physical implementations delegate through their normal
implementation base, which contains no duplicate algorithm. Native TransformationDispatchEffectDef
implements the contract directly. TransformationDispatchDeclaration owns strict JSON parsing and
actual recipe resolution. TransformationDispatchContracts validates metadata/target capabilities and
delegates current checks/commits to the sole ITransformationQueue.

Pure subject resolution/checks guard actual existing Entity.Sync plus scalar/provider/map facts.
The general effect boundary verifies unchanged clock reference/time/speed/pause for every result,
while Applied may mutate legitimate target/identity state. No rollback, second clock or provider store.

## 3. API surface

IPhysicalWorkEffect and PhysicalWorkEffectContracts.ValidatePlan/Check/Apply.
TransformationDispatchDeclaration.Read/Resolve and TransformationDispatchContracts helpers.
Existing work/intent/preview/queue contracts remain the authority; no new host API.

## 4. Files

Standard/WorkEffects.cs and TransformationDispatchEffect.cs; new public-only dispatch proof and JSON;
new thin player case/registration; build/protocol; this stage, evidence, master/audit and relevant docs.
Reuse ManufacturingDispatchTests.Factory as JSON authoring only; no copied simulation logic.

## 5. Errors

Missing references, invalid counts/home/repeat/capability and nonphysical ancestry fail composition.
Premature completion returns typed not-complete; foreign target refuses ownership. Actual queue
refusals use the existing retry profile. Provider mutation/exception faults, never a fake rollback.

## 6. Compatibility

Development only. No legacy reader/parallel engine or obsolete dispatch inheritance. Existing native
physical base remains an active shared implementation for other effect categories, not compatibility.
Save154/replay1 unchanged unless actual persistent payload changes; explicit build advances.

## 7. Verification

ONE NEW exact bounded case: Physical dispatch contracts: authored effects earn timed manufacturing.
Exercise native/direct independent final and node effects; private previews, cancellation, partial
and queued/WIP saves, sole-clock pause, sequential material accounting, interleaved sessions/replay,
invalid used/unused content and actual Applied clock fault. Only this new case may be repaired/rerun.
Compilation as needed; no historical/family/full/Oil/Tycoon/platform runtime.

## 8. Risks

Actual target/subject codec guards cost scoped hashing, not full-world hashing. Arbitrary unrelated
service/entity mutation, CLR sandboxing, rollback and every other effect category remain unproved.

## 9. Non-goals

Tycoon/G19 pending authority, Oil, hosts/demos/platforms/Draw, new queues/ledgers/timers, broad tests.
Do not stage/commit/push before the full original goal/plan is complete and verified.

## 10. Sequence

Inspect existing owners; extract shared physical algorithm; direct native declaration/dispatch helper;
author independent proof and one player case; compile/freeze/execute; refresh scoped tracker/evidence.

## 11. Status

Implemented and scoped-verified on2.376.0/save154/replay1. FINAL200/0/0 in2600ms.
220 selected executable/content inputs frozen2026-10-09 14:08:04 UTC before final execution.
[Evidence](../../docs/migration/P4_PHYSICAL_DISPATCH_CONTRACTS_VERIFICATION.md).
Full P1-P12 remains OPEN; original gate text/checkboxes unchanged. No publication.
