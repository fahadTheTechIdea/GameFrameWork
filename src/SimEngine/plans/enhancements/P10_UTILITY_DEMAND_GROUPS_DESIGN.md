# P10-A9 — Authored shared-stock utility demand groups

Status: focused verified,2026-10-06. Build2.145.0/save76; clean build;242 checks/two exact network
suites (120 contracts;122 physical), zero final failures/aborts.
[Evidence](../../docs/migration/P10_UTILITY_DEMAND_GROUPS_VERIFICATION.md). Full P10/P1–P12 remain open.

## 0. Problem

`UtilityNetworkDemandPolicyDef` in Standard/NetworkDemands.cs selects one named consumer. With two
motion input providers sharing inventory, each can report its one-unit draw satisfied by the same
one-unit stock although physical aggregate admission needs two. Independent per-consumer networks
cannot infer that aggregate obligation; blindly summing their Available fields also counts stock twice.

## 1. Goals

1. Network JSON explicitly declares a nonempty, unique stable named `consumers` group.
2. One existing demand policy sums active next requirements per native resource, then subtracts actual
   shared unpromised stock and inbound reservations once; no new allocation balance or scheduler.
3. Native multi-input motion obtains the complete physical draw and moves through the ordinary engine.
4. Independent consumers, inactive members, mixed resources, shared networks, promises, faults,
   private diagnostics, save/load and sole-clock pause preserve the existing invariants.

## 2. Design

Replace singular `Consumer` with immutable sorted `Consumers` in the existing definition, directly.
`consumers:["propulsion","auxiliary"]` is explicit master-manifest content, not automatic enumeration
or fallback. A single consumer uses the same one-entry array. Every selected provider must exist and
use the actual receiving endpoint. Missing/foreign/malformed observations refuse or fault through
existing context guards; no first-match substitution. A member inactive for the current resource
contributes no obligation; all inactive yields utility.inactive. A resource not declared by any member
is incompatible. An active representation blocker is preserved. Checked total overflow is typed
network.demand-range, never a saturated partial draw.

The same actual inlet quantity snapshot supplies Available and Inbound exactly once. Clamp the
unpromised deficit to current readiness and floor to the existing common native quantum. Existing
LineService rechecks the policy before eligibility and before each actual receipt, preserving shared
network late admission. This is an explicitly authored concurrent obligation, not global optimization
or arbitration across arbitrary consumer groups. Overlapping differently authored groups are not
silently merged; games must declare the complete obligation for that shared store.

## 3. API and files

NetworkDemands.cs changes the existing definition/property and calculation. No NetworkDemandContext,
NetworkDemandSnapshot, view or persistent layout change. Update active network-utilities.json and its
test edits; add embedded/master-listed network-utility-groups.json with native motion and public-only
consumer definitions. Extend only the two existing NetworkSolverTests suites. Build2.145.0/save76;
LineService4/infrastructure view5/motion view5 and existing component codecs unchanged.

## 4. Errors and compatibility

Missing/empty/non-array/duplicate/invalid IDs fail strict JSON. Old `consumer` is removed, including
mixed old/new fields: no alias, shim or compatibility reader. Native quantity overflow refuses visibly.
Mutating or malformed providers fault the existing execution owner; no global rollback promise.

## 5. Verification

Clean `dotnet build Beep.SimEngine.slnx --no-restore` first; a failed build stops before tests.
Only these two exact complete existing suites run (same-name repair reruns allowed):

- Network solvers: explicit contracts, lifetime and guarded execution
- Network solvers: physical selection, refusal and saved continuation

Bound physical waits; no third/family/full gate. Other suites compile only.

## 6. Risks / non-goals

Do not count per-consumer capped Available more than once; actual endpoint quantities are authoritative.
Do not treat inbound promises as physical fuel. No new clock/stock/store/codec/system/fuel grant,
distance model, fleet auto-refuel, physics/global priorities/graph rewrite, Oil/Tycoon/host/demo/Draw.

## 7. Execution

- [x] A9.1 strict grouped declaration and single shared-stock deficit calculation
- [x] A9.2 native/public physical group proof and focused verification
- [x] A9.3 evidence/master/parent/README/help/AGENTS alignment
