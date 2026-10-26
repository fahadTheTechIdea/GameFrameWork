# P11-K1 knowledge ledger durability — scoped verification

## Current M82: observed knowledge provider lookup — 2026-10-22

Native **2.726.0/save161/replay2**, master HEAD
`cb61483ab81404c11944931aedb4703ba694edb5` plus preserved existing work. Original221/210
source-recorded checked/11open/full goal active; no original requirement closes.

### Reproduced gap and original-owner correction

CompanyKnowledgeService.Provider read each IKnowledgeProvider.Id before entering its existing
Observe boundary. KnowledgeView delegates directly to this same service, so a public freshness
lookup could pause the clock or throw without latching the original failure. Direct CheckRecord,
Record and missing-provider lookup had the same gap. Actual before run **184/24** reproduces all24
fresh/restored × four entry points × clock/throw/clock-throw contexts. Private foreign freshness
refusal already correctly avoids provider lookup and remains unchanged.

The original helper now reads each candidate's identity through its existing Observe method and
EntityObservation.ReadProvider. No new provider registry, binding cache, learned table, codec, view,
timer, exception translation or alternate query path. Provider errors retain their original exception;
World remains the sole first-fault owner. The same lookup is reused by record/freshness/share/forget/
candidate validation. Only CompanyKnowledge.cs and EngineInfo.Build change among450 native sources;
development identity advances2.725→2.726, with no payload/schema or old-save compatibility path.

The existing independent StockKnowledgeProvider gains one explicit transient identity-read fault
hook. The new partial of KnowledgeVisibilityTests reuses its original factory and service; raw valid
ledger setup and hostile callbacks are explicitly **protocol instrumentation**, not instant player
discovery. The final negative scope **352/0** checks actual callback execution, private refusal before
lookup, original exception identity, null failed snapshots, cached-view cutoff, save/hash refusal,
zero further game-step progress, first-fault retention and independent saved-session isolation.
This is not a sandbox against every arbitrary hidden CLR/mod mutation.

### Affected positive fixture and physical behavior

The original physical discovery case initially failed7/2 with an unhelpful null order. A second8/2
run exposes its real UnknownDef refusal. The fixture used the same protected unknown supply as
both discovery inputs/tools and observation result, and the stock effect also declared that observed
resource as an execution prerequisite. Modern referenced-content admission correctly refuses this
circular setup; the engine's disclosure rule was not relaxed.

One explicit embedded [physical authoring profile](../../tests/ExtensionProof/content/knowledge-visibility-physical.json)
now supplies known discovery.inputs separately from unknown supply. The original master-selected
documents still pass through the original loader: one native source inventory,40 total capacity,
20 input/20 observed supply; eight consumed/two held inputs and the original18-minute staffed work
are unchanged. There is no stock grant in the driver or additional buffer. The same test factory
can select the profile for its original all/any branch; the interim40/2 failure found that branch
still using the circular old setup and is retained. All44 physical checks now pass.

The existing stock observation effect declares its opaque subject, like the native knowledge effect;
observed output is not a consumed prerequisite. Known costs/tools remain explicit activity
requirements. Actual unobserved supply stays undisclosed until earned work completes, including
initial resource/inspector refusal; afterward its exact original20 is visible while known inputs
have decreased to12. All/any facts, independent technology access, private foreign entity/catalogue
disclosure, retained snapshots, entered-sight deltas, partial saved twins and final restore remain.
The original raw hidden-entity visibility probe now uses selected geometry and a valid separate
point80 placement instead of default source-point overlap. This is labelled protocol fixture setup,
not a new public construction path or claimed cause of the UnknownDef refusal.

### Actual attempts and latest scoped evidence

Five targeted Release builds succeeded0warnings/0errors:14.19/14.20/6.17/14.62/9.99seconds. One
before-negative run and three physical fixture failures are retained; no full/profile/family run.

| Latest exact scope | Passed/failed | Suite/observer ms | PID/exit | Captured image |
| --- | --- | --- | --- | --- |
| Knowledge provider identity: actual lookup callbacks reject mutation and preserve public first faults | 352/0 | 356/497 | 15828/0 | build2 |
| Knowledge freshness: physical survey expiry and exact saved visibility | 84/0 | 1170/1313 | 3916/0 | build2 |
| Visibility: physical discovery, scoped snapshots and saved continuation | 44/0 | 468/605 | 28860/0 | build5 |

Latest **480/0** spans explicitly captured test/proof images with the **same corrected native DLL**,
not a full final-image regression. Negative/freshness cases were not rerun after physical-fixture-only
changes. Freshness84 is the existing native timed survey/expiry/private exact sample/save proof.
No replay or allocation measurement ran in M82; M81's network traces retain their actual2.725 image.

Artifacts: [M82](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/),
[all attempts/commands](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/attempts.json),
[final tested inputs](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/build-5-inputs.json),
[native identities](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/native-scope-identities.json),
[compiled audit](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/compiled-caller-audit.log),
[original gates](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/original-gates-current.json),
[frozen inputs](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/10660754679049e9892c03bf73eb61a0/structural-checks.json).
Each runtime is its exact literal with60000ms bound/5000ms termination/30000-character output. The
matched compiled audit has450 sources/PDB checksums,2937types,12925IL methods,344971instructions,
137 typed resource/spatial calls,14resource/eight selected layer writes and0unknown/unmapped/indirect
sites. Structural coverage is not execution of all callers. Native DLL SHA256:
`31B1EC6A5C53E98E6B3425590830625EC9D69433A62BF8F72FA957ECED8461F4`.

### Self-review and remaining scope

Self-review only. Existing ledger Observe/World first-fault, typed refusals, private immutable views
and original command/event diagnostics remain the owning path; no swallowed callback exception or
new log/audit/notification service. The [shared operational assurance matrix](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
retains wider protected retention/correlation/channel-outage gaps. No native business/clock logic,
Oil/consumer, renderer/Draw, platform, adapter, full/default/profile/family/soak or publication work.

Continue the remaining protected-cell bulk/region/freshness provider boundaries and original
C3/P8/P10/P11 fault/stress/conditional quality-lot/optional-module obligations through their existing
owners. This fixed lookup defect and selected positive proof do not establish every arbitrary mod,
all source strategies, broader corruption/lockstep or full-plan completion. Earlier K1 below is
retained historical evidence on its actual older image, not current2.726 certification.

## Earlier K1 implementation — 2.456.0

Earlier engine2.456.0/save154/replay1; knowledge state3/view5 unchanged. Its original221/184checked/37open counts and scoped proof belong to that earlier checkpoint, not the current status above.

## Actual ownership change

CompanyKnowledgeService remains the sole persisted learned table, sorting/candidate commit, record/first-latest retention, freshness, sharing and forgetting owner. Existing provider/policy callbacks now use a local transient reentry helper delegating original EntityObservation.ReadProvider; finally performs cleanup only, never masks original callback failure. All-loaded provider/policy admission, CheckRecord, record retention/schema, freshness, share/forget subject/admission and candidate save/load validation participate. Legitimate commit is outside pure observation, after healthy validation.

Actual authored policy subject sets are captured/validated/detached under policy observation and reused by the same preview/command paths. No second policy catalog or shadow ledger. Cached KnowledgeView still delegates to this same service, which rejects faulted owned/private/missing/invalid queries before shortcuts, clears seeded collections and withholds snapshot outputs. Original payload/time/resources/work ownership unchanged; no Oil/platform/Draw/advanced Tycoon/protected clock/G19 edits.

## Current public-player proof

ONE NEW exact case only. Four positive native/direct policy × minute/half quantum variants use existing actual point-space master and separately listed JSON knowledge/flow/work documents. Thin pure factory and public Preview/Submit/results, Game.Update, clock pause, immutable work/entities/knowledge, save and replay. No World, TestKit, direct provider/stock/progress mutation in the new driver.

Actual staffed machine performs eight authored game-minute sample work; first partial consumes two finite supply inputs (20 to18) without granting knowledge. Saved partial twin/pause continues to actual completion and observes18 actual stock, never an authored fake grant. Immutable own fact table/data and sharing projections reject mutation, private foreign queries cannot read own observations, freshness derives from the single game clock and expiry keeps historical data. Ordinary sharing preserves sender observation time/data; selective forgetting changes only the sender's reading, not permanent method or recipient history. Cancelled partial re-observation consumes another two once (16) without granting; physical rediscovery consumes final two (14), records later current time and does not refresh recipient history. Partial/complete saved twins, retained old projections, replay and independent session hash agree. Native/direct minute final29; half final52. No Tycoon dependency.

## Current actual negative callback proof

Nine runtime variants: policy Allows for sharing/forgetting and actual authored provider CheckObservation, each throw/clock/clock-throw. One additional CheckObservation clock-throw reached through original Save candidate validation, and one all-loaded policy composition clock-throw. Negative phase/mode/read_after and policies/subject come from an explicit master-listed module/document; provider faults occur after100 actual game-minute steps, never a driver-owned clock/state writer. Negative code lives in the non-friend ExtensionProof module and owns no learned table/work/progress/currency or normal game rules.

All failures preserve original provider exceptions where thrown, latch original World fault, withhold share/forget prefixes, reject cached own/foreign/unseated/missing/invalid subject/freshness/share/forget queries and clear seeded tables. Terminal Save/Hash/Update refuse, first fault remains authoritative, retained frozen private payload remains unchanged and independent session continues. Composition invalid policy zero Initialize.

Arbitrary persistent provider codecs, provider Id getter purity, malformed freshness/retention and loaded semantic fault injection are not comprehensively exercised here. No broad all-provider/CLR sandbox/performance/stress/delta/menu/optional-module/full-regression claim.

## Bounds and attempts

Per-stage480Updates with actual work failure diagnostics; outer60000ms/termination5000ms/output30000 chars. Initial solution compile4.22s failed on proof-only JsonValue.Freeze (internal to engine). Necessary negative-proof repair stores immutable canonical authored string and returns a fresh declared payload; original ledger remains the sole freezer. Targeted compile6.21s terminal clean0warnings/0errors before runtime. ONE runtime FIRST/FINAL2077/0/0 in1219ms, PID26804 Exited0/process1395ms, no output truncation/observer error. No executable edits after FIRST/FINAL, historical/family/full runtime, staging/commit/push.

### Build 1

Command: `dotnet build Beep.SimEngine.slnx`

Terminal exit1; observed4.88342s.

```text
Determining projects to restore...
  All projects are up-to-date for restore.
  Beep.SimEngine -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\src\Beep.SimEngine\bin\Debug\netstandard2.1\Beep.SimEngine.dll
C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\KnowledgeLedgerDurabilityProof.cs(63,89): error CS1061: 'JsonValue' does not contain a definition for 'Freeze' and no accessible extension method 'Freeze' accepting a first argument of type 'JsonValue' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\ExtensionProof.csproj]
  Beep.SimEngine.Testing -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Testing\bin\Debug\net8.0\Beep.SimEngine.Testing.dll

Build FAILED.

C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\KnowledgeLedgerDurabilityProof.cs(63,89): error CS1061: 'JsonValue' does not contain a definition for 'Freeze' and no accessible extension method 'Freeze' accepting a first argument of type 'JsonValue' could be found (are you missing a using directive or an assembly reference?) [C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\ExtensionProof.csproj]
    0 Warning(s)
    1 Error(s)

Time Elapsed 00:00:04.22
```

### Build 2

Command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj`

Terminal exit0; observed6.8365987s.

```text
Determining projects to restore...
  All projects are up-to-date for restore.
  Beep.SimEngine -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\src\Beep.SimEngine\bin\Debug\netstandard2.1\Beep.SimEngine.dll
  Beep.SimEngine.Testing -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Testing\bin\Debug\net8.0\Beep.SimEngine.Testing.dll
  QuickGame.Module -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\help\quickstart\Game.Module\bin\Debug\netstandard2.1\QuickGame.Module.dll
  ExtensionProof -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\ExtensionProof\bin\Debug\netstandard2.1\Beep.SimEngine.ExtensionProof.dll
  Beep.SimEngine.Tests -> C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine\tests\Beep.SimEngine.Tests\bin\Debug\net8.0\Beep.SimEngine.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:06.21
```

### Runtime 1 — FIRST/FINAL

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Knowledge ledger durability: physical learning and policy faults preserve private facts" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000
== Knowledge ledger durability: physical learning and policy faults preserve private facts
ledger physical independent=False half=False final=29
ledger physical independent=False half=True final=52
ledger physical independent=True half=False final=29
ledger physical independent=True half=True final=52
   2077 passed, 0 failed (1219 ms)
=== 2077 passed, 0 failed in 1 suites (1.2 s) ===
bounded case: Exited, pid=26804, exit=0, elapsed-ms=1395, stdout-truncated=False, stderr-truncated=False, observer=
```

## Metadata closure

Current closure:1574 source anchors,13 quickstart anchors,843 selected executable inputs; zero hash mismatches/post-pass executable drift.2766 paths/972 Markdown files/8595 local links, zero broken links;18 valid master rows;221 original literal gate/checkbox anchors exact and unchanged (184checked/37open), additional182. Thin driver boundary scan empty. git diff --check exit0 (34 existing line-ending-warning output lines),2754 dirty paths/zero staged. Sole clock/protected G19 hashes unchanged. Six new files: four executable proof/driver/JSON and two stage/evidence documents, all admitted to source anchors. Five previously frozen executable inputs changed (two production); existing consuming dependencies already frozen. No executable edit after FIRST/FINAL. Metadata integrity is not full gameplay/provider/acceptance evidence.

