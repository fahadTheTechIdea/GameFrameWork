# P9-R3 — Retained endpoint physical dock queues

Date2026-10-14. Engine beep-simengine-2.592.0/save157/replay1; existing persistent job/stock/receipt/dock layouts unchanged. No Oil/Tycoon/platform/Draw/host gameplay, full/profile/family execution or Git writes.

## Runtime and authoring

Existing LogisticsService preview now requests productive-only dock access for a retained receipt only when destination entity and port also match the continuing delivery's original physical binding. An alternate site/port backed by the same store uses normal dock waiting/service selection. Existing WorkBoard selection, slot claims, physical FIFO/admission, departure and handling remain sole authorities. P9-R2 still keeps the original job/carrier/capacity receipt and resets unfinished service credit only when physical access changes.

One explicit master-listed JSON add-on authors a distinct alternate facility with ports to existing First/Second stores, one service bay, one waiting slot and one departure slot. No copied inventory or live identity state. Original scenario setup includes this facility through its existing profile. Public-only scoped composition rejects incompatible point-provider/add-on selection rather than silently falling back. Existing stale save154 test assertions now use independently authored current save157 expectations.

## Focused physical evidence

New exact case first physically loads eight warehouse units, interrupts the original haul and obtains a normal automatic First return. It earns a real partial prefix at the original dock. A second ordinary carrier loads two units and physically occupies the alternate bay; disabling its work policy retains that busy bay. Changing the original port's current backing selection loses access to First while First's actual remaining capacity receipt survives.

The paused original carrier retains its old bay and handling credit. Resuming makes it visit the actual original exit before rebinding to the alternate facility's First port. The same job/cargo/store receipt is preserved. Its new destination slot is waiting, not occupied service; changed-access handling resets to zero while the original material prefix stays. Saved twins drive actual travel to the waiting tile, retain exclusive incumbent service, freeze correctly under the one paused game clock and earn no unloading time while waiting. Enabling the incumbent permits real service and physical departure for both carriers. First receives8, Second2, warehouse20; total30 conserved. No repeated pickup, instant unload or leaked inbound promises. Private queue, interleaved isolation and complete ordinary command replay pass.

The existing exact restoration case separately covers three same-original-site episodes, retained partial handling, required egress, busy original service and saved normal continuation. It confirms this fix does not make old earned service credit transferable into a waiting slot.

New driver observations check total material and exclusive new service occupancy throughout bounded waits. Drivers only issue commands, drive Update/clock and read immutable facts. No direct stock, geometry, dock, job, claim or recovery writes.

## All attempts

1. Targeted test/content build exit0, zero warnings/errors,6.35s.
2. FIRST/FINAL new exact alternate-access case75/0,4456ms; bounded PID26884 exit0/4597ms.
3. FIRST/FINAL existing exact original-restoration case209/0,4287ms; bounded PID26116 exit0/4427ms.

284 distinct successful checks; no failed attempt or repair compile. Outputs untruncated, all owned processes terminal, no executable changes after final runs. This is focused scoped evidence, not full regression.

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Named dock alternate access: retained receipt physical egress and busy destination queue" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7000
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Named dock restoration: original receipt, partial handling, physical egress and busy service" --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 7000
```

## Review and remaining work

The existing preview interprets physical access, existing dock owner executes queues/egress, existing stock owner holds material/receipts and one game clock governs elapsed work. No second state or compatibility path. JSON authors facilities and slots; no host decisions were added.

External point/independent tile runtime combinations were not rerun; their historical tests compile only here. Multi-slot/mixed-resource/concurrent replacement/loss/custom-policy/provider fault/global lockstep/fuzz/soak/allocation gates remain open. Original broad requirement checkboxes/counts remain unchanged; frozen A47 evidence is historical. Full plan/goal and final commit/push remain incomplete.
