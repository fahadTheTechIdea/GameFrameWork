# P9-B3 — Authored company alert policy

Current M92/native2.730/save161/replay2: original CompanyAlertView.Access and list preflight stop
cached post-fault reads before valid/private/missing/invalid shortcuts, with cleared outputs.
Actual105/42 reproduction, final147/0 plus repeated physical78/0/two complete replays.
[Actual view-only repair and physical policy proof](../../docs/migration/P10_COMPANY_ALERT_AUTOMATION_VERIFICATION.md#current-m92-cached-alert-cutoff-and-repeated-physical-emergency-recovery--2026-10-22).
Same policy/dispatch/reserve/clock owners and saved payloads; no new policy service or broad gate.

Status: focused implementation verified, 2026-10-05. Parent [P9](P9_PLANS_POLICIES_LOGISTICS_DESIGN.md).
Build2.60.0/save32/work14/work-view16/logistics4/logistics-view8, optional policy1/view1.
**120 checks / two exact suites**, zero warnings/errors/failures/aborts. No broader gate ran.
[Evidence](../../docs/migration/P9_COMPANY_ALERTS_VERIFICATION.md).

Historical B3 gate, NOT rerun on current HEAD. Follow-on [P10-B7f automation](P10_COMPANY_ALERT_AUTOMATION_DESIGN.md)
uses the same alert owner/normal command, never copied policy state or a proxy entity. Current
owner codec3/view4; native incident/physical repair/deferred alert restoration proof scoped separately.

## Problem and outcomes

Original B3 added company-wide Normal/Urgent/Emergency policy to the existing combined work/haul
arbitration. M92 retains that single owner and closes cached-view fault admission, not another dispatcher.

1. Optional sim.company-alerts selects explicit company/profile/level bindings through existing
   ScenarioDef.SettingsFor and master-listed JSON. No implicit profile, level or priority adjustment.
2. ICompanyAlertPolicy owns only current settings/version/change tick through one service Sync.
   CompanyAlertProfileDef is an open authored definition contract in existing content base indexes.
3. JSON rules match declared dispatch scope (or explicit null for all) and actual emergency class.
   Each level explicitly authors its rule list, including empty lists for intentionally neutral policy.
   Signed integer adjustments accumulate with checked 64-bit priority; raw intent priority stays untouched.
4. WorkSystem applies current derived effective priority in its sole combined pass, after actual readiness/
   emergency class and before age/cost/stable identity. No copied policy score on jobs or requests.
5. Preview/command/private immutable views share settings validation. Saves/hashes/replay commands carry
   the same settings. Changing alerts wakes assignment, not physical mutation, refunds or job cancellation.

## Design, contracts and files

Standard/CompanyAlerts.cs: CompanyAlertLevel, immutable settings/snapshots/rules, ICompanyAlertPolicy,
ICompanyAlertProfileDefinition, abstract CompanyAlertProfileDef and JSON AuthoredCompanyAlertProfileDef.
CompanyAlertPolicies is the sole session owner. Scenario settings list companies explicitly; every
selected seat needs a binding. Declared inactive seats within the scenario maximum may have authored
bindings, but do not create runtime companies or state. Unknown/duplicate seats or missing selected
bindings refuse before Initialize. Unknown profile/level/scope and duplicate rules refuse explicitly.

Standard/CompanyAlertModule.cs selects/binds the owner and declares JSON/command/view metadata. No
new system, scheduler, queue, job table, entity component or kernel PlayerTable policy field.
Commands/CompanyAlertCommands.cs writes company/profile/level through normal player ownership checks.
Views/CompanyAlertView.cs returns detached private settings/version/change tick and same-check previews.

Extend WorkerDispatchOffer with derived long EffectivePriority; its public source constructor reports
raw priority only. Internal ranking projection applies optional policy at read/assignment time, never
accepts a source-provided effective score. Existing source candidate/native acquisition rules remain
authoritative. Company alert does not relabel ordinary work as an incident or bypass emergency_eligible,
areas, disabled workers, stock reserves, receipts or routes. Acquired jobs continue and positive-duration
work consumes actual resources as before. Alert edits do not imply preemption or shutdown.

Existing save/work/logistics layouts stay unchanged; new optional service schema1 and view1 use normal
module metadata/codecs. Build and WorkView version migrate; no legacy reader or compatibility path.
Open profile subclasses enable engine-side game modules to author alternative rule content without
editing the scheduler. Profile/source discovery stays with ResolvedContent/ServiceContainer.

## Errors and verification

Malformed JSON, missing level arrays, duplicate/unknown scope references or initial bindings fail before
play. Commands refuse unknown company/profile/level and foreign ownership with the same private preview
code. Saved settings/count/version/age/profile references validate through the same owner. Checked
effective-priority overflow is a provider violation, never a wrapped score or silent clamp.

Two exact suites only: strict admission/private commands/derived ranking/optional composition; then
physical multi-role contention, alert edits while loaded/working, saved twins and interleaved sessions.
All physical waits bounded with job/state/blocker diagnostics. Public-only proof adds a profile subtype
through master JSON and existing contracts. No full/broad/third suite without explicit authorization.

- [x] Explicit Normal/Urgent/Emergency profiles and scenario bindings refuse invalid/missing references.
- [x] Current policy changes actual shared assignment; raw priority/emergency/age/native claims are preserved.
- [x] Private preview/command parity, source extension and derived checked priorities pass.
- [x] Acquired cargo/work survives alert changes, saves/continuation and isolated sessions.
- [x] Build/two suites/scoped checks and master/README/AGENTS/P9/P4 evidence handoff recorded.

Starvation promotion, automatic preemption, evacuation/return policy arbitration, operational shutdown,
fleet/docks/areas/plans/templates, wider provider fault/replay/stress/performance and full framework gates
remain open. No Oil/Tycoon/host/demo/platform/Draw implementation.
