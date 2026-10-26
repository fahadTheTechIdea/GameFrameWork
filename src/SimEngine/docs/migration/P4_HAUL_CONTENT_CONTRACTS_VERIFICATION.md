# P4-G12 — Hauling profile content contract verification

Date: 2026-10-09. Scoped verified only; full P1–P12 and the active goal remain open.

[Stage](../../plans/enhancements/P4_HAUL_CONTENT_CONTRACTS_DESIGN.md).

## Actual implementation and ownership

IHaulLoadProfile/IHaulTripProfile are actual IContentDefinition identities. Native/external carrier definitions, initial/live/saved settings, native LogisticsService composition, load/trip decisions and private trip/service diagnostics resolve actual owned contracts rather than native profile bases. Native classes remain implementations, not an inheritance requirement.

HaulLoadProfileDeclaration owns the original JSON reader, BillSpec resource resolution and minimum/deadline/emergency decision. HaulTripProfileDeclaration owns the original JSON reader, explicit service IDs and native willingness decision. Direct Def implementations delegate to these same stateless declarations. No duplicate policy parser, age/fuel/cargo/reservation store or scheduler exists. Profile callbacks, service metadata and actual composition use the existing provider observation guard. Cargo capacity/native resource/time checks and bound scheduled service compatibility remain authoritative.

An actual saved-demand defect was fixed in LogisticsDemands.ValidateLoadedDemands: demand.Port.IsNone identifies ordinary inventory and must not be passed to EntityDef.Component<IResourcePortDefinition>(namedId). Named definitions are now queried only for named ports. Ordinary stock uses its actual resolved inlet; missing/disconnected named endpoints retain the original exact-definition validation and no inventory substitution. Demand/job/claim payloads and existing component/service/view schemas are unchanged.

## Current evidence

- Core beep-simengine-2.344.0; save154/replay1/work-view95/logistics-view40. Sole GameClock and existing state codecs unchanged.
- Seven build attempts: first failed9.02s with3 new-driver API-name errors; clean4.92s,3.79s,5.81s,3.68s,3.81s,5.43s thereafter, all zero warnings/errors. No stale binary run.
- ONE NEW exact case: Haul content contracts: independent JSON batching and trip rules retain physical fuel and cargo.
- Same-case attempts:0/1 (131ms,PID24404,exit1,observed289ms);24/2 (429ms,PID23968,exit1,observed571ms);71/1 (1023ms,PID22612,exit1,observed1168ms);74/1 (1003ms,PID19820,exit1,observed1157ms);75/0 (1034ms,PID8668,exit0,observed1181ms); FINAL75/0/0 (1025ms,PID33696,exit0,observed1166ms).
- Runtime30000ms, termination2000ms, output24000chars; all exited/untruncated/no observer error. Per-workflow wait700 Updates maximum with actual immutable job diagnostics.
- Executable inputs frozen 2026-10-09 04:52:53 UTC; no subsequent gameplay edits.

The driver compile errors used incorrect dispatch/route property/type names; corrected to actual Haul.Blocker and RouteView. The first runtime used an old retry-only fixture file that omitted required authored work executors; the new case selects the existing full work-retry.json declarations instead. The next run revealed the real default-stock saved-demand defect described above. Later assertions incorrectly expected the aggregate queue to choose a specific blocked carrier and manual player hauls to obey automatic batching. The explicit unrelated courier now belongs to the other company; actual queue admission exposes the intended truck's minimum blocker. Manual command preview is verified as the existing explicit batching override. No logistics ranking, physical movement, packing, timing or manual semantics were changed to make tests pass.

## Proven behavior

Master-listed haul-contracts.json registers direct independent profiles. Native cargo/stock/trip rules admit their initial minimum8/wait5/reserve settings. Selected metadata and settings are private and immutable; unknown contract kinds refuse without changing the hash. Actual trip funding plus a protected fuel floor refuses through the same native preview. Normal configuration explicitly selects the authored no-floor profile without granting fuel.

An ordinary automatic route requests4 supply units. The actual native queue reports logistics.load-minimum and a young request remains unloaded. Explicit manual command preview remains an allowed override of automatic batching, not a teleport. Original request age/deadline later admits real timed loading into the actual cargo store. Future route repetition is disabled by its normal command.

Disabling the real trip policy holds the loaded job, cargo4, fuel and physical position; its private remaining budget reports the same typed disabled blocker. Saved twins continue that hold, and sole-clock pause advances nothing. Normal enable resumes actual movement and elapsed fuel consumption, then timed unloading delivers exactly4 with20 total supply units conserved. Original state codecs, private settings, retained snapshots, replay and fresh interleaved sessions pass. Saved ordinary stock-demand rows now restore through their correct default-inlet path.

Fourteen invalid variants each refuse before Initialize: missing enabled/bypass/minimum/wait; unknown resource and over-capacity native minimum; missing trip enabled/preserve/service array; duplicate/malformed/unknown service; wrong-kind load/trip initial binding. Positive recurring service execution is historical/compile-only, not newly proved by the explicit empty service set.

## Read-only reconciliation

All221 original acceptance records retain identical IDs, phase, document, text and checkbox;99 source-line offsets refreshed. Historical runtime deliverables are explicitly not revalidated on2.344.0. Eight checkpoint headers and12 matching master status cells updated without closing a full phase.

Source audit:611 anchors plus13 quickstart anchors, zero mismatches;2105 file paths,703 Markdown files,5223 local links with zero broken links. All18 master rows retain five columns. git diff --check exits0 with34 historical CRLF warning lines, not errors. New thin-player driver has zero raw World/resource/progress mutation matches. Sole GameClock SHA256 remains35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1. These are read-only metadata checks, not a runtime suite. Executable frozen inputs and empty staging index are checked separately.

## Limits

Historical native/tile/external carrier callers compile but were not runtime rerun. All positive service-source combinations, provider mutation/throw/reentry matrices, exact fuel consumption/accounting across every route/quantum, named disconnected demand breadth, priority contention, emergency bypass, route destruction, endpoint loss, broader lockstep/performance/soak and full acceptance remain open. No broad/family/historical/Oil/Tycoon/platform runtime, business expansion, mandatory budget/money, Draw/adapters or host gameplay.

The lifecycle skill influenced staged design, shared declarations and evidence reconciliation. User/repository rules override broad-test and early-publishing recommendations. No staging/commit/push before full goal and plan completion and verification.
