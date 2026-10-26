# P9-C10 — Fault-safe logistics and stock projections

## 0. Problem statement

InventoryView and LogisticsView have missing/private/empty early returns before the original World.RequireHealthyUpdate gate. Retained features can therefore describe a failed session as healthy idle state. WorkView.CopyJobs and queue delegates can similarly return empty or publish a successful prefix before later provider failure. This stage advances original P6 failure boundaries and P9 logistics diagnostics, not whole-phase completion.

## 1. Goals

1. Every InventoryView and LogisticsView query rejects a faulted session before missing/private/no-policy/empty results.
2. Cargo, demand, route, work-job and dispatch lists publish detached rows only after the whole read remains healthy; failed outputs are cleared.
3. Original physical hauling, reservations, saved partial work and independent sessions still use the sole resource/work/clock owners.
4. Retained features preserve the first recorded failure; queries never repair or advance gameplay.

## 2. Design

Use World.RequireHealthyUpdate at existing view boundaries, not a new fault state or wrapper service. For collection reads clear the caller output, gather local detached facts, recheck health, then publish. Observe actual cargo/transport/provider codecs through existing EntityObservation.ReadProvider. Preserve original service evaluation, access rules and native resource contracts. WorkView changes are limited to physical jobs, dispatch queues/assignments and optional dispatch-policy queries; remaining policy/planning/provider breadth stays open. Reuse the shared-haul content/factory with an explicit fault-test scenario selecting existing negative PriorActivityProofModule and LogisticsProjectionFaultModule. The latter contributes only a deliberately failing contract callback discovered through original service enumeration; it never replaces primary dispatch ownership. Fault injection is negative only; drivers continue issuing ordinary commands and reading facts.

## 3. API surface

No new gameplay API. Existing InventoryView, LogisticsView and affected WorkView queries now consistently throw InvalidOperationException for an already faulted session. Output references/lists clear before rejection. Build/view identities change; saved payload format does not.

## 4. Files to change

Modify the three existing views, module view registrations and EngineInfo build. Add one focused test and explicit fault-probe master; reuse existing content/factory and negative probe. Update this stage, evidence, master and audit without closing original compound gates.

## 5. Error handling

Healthy missing/private/no-policy results keep their current refusal semantics. Faulted queries throw before those results. A callback failure records the first error through the existing observation guard and exposes no list prefix. Caller publication errors are not gameplay errors. No retry, teleport, state rollback or synthetic stock.

## 6. Compatibility

Development only; no shim or old-build save reader. One owner per capability.

## 7. Verification

Build the completed slice. Run ONE NEW exact bounded case only (30000ms child limit, 5000ms termination). Same-case repairs allowed. Exercise public timed hauling and checkpoint continuation before injecting a real update failure, then retained missing/private/empty/normal queries. No historical, family, full, Oil, Tycoon or platform runtime.

## 8. Risks

Do not claim generic provider rollback or all-view fault coverage. Detached retained facts remain usable as historical facts, but new queries must reject. Extra collection allocation is transient; allocation budgets remain unproven. Keep protected G19 files unchanged.

## 9. Out of scope

Remaining work-policy/area/planning views, arbitrary provider mutation/stress/global gates, full acceptance, Oil/Tycoon/platform/rendering, staging/commit/push.

## 10. Execution order

Inspect original gates/providers; revise existing view boundaries; author the one explicit negative master and focused case; build and run that case; preserve all attempt results and final identities; update the authoritative tracker/audit. Full goal stays active.

Outcome: FINAL197/0/0 in666ms, clean final build4.48s. All six builds/five same-case attempts retained; no original compound gate changed. [Evidence](../../docs/migration/P9_LOGISTICS_VIEW_DURABILITY_VERIFICATION.md).
