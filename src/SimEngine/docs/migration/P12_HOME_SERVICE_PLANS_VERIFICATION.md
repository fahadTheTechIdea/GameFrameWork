# P12-D6b — Home-targeted recurring service evidence

Date: 2026-10-06. Scope: Beep.SimEngine only. [Design](../../plans/enhancements/P12_HOME_SERVICE_PLANS_DESIGN.md).

## Current source protocol

Build beep-simengine-2.156.0; save83; WorkBoard27; WorkSystem18; private WorkView47.
Native recurring source codec4/public proof recurring codec3 replace old request/admission semantics.
Source settings/history payloads retain one owner; actual plan origin is embedded once in WorkBoard.
Condition/worker/home/resource/dock/clock state layouts are unchanged. Public ProofCondition adds
typed signal observation over its existing condition value, not another value/codec/provider wrapper.

IRecurringPlanSource.ReadRequest replaces ReadIntent completely. RecurringPlanRequest has exactly one
actual immutable intent or typed refusal. Native JSON explicitly authors target self/worker_home and
worker_nodes; no absent-field fallback. RecurringPlanBinding uses the shared positive machine-node
validator and existing WorkerHomeContracts.ReadHome/CheckHome. Worker_home targets the actual home
and binds selected nodes to the actual source worker. Conditions observe the source worker; spares/
tools observe the selected facility. Ordinary WorkBoard/WorkSystem perform all physical execution.

PlanOrigin lives in the sole existing PlanEntry/codec/private view. Historical source validation checks
actual origin, company, definition, plan ID and original posting time, never today's dynamic target.
Changed/cleared home does not rewrite execution, earn history or invalidate checkpoints. Live prior
posting blocks another posting by that source. Rechecks reject stale captured requests. Manual repeats
have manual origin. No new fleet scheduler, queue, timer, receipt history or gameplay host.

## Final focused gate

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Home service: authored requests, source origins and admission"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Home service: recurring physical repair, home edits and saved continuation"
```

Final build: zero warnings/errors; MSBuild 2.55s. Contract suite:136 passed/0 failed/519ms.
Physical suite:73 passed/0 failed/371ms. Total209 checks/two exact suites/zero failures/zero aborted suites.
Same-name repair reruns only; no third/family/full runtime gate. Runtime source/test/content unchanged
after this final gate; subsequent edits are documentation only.

Proven: strict/missing/null/unknown/duplicate/roleless/nonworker authoring; immutable sorted metadata;
typed unavailable requests; pure/private source views; stable source arbitration and refusal history;
manual/origin distinction, malformed/future/wrong-source/truncated origins; independent public provider
and eight dishonest observation/acknowledgement/settings fault modes. Actual native/public workers and
condition providers travel to bay, consume three spares, protect/release tools, earn work, depart and
repair only the worker. Active home edits retain destination; idle return uses current home. Cancellation
does not repair/refund spares; next eligible posting consumes the new facility's actual stock. Interval
edits/re-enable derive from original earned posting. Mid-work/departure/ended saves, twins, isolated
sessions and paused sole GameClock are exact. All physical waits bounded300 updates with diagnostics.

Earlier iterations found test-only API mistakes (JsonReader rather than nonexistent JsonValue.Int,
Null() rather than method group), private read correctly returning NotFound rather than NotOwned, and
fault7 resetting a posting already at zero (fixture now first posts after one actual game update).
No production fallback was added to satisfy these assertions.

72 older test files mechanically migrate66 build/63 save/28 board/2 work assertion lines; those suites
compile only. D6/D5/B9/D3b/D4 and older runtime results are historical, not rerun or added to this count.

## Ownership review and remaining scope

RecurringPlanBinding owns only authored request binding; IWorker.Home remains the home authority;
RecurringPlanSettings/source owns policy and last posting; PlanEntry owns actual provenance/intent;
Order/node/job owns execution/claim/completion; real ports/condition own stock and integrity. Public
providers implement contracts with no native scheduler cases. Exactly one GameClock is constructed
by World and reused by SimGame; no source may advance its own time. No Draw/platform/adapter or
OilFieldDays/Tycoon/host/demo source was changed.

Full objective remains incomplete: preferred/nearest facility search, fleet preparedness/standby/fuel/
traffic, inspection science, incident breadth, global callback purity, wider provider/protocol matrices,
replay/fuzz/soak/stress/performance and full framework gates remain open. Focused proof is not full
regression evidence. No legacy request method, content reader, save translator or compatibility path.

## SHA256 of verified source

| File | SHA256 |
|---|---|
| Standard/RecurringPlans.cs | 4F6C9099780208DC40B26171DA04514698FAFD0EE6788462123234767D66C49B |
| Standard/WorkPlans.cs | E6F60FE58094043B309F6B3FE0F12412A1C2C3C1901FAE248EAE429896EAA5EF |
| Standard/WorkRecurringPlans.cs | 9D7E7CF13EA6C091C4F87CE5E19EDFED23C8D934CB1D1B9565114741EC757933 |
| tests/HomeServiceTests.cs | D7F7EFEC03A69563EA33B5814EC9FDC08E3546E1651FD8BD2A1952E58588391F |
| content/home-service.json | D30001C1D3559CED0E20254589950CE36EC5A5E30AB0FF3CC91DFB2876E1D854 |
| Hosting/GameClock.cs (unchanged) | 339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31 |
