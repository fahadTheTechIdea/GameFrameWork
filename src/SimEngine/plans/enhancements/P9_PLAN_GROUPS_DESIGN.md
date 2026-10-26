# P9-A3a — Engine-owned whole-group plan controls

Status: focused native slice verified on2026-10-05. Build2.85.0/save55/work-view28; native Work22
and other state layouts unchanged.102 checks/two exact suites (52 contracts;50 physical), clean build;
[evidence](../../docs/migration/P9_PLAN_GROUPS_VERIFICATION.md). Full P1–P12 goal remains partial.

## 0. Problem

WorkPlans.cs stores Group as intent metadata only. Hosts currently need repeated commands to operate
a group and cannot obtain whole-group preflight/per-entry refusals. Extend the same board/records,
not another blueprint/project store, scheduler, group codec or host rule loop.

## 1. Goals

1. Submit a nonempty immutable group set in one ordinary command with per-entry diagnostics.
2. Admission validates all entries, current live duplicates, within-set provider identities and ID range
   before any plan/claim/entity/order allocation. No partial ordinary refusal.
3. Configure live group priority/pause, cancel live members, and reorder an entirely pending live group
   through existing individual execution semantics after whole admission.
4. Repeat an entirely terminal group into an explicit fresh group with fresh plan/order identities.
5. Private views and saved commands/continuations preserve stable list order and actual physical state.

## 2. Design / ownership

IPlanBoard extends in place. WorkPlanGroups.cs is another partial WorkBoard file; _plans Group fields
remain the sole membership authority, scoped by actual company. No persistent group records, index,
priority or pause copies. Individual regroup edits immediately affect derived group selection.
Group query returns current PlanViews from the same projection and applies company privacy.
PlanGroupCheck owns only detached per-entry Plan ID/refusal facts plus first overall blocker.
PlanGroupIntent captures nonempty explicit group and ordered immutable PlanIntents; every entry must
explicitly name that same group. New groups are fresh within company, including retained history;
another company may use the same label. Repeat requires explicit different fresh destination group,
avoiding ambiguous mixed generations. Actual live uniqueness remains provider-defined intent identity.

Shared single-plan submission admission remains authoritative. Group submission additionally checks
joint duplicates and counter range before calling those same native creation paths. Applied operations
recheck current admission; changed committed assumptions fault Update, no rollback/partial success fiction.
Configure/cancel acts on live members; terminal history is unchanged. Entire terminal groups refuse these
operations. Move requires every live member Pending and a distinct owned pending Before or explicit0
for tail; removing/reinserting existing list records preserves member order, IDs and all other rows.
Repeat requires every member terminal and reuses full submission checks for newly authored intents.

Existing jobs, node progress, paid/spent inputs, promises/cargo, retry/claim relations and native Order
priority remain authoritative beneath group pause/cancel. Group promotion stays per-entry native intent
admission: whole submission is not a claim that every physical placement activates simultaneously.
No ghost sites, construction clones, host supply/cancel decisions or instantaneous work.

## 3. API

Add Submit/Configure/Move/Cancel/RepeatPlanGroupCommand under existing command registry.
IPlanBoard PreviewPlanGroup*, mutations and CopyPlanGroup extend the current service alias; WorkView
adds matching private projections. Submit/repeat results expose first allocated plan ID and entry count;
CopyPlanGroup reports actual full IDs/status/blockers. No second command result codec.
PlanIntent.Sync becomes the single command payload serializer reused by single and grouped submission;
the native plan codec still owns active priority aliasing/history and is not replaced by copied intent state.

## 4. Files

Extend PlanContracts.cs, WorkPlans.cs, Commands/PlanCommands.cs, WorkModule.cs, Views/WorkView.cs;
add WorkPlanGroups.cs, Commands/PlanGroupCommands.cs, focused PlanGroupTests.cs and master-listed proof JSON through existing
PlanProofModule. Update stage/master/parents/README/AGENTS/help/evidence together.

## 5. Refusal / fault rules

Unknown/empty/foreign groups disclose no member IDs. Invalid settings, mixed active reorder, live repeat,
existing destination group, any malformed/provider-refused/duplicate intent or exhausted identity range
refuses the whole command without allocation. Per-entry results retain input order and stable IDs.
No hidden count cap. Provider violation during applied re-admission faults normal Update, cannot checkpoint.

## 6. Compatibility

Direct API/content migration; no wrapper/fallback/shim. Advance engine/save/work-view identity for new
command table and views. Work22/native plan layout unchanged: group membership already lives there.

## 7. Verification

Build Beep.SimEngine.slnx. Only these two exact suites (repair repeats allowed):

- `Plan groups: whole admission, ordering and private contracts`
- `Plan groups: physical pause, cancellation, repeat and saved commands`

Test late invalid entry/no prefix, joint duplicates, current membership edits, priority/pause, ordering,
mixed terminal/live semantics, repeat freshness, physical supply/material/tools/travel/paused work/cargo,
private detached views, saved inbox and isolated twins. Bound physical stages with diagnostic snapshots.
Other suites compile/migrate only, no full/family/third run.

## 8. Risks / exclusions

Grouping does not bypass native per-entry placement/work admission or guarantee synchronized activation.
Templates/relative provider geometry/cross-links/automatic repeat/new plan provider kinds, broad provider
fault matrices, replay/stress/full P9 and P1–P12 acceptance remain open. No Oil/Tycoon/Draw/platform work.

## 9. Execution

- [x] A3a.1 Contract/whole admission and sole command serialization.
- [x] A3a.2 Existing-record group controls/private views and actual physical integration.
- [x] A3a.3 Two exact suites and current evidence/docs; full goal remains active.
- [ ] Later templates/provider geometry/cross-links/automatic repeat/provider/replay/stress/full gates.
