# P12-D10 — Emergency reserve replenishment verification

Date: 2026-10-06. Current working-tree evidence, not a clean-HEAD or full-regression claim.
Scope: Beep.SimEngine only. [Stage](../../plans/enhancements/P12_RESERVE_REPLENISHMENT_DESIGN.md).

## Identity and authoritative owners

- Build `beep-simengine-2.162.0`, save89, LogisticsService10, logistics view18, WorkSystem23.
  WorkBoard27/work view50/company policy2/view2/native-public outlet codecs2 unchanged.
- `LogisticsReserveAccess` replaces the old outlet-only enum, with explicit `Protected` and
  `EmergencyOnly`. No alias, old constructor, compatibility reader or fallback is retained.
- Existing immutable `LogisticsDemandSettings` requires access as its sixth constructor argument.
  `stock.<resource>.reserve_access` is required JSON: `protected` or `emergency_only`. The shared
  strict parser serves stock and outlet policies. The shared settings codec adds the access byte
  after priority; unknown/truncated values reject. Existing commands/private previews/views persist
  it, and band-only edits preserve it. Existing demand rows retain identity, age and refill latch.
- Both stock settings and outlet settings must allow borrowing. Only the existing private validated
  emergency-export path omits emergency-accessible stock bands from protection. Public `DemandOf`
  still reports all active intent. Non-stock workflow demands remain protected, including physically
  supplied response inputs waiting for a compatible worker. Loaded non-stock demand rows carrying
  stock-only access reject in source; the focused gate does not prove a full forged-service matrix.
- `LogisticsPolicyRules.InitializeStock` is the sole stock-profile initialization loop, used by native
  and public outlet providers through entity initialization. The existing logistics service owns
  stock policy state; neither component grows a second stock table or matching loop.
- `WorkSystem.PlanOrder` now copies valid automatic-supply obligations before handling unavailable
  requirements. Missing actors must not withdraw delivered-but-unclaimed inputs and allow ordinary
  replenishment to export them. Existing node bindings/spent choices remain authoritative.
- Existing dispatch/jobs/receipts/reservations/cargo/handling/motion execute incident supply and
  ordinary refill. Refilling does not inherit emergency provenance or bypass worker standby/floors.
  No material invention, teleport, duplicate claim store, reserve balance or host gameplay is added.
- Exactly one unchanged GameClock. World constructs it once; World.Tick and entity context Tick
  project Clock.StepIndex. Game updates complete through that owner. Pausing affects supply and refill
  through that same clock; local job progress is not another current-time owner.

`reserve-replenishment.json` is an embedded content fragment merged into the existing master-listed
work document by the test content author, not an independently listed manifest filename. The fixture
adds an explicitly authored donor with real initial stock; all transfer/work decisions are engine
decisions. Tests submit controls and advance Game.Update; they do not implement gameplay rules.
Existing source/test constructors and four stock-policy JSON fixtures were migrated explicitly.
Older suites compile only; their runtime gates were not rerun.

## Current gate

Executed against the fingerprints below after all source/test/content changes:

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Reserve replenishment: strict stock access, shared controls and protected obligations"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Reserve replenishment: physical reserve use, refill and saved continuation"
```

| Gate | Result |
|---|---|
| Solution build | 0 warnings, 0 errors; MSBuild 1.56s |
| Exact contracts suite | 116 passed, 0 failed; 355ms; 1 suite |
| Exact physical suite | 175 passed, 0 failed; 1094ms; 1 suite |
| Combined focused evidence | 291 checks, 2 suites, 0 failures/aborted suites in this final gate |

Runner counts are assertions/checks, not 291 independent scenarios. No third suite, broad family,
full regression, Oil/Tycoon, host/demo, rendering or platform gate was run.

## Proven focused coverage

Contracts exercise all four native/public outlet × native/public policy combinations: shared stock
initialization, public demand reporting, ordinary export protection, strict missing/unknown/mistyped
JSON, unknown/truncated settings codecs, missing outlet policy refusal, native quantity refusal and
preview/command parity, private/frozen settings, band edit preservation, pending edits/save/load/twins.
Both access gates must permit borrowing. A real ordinary workflow obligation is not omitted with the
stock reserve. Existing public worker/mod-demand contracts participate through shared engine paths.

Physical runs consume eight units for a real incident, partially load/save, travel/deliver/repair,
and refill eight units from a finite donor through ordinary physical jobs. Twins stay identical through
partial emergency and ordinary loading, sole-clock pause/resume and unrelated-session isolation.
Repeated incidents exhaust donor stock: final reserve16/donor0 leaves a real four-unit unmet refill
gap and active latch, including save/load continuation; no instant fallback supplies it.

Emergency-only standby admits real response but refuses ordinary refill until the player explicitly
enables ordinary work. A public haul-only worker supplies an incident without a compatible inspector;
the response stays blocked with eight delivered units protected by its workflow demand. Ordinary
refill restores the source from the donor rather than taking those response materials back.

All physical waits are bounded at 400 game updates and report actual job/blocker/toil/cargo/endpoints
and source/donor/destination stock. During slice development, the invalid authored donor position was
corrected, then this bounded test exposed repeated response-supply cycling. The engine supply-ordering
fix removed that cycling; the timeout was not increased and worker requirements were not relaxed.
Earlier development failures are not counted as successes; only the final gate above is green.

## Source fingerprints (SHA256)

```text
src/Beep.SimEngine/Kernel/SaveGame.cs 32F0F7067ADDBB0AA9D3ACB3D9BE7E7AA9E694DF7E0F1785790B737D8C298D3C
src/Beep.SimEngine/Hosting/GameClock.cs 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31
src/Beep.SimEngine/Standard/LogisticsPolicies.cs F734346A46303F7A5561904C89FD8AA9D0816CCA7A3C924DCFBDC19259DC93FE
src/Beep.SimEngine/Standard/LogisticsStockPolicies.cs 2A20E9028FE6CD16627938A314C8E2D440AA8735C33300A64D93B53ADDFE387C
src/Beep.SimEngine/Standard/LogisticsCargo.cs 197522BB8E54B392D7A76193DF934D369EF12E73086701A1EAA42A0CD5B299E4
src/Beep.SimEngine/Standard/LogisticsDemands.cs 6AEA2E993FDD172A49064033ECCC5D58A4FFE433550EB68A4D32340B651682AB
src/Beep.SimEngine/Standard/Logistics.cs 0F62609DF980492229AE3AE37608B8091DD753ED51C343C76F89DC797649641D
src/Beep.SimEngine/Standard/LogisticsModule.cs 164732C95D3BB663B5FE57756C2D7A5AF32A66A50A4FC00170F63783AABC370C
src/Beep.SimEngine/Standard/Work.cs 170E89580F2BC3A9D93D1C1D4ED9ECEDA6067F3EDBE4AF3E99EA39E39A4A8AAF
tests/ExtensionProof/ContractOutletPolicy.cs 21644A99BDC79A39C1AE3CD48196C078794EE9996ECD7E1514DF50D40856E2EC
tests/ExtensionProof/content/reserve-replenishment.json 8D155CC16B9FA1BE008F0CD3126EEF2F6FB03C28A260CB36056D36F9A72C561E
tests/Beep.SimEngine.Tests/ReserveReplenishmentTests.cs 41CDB76C6E2AC3F9C7F551CE33761251F1AEDAF0B6D3AE615FAF712CDFDAEFEA
tests/Beep.SimEngine.Tests/EmergencyMaterialReserveTests.cs B324299BDEFEE188ADF9558A62A1AED3C8DF812E13D658D6D3E2DF5AA35A3EF4
tests/Beep.SimEngine.Tests/Program.cs E063EFAA1F14CC008E6AFF8D3F98F1B6D4F8381A60BF6822A5CD38446748A314
```

## Remaining scope

P9/P12 and the full framework goal remain partial. Maintenance-specific reserve exemptions,
target-specific/distinct-role preparedness, wider supplier/dock/provider/role matrices, fuel/traffic,
science/contamination/native incident formulas, global provider purity, complete forged-state matrices,
full accounting, replay/fuzz/soak/stress/allocation and full acceptance remain open. D9/D8 and earlier
runtime results remain historical/not rerun. Scoped green checks do not close those requirements.
