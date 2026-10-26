# P9-B8 transporter reserves — scoped verification

Date: 2026-10-06. Framework only. Build **beep-simengine-2.106.0**, save **64**.
[Stage](../../plans/enhancements/P9_TRANSPORTER_RESERVES_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Ownership and replacement

LogisticsService remains the only logistics matching/route/demand owner. Its new partial file
LogisticsCargo.cs adds IHaulExecution.ReadCargoDisposition and detached CargoDispositionSnapshot,
not another inventory, scheduler, tank, policy table, receipt book or persistence owner.
The sole ProtectedStock calculation serves both existing export surplus and current cargo disposition:
retained = min(actual Available, max(active demand Desired, explicit outlet Reserve)).
ReservedOut remains committed physical stock. Available above retention is unassigned surplus.
Disabled exports alone do not fabricate a reserve. Actual stock/available/receipt quantities must agree.

Fresh-haul selection replaces its blanket nonempty-cargo exclusion with HasResidualCargo.
Retained operating material is eligible but occupies actual cargo capacity; committed cargo and
unassigned surplus remain excluded. WorkSystem's emergency-interruption call-site uses the same read,
without adding a parallel preemption owner. That call-site is source-reviewed/compiled here; wider
emergency/preemption matrices were not run.

LogisticsView exposes company-private frozen disposition and PreviewStockPolicy. The preview and
ConfigureStockPolicyCommand both call existing ILogisticsStockPolicies.CheckStockPolicy. The old
contradictory command-only public/non-transporter restriction is removed, not preserved as a fallback.
Actual owned private/mobile inlets accept stock bands subject to existing capacity/quantum/refusal rules.

Ordinary authored stock bands replenish powered vehicles through actual courier hauling. Existing
WorkBoard/jobs/receipts/handling/motion/retry/mobile-reach logic remains authoritative. No implicit fuel
resource, free stock, instantaneous transfer, host matching or separate fleet state. Operating motion
consumes only unreserved stock: even same-resource refuel shipment quantities stay protected by native
outbound receipts while a courier spends its own available fuel.

Logistics service6/work system6/logistics view11 identify the changed semantics; save64 and existing
owner payloads remain unchanged. Snapshot facts derive from the current saved owners, never profile
defaults re-seeded on load. Scoped provider-purity guards do not imply global rollback.

## Final focused commands and results

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Transporter reserves: cargo disposition and canonical admission"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Transporter reserves: powered hauling, physical refill and saved cargo"
```

- Build succeeded: zero warnings/errors, elapsed2.24 seconds.
- Cargo disposition and canonical admission: **43 passed,0 failed**,318ms.
- Powered hauling, physical refill and saved cargo: **70 passed,0 failed**,496ms.
- Total **113 checks,0 failures,0 aborted suites**, exactly two distinct full named suites.
- Initial executions also passed; final same-name runs followed identity/schema assertion migration.
  No third, family, preceding or full suite was run.
- 40 preceding test files migrate build2.105.0→2.106.0. Five existing logistics/work schema assertions
  migrate5→6. These preceding suites compile only; no current regression result is claimed for them.
- No source/test C# retains the old2.105.0 build assertion or old work/logistics5 assertions.
  Historical documents retain their original scoped identities/results.
- Scoped tracked-file whitespace and all selected files' trailing-whitespace checks passed; ordinary
  LF/CRLF notices only. All14 source fingerprints below matched the final verified files.

## Proven scope

Actual retained/committed/unassigned quantities, occupied capacity, frozen pure private facts,
ownership/refusal semantics, canonical private/mobile policy preview-command parity and policy
save/load pass. Independent public transporter and outlet-policy implementations use the same
contracts without concrete native casts. Mutating and missing public cargo providers fail explicitly.
Outbound reserved fuel remains committed, not retained; unrelated surplus does not get a fuel exemption.

Both native and independent public transporters execute the full ordinary physical scenario.
A fuelled truck loads four real payload units, travels and runs out of operating stock before arrival.
It retains the same job, loaded cargo, cargo/destination receipts, carrier and original age.
Existing stock hysteresis dispatches a separate powered courier with actual loaded fuel to the mobile
destination. The courier's refuel cargo remains reserved while its own same-resource fuel is consumed.
Save/load during that shortage/refill yields identical continuation. The original shipment resumes,
delivers exactly once, unloads, and stock bands restore the idle truck reserve physically.
Idle reserves gain no motion exposure or fuel expenditure and do not trigger fabricated return trips.
Native stock conservation, cumulative motion-consumption accounting and interleaved session isolation
pass. All physical waits have200-tick failure bounds with current tick/position/fuel/job/blocker diagnostics.

The public proof transporter owns no stock or gameplay scheduler. It simply exposes actual inventory
and authored handling through ITransporter. Its content document is explicitly listed in the master;
tests author player commands and observe outcomes, not refuel/movement decisions.

## Remaining work

Named tanks/holds and selection, home depot/preferred routes/minimum load/maximum wait/service intervals,
proactive travel/fuel budgets, distance/idle/work consumption, base authoring/dynamic ETA/formulas,
broader geometry/capacity/fuel/public-provider/purity/stock-policy/preemption matrices,
replay/lockstep/fuzz/stress/allocation/full acceptance/full P9/P1–P12 remain open.
This is not a completed fleet subsystem or full regression. No OilFieldDays, Tycoon, host/demo,
Unity/Godot or Draw code changed. Full goal remains active/partial.

## Exact source fingerprints

These identify this focused proof, not a repository-wide or full-regression verification.

| Source | SHA-256 |
|---|---|
| `src/Beep.SimEngine/Standard/LogisticsCargo.cs` | `eaef6bd81b3fc57ea2c6d796a0791b28e6e3635f978a88daf125c28bd9b55664` |
| `src/Beep.SimEngine/Standard/Logistics.cs` | `0ea752c17a3e2ef48fe9bb89645b77d4cbd4c88dcbc288e8585c5b6458f93694` |
| `src/Beep.SimEngine/Standard/WorkContracts.cs` | `ae61ba2f2a35d5228da822d71fd6ba4d57cf3c26ea9b404ff3ee61ad3af3a14b` |
| `src/Beep.SimEngine/Standard/Work.cs` | `0f426e9b0ce00cee6f9972935524946cb53bf73caa285f81a970fbbf579aff47` |
| `src/Beep.SimEngine/Standard/LogisticsModule.cs` | `06f1e4f4020e4af8821e8aac0476c91c32f9555f8fa68945a5beabcf73e3dcf2` |
| `src/Beep.SimEngine/Views/LogisticsView.cs` | `a50c6dafe0208d3869ac287c77bbb57cabb76100c19bee9fd2fbf3ffd6a9db89` |
| `src/Beep.SimEngine/Commands/LogisticsPolicyCommands.cs` | `103b128ebd741da3bd03265a0b673d0d0538c3f62b876332585f5bc7d282c1e8` |
| `src/Beep.SimEngine/Kernel/SaveGame.cs` | `d6bb318348f1bb34c2f5f44cddbbb4b0e11e0f78756f487a4210027e24616df5` |
| `tests/ExtensionProof/ReserveTransporter.cs` | `09917089612545883b20af253250aa934b157467e51da1be55ab2824223357da` |
| `tests/ExtensionProof/PointSpaceProof.cs` | `0a57455ff1ee7d2851368f112daeb1c4a135a82e99d944d264b917e36a9e779d` |
| `tests/ExtensionProof/ExtensionProof.csproj` | `220383ac99a55098562546e438728e3bbaaeeaa56295356246ada7ffc777b973` |
| `tests/ExtensionProof/content/powered-hauling.json` | `646854ddcd53ac0d761579a241a3940108b25bb5ef402e0dad7f0033d8c381d7` |
| `tests/Beep.SimEngine.Tests/TransporterReserveTests.cs` | `9282803f2058e5f7137430caf208f10ee428c71f09e08812e870e38437947caa` |
| `tests/Beep.SimEngine.Tests/Program.cs` | `1e74558d4aada78068e0709b486cdd7e1483bdbf2c3289b269917300726019e9` |

