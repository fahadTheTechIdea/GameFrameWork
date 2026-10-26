# P8-D5b scoped automatic supply — 2026-10-08

## Outcome

Implemented native automatic manufacturing supply to exact named/scoped destinations through
the existing demand list, dispatch matcher, WorkBoard jobs, resource claims, movement and timed
handling. This is a focused slice, NOT full P8 or full P1-P12 completion. No staging/commit/push;
the user's publication condition remains FULL goal AND plan finished and verified.

ILogisticsDemandRequests and ITransformationSupplyRequests now take one complete explicit list
of LogisticsSupplyRequest (port, actual endpoint, native Bill). No old Bill overload survives.
Whole admission precedes replacement; unchanged endpoint/resource lines retain original age.
The existing demand codec/view stores exact ports/identities. Matching groups actual endpoint/
resource, not physical entity/resource. Length-encoded stable dispatch identities distinguish
same-resource stores. Exact bindings survive acquisition; original Job receipts govern delivery
even after current selection and requests change. Native entity stock policies/recovery priority
exclude unrelated scoped rows. Scoped reserve/gap computation reads the actual endpoint.

Batch/continuous supply derives from the original transaction vectors and authored port bindings.
Feed/tool/operating aliases aggregate once at the real store. Continuous original tool charge
and catalyst wear use the same receipt/claim path. Catalyst-only next draws keep supply active.
No local fallback, teleported resources, second inventory/scheduler/game clock or host rules.

The scenario is master-listed JSON: stores/resources/recipes/ports/operating rates/tools/warehouse/
truck/capacity/handling/navigation/startup. ScopedHaulGame remains composition only; the new
ScopedSupplyTests player uses public commands, immutable views and Update. Legacy test callers
were migrated for compilation only, without runtime revalidation or compatibility APIs.

Current identity: **2.317.0 / save151 / replay1 / logistics service17 / logistics-view31**.
WorkBoard46/work-view89/inventory-view5, transformer9/transformation-view17/operations-view11
and the sole GameClock are unchanged. Nullable port decoding was corrected in native WorkBoard
and LogisticsService: an unnamed port is Code.None, not an invalid new Code(null). No old reader.

## Verification actually run

EIGHT compilation builds, `dotnet build Beep.SimEngine.slnx --no-restore`:

1. Failed2.72s: two ExtensionProof callers still used the replaced Bill payload.
2. Failed3.91s: remaining older test callers and new migration entity/member mistakes.
3. Failed2.41s: new embedded scoped-supply.json listed before fixture creation.
4. Clean4.07s: complete fixture and caller migration, zero warnings/errors.
5. Clean4.68s: new exact player case, zero warnings/errors.
6. Clean4.22s: corrected new fixture's required unit.work-rate key, zero warnings/errors.
7. Clean4.90s: corrected legitimate unnamed-port decoding, zero warnings/errors.
8. FINAL clean3.49s: corrected historical-job assertion to native transfer journal, zero warnings/errors.

ONE NEW distinct bounded case, FOUR attempts:

`Scoped automatic supply: independent named demands, physical production and saved replacement`

1. 2 passed/1 failed,118ms, suite abort: strict authoring refused new fixture's wrong rate-unit
   key. PID41280 exited1, observed262ms, untruncated/no observer error.
2. 25 passed/1 failed,555ms, suite abort: native save/load attempted Code(null) for an unnamed
   physical source port. PID5736 exited1, observed722ms, untruncated/no observer error.
3. 61 passed/1 failed,8652ms, NO suite abort: physical workflows and saved continuation completed;
   late test assertion incorrectly assumed completed jobs remain forever. PID37108 exited1,
   observed8793ms, untruncated/no observer error.
4. FINAL **63 passed/0 failed/0 suite aborts**,8752ms. Original transfer journal proves the real
   deliveries without relying on pruned job handles. PID10148 exited0, observed8896ms;
   stdout/stderr untruncated, no observer error.

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Scoped automatic supply: independent named demands, physical production and saved replacement' --timeout-ms 15000 --termination-timeout-ms 3000 --output-chars 20000
```

Each stage is bounded to1200 Updates with process/demand/job diagnostics. The child process has
an explicit wall timeout and observed exit. No prior, family, broad, Oil or platform runtime was
invoked. All earlier runtime evidence is historical, NOT rerun on2.317.0. Final executable inputs
frozen after pass at **2026-10-08 21:14:21 UTC**; subsequent edits are documentation/audit only.

Covered: authored money-off opening stock; three same-resource distinct scoped demands; pure
private views/offers; no WIP before complete funding; ordinary truck assignment/travel/timed
pickup; demand-stage/loading/funded/manufacturing/flow/stop checkpoints; interleaved saved twin
identity; changed feed selection with pinned in-flight original store; atomic next request
replacement; exact alias aggregation and retained age; real batch output/tool release and stock
accounting; native command replay; continuous feed/catalyst/operating physical replenishment,
retained catalyst charge, elapsed output/consumption accounting and ordinary cancellation.

## Still open

Automatic scoped source discovery; standing scoped routes/export management; scoped stock bands;
generic work/DAG scoped requirements beyond existing explicit entity supply; invalid/envelope/
provider/corruption/fault/receiving-only matrices; pool-to-pool and purchase breadth; removal/
fuel/road/dock/area/recovery/congestion/emergency stress; team-wide actor/resource claims;
continuous catalyst-only (no feed/utility) authored acceptance; full P8/P1-P12 current acceptance.
This case proves ordinary continuous simultaneous feed/tool/operating demand, not every possible
provider or isolated next-draw combination. Final full-profile authority and Oil consumer scope
remain separate blockers for their gates. Do not mark the full goal complete or publish yet.
