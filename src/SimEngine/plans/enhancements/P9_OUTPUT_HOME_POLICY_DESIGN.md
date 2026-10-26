# P9-PBH1.H — Explicit authored transformation output homes

Subsequent unchanged2.660 [P8-IB1](P8_INDEPENDENT_INVENTORY_BASELINE_DESIGN.md)
resolves the combined case75/16 through correct independent fixture authoring
and current validation/cancellation boundaries; SAME case98/0. [All evidence](../../docs/migration/P8_INDEPENDENT_INVENTORY_BASELINE_VERIFICATION.md).
The non-green record below is historical; broader goal/native fleet stays OPEN.

Status: focused implementation verified on2.649.0/save160/replay1/view24.
Seven literal final cases1318/0; selected combined transformation case remains
non-green75/16. [Every attempt, identities and limits](../../docs/migration/P9_OUTPUT_HOME_POLICY_VERIFICATION.md).
Parent: [producer/berth/home stage](P9_PRODUCER_BERTH_HOME_DESIGN.md) remains OPEN.
Original221 requirements /209 recorded checked /12 open remain unchanged.

## 0. Problem and specification

`Transformer.Deliver` currently assigns `queue.Home` or implicitly chooses the
producer. The queue/preview does not record that latter choice. Definitions and
independent processors therefore lack one shared explicit output-home protocol.
This stage removes that inference, rather than adding a compatibility path.

## 1. Goals

1. Actual `ITransformationDefinition` metadata declares the output-home policy
   from its existing master-listed recipe JSON; native and independent definitions
   use the same strict declaration/validation.
2. Queue admission resolves authored policy and explicit player home override
   through one guarded engine protocol. Existing queue.Home stores that actual
   resolved identity; saves, hashes, replay and immutable queue views need no
   duplicate state. Missing required homes refuse without queue/stock mutation.
3. Completion revalidates the retained identity before resource conversion and
   creation, never substitutes another home after removal/ownership loss, and
   commits the worker binding through the existing WorkerHomeContracts owner.
4. Nonworker results cannot accept meaningless home intent. Missing, invalid or
   inapplicable authored policy fails before Initialize, including unused recipes.
5. Exercise physical native and independent outputs, previews, repeats, saved
   pending/loaded/WIP state, replay, company privacy and refused completion.

## 2. Design / existing owners

`TransformationOutputHome` has Unassigned, Producer and Required values.
`ITransformationDefinition.OutputHome` is nullable solely for recipes without
entity output. Entity-output recipes must explicitly author `output_home` as
`unassigned`, `producer` or `required`; Producer/Required require actual worker
output metadata. Resource-only/continuous recipes have no output-home policy.
There is no missing-key default or old-reader alias.

`TransformationDeclaration` owns JSON parsing and actual definition validation.
`TransformationContracts` owns resolving output-home intent, metadata/actual-home
validation and the shared created-result binding boundary. Existing
TransformerManager preview/admission/append checks pass the resolved identity
into the actual provider queue and verify the exact append as before. Existing
queue codecs retain that ID, including when its target is later removed.

Native idle finite repetition resolves its explicit recipe policy through the
same protocol before appending; Required without a supplied home cannot become
an implicit producer assignment. Repeat entries preserve their already selected
home. No new clock, queue, home store, dispatcher, resource receipt or scheduler.

Native and independent completion check the SAME retained target before creating
results. Existing WorkerHomeContracts commits the sole binding; no raw setter
implementation is copied into processors. Normal absence of a selected home
means Unassigned, not a hidden producer assignment. Provider faults remain fatal,
not compensated or silently downgraded to healthy refusals.

## 3. API and JSON

Recipe example: `"entity_output":"worker", "output_home":"producer"`.
`"output_home":"required"` requires an explicit valid owned home in the normal
enqueue command. `unassigned` allows no automatic home, while an explicit valid
worker-home override remains normal player intent. Invalid numeric home IDs,
inapplicable targets and unsupported spatial/motion metadata refuse explicitly.
Immutable execution previews expose the authored policy; queue facts expose the
resolved home. No host reproduces this decision.

## 4. Files

Original Standard/TransformationContracts.cs, Transformations.cs, WorkerHomes.cs,
Views/TransformationExecutionPreview.cs; independent definition/processor proof,
explicit authored recipe JSON/callers, focused tests, protocol identity and docs.
Mechanical migrations preserve existing authored amounts/times/business rules.

## 5. Errors

Missing Required home: typed `transformation.home-required`. Missing retained
target: NotFound; changed ownership: NotOwned. Nonworker home/inapplicable policy:
InvalidArgument/content error. Unsupported motion/spatial metadata: existing home
Unsupported. Completion refusal retains actual WIP/tools/queue/progress and no
result is published. Invalid provider callbacks latch the original session fault.

## 6. Compatibility

Development only. No legacy/default reader or header rewrite. Bump engine and
view identity for the changed public semantics; existing payload slots remain
authoritative. Older native saves stay preserved historical evidence.

## 7. Verification

Targeted Release build, then separate literal bounded cases for policy admission,
physical completion/refusal, independent provider behavior and existing parking
integration. No full/profile/family run. Match claims to selected cases only.

## 8. Risks

Do not resolve a lost retained home again as producer. Do not allow custom
processors to append unresolved homes or mutate prefix/controls/costs. Keep the
original pure-callback observation/accounting boundaries. Prospective connected
rollout/egress remains the next parent-stage work, not proof inferred here.

## 9. Out of scope

Rendering/adapters, new economics, easier canonical geography/work times, host
steering, world-wide rollout shortcuts, broad tests and Git publication.

## 10. Execution order

1. [x] Audit declaration, actual queue/append guards, native/independent result
   creation and immutable views; specify shared ownership above.
2. [x] Implement contracts/declaration/resolved admission/completion and migrate
   all affected authored content without hidden defaults.
3. [x] Focused physical/durability/refusal/provider checks and explicit identity.
4. [x] Update parent/master/evidence; keep full P9-PBH1 and P9.051 open until
   connected rollout, canonical bindings and full acceptance are proved.
