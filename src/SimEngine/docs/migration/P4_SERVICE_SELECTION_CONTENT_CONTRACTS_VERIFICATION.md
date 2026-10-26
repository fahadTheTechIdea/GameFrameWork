# P4-G10 — Fleet and facility policy content contract verification

Date: 2026-10-09. Scoped verified only; full P1–P12 and the active goal remain open.

[Stage](../../plans/enhancements/P4_SERVICE_SELECTION_CONTENT_CONTRACTS_DESIGN.md).

## Actual ownership

IFleetPolicy is an actual IContentDefinition. MachineFleetService admission, policy views, navigation control and restored membership resolve owned IFleetPolicy aliases rather than FleetPolicyDef. FleetPolicyDeclaration is the sole original native JSON reader, role resolver and admission decision. Native and direct extension definitions delegate to it; there is no second membership store.

RecurringPlanContracts and WorkBoard facility selection resolve IActivityFacilityPolicy rather than ActivityFacilityPolicyDef. ActivityFacilityPolicyDeclaration owns the original node/prefer_home reader and pure preference. Composition checks owned aliases and canonical node identities. The existing bound graph checks still require a positive machine node and compatible worker binding. Rank observes detached facts through the existing provider guard and cannot override readiness, dock capacity, travel, materials or worker selection.

The existing scheduler, recurring source, plan/order graph, worker jobs, reservations, stocks, physical navigation and sole GameClock retain all runtime state. Independent proof definitions derive directly from Def and implement the two contracts. Their JSON file is explicitly master-listed; merely loading a document never selects a module or policy.

## Current evidence

- Core beep-simengine-2.342.0; save154/replay1/work-view95. All existing state schemas and GameClock unchanged.
- Six build attempts: first failed7.71s with two proof compile errors; then clean4.65s,4.53s,4.36s,4.17s,3.88s. Every clean build had zero warnings/errors. No stale binary run.
- ONE NEW exact case: Service selection content contracts: independent JSON fleet and facility policies drive physical players.
- Same-case attempts:23/1 (7448ms,PID41192,exit1,observed7599ms);23/1 (7135ms,PID17332,exit1,observed7295ms);44/1 (849ms,PID40736,exit1,observed989ms);44/1 (843ms,PID18396,exit1,observed988ms); FINAL107/0/0 (17099ms,PID12628,exit0,observed17285ms).
- Runtime bound30000ms, termination2000ms, output24000chars. All attempts exited with untruncated output and no observer error. Physical waits are bounded at1500 Updates with immutable job/position diagnostics.
- Executable inputs frozen2026-10-09 04:30:40 UTC; no subsequent gameplay edits.

The compile correction used the actual WorkResourceBinding.Choice.Use receipt API. The first two runtime failures exposed unrelated parked workers blocking the reused one-dimensional service corridor. Their starting positions were explicitly authored outside that corridor, preserving collision/travel rules. The next two failures exposed an existing tile fixture authoring omission: layered navigation references a condition whose file and TileConditionsModule were not explicitly enrolled. The fixture now declares the actual module, scenario enrollment and sole document ownership. No route predicate, collision, transfer, service or runtime fallback was changed.

## Proven behavior

Point facility selection admits both actual stocked compatible docks. Nearest and home-first independent JSON preferences produce detached immutable candidate ranks; foreign players cannot read those facts. Normal home/preference commands do not grant stock or condition. An enabled recurring source admits a native plan; the worker physically reaches the home service bay at11 before productive progress and consumes exactly three spares32-to29 through original material/tool receipts. Future preference edits and disabled work retain the admitted target/progress. Sole-clock pause advances nothing. Resume completes native service and departure, condition4-to10 exactly once, with the unused source stock32 unchanged.

Tile fleet policies expose actual role dependencies and explicit navigation identities. Ordinary membership commands admit two actual workers in the sole store, reject overlapping membership, and preserve private membership. Normal navigation selection and an eight-unit haul use native physical loading. Whole-fleet disable retains cargo and position; enable completes physical delivery/unloading with20 total units conserved. Retained views stay detached; both point and tile saved twins, replay and fresh-session isolation pass.

Fifteen invalid authoring/configuration variants each refuse before Initialize: missing/empty/duplicate/unknown/wrong-kind roles; missing/empty/duplicate/invalid navigation lists; missing/malformed node; missing preference; unknown/timed bound probe node; wrong-kind facility profile. These are tested after valid point and tile composition succeeds, not masked by a missing fixture module.

## Limits and continuation

Historical native/extension callers compile, but their runtime suites were not rerun. Arbitrary custom-provider mutation/fault matrices, comparative fleet contention, every navigation/area shape, all queued dock/reroute cases, broad lockstep/performance/soak and the full acceptance gate remain open. This is one scoped capability proof, not global engine completion. No broad/family/historical/Oil/Tycoon/platform runtime; no business, mandatory budget/money, Draw, adapter or host gameplay changes.

The lifecycle skill influenced staged design, single-responsibility declarations and evidence reconciliation. User/repository rules override its broad-test and early-publication recommendations. No staging, commit or push before the full goal and plan genuinely finish and are verified.

## Read-only reconciliation

The original221 acceptance records retain all identities, requirement text and checkboxes (149 historically checked/72 unchecked);99 source-line offsets moved. No full phase was promoted to Done. The current metadata check found596 source and13 quickstart anchors with no mismatch,2095 files/699 Markdown documents,5135 local links with no broken targets, and18 valid five-column master rows. git diff --check exited0 (34 existing CRLF warning lines, no whitespace errors). The new thin-player driver has no raw World/resource/progress mutation. Sole-clock SHA256 remains35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1. The staged Git index remains empty. This reconciliation is not another runtime suite or a full dirty-worktree release audit.
