# P8-D5c scoped sources and standing routes — 2026-10-08

## Outcome

Implemented automatic public scoped-source discovery and named standing physical routes through
the existing LogisticsService/WorkBoard/ResourceTransactions owners. This is NOT full P8/P9 or
full P1-P12 completion. No staging/commit/push: publication requires FULL goal AND plan verified.

PreviewSource now enumerates actual local and sorted authored named resource-port definitions.
Only owned Public endpoints participate automatically; aliases deduplicate per physical site/
actual endpoint. The original shared comparison preserves interruption avoidance, export priority,
actual selected travel cost, stable site identity and ordinal names. Named-only sources require
no local inventory. Source endpoints are freshly re-resolved exactly for native whole acquisition.
No port registry, duplicate matcher/inventory/book, host rule or second clock.

The original Route stores source/destination names. ILogisticsRoutes aliases the same logistics
owner; add/settings/remove handlers use that contract. Management previews, private detached route/
offer/job facts, saves, hashes and replay share the original records. Future trips follow current
selection; acquired trips keep admitted stores. Later self-alias selection blocks new trips but
does not corrupt valid stored intent or prevent disabling/removing it. New actual-self intent
refuses. Different endpoints at one physical site remain valid and still load/unload over game time.
Removing/disabling a route affects future dispatch, not physical cargo already assigned.

Job source/destination endpoint identities are now admitted execution facts that outlive spent
pickup/delivery receipts. This fixes loss of original source observation after complete loading.
They are not extra reservations/stock owners. Live receipts validate against those bindings;
ReadHaulEndpoint resolves those original IDs. Existing explicit recovery replanning updates its
admitted destination together with its replacement receipt. That recovery branch was source-
reviewed/migrated for compilation only, NOT runtime-revalidated by this case.

Removed the obsolete input-supply-unavailable code after D5b removed its local-only path. Fixed
legitimate Code.None decoding in the existing manual-haul command. No old overload/format reader.

Normal scenario configuration is master-listed JSON, including actual public/private scoped store
exposure, recipes, warehouse stock, machine/navigation/capacity/handling and startup. EndpointPoolDef
now requires explicit public exposure, forwarded into its existing ResourceInventoryState. All22
older pool declarations across five JSON files explicitly author false; the new three stores author
true/true/false. Those older configurations/cases are compile-only, NOT runtime-revalidated.
Source stock is funded by ordinary timed hauling from JSON warehouse stock, never driver writes.
ScopedHaulGame is composition only; ScopedSourceRouteTests uses public commands/views/Update.

Current identity: **2.318.0 / save152 / replay1 / logistics service18 / WorkBoard47 /
logistics-view32 / work-view90**. Transformer9/transformation-view17/operations-view11/
inventory-view5 and GameClock are unchanged. Existing original codec/schema identities migrated
directly; no legacy reader or parallel persistence model.

## Verification actually run

FIVE compilation builds, `dotnet build Beep.SimEngine.slnx --no-restore`:

1. Clean5.26s, zero warnings/errors, native source/route contracts and master-listed JSON.
2. Failed5.89s: new player case assumed Route/From/To view members. Native projects compiled.
3. Clean5.90s, zero warnings/errors: expose native route provenance in WorkJobView; use its
   actual Source/Destination members. One view schema migrated, not a test-only engine shortcut.
4. Clean6.39s, zero warnings/errors: retain admitted job endpoint identities after spent receipts.
5. FINAL clean5.11s, zero warnings/errors: existing explicit recovery destination binding migrated.

ONE NEW distinct exact bounded case, THREE attempts:

`Scoped sources and routes: public alias discovery, original trips and same-site stores`

1. 107 passed/1 failed,2018ms, NO suite abort. View lost original pickup endpoint when its
   source receipt was fully spent. PID32256 exited1, observed2181ms, untruncated/no observer error.
2. 108 passed/0 failed/0 aborted,2055ms. Actual admitted binding now survives receipt exhaustion.
   PID28036 exited0, observed2197ms, untruncated/no observer error.
3. FINAL **108 passed/0 failed/0 aborted**,1981ms, after existing recovery binding integration.
   PID11836 exited0, observed2123ms; stdout/stderr untruncated, no observer error.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Scoped sources and routes: public alias discovery, original trips and same-site stores' --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Each stage bounds1000 Updates with process/offer/job diagnostics. Child exit was observed under
explicit wall timeout. Only this new case ran. No older/family/full/Oil/Tycoon/platform runtime.
All prior runtime reports are historical, NOT rerun on2.318.0. Post-pass executable freeze:
**2026-10-08 21:32:32 UTC**; subsequent edits are documentation/audit only.

Covered: authored resource-only/no-Tycoon initialization; named-only source without local fallback;
physical source funding; stable z-before-a alias discovery; automatic actual scoped pickup and
manufacturing; changed current source with original saved delivery; native command/management
preview parity/purity; foreign/missing/self/fractional refusal; private route facts and detached
retained settings; scoped pool-to-pool timed trips; changed source/destination and blocked future
self-alias; editable/disabled saved intent; remove with in-flight continuation; same-site different-
endpoint handling; saved twins through those stages; exact material accounting; native public
command replay; private-only store/warehouse excluded from automatic discovery; explicit owned
private-source route physically supplies manufacture with conserved stock.

## Still open

Scoped stock bands/per-port outlet policies and related automation; generic scoped work/DAG supply,
recovery and endpoint-specific operational diagnostics; explicit recovery replan runtime; current
provider/corruption/fault/removed original-store/fuel/road/dock/area/emergency/congestion/stress/
allocation matrices; supplier purchase breadth; global team/resource acquisition; full P8/P9/P1-P12.
The current focused proof does not establish old behavior suites or full industrial baseline green.
Keep remaining original acceptance text/gates and full publication condition unchanged.
