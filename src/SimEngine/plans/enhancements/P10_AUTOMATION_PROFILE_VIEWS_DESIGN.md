# P10-B6b — complete immutable automation profile discovery

Status: native focused slice verified on2.134.0/save72, 2026-10-06. Continues B6;
full P10/P1-P12 remains open.
Current [B7a](P10_CONTROL_ACTION_POLICIES_DESIGN.md),2.135.0/save72, includes this coverage in its
expanded545-check/two-suite gate. Profiles now additionally expose action-policy/command identities;
ControlBinding replaces operation-only binding naming. No compatibility alias or new state/clock.

## 0. Problem

OperationsView.TryGetAutomationThresholds exposes numeric bands but not the input aliases, signals,
provider identities, authored actions or durations needed to configure a rule. CopyCatalogue covers
entity prototypes, not rule profiles. A thin player host cannot discover and bind a JSON-authored
automation profile entirely through immutable engine views.

## 1. Acceptance

1. One detached immutable profile projection exposes stable profile/input/signal/provider identities,
   typed signal kind/metric/unit, threshold declarations, initial named state/latch, recovery identity,
   authored actions, arbitration, dwell and cooldown.
2. Existing OperationsView discovers profiles in stable ID order and reads one by ID. Unknown,
   hidden and unseated callers obtain no metadata; a hidden referenced signal/recovery withholds the
   complete profile, never returns a partial binding or substituted definition.
3. Configure admission uses that same content-visibility check. Existing settings validation,
   rule evaluation, ordinary controls and sole GameClock remain the execution owners.
4. Views expose no content definitions, executable contracts, codec registrations, providers or World.
   Retained values cannot change after commands, updates, save/load or another session's execution.
5. Player proof discovers a public-only JSON rule, selects live owned capability IDs, submits explicit
   settings and observes deferred controls using public views/commands/Update only.

## 2. Design and API

Add value-only AutomationProfileSnapshot, AutomationInputSnapshot and AutomationProviderSnapshot
under Standard. Projection copies actual resolved declarations; it owns no configuration or runtime
state. One internal visibility predicate serves OperationsView and existing CheckConfigure.

Replace threshold-only TryGetAutomationThresholds with TryGetAutomationProfile(profile, out snapshot).
Add CopyAutomationProfiles(IList<AutomationProfileSnapshot>); clear caller output and sort by stable
profile ID. Thresholds remain part of this single profile projection. No compatibility wrapper.
CopySignalSourceIds discovers actual named IAutomationSignalSource capabilities, including providers
that are not operations. Existing Owned projection admits only actual seated owners or PlayerId.None;
invalid negative/unseated viewers never inherit observer access. No second capability registry.

## 3. Files and errors

Standard/AutomationProfileSnapshot.cs, Standard/AutomationSystem.cs, Views/OperationsView.cs,
Standard/OperationsModule.cs, Kernel/SaveGame.cs, directly affected automation tests and build identity
assertions; this stage/master/evidence/README/help. Missing or inaccessible profile returns false/null
or is absent from the list; configure returns UnknownDef. Null destination list throws explicitly.

## 4. Compatibility and timing

Breaking view API/version, no old reader/facade. Build2.134.0, operations view5; save72/automation3 and
all persistent owner layouts remain unchanged. Only metadata changes: no additional timer, state
machine, controller, signal reader, policy store, command or system phase.

## 5. Verification

Clean build before exactly two complete suites:

- Automation: threshold and recovery contracts, bindings and private controls
- Automation: threshold edits, recovery, deferred timing and saved processes

Extend these existing suites; no third/family/full gate. Other suites compile only. Check immutable
metadata, sorted discovery, caller output clearing, exact JSON values, independent profiles, unknown/
unseated/hidden dependencies, pure hashes, public-only player setup and saved continuation.

## 6. Risks and non-goals

Do not let copied provider declarations carry executable registration handles. Do not expose a rule
with undisclosed binding dependencies or infer settings when a host omits them. Actual action/control
admission remains engine-owned. Utility physics, reserve actions, runtime duration/action edits,
whole visibility-provider matrices, replay/stress/global rollback/full framework acceptance remain
open. No Oil/Tycoon/host/demo/platform/Draw implementation.

## 7. Execution

- [x] Detached profile values, shared visibility and single view replacement/discovery.
- [x] Focused contracts and public-bound player execution proof; explicit build assertions.
- [x] Clean build and the two exact suites; record evidence and align staged documentation.

## Recorded gate

Clean build, zero warnings/errors;479 checks/two exact complete suites (259 contracts;220 execution),
zero final failures/aborts. [Evidence](../../docs/migration/P10_AUTOMATION_PROFILE_VIEWS_VERIFICATION.md).
Includes previous B5/B6 coverage, not additional388 current checks. Native signal-only discovery uses
the existing inventory fixture/reader. Independent JSON rule setup uses only public player views,
commands, Update, save and hash. Hidden rule/signal/recovery dependencies use actual knowledge/visibility
owners; knowledge observation is test instrumentation, not a newly implemented discovery activity.
62 build assertion files migrate; other suites compile only. No clock source or persistent codec change.
Lifecycle guidance kept sole-owner projection, scoped verification and stage/master documentation aligned.
