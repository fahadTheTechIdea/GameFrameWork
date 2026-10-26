# P3-PB1 — Public callback selected-space verification

Date: 2026-10-12. Canonical SimEngine only. Engine2.506.0/save154/replay1; view schemas unchanged.

## Implementation and ownership

Original `PlayerView.CanSee`, `PlayerView.Inspect`, and `ViewFeatureRegistration.Create` include actual selected space in the existing observation scope. Visibility and inspection also observe the original target entity codec. Write/restore changes the original spatial revision and cannot pass as a pure read. No second geometry, policy, stock, entity, scheduler, fault or clock owner exists.

The existing proof module owns deliberately invalid callbacks and writes actual authored scalar samples; the new driver uses JSON/public views/Update/save/replay only. Existing authored defaults are unchanged. All12Tycoon source files and sole clock implementation remain unchanged in this slice.

## Actual build and runtime evidence

- ONE solution build, `dotnet build Beep.SimEngine.slnx --no-restore -v minimal`: clean,0warnings/errors,7.89s.
- ONE focused repair build, `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`: clean,0warnings/errors,7.00s.
- FIRST:1460passed/88failed in21166ms; PID27116/process21334ms/Exited1, untruncated.
  These were fixture expectations:22 assertions expected a spatial-specific message instead of the actual provider codec mutation message;66 checked fault deltas before ordinary Update publication. Production code did not change during repair.
- FINAL:1614passed/0failed/0aborted in20888ms; PID17184/process21051ms/Exited0, untruncated/no observer error. No executable edits afterward.

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Public read callbacks: selected space integrity private output and physical continuation" --timeout-ms 90000 --termination-timeout-ms 5000 --output-chars 24000
```

## Covered requirement behavior

Healthy minute/half-minute JSON scenarios prove pure factory caching, own/foreign visibility, staged copies, private inspect facts, scalar layer reads, retained snapshots, saved twin updates, replay and isolated sessions.

Sixty-six negative sessions cover eleven query paths × write/write-restore/throw-after-write × two quanta: counts, own/foreign lookups, all/owner-filtered copies, regional entities, occupant lookup, owner/observer inspectors and owner/observer factories. No invalid callback yields successful facts, seeded/staged prefix, retry, healthy hash or save. First fault and original errors are retained. The normal Update publishes each viewer's safe terminal facts without advancing the one clock.

The new case also reuses current public-input `TilePresenceBindingTests.Physical(c,true)`: actual material hauling, machine manufacture/rollout and physical construction, with saved continuation/private facts/replay. No older registered case or family registry is invoked.

## Limits and remaining gates

Scoped proof of three callback boundaries, not arbitrary unpersisted CLR state, every other metadata/provider callback, allocation/stress budgets, whole P3.008 or the full goal. Original221 gates remain191checked/30open; additional237. Authorized final full regression remains deferred until implementation finishes.

Metadata-only profile inspection:705registered/included,0excluded,0long markers,0runtime suites. This is not the full executor. No Oil/Tycoon rules/platform/Draw/clock implementation changes; no staging/commit/push.

## Post-pass metadata verification

After FINAL, metadata-only checks validated1818 source anchors,1017 executable inputs and13 quickstart anchors with zero hash mismatches or executable drift. Repository inventory:3001 paths,1081Markdown files,8773 local links/0broken,18 primary/historical master rows/0malformed. All221 original gate literals and line anchors validate unchanged. Diff check exits0 (34existing LF/CRLF warnings);2989dirty paths/0staged. All12Tycoon files and the sole clock implementation match the prior checkpoint. Three new files were created; the previously unanchored existing ViewFeatureRegistration source was explicitly added to source/input inventories. No full regression, staging, commit or push occurred.
