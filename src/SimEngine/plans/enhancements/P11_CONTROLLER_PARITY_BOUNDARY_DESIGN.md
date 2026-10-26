# P11-C7: controller parity and terminal observation boundary — Design Document

Original parity/view implementation remains; [M105 current profile metadata boundary](P11_STRATEGY_CONTENT_CONTRACTS_DESIGN.md)
extends the same controller owner. Its existing strategy/network handover cases run, not a
rerun of every historical C7 terminal query. The problem below describes the original C7 gap.

## 0. Problem Statement

Original `PlayerControllersView` forwards cached queries after a terminal fault,
while the original controller owner returns early for missing/private bindings
without health validation. Several pure controller callbacks also use finally
verification that can replace their original exception. This undermines the
same authoritative boundary required for rival/player command parity.

## 1. Goals

1. Cached controller diagnostics/previews reject the original terminal session
   before private/missing/invalid shortcuts, clearing outputs and never retrying.
2. The shared observation owner recognizes actual IPlayerControllerState codecs;
   pure controller callbacks use that owner, retain first failure and publish
   complete detached facts only.
3. One current exact public-input proof compares human seat and decision-context
   previews/results across typed refusals, then physically races human/rival
   construction/recovery with finite resources, two clocks and saves/replay.

## 2. Design

Extend `EntityObservation.CaptureProvider`'s existing codec selection with the
existing `IPlayerControllerState` contract. Replace ad-hoc controller callback
try/finally scopes with the shared successful-read verification/catch boundary.
No new observation/fault/clock owner or controller state is introduced.

Original controller owner clears destinations, requires healthy world before
shortcuts and stages controller/playbook facts locally. Observe the selected
controller state and metadata, not the whole service Sync halfway through Update:
cadence/inbox fields are finalized by their existing declared phases.

Public views also guard cached calls and clear outputs before terminal refusal.
Native decisions continue through `World.Check`/`World.Submit`, exactly the same
dispatch owners as human seat preview/submission. No strategy/physical rule change.

## 3. API surface

No new public contract or state payload. Build2.451.0/save154/replay1; original
controller codec1/view7 unchanged. Existing pure recovery JSON factory may gain
internal visibility/authoring hook for the new proof, never live world writes.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `src/Beep.SimEngine/Kernel/EntityObservation.cs` | medium |
| Modify | `src/Beep.SimEngine/Standard/PlayerControllers.cs` | medium |
| Modify | `src/Beep.SimEngine/Views/PlayerControllersView.cs` | medium |
| Modify | Existing build/protocol fixture and runner | low |
| Modify | Existing pure recovery test factory visibility/clock authoring hook | low |
| New | `tests/Beep.SimEngine.Tests/ControllerParityBoundaryTests.cs` | medium |
| New | `docs/migration/P11_CONTROLLER_PARITY_BOUNDARY_VERIFICATION.md` | low |
| Modify | Master/current audit/checkpoints; original gate only if fully qualified | low |

## 5. Error handling matrix

| Observation/input | Required outcome |
|---|---|
| Healthy owned diagnostics | Pure complete detached facts |
| Healthy foreign/missing binding | Empty private output/ordinary typed refusal |
| Faulted cached view, every binding/selection | Original terminal refusal, cleared output |
| Pure callback throws or mutates state/clock | First failure latched; no partial publication/save |
| Human vs decision-context refused command | Identical current code and actual result, no stock grant |
| Occupied build/site recovery | Ordinary typed wait/cancel/new intent, physical execution only |

## 6. Backward compatibility

Development-only. No compatibility/legacy path or alternate controller executor.

## 7. Verification

ONE NEW exact case, same-case repairs only; one solution compile attempt then
necessary targeted repairs. Terminal clean compile before runtime. Bounded
60000ms/termination5000ms/output30000chars, physical stages512Updates with last12
private diagnostics. No historical/family/full runtime. Actual master/JSON,
public seats/decision inputs/facts, saves/replay/sole Update clock only.

## 8. Risks

No universal hostile-CLR/private-purity proof or universal AI-strategy claim.
Do not hash the whole service during an unfinished Update. Provider observations
remain scoped guards over existing state, never a second ledger or sandbox.

## 9. Out of scope

Optional economics expansion/G19/clock changes, Oil/platforms/Draw, broad pressure/
knowledge/control-provider/stress/global/full P1-P12 and publication. Preserve
all original requirement text/anchors; claim only exactly qualified acceptance.

## 10. Sub-task execution order

1. Inspect actual controller/view/observation/public command paths and authored recovery.
2. Repair original observation/controller/view owners and update build identity.
3. Register one combined public-player proof; compile/run bounded exact only.
4. Freeze executable evidence, update tracker/stage/audit and check integrity.

Status: scoped implementation and exact verification complete. FINAL2503/0/0
in10036ms; four clean compiles/four runtime attempts retained. Only original
P11.acceptance.031 closed, unchanged literal/line1592. Original221 gates:
183checked/38open, additional177. Full goal/plan active, no staging/commit/push.
[Verification](../../docs/migration/P11_CONTROLLER_PARITY_BOUNDARY_VERIFICATION.md).

### Verification boundaries

Nine input forms mapped to both companies compare actual seat/decision previews
and refused results; native playbook uses same ordinary dispatcher. Four physical
native/independent-recovery minute/half-minute flows spend20→12 each company,
earn travel/cargo/work, retain saved answers/cancellation/replacement and replay.
Three actual authored failures gate cached own/foreign/missing/observer views;
first callback cause preserved, no retries or partial publication.

Historical overlapping startup at0 correctly blocks both point-space machines;
new JSON fixture authors rival depot/carrier70 vs player0, sites30/42. No collision
bypass or instant fallback. Half-minute proof waits actual one-minute cadence
for placement answer. Earlier bounded failures retained, no old runtime reruns.
Controller persisted mutation detection is source-reviewed, not a manufactured
hostile callback runtime. Broader provider/private/control/pressure/budget/global
gates remain OPEN. Metadata byte/literal/link closure follows exact evidence.

### Metadata closure

1499 source anchors/770 selected executable inputs/13 quickstart anchors match;
zero post-FINAL drift.962 Markdown documents/8580 local links valid;18 master
rows and all221 literal gates validate. Only P11.031 checkbox changed; zero
original text/document/line changes. Four prior selected inputs changed, nine
added (eight actual previously existing files plus new driver). Original
PlayerControllersView added as source anchor, alongside three new files.
Freeze2026-10-11 18:41:05 UTC; diff --check0/34 existing line-ending warnings,
dirty2723/staged0. Four protected clock/G19 hashes unchanged. No full regression
or completion/publication claim; all remaining38 original gates stay OPEN.
