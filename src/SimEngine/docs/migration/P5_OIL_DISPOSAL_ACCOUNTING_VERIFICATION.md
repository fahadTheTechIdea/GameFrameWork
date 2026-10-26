# P5-A27 verification — Native disposal and committed resource diagnostics

## Result and scope

Core2.547.0/save156/replay1. Existing state/view codecs unchanged.
FINAL99/0/0 in11782ms, observer11925ms, PID12460, exit0; stdout/stderr untruncated.
One new affected public Oil case only, no old/family/full suites.
[Design](../../plans/enhancements/P5_OIL_DISPOSAL_ACCOUNTING_DESIGN.md).

## Authoritative implementation

Oil DisposalDef/component/direct Take path are deleted. Canonical water_disposal uses the existing
named disposal transformer, original inventory/oil.stock port, existing40-slot WIP policy and original
Oil operation gate. One dispose_produced_water continuous recipe declares30barrels/hour actual input,
no outputs/tools/operating materials, stop cancellation and no monetary annotation.
The old hour-lump disposal is intentionally replaced by physical continuous throughput; no pulse shim.

The existing transformation validator admits input-only continuous recipes, but rejects empty primary
flows. AutoRepeat/auto_repeat are removed in favor of IdleTransformation/idle_transformation throughout
active source/fixtures. The sole existing idle queue selector can choose a supported finite or
continuous recipe; continuous queued repeat remains forbidden. No new sink scheduler, progress owner,
stock class or clock. Six older source/test/provider callers are migrated; only ExtensionProof is
compile-validated, not those older runtime suites. Its obsolete continuous-default refusal is removed;
empty-flow and missing-idle invalid metadata checks remain authored.

ResourceFlowsModule/EnvironmentalMetricsModule are master-declared once, game-provider registered,
and selected in all six scenarios. Explicit512records/company is journal retention, not a world cap.
The native cumulative disposed-water metric selects consumed produced_water with exact recipe cause,
unit.barrel and hourly rate. Safe disposal is not invented pollution, spill or destroyed/sold output.
Transformer/ResourceConversions/ResourceTransactions retain every actual claim/progress/quantity change.
The journal alone owns history; environmental facade derives metrics; no additional stock ledger.
Oil commerce only closes its physical quantity day. Optional Tycoon retains financial sales ownership.

## Focused command

```powershell
dotnet OilCo.PublicProof/bin/Release/net8.0/OilCo.PublicProof.dll --bounded-exact "Oil public resource accounting: continuous disposal control saved claims metrics and sold history" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 100000
```

## Runtime proof

Actual canonical Oil modules/components, native continuous execution and selected flow/environment
owners execute. Explicit fixture inputs:24x24 frontier/sandbox, one human, water_disposal as starting
HQ with25water and no vehicles, founding radius100. Separate terminal starts with two crude.
Unpowered negative adds an actual power_use dependency with no source.
Normal engine placement previews and FoundCompanyCommand initialize stock; no direct World/stock/
balance writes, domain projection, fake production or construction completion.
This does not prove constructing the structures through full physical project workflows.

Both monetary frontier and money-off Sandbox prove partial processing over real minutes,
stock plus continuous committed consumption conservation, exact cumulative journal and environmental
quantities/native unit/rate, public disable/enable, paused/resumed saves and twin identity.
Foreign metric/history facts refuse and clear. Sandbox performs real disposal without Tycoon.
Unpowered disposal retains all25water and reports no consumption metric from initial stock.
Actual terminal sale records two crude as SoldRaw, not consumed/disposed; save/load preserves
retained/cumulative Sold facts and the account. Unrelated sales do not contribute to disposed-water.
A recipe with no input/output rates refuses before play.
No journal expiry, oversized/fault/reservation contention/replay/lockstep/full-size/mod stress claim.

## Attempts and builds

Affected public builds all clean, zero warnings/errors:3.75s,1.60s,3.07s,3.51s.
Separate affected ExtensionProof compile clean2.08s after FINAL; no source edits or runtime test there.
No full solution/old test project/host baseline is claimed green.

Same new case only:
1 actual engine rejected input-only recipe:0/1,270ms, observer407ms,PID33840,exit1.
2 rejected the authored continuous auto_repeat:0/1,189ms, observer303ms,PID44188,exit1.
3/FINAL99/0/0,11782ms, observer11925ms,PID12460,exit0.
No timeouts or truncated runtime observer outputs.
One failed combined apply_patch used a nonexistent standalone document line; the whole patch
changed nothing. Some read-only diagnostics used incorrect paths/truncated output; nothing inferred.
Only docs/audit edited after FINAL; later ExtensionProof compile did not change source.

## Remaining gates

Original221 texts/checkboxes/line anchors retain193recorded checked/28open; additional291.
No original gate closes from this scope. Full goal/plan, actual construction/manufacture/drilling/
hauling/incident/rival/private/full-size/provider/fault/durability acceptance remain unfinished.
Previous sales/research/cost proofs are historical scoped evidence, not rerun here.
Focused tests only; no platforms/Draw/host gameplay, new Tycoon rules, staging/commit/push.

## Final metadata

2100 source anchors,13 quickstart anchors,1226 selected executable inputs: zero hash drift.
8694 local links: zero broken;18 master rows valid; external thin-driver boundary search empty.
All221 original criterion texts/checkboxes/line anchors match.
Git diff --check exit0 with34 existing diagnostic/warning lines;3184 dirty framework paths, zero staged.
GameClock, supplier, valuation and account-definition policy hashes remain unchanged.
One journal/metric master reference each; both selected by all six scenarios.
No executable drift after FINAL. Full goal/plan remains open.
