# P7-PA2: typed physical haul port availability — Design Document

## 0. Problem Statement

Standard/HaulResourcePorts.cs PhysicalPort projects ResourcePortContracts.Bind(...).Store and drops the actual unavailable Blocker. ReadHaulEndpoint also returns nullable IResourceInlet. Logistics.Preview and LogisticsDiagnostics then translate legal named original loss into generic NoCapacity/NoStorage, concealing provider-calculated cause. Existing original independent receipts/cargo already survive correctly; these owners must not be duplicated.

## 1. Goals

1. PhysicalPort and IHaulExecution.ReadHaulEndpoint return the existing explicit ResourcePortResolution, never null-only availability.
2. Generalize that one result's Store to IResourceInlet so receiving-only unnamed ports remain legal; named IResourcePort validation still requires its existing outgoing-plus-incoming contract.
3. Haul previews/commands, source requests, stock/route configuration and private diagnostics preserve actual unavailable blockers, with no invented zero stock or current-endpoint substitution.
4. Existing destination recovery classification/policy permissions remain authoritative; legal unavailable access retains exact receipt/cargo/handling chronology through save/load.
5. Migrate all actual callers in place and prove named physical loss/restoration and malformed provider availability with bounded public inputs, plus resource-only manufacturing and receiving-only inlet support where actual composition permits.

## 2. Design

ResourcePortResolution remains the sole availability result: ReadyStore(IResourceInlet) or Unavailable(Code). ResourcePortContracts.Store validates malformed outcomes and named IResourceOutlet identity/company/capacity inside the actual provider/entity observation boundary. ResourcePortBinding casts only after that named contract validates. No competing endpoint result or stock model.

LogisticsService.PhysicalPort returns typed missing entity, missing storage/port, incompatible resource or original-unavailable results, and forwards exact named binding blockers. Actual pure resolution uses original EntityObservation and whole site state. All selector/discovery/demand/recovery/stock/scheduling callers consume this one result; mutation paths freshly re-evaluate readiness.

Preview/PreviewSource and diagnostic/configuration admission forward actual unavailable Code. ReadHaulEndpoint preserves the original job validation/endpoint identity and returns typed resolution. WorkBoard handling and receipt validation consume Store only where immutable authored quantum or actual surviving book validation explicitly permits an unavailable physical site. CheckDestinationBinding remains the existing MissingGeometry/DestinationBindingLost recovery classifier, not a new permission model. Load keeps existing transfer-status semantics.

## 3. API surface

ResourcePortResolution.Store and ReadyStore parameter change from IResourceOutlet to IResourceInlet. IResourcePort remains a named outgoing/incoming binding through validation. IHaulExecution.ReadHaulEndpoint now returns ResourcePortResolution. No legacy overload/null-resolution adapter, new public command or save payload. Core behavior identity2.478.0/save154/replay1.

## 4. Files to change

| Action | File | Lines | Risk |
|---|---|---:|---|
| Modify | Standard/ResourcePorts.cs, HaulResourcePorts.cs, WorkContracts.cs, Work.cs | 65 | high |
| Modify | Logistics/Diagnostics/Demands/Recovery/Scheduling/StockPolicies | scoped | high |
| Modify | actual utility/test outlet consumers and protocol/registration | scoped | medium |
| New | public-player physical availability proof | scoped | medium |
| Modify | stage/evidence/master | scoped | low |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing physical entity | Explicit NotFound |
| Missing named port/storage | Explicit NotFound/NoStorage, no local inventory fallback |
| Named unavailable original | Actual provider blocker; original receipts/cargo unchanged |
| Fresh unavailable source/destination | Same typed blocker in preview and command refusal; no acquisition |
| Malformed ready/blocked provider outcome or inlet-only named store | Original observed provider fault; healthy save denied |
| Legal unnamed receiving-only inlet | Ready receiving binding; no forced outgoing facade |
| Surviving book but inaccessible original physical site | Existing receipt validation/recovery policy retains authority |

## 6. Backward compatibility

Development only; migrate signatures and all callers directly, no compatibility/null fallback path.

## 7. Verification

One solution compile and necessary focused repairs only. ONE NEW exact bounded case through actual file/master, public player commands/previews/results, immutable views, Game.Update, saved twins, replay and one clock. No historical/family/full runtime.

## 8. Risks

Do not alter optional business/supplier semantics or existing declared recovery permissions. A paused demand may have unknown physical stock, never claim a computed zero stock or shortage. Broad demand-offer visibility/provider/private CLR/policy/route/congestion/stress/full matrices remain separately open. Saved references remain in original jobs/books, not the result struct.

## 9. Out of scope

Oil/platform/Draw/Tycoon/protected G19/sole-clock source edits, broad/full P1-P12 closure or publication.

## 10. Sub-task execution order

1. Generalize the original availability result and guard named result validation inside existing observation.
2. Migrate physical resolver/haul contract and every current logistics/work caller; preserve typed command/diagnostic blockers and original recovery permissions.
3. Add bounded public-player proof, retain all builds/attempts and update current evidence/master without shrinking original gates.

## 11. Scoped result

Core2.478.0/save154/replay1; existing common availability result now serves named/unnamed physical ports and typed haul reads. Actual preview/diagnostic/configuration blockers retained, malformed result validation observed, original recovery permissions/receipts/cargo/time owners unchanged. ONE NEW FINAL268/0/0 in8552ms; two receiving-only minute/half flows, four native/point minute/half original loss/restoration, two native/point whole production and five provider faults, saved twins/private facts/replay/isolation. Four builds/two runtime attempts retained; FINAL focused compile clean5.77s, no executable edit afterward or broad runtime. Original221/186checked/35open unchanged; additional205. Broad/provider/work-leaf/demand/policy/optional-business/congestion/stress/full P1-P12/publication OPEN. [Evidence](../../docs/migration/P7_PHYSICAL_PORT_AVAILABILITY_VERIFICATION.md).

Fresh read-only metadata closure:1696 source anchors,942 selected executable inputs,13 quickstart anchors; all SHA drift0. Inventory2884 paths/1018 Markdown files/8654 local links, broken0. All18 master rows parse; all221 original literal/checkbox/line anchors validate with0errors, unchanged186checked/35open. New driver boundary scan0, git diff --check exit0 (34 existing warning lines), dirty2872/staged0. Protected sole-clock/G19 hashes exact; goal_complete=false/additional205.19 existing selected executable inputs changed;4 newly selected inputs comprise3 newly created files and previously existing LogisticsDemands.cs.5 new source files;13 existing production sources changed/zero new. No runtime rerun for documentation checks.

