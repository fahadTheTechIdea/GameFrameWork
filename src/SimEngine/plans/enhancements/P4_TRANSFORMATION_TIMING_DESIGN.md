# P4-G20b — One transformation timing owner and independent continuous execution

Status: scoped verified, 2026-10-09; core2.355.0/save154/replay1. FIRST/FINAL58/0/0 in10761ms;
clean7.74s build;39 executable inputs frozen07:36:17UTC.
[Evidence](../../docs/migration/P4_TRANSFORMATION_TIMING_VERIFICATION.md).
Full P1–P12 OPEN, no staging or publication.

## 1. Problem and measurable goal

ITransformationDefinition.RequiredTicks permits content providers to override the same duration that
TransformationSnapshot already converts through the original session ISimulationTime. Execution,
composition and inspection can therefore disagree. Duration must be authored data; conversion must
remain solely the selected session time owner.

Remove the duplicate method from recipe contract, shared declaration, native definition and public
independent proof. Replace existing execution/composition/inspection calls with world.Time.Ticks(Duration).
Existing activity graph captures authored duration into original immutable nodes already; do not
expand this slice into activity changes or add a conversion helper/clock/compatibility method.

## 2. Owners and files

Standard/TransformationContracts.cs and Transformations.cs; ExtensionProof/TransformationContentContractsProof.cs.
Mechanically adapt three legacy-test compilation references in SessionTimeTests.cs to original
SimulationTime.Ticks(shared.Duration), without running that historical case.
Reuse thin authoring/player helpers from TransformationContentContractTests; no copied simulation rules.
Increment build identity, retain save154/replay1 and original state/view schemas.

## 3. New exact proof

ONE NEW case: `Transformation timing: one session converter and independent continuous physical flow`.
Author ninety-second batch through existing JSON/master, exercise minute and ten-second sessions.
Normal commands/physical supply, timed WIP, matching Required facts, partial save/load twins,
pause, single entity result and material/tool accounting. No stock/progress writes.
Use the existing master-listed continuous metadata row as authored input to the same actual recipe kind,
with no new workflow/model/queue/system. Exercise native flow/rates/tool wear/operating inputs,
physical supply, saved twins, pause, cancellation and canonical replay through public commands/views.
Bound every stage and diagnose actual clock/phase/blocker/cargo/stock. One new case repairs only;
no old/family/full/Oil/Tycoon/platform runtime.

## 4. Constraints and remaining gates

Subsequent [G21](P4_PROCESSOR_CONTENT_CONTRACTS_DESIGN.md) removes original processor's concrete native
metadata dependency and narrowly proves independent authored processor kinds reusing the same physical
runtime, with content-wide metadata validation. This G20b case was not rerun on2.356.0.

No business/default changes, Tycoon/Oil/platform/Draw work, duplicate rate math/catalog/clock or shim.
Resource catalog type bindings await recorded user scope authorization. Arbitrary provider callback
faults/metadata mutation, runtime network/staffing/technology/knowledge, endpoint loss/concurrency/
lockstep/shuffling/fuzz/soak/performance/global/full acceptance remain open.
Skill lifecycle directs staged design/code/proof/docs; focused user/repository constraints take precedence.
Commit and push only after genuine full goal AND plan completion and verification.

[Metadata stage](P4_TRANSFORMATION_METADATA_DESIGN.md), [parent](P8_TRANSFORMATIONS_DESIGN.md),
[tracker](MASTER_TRACKER.md).
