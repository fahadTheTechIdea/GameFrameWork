# P11-C2 — Core selection and stable playbook execution evidence

Date:2026-10-05. Current working-source evidence, not committed HEAD or full regression.
Standalone framework runtime/contracts/content proof only; no Oil/Tycoon/platform/Draw work.

## Identity and responsibility

Build2.91.0/save60/native-playbook-state2. Controller-system/view1 and other native layouts unchanged.
The command table changes ordering/identity; the provider codec changes directly. No old reader/ID alias.

Commands/PlayerCommands.cs contains the existing SetControllerCommand and its sole handler. World
registers core.set-controller once under engine ownership before module composition. WorkModule no
longer registers it; its old mixed-file command/handler are removed. CheckController/SetController,
PlayerTable, CommandInbox, actual replies and their ordinary Sync remain the only execution owners.
No optional work/movement/spatial/resources/economy/controller module is required for core selection.
Without an authored controller module, selection alone installs no default gameplay AI.

The existing optional public controller/provider contract and master-listed JSON stay unchanged.
Native playbook now saves step/pending-command/first-plan identity in its sole embedded state2 codec,
not a submitted boolean or group membership/progress ledger. Accepted PlacePlanTemplateCommand already
returns first stable plan identity/count. The existing native contiguous allocation contract is explicit
on IPlanBoard; handler validates the whole returned range before publishing success. Provider verifies
actual answer command/count/range and observes those real private PlanView identities, not a mutable
group label. Actual native priority/pause/group/cancel/work/resource/machine records remain authoritative.
Completed steps advance once; a subsequent step waits until a later decision. Cancelled/failed/missing
linked work remains blocked, never borrows unrelated completion or silently repeats.

## Exact final gate

~~~powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Player controllers: authored bindings, scoped decisions and command parity"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Player controllers: physical playbooks and saved cadence"
~~~

Final build:0 warnings,0 errors,1.67s.
Contracts:92 passed,0 failed,256ms.
Physical:50 passed,0 failed,484ms.
Total:142 checks/exactly two suites, zero final failures/aborts. Only these two suites were repeated.
The same names extend C1 coverage; no third/family/full run. Thirty other test files mechanically
migrate current build/save assertions and compile only; earlier other-suite evidence is not rerun.
git diff --check passes. Source search confirms one registration/handler, no old runtime command ID.

A new unrelated-group fixture initially hit its1200-update diagnostic bound: paused original projects
retained the only compatible machine, so the unrelated group correctly reported work.worker-busy.
The fixture now issues normal player cancellation before that group runs. No engine availability,
machine reservation, stock, travel or pause rule was bypassed to make the test pass.

## Proven current behavior

- Core-only session has no space, work/resource services or gameplay systems and exactly one engine-
  owned selection command. Pure preview, ordinary enum refusals, actual selection and saved pending
  core command agree. No selected controller module means no hidden autonomous gameplay.
- Public authored controller composes with no plan/work/spatial module. Explicit profile selection,
  absent-profile preview/refusal parity, real delayed reply, private facts, cadence, saved handover and
  interleaved native provider state remain deterministic through the ordinary inbox/codec.
- Native state2 rejects negative step/ticket/identity, mutually pending-and-linked phases, overflowing
  identity range and a terminal cursor retaining execution. These are scoped, not exhaustive guards.
- Existing C1 command budget/expired-context faults, schema/cadence/reply guards, private views and
  physically supplied single-stage saved behavior continue through the same two named suites.
- Two authored sequential templates create four real structures. Explicit proof content requires four
  supplies per structure;20 actual opening supplies become4 after16 physical inputs are consumed.
  Travel, deliveries, earned work and native effects—not controller state—create the structures.
- Player pause retains actual machine commitments/progress. Changing both plan labels after the real
  accepted answer preserves stable execution links across saves. Resume completes first-stage work
  before expansion; checkpoints at both physical stages continue with identical twin hashes.
- Player can regroup before slow-cadence delivery of the accepted answer. Saved actual mail still
  binds the same stable plans, despite the original label disappearing before the controller sees it.
- A separate ordinary physical group can reuse the retired original label. Original linked cancellation
  remains terminal for the playbook even after that unrelated group really completes. No expansion,
  borrowed completion, automatic restart or duplicate plan appears. Saved twins agree.
- Whole-group cancellation through ordinary commands leaves typed strategy blockers and native
  resource/machine cleanup; completed multi-step cursor never repeats without authored new intent.

## Remaining gates

General private world knowledge, pressure/pacing, configurable expansion/risk/reserves/recovery,
multi-step alternative/parallel strategic content, editable/repeat playbooks; arbitrary provider
purity/CPU/schema/reentrancy/fault matrices, malformed accepted result callback tests and exhaustive
cross-provider saved execution-link validation; combined technology/objective/incident integration;
replay/lockstep/shuffled inputs/fuzz/soak/allocation budgets and full P11/P1–P12/P5 adoption remain open.
The new result guards are source-reviewed plus exercised by valid native results, not an exhaustive
lying-provider proof. No transactional rollback or fully green broad regression is claimed.

Development-lifecycle guidance aligned stage, single ownership, bounded physical proof and evidence.
