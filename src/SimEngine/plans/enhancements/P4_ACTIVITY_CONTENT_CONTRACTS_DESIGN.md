# P4-G7 — Open authored activity content

Historical proof on2.339.0; not rerun on2.340.0. [P4-G8 work catalogs](P4_WORK_CONTENT_CONTRACTS_DESIGN.md) replaces remaining native requirement/executor/role lookups with actual content contracts and shared group/resource/staffed owners. [Current 75-check scope](../../docs/migration/P4_WORK_CONTENT_CONTRACTS_VERIFICATION.md); wider provider/runtime/full acceptance remains open.

Status: scoped verified 2026-10-09, 84 focused checks; full P1–P12 remains open.

[Verification](../../docs/migration/P4_ACTIVITY_CONTENT_CONTRACTS_VERIFICATION.md). Executable inputs frozen 2026-10-09 03:32:06 UTC.

## Specification

Before this stage, ActivityDef was required by native catalog capture, previews, bound construction/infrastructure, incident workflows, transformation staffing, knowledge visibility and technology unlocks. These consumers now resolve IAuthoredActivityDefinition; native classes remain implementations. The same graph/execution and physical owners are preserved.

Introduce IAuthoredActivityDefinition over the existing activity/player/target-plan interfaces and IContentDefinition, including current orderability/target metadata. IActivityDefinition remains the graph/execution contract for bound construction/component/service plans that are not standalone file definitions. No parallel graph or intent model.

## Design and stages

1. Extract existing ActivityDef JSON nodes/settings parsing/resolution to one ActivityDeclaration, compiling the same ActivityGraph/ActivityNodeDef exactly once. Native and independent declarations use it. Extract existing player intent/target methods to one ActivityDefinitionContracts owner; native virtual methods delegate and keep existing specialized overrides.
2. Native activity catalog captures actual IAuthoredActivityDefinition aliases, not native inheritance. Admit actual content/plan identities and metadata after content resolves before Initialize; bound IActivitySource declarations and WorkBoard lookup/state remain the same owner.
3. Migrate all ActivityDef consumers to the authored contract: WorkView, construction, lines, incident content, staffing, unlocks and visibility. Native registrations/subclass implementations remain implementations. No alternate selector/executor/requirement registry.
4. ONE NEW bounded thin-player case uses explicitly master-listed direct activity JSON with no ActivityDef base; native construction/incident/player commands execute it with dependencies, parallel branches, exact material/tool requirements, physical worker travel/time, cancellation, saves/replay/private previews and malformed graph/content rejection. Reuse existing proof authoring/driver helpers.
5. Freeze executable inputs and reconcile stage/master/audit with scoped evidence. Original acceptance text/checkboxes unchanged.

## Protocol and limits

Core2.339.0/save154/replay1; all existing state schemas and sole GameClock unchanged. Remaining requirement/executor/role catalog contract migrations, arbitrary bound-provider/plan fault matrices, global purity, broad lockstep/soak/scale/full P1–P12 acceptance remain open.

Build as needed; only ONE NEW exact bounded runtime case, repair/rerun only that case. No broad/full/historical/Oil/Tycoon/platform runtime, compatibility reader, duplicate graph/progress/reservation/intent, mandatory money, business expansion, Draw/host gameplay or second clock. Commit/push only after FULL verified goal AND plan completion.
