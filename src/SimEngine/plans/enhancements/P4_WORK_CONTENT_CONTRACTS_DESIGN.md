# P4-G8 — Open work requirement, executor and role catalogs

Status: scoped verified 2026-10-09, 75 focused checks; full P1–P12 remains open.

[Verification](../../docs/migration/P4_WORK_CONTENT_CONTRACTS_VERIFICATION.md). Executable inputs frozen 2026-10-09 03:48:11 UTC.

## Specification and goals

P4-G7 opened authored activity definitions, but node resolution and runtime admission still require WorkRequirementDef/ActivityExecutorDef/WorkerRoleDef. New JSON workflows must select actual contract content, not inherit native gameplay implementation classes.

1. IWorkRequirement and IActivityExecutor are file-owned IContentDefinition contracts. Requirement topology is declared through IDependencyNode and must exactly match actual group entries; the existing DependencyGraph compiler owns cycle/reference checks.
2. IWorkerRoleDefinition exposes the canonical typed role identity. All worker, node, policy, fleet, alert, staffing, dock and save consumers resolve this owned contract.
3. One WorkRequirementGroupDeclaration parses and binds all/any entries; native and independent definitions delegate to it. One StaffedActivityExecution owns existing authored approach/productive/departure behavior and parsing; native/direct executors delegate, never copy toils or worker logic. ActivityExecutorContracts owns worker-role validation. ResourceRequirementDeclaration and EntityResourceSourceDeclaration share native/direct resource parsing and source admission; endpoints alone own stock/claims.
4. Existing WorkBoard evaluation, reservation, progress, choices, resource claims, phase jobs and saves remain authoritative. No compatibility overload/alternate loader/copy runtime/second scheduler.
5. One NEW bounded player proof selects independent file-owned role, staffed executor, nested all/any groups, worker guard and consume/hold resource declarations through an explicit master document. It validates actual aliases, physical time/travel/material/tool accounting, persistent plans, private diagnostics, save/load/replay/session isolation and invalid references/topology/metadata before Initialize.

## Owners and files

Standard/WorkerRoles.cs owns typed role authoring/admission; Standard/ActivityExecutors.cs owns native execution implementations and staffed behavior; Standard/WorkRequirements.cs owns group declarations. Work.cs/WorkScheduling.cs/WorkActivityPrerequisites.cs keep the original graph/admission execution. Generic role/requirement consumers migrate in place, including proof callback compilation. Native definitions remain real implementations, not fallback instances.

A necessary fleet dependency metadata change uses IContentDefinition instead of Def so actual role contracts do not require an implementation base. Actual fleet state and behavior are unchanged.

## Stages and verification

Implement contracts/shared declarations; migrate consumers; author an independent module/document and thin player case; build; run ONLY the new exact bounded case (repair/rerun only it); freeze executable inputs; update primary trackers and machine audit without changing original acceptance text/checkboxes.

Target core2.340.0/save154/replay1; existing saved/system/view schemas and sole GameClock unchanged. No full/family/historical/Oil/Tycoon/platform runtime. Historical consumers compile only.

## Open gates and non-goals

Broader direct resource-source/executor strategies, provider malicious-callback/global purity/fault matrices, all original alternatives/fluid/tools/technology/areas, environmental breadth, lockstep/stress/soak/scale/full P1–P12 acceptance remain OPEN. No mandatory money, business expansion, Draw/host gameplay, compatibility reader, duplicate graph/claims/progress or second clock. Commit/push only after FULL verified goal AND plan completion.
