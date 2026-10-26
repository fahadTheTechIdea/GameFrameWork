# P9-C8g — Provider-owned physical dock claim exclusivity

Status: focused implementation verified, 2026-10-06. Parent: P9; prerequisite: C8f.
Build2.122.0; clean build,71 checks/two exact suites (41 admission;30 physical), zero failures/aborts.
[Evidence](../../docs/migration/P9_DOCK_GEOMETRY_CLAIMS_VERIFICATION.md).

Historical gate, not rerun by [C8h shared-bay FIFO](P9_SHARED_DOCK_QUEUE_DESIGN.md). Current build2.123.0
extends the same WorkBoard/overlap protocol; no persistent layout change or second queue owner.

## 1. Requirement and ownership

Different endpoint or slot identities do not prove different physical places. Existing WorkBoard
job source/destination slot fields remain the only claim store. Compare live claims across all
companies through the actual spatial space's optional `ISpatialOverlap` contract. The selected
provider alone interprets detached geometry. Missing/unsupported capability refuses admission;
undefined or mutating responses fail explicitly. No geometry fallback, spatial reservation index,
second queue, legacy reader, host gameplay, platform dependency or world-size cap is introduced.

Native tile geometry uses half-open rectangular footprints with wide end arithmetic. Public point
geometry uses current position, not interpolation history. Edge contact alone is not tile overlap.
Capture and comparison are pure; retained views and preview hashes must remain unchanged.

## 2. Execution and durability

The same claim check governs admission, retained/alternative replanning, waiting promotion and
departure. Claims include remote destinations and paused jobs before physical occupancy exists.
Skip only the current job's own sequential commitments and missing participants pending existing
cleanup. Save/load validation rejects overlapping claims between distinct live jobs. Existing
receipts, progress, actual movement, job lifecycle and serialization remain authoritative.

Bump engine build identity; payload layouts and state schema numbers stay unchanged. Saves require
the exact current build, with no compatibility path. Provider purity guards are not global rollback.
Arbitrary moving facilities, cross-facility fairness, hull/trajectory reservations, deadlock recovery,
fleet policies, replay/stress/allocation budgets and full P1–P12 acceptance remain open.

## 3. Locked focused gate

Build, then only these two complete exact suites; same-name repairs are allowed:

- Dock geometry claims: cross-facility admission and checkpoint ownership
- Dock geometry claims: physical mixed queues and saved continuation

Cover differently named overlapping facility bays/waits, native rectangular edges/large
coordinates, public point geometry, foreign-company claims, unsupported/faulty providers, pure
preview, corrupt checkpoint, timed mixed activity/haul and saved interleaved continuation. Physical
stages have bounded updates and diagnostic failures. Prior suites compile only; no family/full gate.

## 4. Tracker

- [x] Add geometry contract to actual native/public spatial owners.
- [x] Replace identity-only claim checks in the existing WorkBoard.
- [x] Build and verify only the two named suites.
- [x] Record source-specific evidence and align master/parent/guidance.
