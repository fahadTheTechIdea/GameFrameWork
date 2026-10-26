# P10-A9 — Shared-stock utility demand groups verification

2026-10-06. Framework-only focused evidence; full P10/P1–P12 remain open.

## Actual responsibility and identity

Build **beep-simengine-2.145.0**, save **76**. LineService4/infrastructure view5/motion view5 and
existing utility, inventory, exposure and reservation layouts are unchanged. The existing native
UtilityNetworkDemandPolicyDef now requires an explicit nonempty immutable sorted `Consumers` group.
Singular Consumer/consumer is removed; one-member arrays use the same active implementation.
Missing/empty/duplicate/invalid/old/mixed old-new JSON refuses without a fallback or compatibility reader.

One policy sums active actual next requirements per native resource. It subtracts the actual common
inlet's unpromised stock and incoming reservations once, not each consumer's independently capped
Available bill. Existing ResourceQuantitySignals/UtilityContracts/NetworkDemandContext guards preserve
actual provider/endpoint/native quantities and purity. Current readiness/common quantum bound the
result. Checked aggregate overflow produces zero network.demand-range, never saturated partial intent.
Inactive selected members contribute nothing; all inactive retains utility.inactive. Undeclared active
resource is incompatible; missing selected member refuses the complete group. No silent substitution.

The same existing LineService demand read remains before eligibility and immediately before each
physical receipt. Two networks cannot deliver twice against stale grouped demand. Actual source/pool/
endpoint stock, native transaction receipts, existing flow history and motion admission stay the only
execution authorities. No new stock, allocation table, consumption owner, scheduler, system, codec,
clock or loop exists. The explicit group authors concurrent intent, not global optimization across
arbitrary overlapping groups; games must declare the complete intended obligation for a shared store.

New embedded network-utility-groups.json is explicitly master-listed by the existing fixture factory.
It declares native multi-input motion, native multi-processor controls, and independent public-only
named consumers over actual inventory. Tests submit ordinary movement/process/control commands and
observe engine facts; they do not implement filling, fuel arithmetic or movement progression.

## Final verification

`dotnet build Beep.SimEngine.slnx --no-restore`: **0 warnings,0 errors**, MSBuild **3.08s**, completed
before the final tests. Only these two complete exact existing suites ran:

| Exact suite | Passed | Failed | Reported time |
| --- | ---: | ---: | ---: |
| Network solvers: explicit contracts, lifetime and guarded execution | 120 | 0 | 643ms |
| Network solvers: physical selection, refusal and saved continuation | 122 | 0 | 694ms |

**242 checks, two exact suites, zero final failures/aborts.** Includes the existing network-solver/A6
utility checks, not an additive historical count. No third/family/full regression. A8 motion169/A7
requirements291/A5 running146/B8 resource447/B7e automation1069 were not rerun; other suites compile only.
Current build assertions migrate mechanically without claiming those suites were executed.

Coverage: strict explicit groups, stable immutable binding order, pure/private diagnostics, same
stock exposed to independent members, outgoing promises, shared full/partial incoming promises, mixed
resource dimensions, native idle/partly disabled/all-disabled obligations, aggregate range refusal,
group filling and actual finite-stock conservation, public provider faults, missing-member no-fallback,
two networks late-rechecking the aggregate, reserved saved twins and sole-clock pause.

The native motion proof starts with one physical unit: each consumer is individually satisfied but
their actual combined draw refuses. Group demand supplies the missing unit; ordinary movement reaches
its destination and both providers' actual consumed quantities preserve finite-stock conservation.
Different authored rate periods preserve the slower provider's prepaid interval without recharging it.
Saved sessions continue identically through physical supply and motion. Waits are bounded to40 game
update steps with diagnostics; the older exact suites retain their existing bounded scenarios.

An intermediate prepaid assertion assumed movement could use later network delivery in the same
update. Authoritative orders are Movement410 then Infrastructure420. The test now verifies zero
exposure after that receipt and waits for the first actual funded movement before inspecting prepaid
facts. Engine phase order and consumption behavior were not changed to satisfy the assertion.
Final current-source gates above supersede the intermediate test-only failure.

## Fingerprints

| Source | SHA256 |
| --- | --- |
| Standard/NetworkDemands.cs | 8CFF26785737F77C454D24BB60FAB4D7BB3B4AB99325D22A226D9D7931AFBBBD |
| Beep.SimEngine.Tests/NetworkSolverTests.cs | D24060EA5977F763F8FD813DA3C40322344070F009A4ECCBF9A362462B6F4DAF |
| ExtensionProof/content/network-utilities.json | 411EC8BE64DC582AB7C925E8F61378577C03C1B9758154D2888CC353FE4D84D7 |
| ExtensionProof/content/network-utility-groups.json | 0EE65F5E887443E28CC4FFD57C89A54790400B68805BF532909E004DEBF27430 |
| Hosting/GameClock.cs | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |

Exactly one GameClock construction remains in World. Its source is unchanged from A8; local work/
exposure and last-success coordinates derive from that clock, never independent current-time fields.

## Open scope

Arbitrary overlapping-group/global utility arbitration, wider physical utility consumers/work/incident
integration, graph-independent providers, fleet policies, electrical/thermal/pressure physics, global
rollback/fuzz/replay/lockstep/soak/allocation gates and full P10/P1–P12 remain open. No OilFieldDays,
Tycoon, host/demo/harness gameplay, platform SDK or Draw changes were made.

[Stage](../../plans/enhancements/P10_UTILITY_DEMAND_GROUPS_DESIGN.md) ·
[Master](../../plans/enhancements/MASTER_TRACKER.md)
