# P3-S8 provider-neutral motion — focused verification

Date: 2026-10-04. Working-tree source, current engine build 2.19.0 / save format 11.

## Scope and commands

```powershell
dotnet build Beep.SimEngine.slnx --no-restore -v:q
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider motion: non-tile work, home travel and saved lifecycle"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Provider motion: physical cargo survives interrupted non-tile access"
```

Only these two exact suites ran. No broad filter, full regression, oil/Tycoon/platform project ran.
Final results on 2.19.0 / format 11: **30 passed / one suite (148 ms)** and
**22 passed / one suite (100 ms)**, zero failures or aborted suites. Combined unique scope:
**52 checks / two exact suites**. Only these same suites were rerun after final source changes.
The solution build has zero warnings/errors. Runtime remains dependency-free netstandard2.1.

## What the source and checks establish

- IMotion is the coordinate-neutral lifecycle of the existing motion owner. IMobility inherits it for
  tile-specific goals/navigation. MoveStatus moved to Kernel without a compatibility namespace alias.
- MovementSystem enumerates IMotion, not tile mobility. Idle-worker control reads the same contract.
  No added movement system, position mirror, capability registry, state machine or scheduler exists.
- A non-friend engine-side module selects an authored fixed-point point-space through the master
  manifest. It uses no TileCoord, TileRect, ITilePresence, IMobility, TileMap or NavService.
- One presence codec owns current/previous coordinates; one motion codec owns goal/status/elapsed
  movement. The selected space owns saved route access. Module JSON explicitly owns initial route
  access, positions/rate/state bindings; no default-open gameplay path substitutes for omitted content.
- Player SetHome/PostOrder/SetStockTarget commands execute existing engine work and logistics rules.
  Test code drives Update and observes facts; it does not decide travel, work, transfer or recovery rules.
- The machine travels before working, performs all six authored work ticks, and physically returns home.
  The same movement phase advances it once per tick. Retained coordinate snapshots remain immutable.
- Saved moving sessions continue identically through arrival, work and home return. Matching identities
  from another session refuse interaction. A tile-only command explicitly refuses a non-tile machine.
- Tile Mover remains the single owner of both IMobility and IMotion, advances once, and contributes its
  authored state to the same Entity.Update resolver.
- Existing ports/transporter limit cargo capacity. Physical loading changes location of stock but does
  not deliver remotely. Closing access freezes travel and retains cargo; interrupted hauling releases/
  reopens through existing job rules. Access and cargo survive save/load. Opening access triggers normal
  conservative recovery/replanning, delivers the requested stock, and produces identical twin hashes.
- All 20 authored resource units remain accounted for across source, destination and carrier.

## Identity, review and remaining work

The repository at S8 start already declared build 2.18.0 / format 11, despite stale tracker text naming
2.17.0 / format 10. Existing format-11 changes were preserved. This slice bumps only build identity to
2.19.0 for changed simulation behavior; existing core motion/status layouts are unchanged. New proof
providers declare their own codecs in the ordinary module pipeline. Old saves/peers require exact build
identity; no compatibility reader is introduced.

The lifecycle skill guided staged design, single-owner/interface review and tracker/evidence updates.
User/repository rules override its generic full-regression recommendation.

P3 remains partial. General route/placement/rollout/upgrade contracts, optional-provider requirement
validation, comprehensive layer/lifecycle/privacy/robustness tests and authorized broader regression
remain open. P4 activity composition/DAG, P7 resources and later gameplay stages are not completed.
The point provider proves another topology, not 3D physics, platform rendering, docks or production rollout.
