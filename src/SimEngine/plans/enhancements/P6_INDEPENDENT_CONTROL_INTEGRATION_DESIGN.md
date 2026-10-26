# P6-I2: Independent research/control/response integration

Status: implemented/focused-verified, final128/0 on unchanged engine2.605.0/save159.
[All attempts and limits](../../docs/migration/P6_INDEPENDENT_CONTROL_INTEGRATION_VERIFICATION.md).
Follows P9-L2 request-scoped docks; does not replace the
original P6.acceptance.008 or the full industrial-colony acceptance.

## Problem and completion criteria

The existing Planet X control acceptance stalls in its one-dimensional space:
the carrier blocks the research/response machine, and shared bays later require
machines to cross. Keep all original machines, finite stock and physical timings.
Complete its entire existing research, private knowledge, incident response,
deferred shutdown/restart, finite extraction, manufacture, construction, save/load
and replay assertions through public commands/views. No teleport or longer bound.

## Changes and ownership

1. Author a carrier parking endpoint and control-scenario startup at position100.
   This is ordinary JSON intent through the existing world-setup owner.
2. Give the original mine separate extractor and ore-handling service/exit bays,
   using the shared P9-L2 slot metadata. Keep the extractor left of haul traffic.
3. Give original beacon/site separate builder and alloy-handling bays. Build at30
   after finite extraction/refining, rather than requiring passing in a line.
4. The driver issues a predetermined player sequence: research and response,
   observe worker returned home, enqueue refining/extract, observe full output and
   parked carrier, manufacture the builder, clear its home with SetHomeCommand,
   then construct. Existing engine owners determine admission, travel, supply,
   progress, failure, quantities and completion; no driver scheduling or game rules.
5. Retain all existing provenance/private/pause/save/replay assertions and bounded
   waits. Add actual spatial-order and dedicated-bay observations where relevant.

## Verification and limits

Build targeted test project before one exact existing control acceptance runtime.
Record all failures and fixes. No full/profile/family suite or Oil/Tycoon/platform/
host implementation. No compatibility, copied model, new dispatcher or clock.
Native mixed queues/provider/fault/stress and original broader acceptance stay open.

## Stages

- [x] I2.1 — Author feasible parking and request-specific service geometry.
- [x] I2.2 — Public player-input acceptance executes the entire original chain.
- [x] I2.3 — Focused evidence and master tracker reconciled; no broad closure inferred.
