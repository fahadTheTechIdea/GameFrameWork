# P10-B1 — Engine-owned authored automation

Current M147/native2.772/save161/replay2/inventory view11: rules, operation/company/input
policies and now stock/outlet actions use sole reusable engine declarations through contracts.
Native resource-action inheritance is deleted; common substantive linker/admission stays shared.
Normal automation, deferred commands, physical response, resource/time/state owners remain.
Final749/0 across two exact scopes on their recorded test/proof images with identical native;
four coupled complete physical streams plus four existing visibility verifications.
[Current stage](P10_CONTROL_CONTENT_CONTRACTS_DESIGN.md),
[attempts and remaining scope](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md#current-m147-reusable-resource-controls-and-detached-quantities--2026-10-23).
Historical B1 and earlier C7 results below are not new-runtime reruns. P10.022 remains open.

Ownership update P10-C7: actual IControlRule/IControlActionPolicy/IControlInputRecoveryPolicy aliases replace native-base requirements. Same order290/state5, private profiles, normal dispatcher and next-update decisions. Scoped87-check proof does not rerun this historical stage. [Stage](P10_CONTROL_CONTENT_CONTRACTS_DESIGN.md), [evidence](../../docs/migration/P10_CONTROL_CONTENT_CONTRACTS_VERIFICATION.md).

Status: implemented and focused-verified on 2026-10-05: build2.74.0/save45, clean solution build,
215 checks across two exact suites (86 contracts;129 timing/process). See
[verification](../../docs/migration/P10_AUTOMATION_RULES_VERIFICATION.md). Continues P10, not
completion of utility/network or full framework acceptance.

Current follow-on [P10-B7e complete stock-band controls](P10_STOCK_BAND_CONTROLS_DESIGN.md),2.139.0/save76,
extends the same two suites to1069 checks (622 contracts;447 execution), clean build. One explicitly
selected typed stock level replaces desired-only API/provider/JSON. Existing normal owners preserve
other bands/priority/age and physical claims; real work shortages respect lower/raised ceilings.
Native/public private/refusal/physical/saved proof includes B5/B6/B6b/B7a/B7b/B7c/B7d, not additive.
One GameClock unchanged; utilities/wider providers/full P10 remain open.
[Evidence](../../docs/migration/P10_STOCK_BAND_CONTROLS_VERIFICATION.md).

Preceding follow-on [P10-B7d editable runtime branches](P10_RUNTIME_ACTION_SETTINGS_DESIGN.md),2.138.0/save75,
extends the same two suites to909 checks (522 contracts;387 execution), clean build. Explicit runtime
branch settings and actual applied history use existing owners; same-latch edits re-earn dwell while
retaining cooldown, and unselected edits avoid redundant commits. Native/public normal actions and
physical stock/reserve hauling survive edits and saves. Includes B5/B6/B6b/B7a/B7b/B7c, not additive.
One GameClock unchanged; arbitrary stock-limit actions/utilities/full P10 remain open.
[Evidence](../../docs/migration/P10_RUNTIME_ACTION_SETTINGS_VERIFICATION.md).

Preceding follow-on [P10-B7c native stock-target actions](P10_STOCK_TARGET_ACTIONS_DESIGN.md),2.137.0/save74,
extends the same two suites to832 checks (493 contracts;339 execution), clean build. Existing demand
owner resolves immutable desired intent preserving current limits/priority; generic stock mutation
bypasses removed. Shared resource-action linking, private resource-specific discovery, timed native/
public refill and saved twins use existing owners and sole GameClock. Includes B5/B6/B6b/B7a/B7b,
not additive. Arbitrary limit automation/runtime edits/utilities/full P10 remain open.
[Evidence](../../docs/migration/P10_STOCK_TARGET_ACTIONS_VERIFICATION.md).

Preceding follow-on [P10-B7b native outlet reserve actions](P10_OUTLET_RESERVE_ACTIONS_DESIGN.md),2.136.0/save73,
extends the same two suites with B5/B6/B6b/B7a and actual reserve controls.682 checks/two exact suites,
clean build; existing IOutletPolicy/normal command/jobs/reservations and one GameClock remain owners.
Immutable reserve intent preserves later unrelated settings, refuses current capacity conflicts and
continues actual native/public hauling after saves. Stock-band actions/runtime edits/utilities/full
P10 remain open. [Evidence](../../docs/migration/P10_OUTLET_RESERVE_ACTIONS_VERIFICATION.md).

Preceding follow-on [P10-B7a normal-command actions](P10_CONTROL_ACTION_POLICIES_DESIGN.md),2.135.0/save72,
extends the original two suites with B5/B6/B6b coverage and contract-based ordinary command dispatch.
545 checks/two exact suites, clean build; historical counts are not additional current checks.
Existing rule/command/settings/evaluation owners and the single GameClock remain authoritative.
Detached immutable metadata supports player configuration without host-side gameplay. Utility physics,
reserve-target actions, broader control editing and full P10 remain open.

Historical follow-on [P10-B2 inventory readiness](P10_INVENTORY_SIGNALS_DESIGN.md),2.124.0/save67,
adds stateless JSON stock/available/incoming signals over actual native and independent inventories.
The same B1 reader/order290/deferred handler consumes them; no B1 codec/control/timer changes.
115 checks/two exact follow-on suites; [evidence](../../docs/migration/P10_INVENTORY_SIGNALS_VERIFICATION.md).
Original B1 suites compile only, not rerun. Numeric/remote signals, stock-target controls and utilities
remain open; this does not replace their physical execution owners.

## 0. Problem

Operations.cs exposes the sole typed signal reader and normal ConfigureOperation command handler,
but no rules, bindings, deterministic evaluation or persisted next-step actions. Hosts cannot own
these decisions. Network/production migrations remain separate required work, not a reason to put
process control in a demo.

## 1. Goals

1. Master-listed JSON authors stable rule profiles, exact signal units, comparisons, boolean/all/any
   conditions, hysteresis, actual-session dwell/cooldown and explicit on/off control actions.
2. Ordinary commands configure/remove company-private live rule bindings; previews use identical checks.
3. One runtime/codec owns rule state, deferred actions and diagnostics. Existing providers retain
   process state, resource stock and controls. No second signal reader or utility inventory.
4. Missing/foreign/incompatible inputs and conflicting enabled targets fail visibly. No fallback.
5. Save/load/twin continuation retain every timing/decision stage through public engine views.

## 2. Design and owners

Optional `AutomationModule` requires `sim.operations`, declares `control_rules` via the existing
master catalog and composes one `AutomationSystem` at order290. That same system aliases
`IAutomationRules`; it alone owns live company/local-rule records and Sync. No persistent service
codec alias is added. Native process systems remain their ordinary owners at later declared orders.

`IControlRule`/open `ControlRuleDef` expose immutable input declarations, initial named boolean state,
initial decision latch, P3 durations, explicit actions/arbitration and pure `Evaluate` over detached
signals/prior state. Native `AuthoredControlRuleDef` uses the existing `DependencyGraph` compiler,
not another cycle checker. Named compare/boolean/all/any nodes must all contribute to the root.
Signal references resolve to actual AutomationSignalDef aliases; exact unit/kind checks happen before
tick one. Custom JSON rule subtypes can implement the same contract without scheduler changes.

`AutomationSettings` binds each declared input name to an exact entity/named source and one exact
entity/named target operation. Company/local rule IDs are stable and unique. Settings freeze/copy
input maps, not providers. An explicitly authored `exclusive` arbitration rejects two enabled rules
for the same operation through the same configure admission. Stable company/local ID order governs
the pass; no implicit last-writer-wins or hidden priority.

At tick t/order290: capture availability for all due rules before changing any operation, then
apply due actions from t-1 after current reference/ownership/unit validation;
invoke the registered ConfigureOperation handler's normal Check/Apply, never another control path.
Then capture the complete eligible rule input set before evaluating any rule. Evaluation cannot
configure an operation; it emits an immutable decision/state only. Stable dwell and cooldown gate
one intent for t+1. Applying all old intents before capture prevents intra-pass evaluation feedback.

Saved record facts: explicit settings, named hysteresis state, last applied latch/has-applied,
candidate/since, cooldown deadline, pending latch/due, last applied tick and typed blocker. Definition
actions supply pending payloads; no copied process controls/stock or world command-sequence authority.
Unavailable inputs or failed application suspend the rule until explicit successful configuration;
old pending intent is cleared without reverting any real process action. Disabling/removing policy
cancels its uncommitted intent, never rewinds production/resources. Pausing game freezes all deadlines.

`OperationsView` adds private rule facts and configure/remove previews, not an alternate view.
Snapshots contain detached immutable maps and timing/decision facts, never execution handles.
`AutomationSignalDef.ReadFrom` stays the sole signal binding validator; touched source mutations
during purportedly pure reads fault. Successful ConfigureOperation application verifies actual
provider controls; a lying provider faults the normal tick and cannot save/hash partial state.

## 3. JSON/API

Profiles require inputs, conditions/root, initial_latch, dwell/cooldown, when_true/when_false and
arbitration. Comparison nodes explicitly author kind above/below, input, unit, on/off and initial
hysteresis state. Boolean nodes author expected; logical nodes author nonempty dependencies.
Missing/unknown fields, references, cycles, unused nodes/inputs, reversed bands or wrong units refuse.
Durations use RationalDuration and actual ISimulationTime, with explicit zero permitted.

ConfigureAutomationCommand names local ID plus immutable settings; RemoveAutomationCommand names ID.
An enabled rule's explicit signal/operation references validate before registration. Controls require
same company and both action units/bounds to match the actual named operation. No provider inferred
from component order, name suffix or an existing concrete Producer lookup.

## 4. Files

New Standard/AutomationContracts.cs, ControlRules.cs, AutomationSystem.cs, AutomationModule.cs;
Commands/AutomationCommands.cs. Extend Operations.cs/OperationsModule.cs/OperationsView.cs and
current identities. Master-listed proof JSON and public-only provider; focused tests and linked docs.

## 5. Errors

Typed missing/company/provider/signal/unit/conflict refusals; unavailable never becomes zero.
Normal command target bounds/refusals remain authoritative. Corrupt state refuses identities,
content/state keys, impossible timing/pending phases or duplicate active operation ownership.
Touched read mutation/lying committed operation faults Update; no compensation or resumable checkpoint.

## 6. Compatibility

Clean development revision: no compatibility facade/reader/legacy behavior. Build2.74.0/save45;
automation system schema1; operations-view2. Existing process/work/resource/logistics layouts unchanged.

## 7. Verification

Build solution and run only these exact names (including affected operation checks inside them):

- `Automation: authored rules, bindings and private controls`
- `Automation: deferred decisions, timing and saved processes`

Prove strict JSON/topology/units, ordinary command/preview/private facts, conflict refusal, bounded
target validation, mod rule/provider, deferred feedback, hysteresis/dwell/cooldown, pause/removal,
unavailable references, native production and independent operation behavior, saved stages/twins,
session isolation, corruption/fault guards. Other suites compile only; broad/full gates stay open.

Executed proof includes pending-input failure before commit, same-source and cross-rule feedback,
thirty-second session quantum, deadlines beyond 32-bit tick range, and actual native production
pause/resume with retained progress and exact-once material consumption. Saved twins cover candidate,
pending, committed cooldown and suspended stages. Checkpoints reject impossible current/future
candidate timestamps and tick-zero pending actions. Build2.74.0/save45 is deliberately breaking;
there is no old reader. Lifecycle guidance kept design, single owners and evidence aligned.

## 8. Risks

Evaluation sees settled phase290 facts, not mid-process changes. Internal action diagnostics belong
to the rule, not invented player command sequences. Pure read mutation guards detect touched named
providers, not every possible malicious cross-owner mutation. Explicit exclusive arbitration is the
implemented authored policy; richer arbitration remains separate content/provider work.

## 9. Out of scope

Utility solvers, WIP/transformer migration, arbitrary target types beyond IControllableOperation,
pollution/incident progression/rivals, hosts/Draw/platform adapters/Oil/Tycoon, broad fault/stress/replay
and full framework acceptance. Stock/reserve targets need their own existing control providers.

## 10. Order

1. Immutable rule/binding/evaluation contracts and strict master content.
2. Existing command/signal guard reuse and one runtime/codec at phase290.
3. Normal policy commands and existing private operation view.
4. Public-only provider/content proof and exactly two gates.
5. Stage/evidence/master/README/AGENTS update together; keep full goal active.
