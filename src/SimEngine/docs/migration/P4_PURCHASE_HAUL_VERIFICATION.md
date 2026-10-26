# P4-I3 — Timed supplier hauling and physical funding

Date:2026-10-07. Build `beep-simengine-2.213.0`, save117.
Scope: native/public-contract real-time purchases, payment/route retry and saved continuation.
Not full budgets, endpoint/fuel/dock/provider/replay/advanced-market/framework acceptance.

## Final result

ONE exact new scenario: `Tycoon hauling: timed purchases, payment waits and physical continuation`.
98 checks passed,0 failures,0 aborted suites,484ms. ONE clean solution build0 warnings/errors5.66s.
First attempt: no compile/fixture repair, repeat gate, older/family/full/platform/OilFieldDays suite.
93 existing metadata files migrate151 build/save assertion lines; their scenarios compile only,
NOT rerun. Physical board40/work40/work view73/transformation view9/account3/business view4/
supplier4 payloads remain unchanged. No gameplay edits after the successful gate.

## Actual engine ownership

- `HaulHandlingCodes.PurchaseRefusal` translates typed purchase refusal to the SAME stable live
  payment/stock/capacity wait identities. Logistics preview no longer invents enum-derived blockers.
  This is physical protocol translation, not another economic rule or purchase authority.
- Existing LogisticsService/WorkBoard/WorkSystem perform source selection, dispatch, native receipts,
  movement, progressive handling, cargo protection, policy completion and authored retry. Existing
  ResourceTransactions and optional Tycoon accounts settle only verified actual load chunks. Native
  prices/values remain explicitly authored; dispatch/travel/unload/load restore never settle again.
- Four explicit JSON documents declare physical content, master module/document composition,
  independent mode flags and optional account/value/income settings. All new gameplay inputs are
  authored JSON and registered native/public contracts; no scheduler/provider switch is introduced.
- Existing public-only point-space/finite-supply extension contracts execute under the engine loop.
  Tests introduce no gameplay provider. Tests only submit player stock-policy/production/access
  commands, read immutable views/actual authoritative state, save/load and drive bounded Update.
  They never assign/create jobs, reserve/consume receipts, call load/unload, charge/credit accounts,
  synthesize funding, calculate movement/handling rules or implement retry logic.
- The SAME Tycoon account owns balances, supplier purchase settlement and generation income.
  No new store, job, queue, financial codec, scheduler or clock. Basic purchasing/income do not
  activate advanced Tycoon; resource-only hauling installs no fake monetary provider.

## Executed gate

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Tycoon hauling: timed purchases, payment waits and physical continuation"
```

Build: clean,0 warnings/errors5.66s. Same turn's only scenario:98 passed,0 failed484ms.
Every stage drives at most240 engine updates with job/position/cargo/destination/refusal/retry
snapshot on timeout. It is one coherent new affected scenario, not a broad substring family filter.

## Proven behavior

- Native authored-creation and independent finite-stock supply: preview chooses an actual carrier;
  no payment at dispatch/travel. Partial load2 debits10, full load8 debits40. Physical cargo travels,
  destination unload proceeds in earned chunks, and finite source ends with12 of its original20.
- Saved outbound travel, partial loading, full-cargo travel, partial unload and completed continuation
  preserve identical hashes, exact stock and balances. Unload/restoration never pays a second time.
  Fulfilled desired stock8 stops buying; purchased creation does not earn generation income.
- Real native manufacturing starts while a truck travels, spends90 of explicit opening100 and
  creates1 physical support unit after its duration. Only2 supplier units can subsequently load for10;
  unpaid remainder waits at the supplier with cargo2, unchanged earned service and authored backoff.
- Saving/retrying the payment wait preserves cargo/progress/account zero. Player orders native timed
  generation of6 support units; the separately whitelisted primary policy earns30 after actual output.
  That income funds the remaining6 supplier units; the SAME saved shipment resumes and delivers8.
  Final balance0, generation settlement count1, no direct fixture credit or double reward/payment.
- Unaffordable full-load preview and derived dispatch facts expose haul.payment-wait. No unaffordable
  job, cargo or charge is created. Money-only/trading-off profile keeps explicit disabled-trading refusal.
- Player route closure stops a loaded machine, preserves cargo8/balance60 and saves authored wait.
  Reopening actual access causes physical delivery, without repurchase or teleported resources.
- Resource-only mode replaces the source with authored real stock and omits Tycoon. The same public
  stock-policy command moves8 from source20 through real progressive hauling to destination, ending
  source12/cargo0 with identical saved continuation and NO economy/trading/settlement services.

## Source and boundary evidence

See [scoped source inventory](P4_PURCHASE_HAUL_SOURCE_MANIFEST.json) for current hashes:
179 files,0 duplicate paths,0 mismatched hashes.14 changed documents contain1118 local links,
0 broken targets. These are read-only source checks, not extra runtime suites.
GameClock SHA256 remains `339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`;
exactly one constructor remains in Kernel/World.cs. No independent timer, money loop, rendering,
adapter, host/demo or OilFieldDays edits. Read-only diff/hash/local documentation-link checks are
source validation, not extra runtime suites. Previous reports/manifests remain historical and unchanged.

## Still open

Persistent competing budget reservations, full price/valuation/settlement/commit adversarial matrix,
endpoint removal, fuel/service shortage, dock capacity/congestion/recovery matrix, mixed stock/supply
acceptance, replay/lockstep/session stress, global older fixture/profile migration, seller revenue,
market exchange, advanced Tycoon activation and broader I4/H2/H3/framework completion. Point-space
proof does not validate every spatial/media/provider implementation or a game platform renderer.
