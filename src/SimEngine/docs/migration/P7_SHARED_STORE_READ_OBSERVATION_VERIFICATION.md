# P7-N2 shared-store read verification

Engine2.611.0; save159/replay1/inventory-view9 unchanged. The existing
InventoryView.ReadStore now captures registered persistent endpoint services via
EntityObservation before endpoint metadata and capacity/receipt callbacks, and
verifies those original codecs before publication. The target entity, selected
space, clock and facade metadata checks remain in place. No state/codec/book added.

The external proof PoolPort now calls the declared endpoint provider Resolve
instead of bypassing it via its concrete Get. Existing negative instrumentation
adds capacity/receipt read modes that take one physically delivered unit from a
real sibling pool while leaving the selected endpoint version unchanged.

## Every build and runtime attempt

Affected project command: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

1. Reproduction build: exit0, zero warnings/errors, reported5.00s.
2. Selection literal baseline:344passed/7failed,4936ms; bounded child34640
   exit1/5077ms. Both callbacks silently mutated a sibling; outputs/save/later
   reads stayed healthy. These failures are retained, not reclassified.
3. Correction build: exit0, zero warnings/errors, reported6.14s.
4. Selection literal final:351passed/0failed,4746ms; child31500 exit0/4880ms.
5. Logistics durability literal final:197passed/0failed,782ms; child29328
   exit0/916ms.

Exact runtime names:

- `Resource port selection: engine-owned controls preserve live physical claims across native and point restoration`
- `Logistics view durability: retained stock cargo jobs and empty queues reject first update fault`

Each used `--bounded-exact`, `--timeout-ms 60000`, `--termination-timeout-ms 5000`,
`--output-chars 4000`. Every child is confirmed terminal; no truncated stdout/stderr.
Final scoped sum548/0, not a full/profile/family regression.

## Evidence and limits

Normal physical trips, exact saved original endpoint receipts, neutral private-book
refusal, immutable history and replay remain in the existing selection case. New
negative checks prepare8units through an ordinary physical haul, save/load, switch
the current binding, then arm only provider fault instrumentation. The read really
leaves7units in the sibling, faults and withholds output. Later reads/save refuse;
detached healthy facts survive, and the restored separate session advances.

These guards detect changes represented by the providers' declared original
codecs. They are not a sandbox for arbitrary unpersisted/global CLR state, and
this stage does not claim every other port/read/transaction consumer is covered.
Capturing registered persistent endpoint owners is transient observation, not
another resource ledger; no owner is selected through a potentially hostile
endpoint identity getter before capture.

442core C# files, combined path/hash digest:
`aad34e204352fac6fa9448c52cf9d18d18332566a0a67e659a173cd373dbcbcf`.
Original221/199recorded checked/22tracked open unchanged. P7.009, broader transaction,
privacy and full-goal requirements remain open. No Oil/Tycoon/platform/host/Draw
changes, broad runtimes or Git publication.
