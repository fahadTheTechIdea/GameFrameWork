# P12-A4 / P4-G6 — Open incident workflows and work-effect content

Historical proof on2.338.0; not rerun on2.339.0. [P4-G7 authored activity contracts](P4_ACTIVITY_CONTENT_CONTRACTS_DESIGN.md) now replaces the concrete ActivityDef response link and proves an independent five-phase authored DAG through the same physical owners. [Current scoped evidence](../../docs/migration/P4_ACTIVITY_CONTENT_CONTRACTS_VERIFICATION.md). Requirement/executor catalogs and full incident breadth remain open.

Status: scoped verified, ONE NEW exact bounded case77/0/0. Full P1–P12 remains open. [Verification](../../docs/migration/P12_INCIDENT_CONTENT_CONTRACTS_VERIFICATION.md).

## Problem and goals

IncidentDef and IncidentSignalDef resolve IncidentWorkflowDef rather than the existing IIncidentWorkflowDefinition. IncidentWorkflowDef alone validates concrete completion effect types. WorkBoard and authored activity consumers resolve WorkEffectDef despite IWorkEffect. This blocks file-owned independent workflows/completion effects from using the same native physical response.

Success: all work-effect consumers resolve actual IWorkEffect content; native/independent incident workflow JSON uses one parser and admission rule; completion metadata is a typed contract and invokes one engine-owned episode completion implementation; direct JSON extensions execute real work, not host-side repair.

## Design and stages

1. Extend IWorkEffect with IContentDefinition. Migrate engine effect resolution in WorkBoard, entity activities, technology and networks; native bases remain implementations only. Use existing scoped observation guards for effect preview/plan-validation callbacks, without a second state store or scheduler.
2. Introduce IIncidentCompletionEffect over IWorkEffect with explicit Source/BoundResponse metadata. Extract existing completion metadata parsing to one IncidentCompletionDeclaration. ResponseWork.ApplyCompletion performs original normal episode-bound completion; native and direct content implementations delegate to it.
3. Extract IncidentWorkflowDeclaration from IncidentWorkflowDef. Native/independent descriptors share kind/response/phase parsing and actual content aliases; resolve binds references only, then composition applies IncidentPhases validation after all content resolves. Resolve IncidentDef/IncidentSignalDef via IIncidentWorkflowDefinition. Validate every selected workflow during composition before Initialize, including independent implementations. Phase progress remains derived from WorkBoard nodes.
4. ONE NEW bounded public-player case uses master-listed independent workflow/completion JSON, normal incident/alert/work commands, physical travel/material use, immutable/private phases, cancellation/resumption, new episodes, pause, saved twins/replay/isolation and pre-Initialize malformed authoring. Reuse original shared thin authoring/input helpers; no raw world or stock/progress mutations.
5. Freeze executable inputs, record attempts and reconcile master/audit; preserve all original acceptance checkbox text/status.

## Files and errors

Core: Standard/WorkEffects.cs, Work.cs, EntityActivities.cs, Technology.cs, Networks.cs, IncidentWorkflows.cs, IncidentSignals.cs, Incidents.cs, IncidentResponseModule.cs and shared declaration source. Extension proof and explicit JSON sections/master document; one new test and protocol identity.

Missing/wrong workflow/effect references, duplicate/absent phase mappings, invalid kind, missing bound completion, replacement/node completion and player-orderable incident plans refuse before Initialize. The original graph owns cycles, physical work/roles/resources and once-only execution. Preview/validation callback violations use existing fault cutoff, not compensation.

## Verification and constraints

Build Beep.SimEngine.slnx as needed; run only the NEW exact bounded case, repairing/rerunning that same case. Core2.338.0/save154/replay1, unchanged existing state schemas/sole GameClock. No compatibility loader/native-base fallback, duplicate workflow/graph/phase progress, Oil/Tycoon/business expansion, mandatory budget, Draw/platform/host gameplay or broad runtime.

Activity/requirement/executor catalog migrations and exhaustive provider/fault/lockstep/soak/scale/full P1–P12 acceptance remain open. Full goal AND plan must genuinely finish and be verified before staging/commit/push.
