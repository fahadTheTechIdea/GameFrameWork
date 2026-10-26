# P10-A4 — authored resource-network solver execution

Status: historical native focused slice verified,2026-10-06. Build2.128.0; not rerun.
Current [B4 connected resource signals](P10_NETWORK_SIGNALS_DESIGN.md) advances build2.130.0;
save70/LineService4/view4 remain unchanged. Solver suites compile only this stage.
Preceding clean build;114 checks/two exact
suites (63 contracts;51 physical), zero failures/aborts.
[Evidence](../../docs/migration/P10_NETWORK_SOLVERS_VERIFICATION.md). Full P10 remains open.

## 0. Problem and acceptance

The preceding Standard/NetworkFlow.cs hard-wired every ranked due leg into a transfer loop. Adding an engine-
side network solver requires changing that coordinator, despite the planned INetworkSolver extension
seam. Replace the hard-wired path with explicitly selected immutable content and bounded execution.

1. NetworkDef requires a JSON solver reference resolving open NetworkSolverDef/INetworkSolver.
   Missing/inactive/wrong references fail; no implicit native solver or legacy reader remains.
2. Built-in resource_transfer_network_solvers executes existing ranked due legs through one
   NetworkExecutionContext. External registered solver content can select/restrict those same legs
   without editing the coordinator or creating another graph, inventory, clock or scheduler.
3. Context contains detached immutable requests and the one clock's completed-step timestamp, never
   mutable World/entity/endpoint handles. Requests bind to their actual current context by identity.
   Each may be attempted once; amounts must be positive, within earned allowance and exact quantum.
4. LineService retains eligibility/arbitration/graph/stock/receipt/history/Sync ownership. Context
   execution rechecks actual capacity/stock and commits existing verified receipts; failed/refused
   attempts cannot fabricate stock or rotate history. Solvers cannot bank unused allowance.
5. Context expires in finally. Cross-network/foreign/stale/duplicate/invalid requests or participant
   mutation fault normal execution. Saves/interleaved sessions preserve actual elapsed/history state;
   no context/request queue or provider calls become authoritative saved state.

## 1. Design and API

Standard/NetworkSolvers.cs owns INetworkSolver.Id/ValidateTime(ISimulationTime)/Advance(NetworkExecutionContext), open abstract
NetworkSolverDef, native ResourceTransferNetworkSolverDef, detached NetworkFlowRequest and typed
NetworkFlowAttempt. NetworkExecutionContext.Candidates exposes read-only ranked requests. Its
Transfer(request, maximum) admits only the exact request object and captured allowance/quantum.
Known current stock/capacity/receipt refusal returns zero plus a typed blocker. Programmer/provider
contract violations throw and fault the current engine Update; no silent fallback or substitution.

Existing Flow captures/advances/ranks all legs before any solver executes, orders networks stably,
then invokes each explicitly selected solver. Native solver iterates the ordered requests and
attempts their earned allowances. One ExecuteFlow method remains in LineService for actual admission,
receipt transfer and successful history. Guards validate participating owners before solver-mediated
execution and after returning; intentional verified transfers refresh their observation baseline.
No solver owns current time. GameClock/World.Step remain the only game-time lifecycle owner;
completed-step values and eligibility durations are timestamps/work, not additional clocks.
Composition calls ValidateTime against the actual session's immutable time converter; context.Time
references that same converter. Authored solver intervals incompatible with its quantum fail before
Update. A provider can use the one clock's completed-step timestamp, never advance another clock.

Build2.128.0; save70/LineService4/view4/other state layouts remain unchanged. Content fingerprints
include explicit selected solver definitions; old builds/content refuse, never migrate at runtime.

## 2. Content and ownership

```json
{
  "resource_transfer_network_solvers": [{"id":"field.transfer"}],
  "networks": [{"id":"field.network","solver":"field.transfer"}]
}
```

Snippet shows changed fields only. Existing explicit line/capacity/native rational rates/flow_policy
remain required. Master-manifest documents and declaring modules own each section. Registered
derived NetworkSolverDef providers require no closed strategy enum or concrete provider switch.
Future utility physics/graph providers may extend these seams; this native resource solver does
not invent pressure, power allocation or balances for undeclared game content.

## 3. Implementation and verification

1. Add design/tracker; inspect existing flow/receipt/clock owners.
2. Register solver content, revise NetworkDef, and migrate all current authored network fixtures.
3. Replace actual transfer loop with the bounded context/sole owner commit method; preserve codecs.
4. Add public-only independent solver/fault probes and exactly two bounded suites:
   - Network solvers: explicit contracts, lifetime and guarded execution
   - Network solvers: physical selection, refusal and saved continuation
5. Migrate earlier build assertions/compile only; completed clean build precedes focused gates.
6. Align master/parent/AGENTS/README/help and scoped source/verification evidence.

Prove native execution, independent JSON policy selection, skipped/unbanked allowances, actual stock/
capacity/refusal conservation, multiple networks/resources, saved interleaved sessions, pause on the
sole clock, missing/wrong refs, invalid/foreign/repeated/expired requests and mutation faults before/
after provider-mediated transfers. Tests drive actual contracts/Update, never copy flow or scheduling.

## 4. Limits and risks

Solvers are trusted engine modules using guarded contracts, not a security sandbox. Full global
mutation rollback/third-party determinism/stress/allocations remain open. Candidate capture is the
existing resource-flow phase; native no same-step feed-through remains unchanged. Graph-independent
provider construction, pressure/thermal/electric utilities, IUtilityConsumer admission/satisfaction,
loss/disposal policies, runtime player policy edits/cross-network fairness/full P10/P1-P12 remain open.
No legacy/compatibility, game-size cap, host/demo/platform/Draw or full regression gate is included.
