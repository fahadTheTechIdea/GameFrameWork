# P6 — Planet X authored technology, safety control and response

## 0. Scope and status

Scoped complete:121 checks,0 failures/aborts in3625ms. SimEngine contracts and explicit game-content JSON only.
No Oil, Tycoon, platform adapter, renderer, military or population changes.
This strengthens the independent-game mod proof; it does not close all P1–P12 gates.
Commit and push remain deferred until the full goal and plan are complete and verified.

## 1. Single owners

- Existing Planet X factory composes an explicitly supplied master and optional documents/modules.
- Existing native WorkBoard executes research and phased incident response.
- Existing company knowledge owns learned technology and native content-access gates.
- Existing automation owns rule settings, latch, cooldown and next-update actions.
- Existing incident owns the active episode; native pressure owns its finite authored budget.
- Existing spatial resource, port, hauling, transformation and construction owners remain unchanged.
- One stateless engine-side signal component queries actual knowledge and named incident contracts.
  It owns no cached readiness, scheduler, inventory, learned table, clock or response state.

## 2. Master-listed authoring

`planet-x-control-master.json` reuses the original resource-only Planet X documents and selects
knowledge, technology, automation, incidents and scenario pressure explicitly.
`planet-x-control.json` adds a safety technology, exact prototype/recipe unlocks, real research
input, typed Boolean signal, operation action, control profile, response DAG, finite pressure,
and two stationary startup entities. No existing definition ID is duplicated or rewritten.

## 3. Requirements and physical work

Safety research requires an extractor-role machine, travel to its actual lab service berth,
two minutes of productive work and one alloy. It unlocks rover assembly and beacon construction.
The leak response detects for one minute, then waits for the same learned safety technology.
Repair consumes one alloy through native target-resource requirements and earns three minutes
of staffed work. Restart earns one minute before the bound episode completion effect.
No command, harness rule or signal grants research or resolves an incident.

## 4. Explicit starting reserves

The separate lab and watch station each author one starting alloy in JSON. These two units are
disclosed finite bootstrap research/response reserves, not ore production or automatic refill.
The original twelve underground ore, empty mine/refinery inventories and six-alloy production
chain are unchanged. Final accounting must distinguish initialized two alloy, produced six alloy
and consumed twelve ore plus eight alloy.

## 5. Safety control

A named signal returns true only when the company's authored technology is known and its actual
named incident is inactive. Unknown technology returns a valid false, not unavailable data.
Malformed/missing references remain typed failures. The authored Boolean rule disables the
refinery while unsafe and enables it after earned recovery through the existing deferred action
boundary. Hosts select actual bindings and submit the detached authored profile; they do not
implement safety predicates, recipes, delays or workflow advancement.

## 6. Privacy and durability

Read-only technology, signal, operation, incident, pressure and work views expose actual private
facts. Reads may not change the hash. Retained snapshots stay detached. Pending control,
technology-blocked response, partial research/repair and finished saves resume identically.
An untouched session remains isolated; canonical player-command replay ends at the same hash.

## 7. Public acceptance driver

One new exact bounded case uses only normal previews/commands, immutable views, GameClock,
Update, save/load and canonical replay. No Game.World, private reflection, instant spawning,
direct inventory mutation, layer rewrite or manual incident raising.
Each physical wait has a 2,000-update/30-second observational ceiling and public diagnostic
snapshot. This watchdog cannot interrupt a single synchronously stalled Update.

## 8. Verification scope

One distinct NEW exact case:
**Planet X control acceptance: authored research, deferred safety control and physical incident recovery**

Compile with `dotnet build Beep.SimEngine.slnx --no-restore`.
Run only that exact case. Repair/rerun only observed failures of this same new case.
Record all build/runtime attempts; no old suites, family filters, full regression, Oil, Tycoon or
platform execution. A clean focused result is not a full industrial regression claim.

## 9. Freeze and evidence

Executable inputs frozen at2026-10-08 15:09:04 UTC after final green. THREE builds (first failed
on inaccessible internal validator, then clean4.89/3.43s); TWO attempts of the SAME one new case
(51/1 aborted on an invalid assumption that a completed/pruned research order remained readable,
then121/0/0). Research completion is observed through the actual private knowledge record and
previously observed productive work. No engine behavior changed to satisfy that assertion.
[Focused verification](../../docs/migration/P6_PLANET_X_CONTROL_RESPONSE_VERIFICATION.md).

After the final green, freeze executable source/proof/tests/JSON. Update this stage, scoped
evidence, master tracker and source manifest from actual current files and read-only checks.
Do not alter executable inputs after green without a new authorized verification slice.

## 10. Remaining full-plan gates

Broader provider faults, starvation/fairness, resource accounting stress, corruption, lockstep,
performance/allocations, other game/Oil acceptance and the authorized full regression remain
open unless independently proven. This slice introduces no core runtime or save-format change.

