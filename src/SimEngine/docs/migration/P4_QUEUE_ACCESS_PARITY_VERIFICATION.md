# P4-QP1 — Private queue access parity verification

Date2026-10-12. Canonical SimEngine only. Engine2.510.0/save154/replay1/transformation view23; operations13/motion12/logistics43/inventory8/tile8/business6/Pulse3 unchanged.

## One authoritative access evaluator

Original TransformerManager.CheckEnqueue/CheckCancel now perform healthy/live/owned/current-sight checks before reading queue capabilities, recipes, indices or providers. Missing/dead/foreign/hidden queues all refuse NotFound, so argument errors cannot reveal private queue state. Their original provider/account guards andphysical queue/material/tool/timing owners remain intact.

TransformationManagementView current facts use PlayerView.CanObserveOwned. Its typed admission previews call the same ITransformationQueue checks as ordinary command handlers, with the actual viewer. Observer diagnosis evaluates the actual owner's current permission/refusal; it does not grant a privileged append/cancel input path. Foreign commands intentionally now match existing private management refusal. No compatibility mapping or second queue evaluator was created.

Five old fixture assertions migrate mechanically: TransformationCancellationTests, ManufacturingDispatchTests, ProcessOnlyGameTests, PlanetXProductionTests andTransformerTests. Their registered cases were not executed. Existing selected InventorySight module andpublic physical helper are reused unchanged.

## Actual builds/runtime

- ONE solution build, `dotnet build Beep.SimEngine.slnx --no-restore -v minimal`: clean0warnings/errors,8.83s.
- ONE pre-runtime focused build, `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`: clean0warnings/errors,7.53s. The observer fixture expectation was clarified to retain the actual hidden owner's refusal before first execution.
- NEW FIRST/FINAL:619passed/0failed/0aborted in2651ms; PID29216/process2954ms/Exited0, untruncated/no observer error. No executable edits after FINAL.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Queue access parity: private missing hidden refusals preserve physical WIP and replay" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

## Executed proof

Four minute/half-minute completion/cancellation fixtures physically haul finite supply, start original WIP andhold reusable tools. Typed andordinary previews agree for valid append/cancel, invalid count/index, foreign/missing queues andhidden owned queues. Metadata menus, queue rows, WIP/progress andexecution projections are withheld while hidden; seeded queue rows clear.

Normal visibility commands hide the working destination. Hidden saved twins retain actual sight andoriginal material/receipt state under equal local pacing. Four ordinary append/cancel requests from owner andforeign seat execute through the real command pipeline andreturn identical NotFound. Their requests neither append/cancel nor alter WIP/tools; the simulation is allowed to make normal productive progress during that Update.

The authorized observer retains real state visibility, but its diagnostic preview reflects the hidden owner's refusal. Reveal restores the same living processor andordinary control. Original completion/cancellation, actual source/target conservation, reusable receipt settlement, retained facts, final saves, session isolation andcommand replay all pass. This is one new authored case, not an older case/family/full executor.

## Scope and remaining requirements

Original221 gates remain192checked/29open; additional241. No original gate closes from this slice. Complete P4.014 across other command families/private projections, P3.008/P11.033, arbitrary provider/account/preview faults, stress/global/full P1-P12 andthe full goal remain open.

All12Tycoon files andsole clock implementation remain unchanged; no Oil/platform/Draw edits, legacy shim, staging, commit or push. Authorized final full regression remains deferred until implementation finishes. Profile metadata only:709registered/included,0excluded/0long markers/0runtime suites; full executor not run.

## Post-pass metadata verification

After FINAL,1832 source anchors,1024 executable inputs and13 quickstart anchors validate with no hash mismatch or executable drift. Inventory:3014paths/1089Markdown/8781local links/0broken;18master rows/0malformed. All221 original gate literals/anchors remain unchanged (192checked/29open). Diff check exits0 (34existing LF/CRLF warning lines);3002dirty paths/0staged. All12Tycoon files andsole clock implementation match the prior checkpoint. Three new files were created; existing TransformationCancellationTests was explicitly added to the input freeze after its mechanical refusal migration. No full regression, staging, commit or push occurred.
