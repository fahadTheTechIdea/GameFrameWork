# P7-C7 direct inventory lifecycle observation — scoped verification

Date: 2026-10-15. Current engine `beep-simengine-2.600.0`, native save159/replay1.
Optional external `atomic_inventory` codec2 adds only a saved hostile-probe marker.
No native state payload changes, compatibility readers, new stock owners or clocks.

## Implemented

The original ResourceTransactions.PrepareInventory now requires a healthy session
before early refusals. Its actual inventory compatibility, quantum, availability and
capacity callbacks run inside the existing EntityObservation.ReadProvider boundary,
observing the actual component, whole endpoint entity, selected space and sole clock.
The old entity-version-only purity check is removed. Evaluate and Commit share this
single preflight; original owned commit and optional settlement paths remain.

The shared original AtomicInventory proof delegates stock to ResourceInventoryState.
JSON selects exactly one lifecycle callback and hostile private-state/clock/throw
behavior. Pausing after startup arms the probe. The private saved marker changes
without Entity.Touch; counts are transient diagnostics. There is no second inventory.

The existing physical lifecycle test had stale pre-WIP and pre-owned-receipt
assumptions. It now observes actual local-to-WIP transfer, timed conversion, output
and exact native totals. Its explicit journal capacity32 retains the legs it asserts;
the two-record default fixture is unchanged. Local work is checked after bounded
earned completion, not assumed to consume immediately. Provider failures target the
actual TakeReserved/ReceiveReserved callbacks, not obsolete Remove/Add calls. A
detached last-healthy page is retained; faulted live history correctly refuses.

## Commands

Build (only the test project and its referenced libraries):

```powershell
dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal
```

Each runtime attempt uses only one literal case, an external60000ms watchdog,
termination5000ms and output7500 characters:

```powershell
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource lifecycle observation: direct preflights protect original state and first fault" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Resource lifecycle: authored timed transformation, work and provider violations" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 7500
```

## All attempts

| Attempt | Outcome |
|---|---|
| Probe-only build, prior to production fix | Clean12.54s; session71709 terminal exit0 |
| New literal RED, old preflight | 185 passed/142 failed,1157ms; PID12536 exit1/1531ms; stdout tail truncated at authored7500 chars. Private/clock mutation escaped and thrown callbacks did not latch. |
| Guard/build identity implementation build | Clean17.76s; session61786 terminal exit0 |
| New literal first GREEN | 327/0,2437ms; PID31956 exit0/3120ms |
| Historical physical literal, unchanged old assertions | 15/5,1507ms; PID16888 exit1/2074ms. Stale initial Consumed/local endpoint assumption, premature local consumption and subsequent out-of-range read. |
| WIP/owned-receipt proof migration build | Clean6.65s |
| Physical literal repair1 | 26/1,528ms; PID28404 exit1/674ms. Live history correctly refused after terminal source-receipt fault; test still attempted a fresh feature read. |
| Retain detached last-healthy page build | Clean5.61s |
| Physical literal repair2 | 41/1,498ms; PID27156 exit1/645ms. Actual destination receipt was verified by conversion output, not standalone transfer. |
| Correct exact consuming-owner diagnostic build | Clean6.45s |
| New literal FINAL | **327/0**,731ms; PID28608 exit0/890ms, no output truncation |
| Physical literal FINAL | **42/0**,610ms; PID31956 exit0/764ms, no output truncation |

All five builds had zero warnings/errors. No observation timeout or live process
was restarted. All processes above are terminal. No executable edits after final runs.

## Proven scope

369 final checks across two literal cases; no failures/aborted final suites.

- Twenty-four negative flows: accepts/capacity/available/incoming x private/clock/
  thrown callback x Evaluate/Commit. Every violation returns no healthy result,
  latches first fault, leaves stock/entity version unchanged, emits no pending
  success and refuses even subsequent invalid requests without callback retry.
- Original exception message retained, no healthy save/hash or clock advancement
  after terminal fault; other session hash unchanged.
- Healthy direct preview remains pure while paused. Original commit works in
  saved twins; settled inventory/journal hashes and save/load match.
- Existing file-authored resource-only, mapless manufacturing and timed local
  work run through original owners. Real WIP transfer is not counted as consumed.
  Exact input3/output1 and local consumption2 remain distinct, with bounded
  completion, entity output and saved twin identity.
- Physical malformed source, destination and work consumption receipts fault the
  actual execution owners; live history refuses and partial state cannot save.

## Not proven / remaining

The full original goal and 26 recorded open gates remain open. This is part of
P7.acceptance.017 provider/fault work, not completion of that compound gate or
P7.acceptance.009. No broad/profile/family, performance/allocation, lockstep/replay,
all-provider/private-CLR, interleaved live-update or full Oil validation was run.
The selected-space guard is installed but this case does not inject a space codec
mutation. Older unrelated lifecycle batch assertions (including historical format
expectations) were not run or silently claimed green. Frozen completion audit is
untouched. No Tycoon/Oil/platform/renderer/host changes, staging, commit or push.
