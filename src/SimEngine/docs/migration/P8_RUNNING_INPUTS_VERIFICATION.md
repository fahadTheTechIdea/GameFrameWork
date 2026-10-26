# P8-B2 elapsed-time operating input verification

Date:2026-10-05. Source2.83.0/save53/transformer5/transformation-view6.
Manager2/work22/logistics5/work-view27/logistics-view10/resource-flow5/inventory layouts unchanged.
This is focused slice evidence, not full regression or P1–P12 completion.

## Commands and final evidence

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Transformation running inputs: authored rates, admission and private saved facts"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Transformation running inputs: physical supply, controls, cancellation and timed consumption"
```

Final build:zero warnings/errors,2.52s. Contracts:44 passed/0 failed,356ms. Physical:54 passed/0 failed,
720ms. Total98 checks/exactly two suite names, zero failures/aborted suites. Repairs repeated these
same names only. Other test identities/JSON/callers migrated and compiled; no third/family/full run.

## Proven ownership and behavior

- Existing master/module loader owns explicitly listed running-inputs.json and running-input-flows.json.
  The public proof module contributes JSON only; no test/demo-owned gameplay loop, fuel stock or timer.
- Native source inventory remains the operating store. Required immutable rates retain actual resource/
  native-unit aliases. One stateless exact cumulative calculator owns ceil-at-packing-quantum math;
  one native processor Sync owns elapsed successful running ticks, never an additional consumed balance.
- Missing/unknown/duplicate/non-native/zero/invalid-duration/unsupported-unit/insufficient-startup-capacity/
  unrepresentable first-draw content refuses. Unaligned rational fluid periods, rational session quantum,
  discrete cumulative ceiling, empty next increments and full-width elapsed intermediates are checked.
- Raw3/tool1/first-burn1 must be physically available together before startup. Insufficient fuel leaves
  stock/promises/progress/WIP untouched. Mixed running shortage spends none of the available sibling
  resources and freezes actual progress/elapsed time. Whole draw uses existing lifecycle transactions.
- Identical12-minute-work batches at work rates0.5/1/2 burn8/4/2 actual units respectively. Saved twins
  continue identically. Disabled and signal-gated work has no burn/time; changed target resumes actual
  accumulated lifetime. Completed full-output waits preserve WIP/tools without extra fuel or result.
- Cancelling after4 running minutes returns raw3/releases tool1 but retains burn2. Completed wait
  cancellation also keeps burned fuel spent. Idle draws nothing and completion resets batch time.
- Empty factory requests physical startup5 through existing named logistics. Real carrier pickup,
  travel and timed handling precede WIP. Working shortage retains raw3/tool1 and elapsed3, emits one
  actual selected-source demand, then real repeated supply completes. Finite source20→12 and local
  tool1 account for raw3/tool1/burn4. No stock teleportation, second scheduler or fabricated fuel budget.
- Private snapshot reads leave hash unchanged; saved snapshots remain detached. Explicit128-record
  company journal retains actual burn4 once; rival history/views refuse. Pending physical hauling and
  paused/working/completed states restore with identical hashes. Negative/future lifetime cannot save;
  corrupted normal Update faults and cannot hash/checkpoint.

## Repairs / limitations

Initial fixture embedding and mixed-capacity measures were corrected before the final gate. The
history test initially selected the existing two-record policy; its explicit scenario now owns128
retained records instead of changing shared defaults. A3.5-second quantum exposed existing exact
logistics schedule admission; the rational-time proof uses an admitted half-second quantum. Arbitrary
unaligned system schedule composition is not proven or changed by this slice.

No independent elapsed-consuming executor/provider fault matrix, priced running supply, blocked raw
return with fuel, multi-processor fuel fairness, remote ports, idle/travel fuel, utility-network solver,
continuous outputs, catalysts, broad replay/stress/soak or full acceptance was executed. Existing
resource transaction provider guards are reused, not re-proven as a complete matrix here. Keep all
those roadmap gates open. No compatibility reader/default, Oil/Tycoon, Draw or platform implementation.
