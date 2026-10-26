# P9-A11 — Company-private persistent blueprint library

Earlier M131 uses the existing board's actual owned/current visibility before interpreting
revision conflict; no replacement catalog or implicit revision repair:
[current recurring-library design and scoped proof](P9_RECURRING_LIBRARY_DESIGN.md#current-m131-actual-selection-disclosure-and-shared-player-admission--2026-10-23).
Two literal finals615/0 on2.764 do not rerun the historical broad gates below.

Current M134 qualifies discarded receiver inputs in the existing selector:
[existing recovery design](P9_RECOVERY_DESTINATION_POLICY_DESIGN.md#current-m134-discarded-receiver-dependencies-through-operating-areas--2026-10-23).
Two literal411/0 results on unchanged2.765 do not rerun the template bodies recorded here.

Historical checkpoint. Original callbacks are included in the current
[A12 recurring library gates](P9_RECURRING_LIBRARY_DESIGN.md). Current identities are recorded
there; the stage numbers below remain original evidence, not an additional regression run.

Status: focused implementation verified on 2026-10-07. Build2.179.0/save97/board31/work31/
work-view59/construction-view4. Clean build;626 assertions/two exact template suites (352 contracts;
274 physical), zero failures/aborts. [Evidence](../../docs/migration/P9_PLAN_TEMPLATE_LIBRARY_VERIFICATION.md).
Extends approved P9, not completion of P1–P12.

## 0. Problem

Standard/WorkPlanTemplates.cs already owns capture, provider validation, expansion and physical
placement, but captured player designs have no durable company library. Commands/PlanTemplateCommands.cs
only carries a snapshot. Hosts must not own library gameplay or resolve mutable selections themselves.

## 1. Goals

1. Store, revise, rename, remove and read immutable private designs through IPlanBoard and WorkView.
2. Resolve a stored design at an explicit expected revision in the existing placement command.
3. Use the same PlanTemplate JSON, provider validation, DAG and whole-group execution admission.
4. Preserve submitted plans when their source design changes or is removed.
5. Save/hash/reload pending commands and library state once, with no independent clock or executor.

## 2. Design

WorkBoard owns actual stored records with monotonic long IDs, company, unique per-company name,
positive revision and immutable PlanTemplate. IDs never recycle after deletion. Update/remove require
the exact current revision; no last-writer-wins or missing-record creation. Create explicitly uses
ID/revision zero. Structural storage validation reads every provider entry but does not create sites,
consume stock or require an anchor. Whole physical placement still runs ordinary actor admission.

PlanTemplateSelection is immutable explicit snapshot-or-stored command intent. Stored selections
carry an ID and positive expected revision. PlacePlanTemplateCommand replaces its old Template field
with Selection, resolved by the board before calling the existing Preview/PlacePlanTemplate owner.
No alternate placement command or scheduler. Library views are detached immutable records and expose
only the viewing company's designs. Existing master PlanTemplateDef remains the immutable authored
catalog; library records are player gameplay state using its same schema, not another content loader.

The existing board Sync embeds the library codec, validating canonical IDs, revisions, ownership,
unique names and provider content on both writing and reading. Invalid records fail explicitly.
Build/save/board/view identities migrate directly; no old wire/save reader or compatibility facade.

## 3. API

IPlanBoard: PreviewStorePlanTemplate, StorePlanTemplate, PreviewRemovePlanTemplate,
RemovePlanTemplate, ReadStoredPlanTemplate, CopyStoredPlanTemplates and ResolvePlanTemplate.
WorkView delegates management previews, immutable listing/read and selection-based placement preview.
StorePlanTemplateCommand returns actual ID in Arg0 and revision in Arg1; remove changes the library
only. PlacePlanTemplateCommand retains normal physical plan results and uses PlanTemplateSelection.

## 4. Files

Standard/PlanTemplateLibrary.cs, WorkPlanTemplateLibrary.cs; existing PlanContracts, Work.Sync,
WorkModule, Views/WorkView, Commands/PlanTemplateCommands, Standard/PlanPlaybooks, engine identity;
existing command callers and PlanTemplateTests callbacks; stage/evidence/tracker/README/AGENTS/help.

## 5. Errors

Missing/foreign ID: NotFound without leaking ownership. Stale revision: typed revision conflict.
Duplicate private name: typed name conflict. Invalid identity/name/template/space: InvalidArgument.
Missing definition/unsupported provider: existing typed codes and per-entry diagnostics.
Overflow: IdentityRange. Late entry failures commit no record or partial placement.

## 6. Compatibility

Direct development revision. No legacy parser, optional fallback source, ID reuse or copied execution.

## 7. Verification

Clean solution build. Only the two existing exact suites:

- Recurring templates: authored groups, private admission and cycle ownership
- Recurring templates: physical projects, cancellation and saved continuation

Append library assertions for immutable capture, JSON/wire, private ownership, stale and late refusal,
monotonic IDs, saved command inbox, pure previews, strict corrupted state, real finite-stock construction,
source-edit/removal independence, cancellation, pause and saved interleaved continuation.
Other suites compile only; no full or broad-family run.

## 8. Risks

Revision checks must occur at command execution, not host selection; IDs must survive deletion to
prevent queued commands selecting replacement records. Provider callbacks must remain pure. Existing
normal placement, not library storage, owns resource/target admission and actual execution decisions.

## 9. Out of scope

Automatic recurring-source replacement with library selections, active graph rewrites, rotation,
survey/reference-policy expansion, history retention, global purity/replay/performance/full-roadmap
acceptance. No OilFieldDays, Tycoon, host/demo gameplay, platform adapter or Draw changes.

## 10. Stages

- [x] A11.1 Private records, revision commands and same-board admission/selection/codec.
- [x] A11.2 Direct placement callers/view/schema migration and focused library proof.
- [x] A11.3 Exactly two gates, scoped evidence and authoritative tracker reconciliation.
- [ ] Recurring-source library selection/revision policies, wider providers, lifecycle history and full-roadmap acceptance.
