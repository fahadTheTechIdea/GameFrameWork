# P9-A2b generic plans and authored construction — focused working-tree evidence

Date: 2026-10-05. Scoped proof of this working tree, not clean HEAD/full regression or completion.

Historical 2.70.0 evidence. [P7-D2/P3-S11](P7_PROVIDER_RECOVERY_VERIFICATION.md) replaces the remaining
generated salvage/tile-only recovery path; its new evidence is separate, not a rerun of this gate.

## Identity and commands actually run

Current **2.70.0 / save42 / work20 / work-view25 / logistics4 / logistics-view9 / construction-view2**.
One work codec owns intent/ordering/history/native links; native node/jobs/resource/logistics/
transporter/handling layouts remain their existing authorities. No old-save reader or compatibility
API. Explicit `PlanIntent`, `IPlanDefinition` and generic placement replace the narrow activity and
tile construction paths directly.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Plans: authored intent and construction admission contracts"
dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Plans: physical sites, pause and saved continuation"
```

Final build: **0 warnings, 0 errors, 2.59 seconds**. Final exact-suite results:

| Exact suite | Passed | Failed | Runtime |
| --- | ---: | ---: | ---: |
| Plans: authored intent and construction admission contracts | 135 | 0 | 344 ms |
| Plans: physical sites, pause and saved continuation | 175 | 0 | 526 ms |

Total **310 checks / two suites**, zero failures/aborts. These suites were rerun while refining the
same slice; no third exact name, broad family, default runner or full gate ran. They call the retained
A2a policy/physical tests through the generic API, not a second test-runner invocation.

## Implementation and proof

- PlanIntent owns only deeply cloned/frozen provider JSON, definition/group/priority/pause intent.
  IPlanDefinition extends existing IActivityDefinition resolution, without another registry/store.
  Pending entries have no entity/order/resource claim. Active priority/progress/stock remain native.
- WorkBoard/IPlanBoard alias exactly. The same codec verifies live arguments/identities/company/
  exclusive native links before writes/hashes and after full restoration. Actual order/graph/owner/
  target/priority/tick identity and exactly-one-new-order checks guard activation.
- ConstructionService/IConstructionProjects/IResourceRecovery alias the same owner. Site/target
  definitions resolve exact catalog contracts; site state aliases target construction content.
  Generated-site authoring is deleted. Sites are explicit ordinary prototypes in master-listed JSON;
  point-construction.json is a separate declared document with source provenance checked.
- Empty site creation inventory, exact target/site binding, material envelope capacity, resource
  filters and quantum validate before Initialize. Missing site, wrong target, free creation stock,
  too-small capacity and incompatible filter content refuse explicitly.
- Selected ISpatialConstructionPlacement owns strict JSON decoding, stable geometry identity and
  shape/occupancy admission. Point and tile construction share the native owner; no raster conversion
  or engine-imposed size cap. Equivalent key order/decimal spelling cannot create duplicate intent.
- Pure/private provider previews, unknown/wrong geometry, authored bounds, command priority, occupied
  pending waits, actual timed demolition freeing tile placement and then physical site admission pass.
  Direct point commands share placement admission/site/order identity without a second plan entry.
  Pending/cancelled history and queued direct construction command save/continue identically.
- Both selected spaces actually haul and consume inputs, travel/work through their authored graph,
  pause without earning progress, resume saved twins and produce one target only after completion.
  Completion retires the empty actual site, preserves its geometry, and records actual former target
  identity in terminal history. Point proof consumes eight existing source inputs (20 -> 12), not
  fabricated stock. Empty actual point cancellation uses the same native disposition owner.
- Wrong-target and extra-order public-contract provider probes fault actual Update. Faulted partial
  activation cannot save or hash as success; no compensation, substitute order or instant fallback.
- Previous activity policy/physical proof remains inside these two suites: actual ordering/priority/
  group controls, target blockers, pause/native tools/inputs, unacquired supply withdrawal/acquired
  cargo delivery, repeat/cancellation, independent authored provider/DAG and isolated saved stages.
  Physical waits have bounded update counts and engine plan diagnostics on timeout.

## Required remaining work

This completes A2b only. P9/P3/P4/P7 and the full framework goal remain partial/active. Existing tile
salvage buffers still derive from authored capacity settings; generic stocked recovery/salvage and
demolition geometry remain required migration. Unsupported stocked geometry retains/refuses through
the existing typed blocker; this gate proves empty/fully consumed point sites, not stocked point
recovery. No inventory discard or silent geometry fallback is permitted.

Network/survey/upgrade/removal intent providers, construction/network templates, bulk groups,
automatic repeat/history lifecycle, all-dispatch areas/fleet/congestion/replanning and broader
provider fault/replay/fuzz/soak/performance/lockstep/full acceptance remain open. P8/P10/P11/P12
and other tracker gates are not closed by this proof. Earlier suites and migrated fixture/API/identity
callers compiled only. No OilFieldDays/Tycoon/host/demo/platform/Draw work, commits, publication or
memory writes were performed. Lifecycle skill guided stage ownership/documentation and the focused
gate; repository/user restrictions took precedence over general full-regression advice.
