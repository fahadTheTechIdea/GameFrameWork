# P7-N1 named commitment view evidence

2026-10-15. Engine2.610.0/save159/replay1/inventory-view9. Four literal finals1132/0.

## API and ownership

Existing InventoryView methods now require an explicit port:

```csharp
bool TryGetCapacity(EntityId entity, Code port, out CapacityCheck capacity,
    ResourceEndpointId? original = null);
bool TryGetReservations(EntityId entity, Code port,
    out IReadOnlyList<ResourceReservation> reservations,
    ResourceEndpointId? original = null);
```

Code.None selects ONLY the actual local IInletPort. Named ports use the existing
ResourcePortContracts.Store resolver/availability/identity/company/packing checks.
An unavailable explicit original returns false/default facts; never another
selection or local stock. One private resolver/read boundary in InventoryView
observes actual endpoint identity/version/company/policy and existing provider/
whole-entity/space scopes. CapacityCheck remains immutable; receipt records copy
the actual book into a read-only list. No new balance, registry or claim model.

Entity sight/ownership is required. Private store facts additionally require actual
store ownership or explicit observer. An owned proxy is not authority over a
public neutral pool's private book. Public stock/physical receiving access remains;
these private diagnostics refuse rather than guess requester ownership from tokens.

Old two-argument signatures are removed, not forwarded.60calls in28framework test
files now explicitly select Code.None. Their compilation is verified; all28cases
were NOT run. No Oil project/business implementation change. Proof sight module
requires view9. Build2.609.0→2.610.0; save159/replay1/state layouts unchanged.

## Current assertions

The existing selection scenario still physically loads/unloads, changes selection,
waits on unavailable original binding, saves/restores, delivers exactly8units and
verifies replay in native tile/independent point and30/60second compositions.

New assertions prove named-only capacity/book access without local fallback;
missing and unavailable-original refusal with empty outputs; actual original
inbound claim equals remaining cargo; actual cargo outbound receipt protects the
same quantity; changed current store inherits no receipt; original tokens/owners/
resources/directions/remainders exactly match another authorized route and its
restored twin; immutable historical records and final native cleanup; foreign and
faulted-query refusals; old signatures absent.

A separate JSON variant changes ONLY public source.first to neutral ownership.
Ordinary player hauling creates an actual inbound claim. Player sees public stock
but private capacity/book queries refuse; observer proves the nonempty original
receipt exists. Reads alter no hash/time. Existing ScopedHaulGame gains only an
optional authored-pool edit hook, no gameplay logic or new owner.

The pool emits Fits without usage rows. Tests preserve that actual provider result,
not fabricated dimensions. Capacity snapshots remain immutable; existing richer
capacity providers retain their own CapacityCheck values. Selection waits now have
1000Update/20second diagnostic bounds, no enlarged update budget.

## Every build/runtime attempt

Build command throughout:
`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

Seven reported build outcomes:8.30s/0,4.78s/0,4.80s/0,4.25s/0,4.61s/1,
4.46s/0,5.02s/0. Successful builds have zero warnings/errors. The failed build
was test CS1061: JsonValue has no Items property; corrected to Count/index. No
runtime started from that failed compile. All runtime follows terminal clean build.

- Original selection baseline2.609:228/1,4501ms, PID32664 exit1/4651ms; ONLY stale
  save154vs159. Updated to independently authored protocol expectation.
- Edited selection296/8,4299ms, PID29312 exit1/4442ms; assertions wrongly required
  usage rows from valid Fits-only capacity. Corrected to actual status/immutable
  facts and unchanged-original comparison, no engine default invented.
- Selection intermediate308/0,4405ms, PID31776 exit0/4543ms.
- Inventory visibility FINAL379/0,1249ms, PID29736 exit0/1384ms.
- Bidirectional234/5,2620ms, PID29252 exit1/2757ms; obsolete resolver return-type
  expectation plus two obsolete generic refusal codes in two compositions. Source
  confirms ResourcePortResolution.Store is IResourceInlet and HaulResourcePorts
  returns NotAccepted for incompatible material, NotFound for missing port.
  Updated exact expectations; physical/inlet invariants and no-mutation hash kept.
- Logistics durability FINAL197/0,778ms, PID10984 exit0/912ms.
- Bidirectional FINAL239/0,2649ms, PID9308 exit0/2787ms.
- Neutral variant291/1,4285ms, PID17564 exit1/4422ms; fixture changed ALL pool owners,
  including unrelated nonpublic stores, causing legitimate foreign-stock validation
  failure in Update. Restricted JSON change to intended public source.first only;
  no authorization relaxed.
- Selection FINAL317/0,4278ms, PID29356 exit0/4414ms.

Only four literal selectors ran, with `--bounded-exact`,60second timeout,5second
termination deadline and bounded output:

1. `Resource port selection: engine-owned controls preserve live physical claims across native and point restoration`
2. `Inventory visibility: cached hidden entities refuse facts during physical saved work`
3. `Bidirectional ports: universal inlet contracts explicit compatibility and timed structure cargo return trips`
4. `Logistics view durability: retained stock cargo jobs and empty queues reject first update fault`

Final317+379+239+197=1132/0 excludes earlier attempts. Every process terminal; no
timeout/aborted child. Earlier suite exception is retained, not passing coverage.
Later fixes alter tests/fixture authoring only; production unchanged after API build.

Core442 C# SHA256:
`6e74f5dc56e85dfd8005b33969064c7334353c16e7cee7a655aa63616e3b55b6`.

## Limits

This is named commitment visibility/continuation, NOT complete P7 atomic tool/dock/
graph/inbound/batch acceptance, arbitrary provider purity, private-content metadata,
per-requester neutral book filtering, all migrated callers' runtime or global
stress/allocation/soak. Public neutral books stay private; no guessed token-owner
filter. Original P7.009 and other original gates OPEN. No checkbox changes:
221/199recorded checked/22tracked open unchanged, full objective active.

No full/profile/family runtime, Oil/Tycoon/host/platform/Draw changes or Git staging/
commit/push. Previous build evidence historical unless explicitly rerun here.
