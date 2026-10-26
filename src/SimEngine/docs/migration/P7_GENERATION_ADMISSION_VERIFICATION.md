# P7-GA1 — Generation admission verification

Date: 2026-10-15. Scoped implementation, not full regression or plan completion.

## Implemented ownership

- Original `ResourceTransactions.PrepareGeneration` observes the actual optional
  generation participant through `EntityObservation.ReadProvider`, including its
  original persistent codec, selected space, clock and component entity when bound.
  Refusal/ephemeral commit validation occurs inside that interval. Real `Commit`
  remains legitimately mutating, not incorrectly wrapped as a pure read.
- Original `IResourceTransaction.EvaluateConversionClaims` now requires `Code cause`.
  It validates physical inputs/outputs first, aggregates **requested claim deltas**
  rather than whole held receipts, and calls the same generation admission owner.
  Typed refusal and aggregate overflow use existing settlement/refusal semantics.
  Original endpoint verification still surrounds callbacks.
- Original `Transformer.CheckFlow` supplies the authored transformation ID and
  propagates the typed refusal. Its separate direct generation callback is deleted.
  There is no old overload, fallback, additional clock, stock/claim book or scheduler.
- Build identity `beep-simengine-2.632.0`; save159/replay1 unchanged. No Tycoon
  implementation, economic amounts/rules, Oil, client, platform or Draw edits.

## Proof scope

New independently compiled ExtensionProof module has one explicitly master-listed
JSON profile, an original persistent marker/commit counter and transient diagnostic
hooks. It has no money/accounts/valuation or alternate stock/gameplay rules.

Exact case `Generation admission: original participant codecs guard inventory
conversion and requested claim preflight` exercises fresh/restored worlds across
inventory preview/commit, conversion preview/commit and claim preflight. Thirty
negative contexts cover silent private codec mutation, mutation then original
throw, and clock mutation then original throw. Assertions cover first-cause phase,
no stock/receipt/time/snapshot/event/commit prefix, poisoned save/hash rejection,
independent saved-session health, authored typed refusals, exact context fields,
healthy twin identity and checkpoint round trips.

Four additional fresh/restored native-storage contexts distinguish fresh one-unit
claims from one-unit deltas on actual three-unit held receipts. They verify exact
consumed/produced context, same-store coupled admission, invalid cause and physical
capacity refusal before callbacks, retained claims and hash identity. An absent
participant remains valid physical-only generation.

## All observed attempts

The prior interrupted observation's build/test outcome was not available in the
continuation summary. Read-only process inspection found **no matching live engine
build/test process** before the new build. That lost observation is not represented
as either a passing build or a passing/failing test.

All four observed targeted builds used:

```text
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

They succeeded with zero warnings/errors in1.20s (pre-repair),6.62s (implementation
and extended proof),4.36s (native coupled-storage fixture),4.60s (continuous fixtures).
No full solution test, family/profile filter or broad acceptance bundle was run.

Every runtime call used the single literal with `--bounded-exact`, timeout60000ms,
termination timeout5000ms, bounded output6500 or4500 characters. All returned terminal
`Exited`; no timeout-triggered restarts, live handles or aborted suites remain.

| Exact case / attempt | Passed / failed | Child PID | Suite / process ms |
|---|---:|---:|---:|
| New generation proof, before production repair |280 /62|8896|336 /472|
| Extended new proof, after repair; invalid diagnostic coupled-store fixture |399 /12|17856|385 /523|
| New proof, corrected native coupled-store fixture |411 /0|35384|363 /503|
| Tycoon income: committed primary production and independent physical generation |101 /0|12876|351 /495|
| Continuous transformations: physical flow, waits, cancellation and saved throughput, stale fixture attempt |30 /3|4316|546 /683|
| Same exact continuous case, corrected authored fixtures |66 /0|26776|7946 /8086|

Baseline output was truncated by its explicit6500-character bound; final aggregate
counts/terminal status were present. Other observed runtime outputs were untruncated.

### Fixture corrections, not relaxed engine requirements

The independent `process_stock` supports ordinary inlet/outlet operations but does
not implement coupled conversion. The new claim-delta proof now explicitly authors
the existing native `storage` with the same20 initial units/40 capacity instead of
pretending the independent provider implements that capability. No capability or
compatibility fallback was added to production.

The old continuous backpressure fixture expected a full local store to reject a
net-shrinking conversion. Same-store capacity is correctly checked atomically; the
fixture now authors six output units so the requested step actually grows stock.
Output substitutions also now author matching explicit output-port maps; the
resource-free generator explicitly empties input/tool/operating maps. No validators
were bypassed and no production amounts/rules were changed to satisfy a test.

Final focused results: **578 passed, zero failed across three exact cases**. The
411/101 results preceded only the final continuous-test fixture edits; production
core and those two case/probe sources were unchanged afterward. Their cases were
not rerun merely because an unrelated fixture in the same test assembly changed.
The final66 result used the final rebuilt test assembly.

The income case verifies unchanged authored100000 gold value, sequential output,
no credit for transfers/cancellation/recycling, multi-endpoint once-only generation,
continuous output/refusal, accounts-with-income-off and survival-with-no-Tycoon.
The continuous case verifies actual inputs, tools, elapsed fuel, outputs/co-products,
physical supply, capacity/input waits, cancellation, fluids, resource-free generation,
operation gates and saved continuation. These are scoped proofs, not global gates.

## Current identities and audit

Sorted path/content SHA256 over442 current core C# files:
`aad52ac6e32d8a9dafed1d409fd3b621ec0b119a18dc877a17f30007a14d3148`.

Final Debug core DLL:
`AE00A0590FA7EA577BE7E435D3F644721798CC0C026C29127870D89D32A034F8`.
Final Debug test DLL (continuous final run):
`E9735B979BFDD943D53B4CA99A0348851036EF5811E28A98B63404C0299AD481`.
New test source:
`779528F7BA34198E9AC0B54814EEC17983C55C52911349C6D36800E6FB671810`.
Independent probe:
`792F8546B8FD3BC8B93347FDC323BF387F1FF793845DF8197748D328106A51A2`.
Authored profile:
`2CAF766834DA9991918353BE743269755EC6C36E5D1BB358BFEC97839E199370`.

Original frozen audit IDs/literal requirements were mapped to current documents:
221 total,208 recorded checked,13 open. **No original gate closes here**; previous
closures are historical evidence, not globally revalidated by this slice. P7.017,
P8.017, full optional-module/durability/performance/profile gates and full goal remain
open. Actual commit callback breadth, unrelated/unbound participant effects, all
resource providers and stress scenarios are not exhaustively proven by this case.

No Git staging, commit or push. Publication remains deferred until the full goal
and plan are actually complete. No memory file changes.

Final hygiene: `git diff --check` exit0 (35 existing line-ending warnings, no other
diagnostics); explicit changed/new stage-source whitespace check clean; staged path
count0. Search found no Oil caller of the removed claim-preflight signature. No
matching engine test/build process remained at final read-only process inspection.
