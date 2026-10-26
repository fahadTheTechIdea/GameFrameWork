# P10-A5 — generic resource utility consumer evidence

2026-10-06, local dirty worktree. Source-scoped focused proof, not clean-HEAD/full-regression evidence.

## Current identities and gate

**beep-simengine-2.141.0**, save76, operations view9, stateless utility_signal/schema1/component1.
Existing Transformer6/TransformerManager2, resource/inventory/automation owner layouts remain unchanged.
UtilityDemandSnapshot replaces TransformationOperatingSnapshot; IUtilityConsumer replaces
ITransformerOperatingInputs. No old constructor/contract/model alias or compatibility path remains.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Transformation running inputs: authored rates, admission and private saved facts"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Transformation running inputs: physical supply, controls, cancellation and timed consumption"
```

Final clean build:2.98s MSBuild, zero warnings/errors. Exactly two complete directly affected suites:

| Suite | Passed | Failed | Time |
|---|---:|---:|---:|
| Transformation running inputs: authored rates, admission and private saved facts |75|0|419ms|
| Transformation running inputs: physical supply, controls, cancellation and timed consumption |71|0|1474ms|
| Total |146|0| |

Zero final aborts. Includes preceding running-input tests; earlier utility gate72/68 is superseded,
not additive. Preceding B8 resource-signal447/B7e automation1069 suites were not rerun. Other suites
compile only. No third/family/full suite gate. Scoped git diff --check completed without errors;
explicit untracked source/docs whitespace scan also passed.

## Repairs before the final gate

- Public extension proof initially referenced internal ContractIdentity and failed compilation.
  Removed that internal reference; proof uses public contracts only. No InternalsVisibleTo added.
- New available-vector construction initially added zero quantity lines to Bill, which correctly
  refuses them. Native/proof capture now emits positive contributions only; absent lines mean zero.
  The same two suites initially aborted, so those outputs are not success evidence.
- Fixture added a manifest dependency not present in its actual code module. Existing manifest
  validation refused it. Removed the invented dependency; explicitly selected utility modules/documents
  declare their own matching dependencies. No composition admission was weakened.
- Final strengthening includes multiple native resources, physical supply diagnostics, derived inactive
  completion and typed next-draw representation refusal. All final checks pass.

## What is proven

One existing consumer/stock/progress/codec owns native operating resources. Native Transformer exposes
UtilityStore and ReadUtilityDemand over that same owner. Detached snapshots carry current-execution
resource IDs, next actual native draw, available contribution, cumulative consumed quantities, drawing
state and range blocker. Satisfaction/shortage derives from complete vector equality, never another
saved latch or allocation balance. NextBlocker retains representation-only semantics so actual logistics
requests do not expire just because their current resources are missing.

The existing private OperationsView discovers/reads named consumers; no new view manager/registry is
added. UtilityContracts guards the actual provider/entity/store before and after reading, native units,
quanta, available stock/promises, endpoint/version/identity and elapsed running lifetime. Invalid/mutating
independent providers refuse. A public-only stateless demand provider works without Transformer casts;
its consumed bill is empty and it does not pretend to demonstrate physical execution.

Explicit master-listed utility_signal metadata binds the selected consumer. Existing signals and
resource_signals publish boolean utility.ready and requested/available/consumed native quantities.
Shared ResourceQuantitySignals.ReadNative owns exact unit conversion. An exact pair request converts
without rounding. Inactive demand and unrepresentable draws withhold signals with typed blockers;
ordinary shortage publishes available false readiness and real partial numeric contribution. Private
refusals disclose no values. Invalid consumer/metric/schema/unit JSON refuses before play.

Native whole-set physical consumption is verified separately in the same two existing suites: physical
pickup/travel/delivery/startup/running shortage, raw WIP and protected tools remain distinct; actual
draws are recorded once by the original flow journal. Multiple resources wait together without partial
burn. Competing actual reservations freeze progress and consumed quantities; release resumes exactly
one running draw. Current-execution elapsed/consumed facts and actual claims save/continue identically.
Controls/gates/output waits/cancel/completion preserve original consumption rules. An explicitly paused
GameClock advances neither work nor state. GameClock source remains unchanged; no utility clock exists.

## SHA256 source fingerprints

| File | SHA256 |
|---|---|
| Standard/UtilityConsumers.cs |69CD6A73936A46D58AB987EB66BB59B151182E81F3D3C3D6402A7A669C91EE72|
| Standard/UtilitySignals.cs |30C9A673DBC4CA23EC88E5FBFBBD9E9CBCA23FC0309C82D0766BE87C3DBBA18F|
| Standard/TransformerOperatingInputs.cs |89E4B9ACEA0A34BE758C5B334EEB34636783F2469542C4E0468D184CC73AF467|
| tests/TransformationRunningTests.cs |8BB0C225B4D15B3D1F55DE5CA49FDE858A848D8EF4044007DF0C640B18B2E98A|
| ExtensionProof/UtilityConsumerProof.cs |8D4C3DCF5B63ECD947AEB65F33F9BD085ADB21D6CE8E54BE5A65B8BE3D9962FB|
| content/utility-consumers.json |EE60656F3F9AC12A459E8BCE73097965453B7BB11AA2F22EB2C8431B8F828495|
| Hosting/GameClock.cs (unchanged) |339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31|

## Open scope

This is resource-consumer admission/satisfaction over physically supplied stock, not cross-network
utility allocation, electrical/thermal/pressure physics or graph-independent providers. Generic utility
work requirements, editable priorities, broader providers/global rollback/replay/lockstep/fuzz/soak/
allocation budgets and full P10/P1-P12 acceptance stay open. Guards protect trusted contracts; they are
not a security sandbox or global rollback proof. No Oil/Tycoon/host/demo/platform/Draw modifications.
