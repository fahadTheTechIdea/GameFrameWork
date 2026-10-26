# P9-L2: Request-scoped physical dock slots

Status: stages 1–3 implemented and focused-verified on engine2.605.0/save159/replay1.
Stage 4 subsequently completed in [P6-I2](P6_INDEPENDENT_CONTROL_INTEGRATION_DESIGN.md),
final128/0 full control scenario. Original broad gates/full goal remain open.
[P9-L2 attempts and limits](../../docs/migration/P9_REQUEST_SCOPED_DOCKS_VERIFICATION.md).

## Problem and goals

The current independent research/control scenario stalls because one-dimensional
machines cannot pass. Inspection also exposes a genuine authoring limitation: dock
eligibility applies to the whole endpoint, not individual work/resource bays.
Preserve all machines and physical occupancy; do not merge roles, teleport, extend
deadlines or introduce a second dispatcher to make an acceptance test pass.

Extend the existing immutable WorkDockSlot contract with optional resolved role or
resource restrictions, authored in JSON as work_role/resource (mutually exclusive).
An unrestricted slot inherits its endpoint's existing authored request admission.
All service/wait/departure selection, journey continuation, promotion and active
revalidation must use the same predicate. Existing jobs own all claims and progress.

## Design and stages

1. One shared WorkDockSlotDeclaration reads/resolves identity/kind/request metadata
   for native tile and independent point docks. No duplicated parser or compatibility
   constructor. Existing geometry readers retain offsets, not request policy.
2. Existing WorkBoard selection/queue/routing honors resolved slot filters. Validate
   filters against endpoint roles/resources and require compatible service/exits.
3. Strengthen the existing authored atomic-haul case with a closer work-only bay
   that must never receive a resource haul; preserve full physical saved continuation.
   Checkpoint and restore validation must reject a commitment incompatible with its
   actual request, including retained recovery bays. Negative corruption belongs
   only in explicitly marked test instrumentation, never external game modules.
4. Follow up by authoring separate extractor/hauler/builder bays and parking in the
   complete Planet X control scenario. That scenario's initial failure remains open
   until its full research/automation/incident/manufacturing/build chain actually passes.

## Ownership and API

Same IWorkDockDefinition.Slots and IWorkDock; immutable request metadata only, no
new queue, entity state machine, reservation ledger, clock or platform layer.
WorkDockSlot.Matches(WorkDockRequest) is the sole slot predicate. Existing global
provider admission still applies. Static metadata is saved/hashed through the
original self-describing content manifest; live claims retain original slot IDs.

## Verification

Focused contract/malformed-content checks plus the literal atomic acquisition case,
including per-update stock conservation, strict spatial ordering, saved twins and
full physical departure/home completion. Native/independent readers share the same
declaration. Run compiled external boundary and a native tile physical case as
relevant checks, not full/profile/family suites. Record every attempt.

## Remaining scope

Complete control scenario was required rather than replaced by this prerequisite;
it is now focused-verified in P6-I2. No Oil/Tycoon/platform/host logic, compatibility path,
arbitrary world-size limit or Git publication. Full goal/original broad gates stay open.

## Stage checklist

- [x] L2.1 — Shared strict JSON declaration/resolution and immutable request predicate;
  native tile and independent point providers retain geometry ownership.
- [x] L2.2 — Existing selection, journey, queue promotion, active binding and checkpoint
  validation use that predicate, with no alternate dispatcher or claim ledger.
- [x] L2.3 — Focused metadata/refusal/corrupt-binding checks and full physical
  two-carrier then staffed-work saved continuation; native construction regression.
- [x] L2.4 — Author feasible independent full-control layout and complete original
  research/automation/incident/manufacturing/construction acceptance (P6-I2,128/0).

No compatibility constructor, duplicate metadata parser or saved payload was added.
Static policy is part of the original content manifest; build identity changed,
native save159/replay1 and existing live slot-ID fields remain unchanged.
