# P11-C1 — Saved engine-owned player controllers

Current M106/native2.741/save161/replay2: actual controller schema getters use one original ControllerSchema/ReadProvider boundary during creation, composition validation and Sync. Null/stateful/unique/positive/stable schema rules, native scheduler/replies/state and M105profile metadata remain. Two existing focused cases624/0/three actual network replays; broader strategic gates remain open. [Current schema/codec source and evidence](../../docs/migration/P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m106-actual-controller-schema-admission-and-checkpoint-observation--2026-10-23). [Profile metadata owner](P11_STRATEGY_CONTENT_CONTRACTS_DESIGN.md).

Status: scoped implementation verified, 2026-10-05. Implements the controller/cadence portion of P11,
not full strategic AI.84 checks/two exact suites; clean build. See
[current-source evidence](../../docs/migration/P11_PLAYER_CONTROLLERS_VERIFICATION.md).

## 0. Problem statement

The preceding Hosting/SimGame.cs callback exposed PlayerSeat.Game to every IPlayerController. Engine-authored
strategy, explicit content bindings, saved cadence and durable command answers are absent. Autopilot
is a seat selection without a reusable native executor. Gameplay strategy must not move into hosts.

## 1. Goals

1. Revise the existing IPlayerController in place to Decide(PlayerDecisionContext), no parallel family.
2. Give decisions only scoped views, normal previews/submission, current engine time and detached replies.
3. Select providers/cadence/command budgets through required master-listed JSON and scenario bindings.
4. Own engine controller scheduling, reply delivery and provider Sync once, with deterministic seat order.
5. Ship an authored plan-template playbook that waits for actual physical execution, not fake completion.
6. Prove normal refusals, human handover, private facts, saved commands/replies and isolated continuation.

## 2. Design

PlayerDecisionContext exposes Id/View/Tick/Time/Date/Answers and Preview/Submit/ReportBlocker/TryGetAnswer.
There is no Game, World, observer, services, mutable snapshot, lifecycle or direct Apply access.
Host-attached input callbacks and ScriptedController migrate to that same Decide contract. Host scripts
are external input streams, not saved strategy state. Their context reads current scoped facts/replies.
Delayed scripted answer waits query the same existing bounded private result feed; they do not create
a second result history. Authored controllers instead query their actual durable pending reply snapshot.
Neither path invents a reply when the requested answer is absent.

IPlayerControllerState is an optional codec capability on that existing strategy contract; authored
engine providers require it. PlayerControllerDef uses the sole content catalog and declares positive
rational cadence and positive commands_per_decision. The optional PlayerControllersModule requires
explicit company/profile bindings, rejects missing active Autopilot profiles and composes one system
at order900. Native objectives run earlier; ended games issue no further decisions.

PlayerControllersSystem owns next/last/count/blocker and controller reply routing in one Sync. Its
service alias adds no persistence. Every decision submits normal World.Submit commands for the next
tick; it never calls handlers directly. Correlation tickets identify its submitted commands, not
another execution ledger. Only their real answers are queued durably until the next decision, so a
checkpoint between application and a slow cadence cannot lose replies. Existing CommandResult owns
its shared encoding. Provider state is embedded with explicit schema under the same owner.

Actual PlayerTable Controller remains sole human/autopilot selection. Human bindings are dormant,
keep earned provider memory/replies, and resume once on the next due tick. SetController admission
checks the selected optional controller service; no implicit default profile or cloned seat state.
Runtime decision context expires after callback and enforces the authored per-decision submit budget.

PlanPlaybookDef references existing PlanTemplateDef values, immutable provider-owned anchors and
distinct stable step/group IDs. Its one controller cursor submits ordinary PlacePlanTemplateCommand,
waits for its real answer, and reads the actual private WorkView group until all members are Done.
Failed/cancelled groups remain typed blockers, never silently repeat or fabricate completion. Whole
template placement/prerequisites/resources/machines/travel stay with existing engine owners. New
strategies implement IPlayerControllerDefinition and the same controller/state contracts;
PlayerControllerDef is optional native authoring convenience, not a required base.

## 3. API surface

Revised Hosting.IPlayerController.Decide; PlayerDecisionContext; IPlayerControllerState;
PlayerControllerDef; PlanPlaybookDef; optional IPlayerControllers/private PlayerControllersView.
JSON section plan_playbooks; settings.sim.player-controllers.companies contains company/profile.
No host/game-platform implementation or special rival command is introduced.

## 4. Files

Hosting/SimGame.cs and App/Standard.cs migrate existing callbacks; new Hosting/PlayerDecisionContext.cs;
Standard/PlayerControllers.cs and PlayerControllersModule.cs; Views/PlayerControllersView.cs;
Kernel/Commands.cs codec; native SetController admission; tests/public proof JSON; tracker/stage/docs.

## 5. Errors

Missing/duplicate profiles, seats, invalid cadence/budget or reused provider instances: ContentException
before Initialize. Undefined controller selection: InvalidArgument; missing selected profile: NotFound.
Ordinary command refusal is delivered unchanged. Closed context/budget violation/provider throw faults
normal Update; a partial tick cannot save/hash. Invalid saved schema/identity/timestamps/tickets refuse.

## 6. Compatibility

Clean breaking revision. No OnTick/OnAnswer/OnNews overloads, duplicate AI family or compatibility shim.
Build2.90.0/save59; controller-state1/view1. Other native layouts unchanged.

## 7. Verification

Build the standalone solution; run exactly:

- Player controllers: authored bindings, scoped decisions and command parity
- Player controllers: physical playbooks and saved cadence

Other suites migrate/compile only. No third/family/full gate. Broader knowledge/pressure/recovery
preferences/provider-fault/replay/lockstep/stress/full P11/P1–P12 gates remain open.

## 8. Risks

Per-decision command budget bounds submission, not arbitrary provider CPU time. External providers
must be deterministic and return; no process-global live state. Reply queues grow with actual commands
issued before a handover, not unrelated human commands. Context privacy uses existing private views;
no new fog policy is invented. Normal next-tick refusal remains possible after a ready preview.

## 9. Out of scope

General knowledge/pressure, industrial strategy/risk/recovery content, Oil/P5/Tycoon, adapters/rendering,
full suite runs, rollback, compatibility and forged resources/completion.

## 10. Execution order

1. [x] Narrow the existing callback/Wait path in place, including delayed scripted reply lookup.
2. [x] Implement optional authored controller/cadence/reply owner and normal selection admission.
3. [x] Add native physical playbook and public-contract provider proof.
4. [x] Run the two gates, record evidence and update parent/master/README/help/AGENTS.

At C1, SetControllerCommand registration still belonged to the existing WorkModule. Moving generic command
ownership to an independently selected module and proving non-work controller command composition
were separate gates. [C2](P11_CONTROLLER_EXECUTION_DESIGN.md) now moves the sole registration to the
core session and replaces group-label execution with stable plan links; no extra module or fallback.
