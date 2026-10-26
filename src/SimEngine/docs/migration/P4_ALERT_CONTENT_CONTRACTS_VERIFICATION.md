# P4-G11 — Company alert content contract verification

Date: 2026-10-09. Scoped verified only; full P1–P12 and the active goal remain open.

[Stage](../../plans/enhancements/P4_ALERT_CONTENT_CONTRACTS_DESIGN.md).

## Actual ownership and implementation

ICompanyAlertProfileDefinition now extends IContentDefinition. Initial settings, CompanyAlertPolicies admission/priority/restored settings and CompanyAlertControlActionPolicyDef dependencies resolve actual owned profile aliases, not CompanyAlertProfileDef. Native implementations remain content implementations, not required bases for extensions.

CompanyAlertProfileDeclaration owns the sole original JSON rule reader, canonical ordering, dispatch-scope collection and additive priority adjustment. Native and direct Def implementations delegate to it. Composition validates every actual profile, including unused profiles, against explicit canonical unique scopes and selected dispatch providers before Initialize. Settings admission and priority callbacks use the existing scoped provider observation guard.

CompanyAlertPolicies remains the sole settings/version/changed-time/reserve owner (schema3). Existing WorkSystem, automation order, normal ConfigureCompanyAlertCommand, response DAG, jobs, reservations, stocks and GameClock retain all execution/state. No separate priority cache, scheduler, profile registry or clock was introduced. The proof module adds content registration and the existing diagnostic Initialize probe only; the player driver submits commands and reads immutable public facts.

## Current evidence

- Core beep-simengine-2.343.0; save154/replay1/work-view95. Existing state codecs, alert view4 and sole GameClock unchanged.
- Three clean solution builds:8.44s,6.04s,3.94s; zero warnings/errors. No failed build or stale binary.
- ONE NEW exact bounded case: Alert content contracts: independent JSON priorities drive deferred emergency and physical response.
- Same-case attempts:67/0/0 (692ms,PID35640,exit0,observed848ms); expanded proof25/2/0 (873ms,PID18688,exit1,observed1015ms); FINAL70/0/0 (680ms,PID36332,exit0,observed825ms).
- Runtime30000ms, termination2000ms, output24000chars; all outputs untruncated/no observer error. Physical waits use the existing700-Update bound and immutable diagnostics.
- Executable inputs frozen 2026-10-09 04:39:36 UTC; no subsequent gameplay edits.

The expanded proof initially waited for HasApplied after raising an incident before configuring automation. That correctly allowed the first true/emergency decision to apply, so the subsequent pending-decision assertion was invalid. The driver now observes the actual pending action before application. No automation timing, alert, work, reserve or response behavior was changed to satisfy the proof.

## Proven behavior

Master-listed alert-contracts.json authors direct independent profiles; the same resolved catalog admits them through ordinary scenario bindings and automation action dependencies. Immutable settings remain private. Wrong-kind profile previews refuse without changing the hash. Ordinary commands change urgent/normal settings in the existing owner.

Real emergency offers after incident detection get no boost from a normal rule authored only for nonemergency work. An urgent matching rule produces exactly+17 through the actual shared queue, with a pure query. Emergency rules combine a wildcard+200 and matching sim.work+300 into exactly+500. Deferred automation waits for authored dwell and the next Update; sole-clock pause retains the pending intent and all material. An emergency priority is not a repair or a teleport.

Normal work-policy enable admits native physical response. The machine travels from4 to20, starts productive repair and consumes8 supply units20-to12. Native timed work resolves the incident. Healthy-signal restoration waits for dwell and a later Update, restoring the original independent profile with exact version chronology and no repeated consumption. Retained facts, unchanged other-company policy, saved pending/applied/physical/settled twins, command replay and fresh interleaved session isolation pass.

Fourteen invalid variants each refuse before Initialize: missing normal/urgent/emergency arrays; duplicate rule; malformed/wrong-type scope; wrong-type emergency flag; missing adjustment; unknown bound scope; unknown scope on a third unused profile; initial and automation wrong-kind/missing profiles. The unused-profile case proves validation is not limited to active company settings.

## Limits

Historical native/extension callers compile but were not rerun. Arbitrary custom-provider mutation/throw/reentry/overflow matrices, full starvation/priority contention, all reserve policies, every scope, broad lockstep/performance/soak and full acceptance remain open. The existing guard is scoped observation, not a sandbox for arbitrary CLR code. No broad/family/historical/Oil/Tycoon/platform runtime, business expansion, mandatory money/budget, Draw, adapters or host gameplay.

The lifecycle skill influenced staged design, shared single-responsibility declarations and evidence reconciliation. User/repository rules override broad-test and early-publishing recommendations. No staging/commit/push before full goal and plan completion and verification.

## Read-only reconciliation

Original221 acceptance identities/text/checkboxes remain unchanged (149 historically checked/72 unchecked);99 line offsets moved. No full phase was promoted to Done. Metadata verification found602 source/13 quickstart anchors without mismatch,2100 files/701 Markdown documents,5179 valid local links, and18 valid five-column master rows. git diff --check exited0 with34 existing CRLF warning lines and no whitespace errors. The new player driver has no raw World/resource/progress mutation; the staged index is empty. Sole-clock SHA256 remains35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1. These are source/documentation checks, not another runtime suite or a full dirty-tree release audit.
