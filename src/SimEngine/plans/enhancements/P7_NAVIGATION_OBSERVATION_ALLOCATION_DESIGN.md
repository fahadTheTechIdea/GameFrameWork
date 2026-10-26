# P7-NG1 navigation observation allocation — design and implementation

> Follow-up [P7-NG2](P7_ROUTING_CALLBACK_CONTEXT_DESIGN.md) on2.663 removes remaining captured condition/entry-cost callbacks through the SAME observer. [Measured six-operation allocation and focused fault/physical evidence](../../docs/migration/P7_ROUTING_CALLBACK_CONTEXT_VERIFICATION.md). NG1's older all-tier result below is historical and was NOT rerun; no current full fleet/performance acceptance or broader gate closure is inferred. Current user restriction remains focused tests only, including stage completion.

## 0. Problem and authoritative baseline

P7-SB1 on2.497.0 reduced repeated per-search entry reads, but32-carrier tiers still exceeded the unchanged32MiB peak Update and1GiB accumulated Update budgets. Last native half replay exceeded180000ms. Original P7.acceptance.004 remains open; original221/188checked/33open. The prior turn made concrete engine/evidence progress, not a verified wait or no-progress turn.

Current Standard/Navigation.cs registers ITileNavigationRule through a forwarding closure. NavGrid.Cost guards that closure/space, then TileNavigationContracts.ReadCost guards the actual rule/same space again. The outer closure has no independent persisted owner and performs no gameplay operation after the inner read. Raw callback registrations are different: they require NavGrid's original guard.

## 1. Goals

1. Actual contract rules use one existing TileNavigationContracts admission/observation boundary, without a second wrapper scope.
2. Value-context invocation removes a hot captured closure without pooling or sharing exposed codec hashers.
3. Existing EntityObservation remains the sole guard for clock/identity/provider/target/space and first-fault semantics; all previous convenience callers delegate to it.
4. Independent JSON rule and raw callback paths retain authored cost, invalid-cost rejection, callback exception, clock and spatial mutation faults, retained healthy publication and session isolation.
5. Same complete8/32-carrier physical workload retains conservation/reservations/timing/twins/replay and satisfies original budgets. A completed bounded result is required, not partial metrics.

## 2. Design / API surface

Kernel/EntityObservation.cs: add internal ReadProvider<TContext,T> with explicit value context and Func<TContext,T>. Move the current guard body here unchanged except read(context). The existing Func<T> entry validates its arguments and delegates using a cached stateless invoker per result type. No cached World/provider/target, state copy, second validator or public platform API.

The first bounded case completed with9815 passes/6 budget failures, all guard checks healthy. It identified remaining bound Sync delegate allocation. Existing ProviderScope now records the original codec source (component, system, service, space, controller, explicit callback or none), then directly invokes that same original codec into a fresh StateHasher before/after the callback. Preserve automatic precedence exactly. Selected SpatialScope explicitly uses ISpatialSpace.Sync even if the space also implements another persistence protocol; explicit callbacks retain their exact projection. No codec result, pooled/escaping hasher, persisted state or alternate observation owner is added.

Standard/TileNavigationRules.cs: ReadCost supplies a value tuple containing actual World/rule/minimum/tile and one static stateless read delegate. ReadCostCore keeps original context/minimum/cost conformance checks inside the same guard.

Standard/Navigation.cs: the same NavGrid stores either an actual ITileNavigationRule or a raw callback, both registered through one NavService.Register. Contract Cost calls original ReadCost directly; raw Cost retains original guard. No unguarded native-special-case, new router, gameplay shortcut, cross-update cache, capacity arithmetic or world-size table. Raw null callbacks reject explicitly during registration.

The second bounded case completed with9826 passes/6 budget failures. Navigation's origin constraint scan still allocated a boxed array enumerator for every searched tile. Scan the same frozen component list by index in the same order, and invoke any actual origin constraint through value context in the existing guard. No constraint is omitted or cached; this does not change reach/route admission or add a geometry shortcut. Remaining budget failures are retained until a completed corrected workload proves otherwise.

Engine identity2.498.0/save154/replay1. No authoritative layout changes; prior builds are not claimed compatible.

## 3. Files and contracts

Modify the three engine sources above and Kernel/SaveGame.cs identity; current protocol fixture. New independent ExtensionProof/NavigationObservationProof.cs implements ITileNavigationRule from content/navigation-observation.json and exposes only actual NavGrid cost through an engine read feature. Explicit pause-after-Update fault injection is test-only. Probe layer and module manifest are file authored, loaded through original master composition. No gameplay behavior in the host/test driver.

Existing CapacityLogisticsStressTests exposes its same JSON factory/physical workflow to the new exact driver; no copied simulation driver. New NavigationObservationAllocationTests exercises13 raw/contract healthy/fault sessions, including mutation of an actual per-session persistent raw callback target, then calls the original physical helper directly, not its registry case. Register one new exact case and explicit full-profile entry. No old/family/full suite invocation.

## 4. Error and durability semantics

Throws/negative/below-minimum results never escape as usable costs. Clock/actual layer mutation is rejected by original scopes, first fault latched, next Update stops and preserves each viewer's last healthy space/time with no fabricated delta. Valid queries preserve hash and sole clock; saved/restored and idle sessions remain isolated. No broad sandbox claim about arbitrary unrelated provider writes.

## 5. Verification order

1. Source review of both registration paths and sole guard ownership.
2. One solution build; necessary focused compile repairs only.
3. One NEW exact bounded case,180000ms/grace5000/output24000; same-case repairs only. Independent guards plus existing all-tier workload; memory budgets unchanged.
4. Metadata-only selection/source/links/master/original gate audit. Close original P7.acceptance.004 only after complete declared workload and both budgets pass. Other broader phase/full/Oil/provider gates remain open.

## 6. Risks, exclusions and next work

Do not cache cost results across searches/Updates or weaken callback guard scopes. New generic adapter must preserve target/provider/space argument and exception semantics. Static delegates contain no session data. Raw callback path must not accidentally bypass its only guard. No Tycoon/Oil/platform/Draw/clock changes, legacy shim, pooled escaping hasher or narrower fleet. This is not every dock/incident/topology/quality/fault/soak/all-thread allocation case or whole framework acceptance. Final full regression only after implementation finishes; commit/push only after full goal and plan finish.

## Status

VERIFIED-SCOPED on2.498.0/save154/replay1. NEW exact FINAL9832 passed/0 failed/0 aborted in142153ms; bounded PID7144/process142441ms/exit0, no output truncation or observer error. All13 raw/contract healthy/fault sessions and8 physical fleet tiers complete, including saved twins/replay/isolation/conservation/reservations. Worst peak19232288 bytes and accumulated850639128 bytes are below original33554432/1073741824 budgets.480 hauls across all tiers; physical completion clocks unchanged. No executable edits after FINAL. [Evidence](../../docs/migration/P7_NAVIGATION_OBSERVATION_ALLOCATION_VERIFICATION.md).

Close only original P7.acceptance.004:221 original gates/189 checked/32 open, additional229. Original requirement text/line anchors unchanged, one checkbox changes. Broader phase/transaction/dock/global/fault/full/Oil/acceptance gates remain open; whole goal and plan incomplete. First two budget-failure attempts and failed C#9 compile are retained. Final full regression remains deferred until implementation finishes.

## Metadata closure

Post-pass freeze2026-10-12 16:41:19 UTC:1789 source anchors/1004 selected executable inputs/13 quickstart anchors match actual source with0 drift. Inventory2974 files/1065 Markdown documents/8755 local links/0 broken links;18 valid master rows;221 original literal gate/line anchors validated with0 errors. Original189 checked/32 open, additional229. Nine previous executable inputs changed, three newly created proof inputs; four existing production files, no new production source or authoritative layout. Sole clock/all12 Tycoon sources unchanged. git diff --check exit0 with34 pre-existing warning lines;2962 dirty entries/0 staged. Final documentation annotations are hashed separately; no executable edits after FINAL. One original checkbox changes (P7.acceptance.004),0 original text/line-anchor changes. Full goal/plan and broader acceptance remain open; no old/family/full registry run or staging/commit/push.
