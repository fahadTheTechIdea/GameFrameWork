# P4-I3 — One optional supplier purchase settlement

Date:2026-10-07. Build `beep-simengine-2.212.0`, save116.
Scope: native synchronous supplier purchasing through one optional Tycoon account owner.
Not full trading/seller markets, enabled timed haul/retry, budgets, replay or roadmap acceptance.

## Final result

One exact new scenario: `Tycoon purchasing: sole settlement and aggregate physical admission`.
73 checks passed,0 failures,0 aborted suites,273ms. Final clean solution build0 warnings/errors2.77s.
Five build invocations and three invocations of THAT SAME scenario; chronology below. No older,
family, full, OilFieldDays or platform suite was run.93 existing metadata files migrate151 identity
lines. Four older scenario files also migrate affected contract assertions/signatures; all compile
only, NOT rerun. Account codec3 and physical board40/work40/work view73/transformation view9 remain;
business view4 and supplier component4 reflect the changed public capability/callback contracts.

## Ownership and implementation

- Core contracts separate immutable trading admission, authored supply prices, complete purchase
  intent, prepared account settlement and an exact physical callback scope. None owns another balance,
  stock book, purchase ledger, saved ticket or loop. TycoonModule remains the sole native economics owner.
- IResourceTradingRules aliases the existing immutable BusinessModeRules. IResourcePurchaseSettlement
  aliases the SAME BusinessAccounts/IEconomy/IBusinessAccounts instance. Existing account Sync persists
  actual balances; purchasing introduces no payload/state fields. Game-mode identity already saves flags.
- Existing JSON supplier.sells values price native resource quantities in the explicit selected account
  definition's currency. No guessed currency, valuation substitution or exchange-rate fallback. Basic
  purchasing requires money/trading, not resource valuation or advanced Tycoon activation. Advanced-on
  still explicitly refuses. This is existing fixed supplier provision, not a new seller-revenue market.
- ResourceTransactions validates all actual receipts/endpoints/quantization, admits the complete purchase
  vector through the optional settlement before physical mutation, and sorts transfer legs deterministically.
  Actual commit repeats admission; individually affordable legs cannot overdraw aggregate money. Charges
  group by actual buyer account; unrelated indebted accounts remain untouched.
- After every source/inlet receipt and physical delta verifies, one ephemeral settlement commits, endpoints
  are checked again, then existing resource facts publish. Duplicate/stale prepared payment checks the
  entire captured account vector before debiting anyone. Positive-price under-quantum/range costs refuse.
- Supplier and public-only FiniteSupplier no longer contain affordability, account lookup or TryCharge.
  They expose pure price/physical capabilities. Raw fulfillment requires the exact transaction-issued
  ResourceSupplyExecution scope, invalidated on success/failure; no legacy signature/constructor API.
- Logistics and physical transactions use optional contracts, not concrete BusinessModeRules or copied
  native flags. Haul preview additionally checks actual candidate-load affordability; actual loading repeats
  current transaction checks. No money is reserved while traveling in this slice.
- BusinessView.HasPurchasing reports installed capability. Accounts-only/dormant vendors refuse trading;
  ordinary unpriced engine work continues without Tycoon, money or substitute accounts. Stock-transfer
  admission contains no new economics dependency; module-omitted stock/mixed-leg runtime matrix is pending.

## Executed commands and complete chronology

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Tycoon purchasing: sole settlement and aggregate physical admission"
```

1. Build failed with one new compile error,0 warnings,1.83s: ReservationToken has Equals, not ==.
2. Repair exact equality. Build failed with one new test compile error,0 warnings,4.39s:
   JsonValue.Of has no numeric overload; authored seat count must use Json.Parse("2").
3. Repair numeric authoring. Clean build0 warnings/errors2.88s. First exact scenario0 passes,
   one fixture exception118ms: existing PointStart requires an explicit valid company, not owner-1.
4. Author vendors as company1 without changing startup/game rules. Clean build0 warnings/errors5.10s.
   Second same scenario0 passes, one fixture exception126ms: scenario capacity2 does not automatically
   create two players; request omitted the second human seat.
5. Supply both human player seats through normal NewGameRequest. Clean final build0 warnings/errors2.77s.
   Third SAME exact scenario73 passes,0 failures/aborts273ms. No gameplay edits after this successful gate.

## Covered behavior

Actual owner aliases and view capabilities; values/advanced independence; pure previews and unchanged
game time; transaction-only raw fulfillment; shuffled multi-leg supply; aggregate shortage with no partial
receipts/stock/payment; spent receipt refusal; partial quantity/remainder and saved continuation; native
creation and public-only finite stock supply; disabled and dormant haul/direct refusal parity; two buyer
accounts and unrelated debt; timed resource-only work with module omission; account range/invalid/neutral
admission; stale prepared payment; finite provider lying about consumption before destination/payment.

The last fault case is explicitly DIRECT framework contract instrumentation, not normal Update fault
capture or resumable recovery proof. A violating provider may leave irreversible receipt mutation; no
compensating/fake success is claimed. Scoped native success does not validate arbitrary custom mods.

## Source, clock and documentation evidence

See [scoped source inventory](P4_SUPPLIER_PURCHASING_SOURCE_MANIFEST.json) for current hashes:
168 files,0 duplicate paths,0 hash mismatches.13 changed documentation files contain1093 local
links,0 broken targets. These are read-only source checks, not additional runtime suites.
One unchanged GameClock; SHA256 `339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`.
Exactly one constructor remains in Kernel/World.cs. Local documentation link/hash checks are read-only
source validation, not another runtime suite. No Draw, adapter, host/demo or OilFieldDays changes.
Earlier income/ownership reports and their manifests remain historical; their suites were not rerun.

## Still open

Enabled timed truck loading/travel/unloading, funds changing during travel, recovery/retry/congestion,
mixed stock/supply and module-omitted stock acceptance, persistent competing budget obligations, impure/
invalid price and settlement provider matrix, normal Update/save refusal on provider faults, global native
mode/fixture migration, replay/lockstep/session stress, seller revenue/markets and actual advanced Tycoon.
H2/H3/I4, affected full acceptance and the full implementation goal remain open.
