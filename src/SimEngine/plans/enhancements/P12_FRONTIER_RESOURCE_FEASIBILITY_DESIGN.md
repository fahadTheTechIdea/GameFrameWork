# P12-D13 / P4-G2 — Shared-material feasibility of the current response frontier

## Specification

ReadPreparedness currently checks each frontier node's requirements independently, then obtains
distinct worker coverage. Individually sufficient offers can overcommit a shared material/tool source.
The engine must report the combined claim feasibility for the actual canonical covered frontier.
This is current preparedness, not guaranteed future response or a retained acquisition authorization.

## Design and ownership

Extend SAME WorkBoard.CheckResourceSet selector to aligned sets of existing requirement results and
contexts. Combine ResourceIntents across nodes and call SAME IResourceTransaction.EvaluateClaims.
One iterative prefix-pruned source-alternative search remains here, shared by ordinary single-node
checks and this multi-node check; delete its old single-context search body, not a parallel algorithm.
Existing bindings/spent facts exclude already-held startup commitments; running inputs use their
actual next productive window. Never reserve/consume/release while reading. Existing any-branch
selection and committed choices stay authoritative; this does not switch recipes or invent sources.

Preparedness retains SAME DistinctWorkerMatching coverage. Select each covered worker's actual
candidate result and role, rather than unrelated candidates[0] facts. On an individually ready,
completely covered frontier, build contexts over those selected workers and native execution records,
check the whole material set, and project any chosen source alternatives back into those same rows.
IncidentPreparednessSnapshot.FrontierResources is a nullable existing ResourceClaimCheck: null means
not evaluated because a current prerequisite/worker is missing, not an invented ready result. An empty
material set is logically ready with empty availability. ReadyForFrontier also requires this check.

Selection covers the canonical current worker assignment, not every possible alternate team. Docks,
joint money, any-branch rewrites, future stages, simultaneous worker acquisition and global team
optimization remain explicitly open. No new scheduler, matcher, resource ledger, model or codec.
Actual execution continues to recheck/reserve through existing jobs/resource owners and may sequence
parallel intent physically. Snapshots own no providers or receipts and remain private/immutable.

## API and files

WorkResources.cs: sole selector widened in place; its single-node wrapper delegates to that method.
WorkIncidentPreparedness.cs: actual covered candidate/context selection and detached joint check.
IncidentResponseModule.cs: incident view4 ->5 for additional facts. SaveGame.cs: build2.250.0/save139;
all persistent payloads/Board43/System41/WorkView80 unchanged. Existing fixture gets an explicit
document-name parameter; new master-loaded JSON supplies two physical frontier material obligations.
New exact case uses normal incident commands and real work; no harness gameplay or service registry.

## Verification and sequence

ONE exact NEW bounded case: individual-ready/joint-short, exact shared stock, independent point-space
endpoint, pure/private/immutable facts, real competing receipt, held-versus-consumed requirements,
blocked/saved continuation, actual parallel material commitments and physical response completion.
Only source/compile verification of older identity assertions. Build once; run this case only; repair
and rerun observed failures only. No older/family/full/platform/stale gate. Freeze source/public proof/
tests/JSON after final green, then docs-only ledger/tracker/scoped inventory. One unchanged GameClock.

Canonical engine only; no Oil/host/demo/Draw/platform/business changes. Full P1-P12 goal stays ACTIVE;
publication only after full completion and verification. No legacy reader/shim/fabricated fallback.

## Verified scoped checkpoint,2026-10-08

Implemented2.250.0/save139/incident view5/persistent payloads unchanged. ONE clean build10.58s/
0 warnings/errors; original session57516 resumed to completion, not restarted. ONE exact NEW bounded
case61 checks0 failures/aborts572ms FIRST attempt. No repairs/retries.116 older identity assertion
files/lines compile-only. Source/public proof/tests/JSON frozen after green; docs-only afterward.
No older/family/full/platform/stale gate. Actual independent point-space endpoints/response,
role-disjoint real workers/shared material/competitor claim/tools/pure/private/frozen facts/paused
sole clock/native spent-held records/parallel physical progress/saved twins/completion proved.
Multi-source alternative search is source/compile only here; running inputs/all alternate teams/
joint docks/money/acquisition/provider faults/CPU/replay/stress/full acceptance remain open.
[Report](../../docs/migration/P12_FRONTIER_RESOURCE_FEASIBILITY_VERIFICATION.md).
