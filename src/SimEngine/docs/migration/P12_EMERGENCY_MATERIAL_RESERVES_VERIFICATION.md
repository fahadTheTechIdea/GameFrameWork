# P12-D9 — Emergency material reserve verification

Date: 2026-10-06. Working-tree evidence, not a clean-HEAD/full-regression claim.
Scope: Beep.SimEngine only. [Stage](../../plans/enhancements/P12_EMERGENCY_MATERIAL_RESERVES_DESIGN.md).

## Current identity and sole owners

- Build `beep-simengine-2.161.0`; save88. Old constructor/JSON/wire layout replaced, no compatibility reader.
- LogisticsService9 declares new admission semantics; native/public outlet component registration2;
  logistics view17. WorkBoard27/WorkSystem22/work view50/company policy2/company view2 unchanged.
- One `OutletPolicySettings` contains `ReserveAccess`; existing provider owns current settings.
  `outlet.reserve_access` is required `protected` or `emergency_only`. Reserve-only resource controls
  preserve access/priority/destination filter. Existing command/private preview/shared Sync persist it.
- Existing demand rows alone own quantities/classification/age. Filtered emergency shortage excludes
  ordinary targets and subtracts physical unpromised stock and inbound receipts. Satisfied emergency
  rows cannot confer emergency authority on remaining ordinary demand. Final acquisition rechecks it.
- The explicit outlet floor may be used only by current declared emergency demand when enabled by
  policy. Source own active demand remains protected. Export enabled/filter and all existing worker/
  role/area/route/handling/dock/native capacity gates remain authoritative. Manual/route hauls, player
  priority and company alerts do not supply emergency provenance.
- Existing jobs, owned receipts, actual cargo, handling/motion and authored response DAG perform
  pickup/delivery/repair over game time. Later policy changes/episode retirement do not claw back
  acquired commitments. No emergency stock balance, scheduler, claim store or extra clock.
- Outlet observations use an actual-world/entity check and the existing scoped version/owner/time/
  identity mutation guard. This is not a proof of all arbitrary provider state/global purity.
- Native/public outlet Sync stages and validates decoded settings before publishing. No late native
  envelope refusal can replace existing settings. Existing constructors/fixtures are explicitly migrated.

`emergency-material-reserves.json` is embedded and merged by the test content author into the existing
master-listed work document; it is not an independently listed manifest filename. Four existing JSON
policy fixtures now explicitly author `protected`. 83 files received mechanical identity/save/current
logistics schema or explicit JSON updates; older suites compile only. Existing emergency-haul fixture
is reused through a content-authoring hook; its old mixed-demand expectation is intentionally changed
from16 to8. That older named suite was not rerun.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency material reserves: strict policies, private controls and bounded demand"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Emergency material reserves: physical supply, retained commitments and saved continuation"
```

Final build: zero warnings/errors; MSBuild3.90s (command4.43s).

| Exact suite | Passed checks | Failed | Runtime |
|---|---:|---:|---:|
| Emergency material reserves: strict policies, private controls and bounded demand | 276 | 0 | 359ms |
| Emergency material reserves: physical supply, retained commitments and saved continuation | 152 | 0 | 593ms |

Total428 checks, two exact suites, zero failures/aborted suites. Counts are assertions, not428
independent scenarios. Initial narrower gate220/108 passed; added area/role/public-demand cases272/152
passed; final actual-world observation guard276/152 passed. Same two exact suites only, no third,
family filter, broad/full suite, benchmark or oil gate. Successful build does not mean older runtime
tests passed.

## Behavior proved

- Native/public outlet × native/public individual work policy: required authoring, immutable private
  facts, foreign observation/control refusal, ordinary max-priority/alert/manual refusal, no new IDs/
  claims on refusal, reserve-only edit preservation, saved pending access changes and twins.
- Missing/null/mistyped/unknown JSON and unknown/truncated/non-native saved settings refuse;
  late invalid native decode preserves both native/public owners. Quantum preview/command parity.
- Protected/disabled/destination-filter/source-own-demand/route/emergency-worker/area/role gates
  prevent free stock, receipts and assignment for both outlet providers.
- Four native/public outlet/policy cases: mixed ordinary12 + emergency8 creates exactly8 in owned
  receipts; remaining ordinary demand never stays emergency. Real partial loading, physical delivery,
  material consumption, incident completion, disabled/protected policy edit after pickup, saved twins,
  unrelated-session isolation and sole-clock pause.
- Source own desired15 permits only first5; response waits for remaining3. Explicit policy release
  then supplies remaining3 through a second physical trip; saved continuation consumes8 total.
- Public-only haul worker with public work policy and either outlet physically delivers8 and restores
  identically. It deliberately lacks inspection role, so no fabricated repair completion is asserted.
- Actual episode renewal retires old requester without rewriting cargo commitments or duplicating
  stock shipment. A public-only demand owner supplies ordinary/emergency classification through the
  registered contracts and commands: declared owner alone creates no quantity; ordinary mod demand
  cannot borrow reserve; emergency mod demand borrows only8. Retirement/withdrawal after partial load
  retains actual delivery and cannot leave ordinary refill with emergency authority.
- Each scenario wait is bounded to300 game updates and emits actual jobs/state/blockers/load on failure.

## Source fingerprints after the final gate (SHA256)

| File | SHA256 |
|---|---|
| `src/Beep.SimEngine/Standard/LogisticsPolicies.cs` | `CCC497CB60DE6307258ABC048884328AFEFD023345FDEE6FCB198CF5CF3805D2` |
| `src/Beep.SimEngine/Standard/Logistics.cs` | `AD2E9BF86BDFBD1ECBF7F291C94F90DA39C23E8EFED04F8B3EE12D9081B14174` |
| `src/Beep.SimEngine/Standard/LogisticsCargo.cs` | `E95769A0206B6ABF8F83F5A165C6E0C0EBB4363FB0787BCAF4B6D8E99535ADBB` |
| `src/Beep.SimEngine/Standard/LogisticsStockPolicies.cs` | `F8AA742C0B63D783AC7A419FE868E4454547DAC33CD6F2B83B974E8295AF377B` |
| `src/Beep.SimEngine/Standard/LogisticsScheduling.cs` | `8702621BF25EC2C873F088278238DCDDBCFF4EFD0EA8135290E5653FFD684949` |
| `src/Beep.SimEngine/Standard/LogisticsModule.cs` | `91A57593C4A5CA671BE3BB12B219394D712880A601B1016EEBC19AA6C49EB16B` |
| `src/Beep.SimEngine/Views/LogisticsView.cs` | `8995E0BC8C61CC38FD6947BD98648C63BFEDCE8F79344CDE03208DA70BE51528` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `79D52253278F6D1C03514204B3C24FCA967D43E19B63A3FE88856D7CF701A13F` |
| `tests/ExtensionProof/ContractOutletPolicy.cs` | `806D67B2FEE350A0E93B99161C5008FC02BD09F0A03B4FE181BBAB00027A44F5` |
| `tests/Beep.SimEngine.Tests/EmergencyMaterialReserveTests.cs` | `A2BDC638C6A3B6FDEF928C312022C9C01184050729DFC39BB0479445C2391270` |
| `tests/ExtensionProof/content/emergency-material-reserves.json` | `9B3D862CD14F29F6A571416B15E064F0F899C113963FD674A8B3F0FC752EAED2` |
| `src/Beep.SimEngine/Hosting/GameClock.cs` (unchanged) | `339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31` |

## Remaining goal scope

After documentation edits, all12 recorded source fingerprints and14 current-stage links were
rechecked successfully. No source/test/content edits followed the final runtime gate.

This does not complete stock preparedness/replenishment/science or full P9/P12. Reserve-aware
maintenance exemptions, distinct-role/target preparedness, full supplier/dock/provider matrices,
contamination/incident science, private rivals/progression, fuel/traffic/congestion, global purity,
budget/batch/DAG/content/layer breadth, replay/fuzz/soak/stress/allocation/full acceptance remain open.
P12-D8 and all preceding runtime gates are historical, not rerun. No Oil/Tycoon/host/demo/platform/
Draw changes. Exactly one GameClock, unchanged; internal step coordinates are game updates, not clocks.
