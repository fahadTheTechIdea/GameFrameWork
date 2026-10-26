# P9-D5d actual-port stock/export policies — 2026-10-08

## Outcome and scope

Implemented scoped stock bands and named outlet settings through the original LogisticsService
Demand rows and the original singular IOutletPolicy component. This is not full P9/P8/P1-P12
completion. The goal remains active; no staging/commit/push before full plan and goal verified.

Check/configure/read commands and private views accept an explicit port and optional original
stock endpoint. One actual endpoint/resource owns one band across aliases and physical sites.
Editing aliases preserves original source identity and age. Explicit original references retain
management access after selection changes; a new current selection does not silently move a band.
Hysteresis, stock protection, aggregate multi-resource maxima and incoming ceilings observe that
same actual store and its native promises. Acquired Job bindings/receipts survive later policy edits.

Native logistics_policy now requires explicit profiles:[{port:null|string,profile:identity}].
The existing component stores only its authored port settings; stock/refill/age stay with demands.
Null denotes actual local stock. Named profiles validate exact IResourcePortDefinition metadata.
Conflicting authored bands on aliases of one actual endpoint refuse initialization. There is no
old scalar profile reader, second ledger, inventory, policy service, matcher, scheduler or clock.
One disabled alias does not mark the whole multi-port site disabled if another outlet is enabled.

The existing pure ResourcePortContracts binding owner supplies named stock metadata; an explicit
lost/unknown original returns an unavailable physical binding, never a substitute selected store.
Policy admission also guards the actual non-entity endpoint metadata/version. Same-store local
aliases inherit applicable actual local settings; unrelated scoped stores never inherit them.
Source candidates must evaluate each explicit port policy before choosing the one physical offer:
an earlier disabled alias cannot conceal an enabled later alias. Stock and claims are not doubled.

Existing stock/reserve action definitions expose canonical port/endpoint management references.
Only the original declared stock targets admit automation, preventing alternate aliases or
noncanonical encoded names from bypassing exclusive arbitration. Actions issue ordinary native
commands on the next declared step; no control-action stock copy or independent refill logic.
One rule may address its original store while the current port selects another store.

All baseline gameplay is master-listed scoped-policies JSON: ports/pools, public exposure,
reusable profiles, quantities/capacities, handling, navigation, source stock, startup and normal
resource-only mode. The driver only submits player commands, reads detached views, updates and
saves/replays. Materials are physically hauled from a private warehouse; no direct funding writes.
Money/Tycoon/Oil/platforms/Draw are unchanged and unnecessary for these controls.

## Verification actually run

NINE clean compilation builds with dotnet build Beep.SimEngine.slnx --no-restore:
12.24s,6.19s,6.85s,4.41s,4.08s,3.45s,3.78s,6.28s,FINAL5.58s; each zero warnings/errors.
These include compile-only migration of all seven prior native JSON bindings across six files,
seven inline native/conditional declarations across three older test files, and the existing
independent public policy contract. Those earlier scenarios were NOT runtime-revalidated.

Only ONE NEW distinct exact case was executed, seven attempts:

1. 2 passed/1 failed,91ms: missing explicit embedded-resource entries for the two new JSON files.
2. 2 passed/1 failed,90ms: fixture signal and inventory port both used the same named ID stock;
   corrected the signal to its distinct authored warehouse-stock identity.
3. 56 passed/1 failed,1309ms: player requested twelve units in one eight-unit cargo trip;
   the engine correctly retained all remaining source material and enforced its reserve.
4. 56 passed/1 failed,1170ms: same bounded stage exception, now with direct native preview
   diagnostics identifying the expected reserve refusal. No engine rule changed for this wait.
5. 128 passed/0 failed/0 aborted,1753ms: corrected the player funding to actual eight-plus-four
   physical trips. Policies, aliases, real supply/delivery, saved twins and replay passed.
6. 133 passed/0 failed/0 aborted,1915ms: added canonical target forgery refusal, ordinary command
   parity for missing originals, named destination filters and exact reserve quantum checks.
7. FINAL133 passed/0 failed/0 aborted,1811ms after inline declaration migrations and actual
   endpoint admission guard. Child PID38628 exited0, observed1959ms, stdout/stderr untruncated,
   no observer error. No OS timeout/forced termination; the four earlier exceptions exited1.

Exact command:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Scoped stock policies: actual bands, port exports, aliases and normal control actions" --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Current protocol: core2.319.0/save153/replay1/logistics-service19/logistics-policy-component3/
logistics-view33. Original WorkBoard47/work-view90/transformer9/transformation-view17/
operations-view11/inventory-view5 and GameClock unchanged. Post-pass executable inputs frozen
2026-10-08 21:57:27 UTC; subsequent changes are documents/audit metadata only.

## Boundaries still open

The exact case proves native scoped bands/port exports and ordinary scoped control actions,
not broad provider purity/fault/corruption/removed-store matrices or every configuration.
The new conflicting-alias initialization check is source/compile-reviewed, not exercised with
a deliberately conflicting JSON fixture. Existing explicit recovery layout/source changes are
not a new scoped recovery runtime proof. Generic DAG physical supply, scoped recovery/diagnostics,
emergency-only access, docks/operating areas/congestion/fuel/routes/provider/stress/allocation/
global accounting/lockstep/shuffled arrival/full durability remain open. No historical/family/
full/Oil/platform suite was run. Full acceptance/goal/publication remain incomplete.
