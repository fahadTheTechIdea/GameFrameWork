# P9-C8c physical facility operating areas — scoped verification

Date: 2026-10-06. Current dirty-workspace source; not clean HEAD or full-regression proof.
Build **beep-simengine-2.114.0**, save **65**, WorkBoard **23**, work system **10**, work view **37**,
native tile dock **2**, independent point dock **3**. Existing owner payloads remain unchanged.
[Stage](../../plans/enhancements/P9_DOCK_OPERATING_AREAS_DESIGN.md),
[master](../../plans/enhancements/MASTER_TRACKER.md).

## Single ownership and clean contracts

Mandatory IWorkDock.CaptureSlot(World,Code) supplies detached immutable actual geometry through the
existing SpatialSnapshot contract. Native TileWorkDock resolves authored relative slots from actual
ITilePresence. PointDock is a public-contract-only independent engine extension, not host gameplay
or a fallback. No second position store, route, queue, state machine, clock or reservation book exists.

WorkPolicyRules.CheckGeometry and CheckDestination share the same existing region checker and typed
Disabled/OutsideArea/UnsupportedArea reasons. No area means no extra geometry capture; an explicitly
disabled policy is still disabled, not an inferred area. Native painted/rectangular regions and the
public interval provider keep their actual representations; incompatible geometry is never approximated.

WorkBoard alone checks slot policy, native claims, physical vacancy, reach and stable selection.
Admission requires allowed service and, for ordinary work/delivery, allowed departure before promises.
Pickup has no source-exit toil: no such requirement or movement is invented. Current owned-slot
preview checks actual remaining commitments, not obsolete completed-stage geometry. WorkSystem
rechecks committed slots before execution and on live edits; existing policy pause stops actual motion
and retains job/node, earned work, cargo, receipts and physical claims. No reopen, teleport or fallback
parking occurs. Departed pickup geometry does not constrain remaining delivery. ServiceAvailable is
the shared readiness predicate for arrived FIFO and promotion, not duplicate dispatch formulas.

Captured slot checks guard actual endpoint/carrier versions, session tick and next entity identity,
and require a non-null snapshot in the actual endpoint/session space. Invalid public null/foreign-space/
Touch callbacks refuse usable admission. These scoped guards are not universal provider/global state
mutation rollback. Native unit slot destination geometry is proved here, not all carrier envelopes
or every traversed route tile. Saved excluded geometry is valid durable policy, not fabricated progress
or checkpoint corruption. Existing native presence/WorkBoard/policy Sync remain the sole codecs.

## Final commands and results

Working directory: C:\Users\f_ald\source\repos\The-Tech-Idea\Beep.SimEngine.

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dock operating areas: provider geometry, policy admission and checkpoint ownership"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Dock operating areas: physical slot restrictions and saved continuation"
```

- Final build: **0 warnings,0 errors**, **3.38 seconds**.
- Provider geometry/policy admission/checkpoint ownership: **67 passed,0 failed**, **414 ms**.
- Physical restrictions/saved continuation: **155 passed,0 failed**, **584 ms**.
- Total **222 checks,0 failures,0 aborted suites**, exactly two distinct complete suite names.
- First runs found fixture errors: initial foreign-company machines were commanded through seat0;
  the physical fixture then read a completed job's released Worker ID. Fixtures now use actual owning
  seats and retain observed actor identity before normal release. No engine authority/check was weakened.
- Same-name repair/expansion/final reruns cover the same complete two suites; no preceding/family/full
  run.47 previous assertion files mechanically migrate47 build expectations2.113→2.114 and six
  system expectations9→10; they compile only and their historical workflows are not reverified.
- No third/broad/full/soak/replay/lockstep/extraction/OilFieldDays/Tycoon gate was run.

## Proven behavior

- Native captured bay uses actual negative relative offset, detached unit footprint and actual space.
  Missing slot and cross-session calls refuse. Retained snapshots are immutable facts.
- Endpoint permission cannot authorize excluded service geometry. Native candidate admission exposes
  exact OutsideArea without allocation/hash mutation. Missing allowed delivery exit refuses actual
  haul acquisition before carrier/source/destination receipts; existing source reservations remain empty.
- Painted allowed bays/waits/exits admit normal native logistics. Public point regions use identical
  scheduling/policy owners, require actual work departure geometry, and reject incompatible representation.
  Authored native/public policies save/load without extra codec. Three invalid public geometry probes
  fault explicitly rather than publish usable candidates.
- Real native two-node work selects wait.b and exit.b because earlier authored positions are excluded.
  Actual service arrival precedes work. Live bay/wait restriction pauses existing jobs and actual nodes;
  private job facts report the same typed blocker. No paused motion/work/progress occurs.
- Service restriction and missing permitted departure retain bay/job/work. After earned work, changing
  an already committed exit pauses that actual departure without reopening work, losing progress or
  discarding the exit claim. That earned-work checkpoint loads identically.
- Three physically arrived native actors prove a restricted older waiter retains wait.a while a younger
  eligible worker physically reaches the sole bay. Normal timed service/departure and real home clearance
  then allow the original older node to resume; no duplicate job or special queue is created.
- Native and independent point hauling physically load before an area edit. Pauses retain transporter,
  cargo, destination receipt and selected physical slot across Update/save/load. Restoring only remaining
  native destination geometry permits continuation even though the old pickup is excluded. Exactly8
  arrives, source32→24, actual carrier becomes empty, destination receipt releases.
- Paused and resumed native/public sessions advance interleaved identical Update/commands and hashes.
  Every physical wait is bounded500 Updates with actual tick/job/toil/blocker/slot diagnostics.

## Explicitly open

Whole-route and vehicle-envelope area containment, rotated/3D facilities/cross-facility overlap,
collision/congestion, preferred routes/trip budgets, vehicle service cadence/subject/repair effects,
arbitrary provider/global purity/rollback, broad private visibility/performance/replay/lockstep/stress/
allocation/full P9/framework acceptance remain open. Existing facility placement/authoring/wide-map
proofs are historical and were not rerun. No Oil/Tycoon/host/demo/platform/Draw/size cap changes.

The lifecycle skill kept implementation, stage, single tracker and scoped evidence aligned.

## SHA-256 source fingerprints

Captured after the final source build and both exact suite runs. Only these changed responsibility/
fixture boundaries are fingerprinted; this is not a complete workspace or clean-HEAD inventory.

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/DockContracts.cs | 746BB8BE788B9137A1C7D0D51233150D29B6D81035F5FF5B48C0FF0DFA5A5F1A |
| src/Beep.SimEngine/Standard/WorkPolicies.cs | E534413A2B1F45852926CC3775A800A08CD87DC54278D9DF3D721615B646B85D |
| src/Beep.SimEngine/Standard/WorkDocks.cs | 68B8DEDBB3259FBFE0FB4C2B196A41C43383649DAC393B6CB656A224F9E1AD0C |
| src/Beep.SimEngine/Standard/TileWorkDocks.cs | 56CF61FD380BC9495CEB380DD685270E30DAA8CE9BF8F8FD8F976D60FC340566 |
| src/Beep.SimEngine/Standard/Work.cs | 01AC85EFAA3FDA30EB61792D83F76BCEF39B95C6DC2F53991E1B8008F58358D8 |
| src/Beep.SimEngine/Standard/WorkModule.cs | 5E0BBBE56CBE72F7172D6541FF1EFA3C42DD2C15FEFDEB6D3DA51CF1115B0F27 |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 7636C4F77A4F8AC7DF0430CFCE8E1BE01DD71F6A47405C0940C2B9B851957259 |
| tests/ExtensionProof/PointDockProof.cs | 9A8E38943778B557B2E9A942FDF38F53444852DBF96E92937426952FD4255C4C |
| tests/ExtensionProof/PointSpaceProof.cs | 447B94B096B60316AA82D3497AB329374B2F680F4DCDFEC0A2AEB5FD22B2A1A7 |
| tests/ExtensionProof/ExtensionProof.csproj | 80D46260A439BCD56649BD2AE55913F6A74F115FFBFD230A28E3E3B52D2CCB89 |
| tests/ExtensionProof/content/tile-dock-areas.json | 0AF38CC4332EA760D548C36DC42FD4AC94030DEBDC96EA0BE951A27E86ECC403 |
| tests/ExtensionProof/content/point-dock-areas.json | 88FE2DFB9C994D52FF7179B3F20C077A2455BDA0D41FBBE16DB826EC73578EA0 |
| tests/Beep.SimEngine.Tests/DockOperatingAreaTests.cs | 07814C305DB9F4CE4B763D4F33C253E21BE53A31B9DB9FF8CBF6E0F06BA824E9 |
| tests/Beep.SimEngine.Tests/Program.cs | 41085C53D06EBC41B38779CE94F5A9B8C033AE581745B65C0B5F7FF0A9971068 |

