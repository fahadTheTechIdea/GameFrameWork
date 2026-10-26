# P9-PBH1.R — Connected rollout: engine slice verified, canonical acceptance OPEN

The later [P9-PBH1.O evidence](P9_OIL_PRODUCER_LAYOUTS_VERIFICATION.md) supersedes
the UNMIGRATED/current-consumer claims below: all four actual layouts are now
authored on unchanged2.650 core, canonical founding/preparation pass, a new native
minute11 save exists, and one harbour day times out. Older results here remain
historical evidence, not a claim of completed canonical/performance acceptance.

Date:2026-10-18. [Design](../../plans/enhancements/P9_CONNECTED_ROLLOUT_DESIGN.md).
This is not full P9-PBH1 completion, a clean regression or canonical acceptance.

## Current identity and implementation

Engine `beep-simengine-2.650.0`, save160, replay1; tile_work_dock4, proof point_dock6.
Core448-file sorted path/hash UTF-8 LF digest:
`b5bbb9a12ef5da072b8d294acc0d76f7c4d034058c1b2eb0edf801ce1c627a8f`.
Core DLL: `FECAA1476DC589A9BB5A7D15D5C302A268F064D2293736850475A30F7622F190`.
It is unchanged across the final focused groups below.

Final test DLL: `43C818C76AD142A799C74396B6B40DEC88663CD2B0D8420EA25109975FE50AAC`.
Final ExtensionProof DLL: `3991F5A7CE60383976EF6AC9E50673BBBDC4786990BFE053EC99108CA0B8F9AF`.
Final target build5.26s, zero warnings/errors. The last changes were test lookup
and reference-colony JSON, not runtime core changes.

Native MOBILE rollout no longer scans world-wide rings. Existing IWorkDockDefinition
geometry gains optional IWorkDockRolloutDefinition metadata: stable lane/output/
start/exit identities and explicit positive max_expanded. Actual immutable launch
metadata resides on the SAME dock definition, with no coordinate duplication,
layout registry, queue, stock, traffic claims or clock. Service slots may be
explicitly selected for launch; launch-only layouts use Rollout slots. Launch
slots never match work/haul service requests. Exits are distinct Departure slots.

Native TileMap reads the existing geometry through ITileWorkDockDefinition,
checks actual birth placement and complete planned prefix, then uses the ORIGINAL
NavGrid/PathSearch terrain, occupancy, active-segment clearance, prototype origin
constraints and corner rule to reach an authored exit. ProbeDefinitionRoute copies
the ephemeral unavailable rectangles and validates their bounds; it never creates
an actor or stores a future route. Exhausted declared search budget returns
core.rollout-pending, not successful placement or permanent unreachable.

Find and Check share this admission. Existing EntityRollout guards still own the
live/prospective space/source/clock boundary. Actual later motion owns travel;
the query reserves no future route and cannot promise arbitrary traffic freedom.
Independent PointSpace uses the SAME rollout metadata and its existing physical
vacancy owner, with its own analytic segment geometry. Kernel remains geometry-
neutral. Stationary placement retains its separate original footprint/prefix
placement search; that is not a mobile compatibility fallback.

Strict shared declaration and unused-layout validation reject missing production
bindings, unknown/duplicate/inapplicable outputs, unresolved/invalid start/exit
IDs, parking-as-launch, null declarations and nonpositive budgets before Initialize.
Native output metadata must be actual movable tile presence plus mobility.

## Reproduction and retained attempts

| Attempt | Observed result |
|---|---|
| Original2.649 baseline build | Clean7.12s. |
| Actual disconnected birth counterexample |5/5, pid8448,480/655ms. Held actor occupies producer contact8,5; provider returns success at DISCONNECTED7,6; timed recipe creates one worker, removes its queue item and reports Idle. |
| Initial multi-file edit request | Rejected before changes because one patch named DockContracts twice. Split/merged correctly; NOT a compiler or runtime result. |
| Initial2.650 runtime build | Clean9.30s. |
| Reproduction corrected |10/0 pid2232,444/604ms; Blocked/null placement, no remote result. |
| Existing berth integration |57/0 pid27056,1382/1534ms; queue drains at24, worker physically parked7,6. |
| Independent provider / parking-only home |27/0 pid2036,334/485ms;14/0 pid23168,545/707ms. Parking-only home fixture now selects a separate genuine home, not an impossible parking-only producer. |
| New physical/planning/point proofs | Clean5.60s build;63/0 pid27664,891/1048ms;22/0 pid35036,338/489ms;22/0 pid26820,331/483ms. |
| Final runtime guards | Native prototype applicability and copied/bounded planned-prefix admission; clean8.45s build, first nine final observations below. |
| Existing reference Oil manufacturing before migration |3/1 pid36692,246/401ms: proof.infrastructure.source lacks launch metadata; refused before Initialize, not a successful manufacturing observation. |
| Reference manufacturing fixture authored | Real launch/exit and three parking slots, unchanged initial inputs/recipe times/money; clean5.26s build. Final882/0 below. |
| Reference colony service/parking authoring and smoke | Clean7.16s build;23/1 pid36172,566/789ms: fixture lookup assumed a single factory across two companies. Initialization itself passed. |
| Owned reference lookup corrected | Clean5.26s build; final planning24/0 below. Core DLL unchanged. |

One source-digest command had a PowerShell parse error and was corrected before
use; no digest was inferred from it. A wrong-root read returned no Oil structure
file and was replaced with an actual consumer-root inventory. No diagnostic
bypass, broad/profile/family run or duplicate live observation was used.

## Final focused observations on the same current core

Every literal used separate bounded-exact invocation, timeout30000ms,
termination grace5000ms, output6500chars. All results below exited0 with no
truncation, timeout or abort. Counts are scoped assertions, not full regression.

| Exact literal | Pass/fail | PID | Inner / observer ms |
|---|---|---|---|
| Connected rollout: occupied launch cannot create on a disconnected remote tile |10/0|37384|453 /607|
| Connected rollout: physical clearance resumes saved WIP without remote birth or duplication |63/0|25660|892 /1047|
| Connected rollout: prospective footprints, budget and strict authored lane admission |24/0|32888|415 /568|
| Connected rollout: independent point launch waits for real clearance and replays |22/0|37680|458 /640|
| Producer home: idle output clears service access for physical replenishment |57/0|27176|2384 /2586|
| Output home policy: independent processor uses retained homes and physical saved results |27/0|40184|515 /689|
| Producer home: parking-only facilities do not invent service capabilities |14/0|25388|694 /854|
| Output home policy: explicit JSON, pure admission and resolved physical homes |202/0|27412|1602 /1771|
| Prospective rollout: whole planned geometry and initial inventory without spawning |700/0|9476|159 /317|
| Oil production: canonical timed manufacturing and optional cost annotations |882/0|37300|24223 /24377|

Latest result per literal:2001/0 across ten exact cases; four new rollout cases119/0.
Repeated intermediate observations above are not extra independent coverage.

Artifact groups: first nine were initially run with test DLL2F9311CD... and
ExtensionProof5332045F...; planning initially22/0. Oil's final observation used
test DLLF530EEC8... and the SAME ExtensionProof5332045F.... The final planning
smoke adds two assertions on test DLL43C818C7... / ExtensionProof3991F5A7...
after reference-colony JSON and an owned-entity test lookup changed. Runtime core
FECAA147... is identical in every final group; do not claim all cases were rerun
against the final test binary. No runtime source changed after the8.45s build.

Physical proof preserves four real WIP units while launch is occupied, then uses
an ordinary player movement command. The engine moves that actor, waits for
actual clearance, creates exactly one output, consumes WIP only on successful
conversion, physically reaches parking, preserves saved twins and replays the
same commands/hash. No teleport, resource grant, refund or queue substitution.

Planning proof includes a TWO-cell unavailable wall whose non-origin cell blocks
egress, declared-budget Pending, strict malformed/unused production metadata,
pure repeated queries and no fake entities. Independent point proof waits on a
real occupant, then uses normal future-home intent and release to clear it,
with saved twins and replay. Reference-colony initialization validates explicit
material service/launch/parking. Oil recipe proof retains both full36h manufacture
cycles and original optional18000 charges. Its24.2s runtime is not proof of a
general allocation/CPU/soak budget or broad performance regression success.

## Data migrated and data STILL REQUIRED

Updated framework fixture data: original berth, reference colony factory,
Planet X refinery, independent point emitter and the reference Oil manufacturing
fixture. They select actual launch/exit geometry; no reader fabricates it.
Reference colony retains real steel service and known crane/haul roles, rather
than making its launch-only layout reject physical supply. Planet X has separate
explicit right-side launch/exit slots, not a route crossing its planned root.

The ACTUAL consumer `content/oil/structures.json` remains UNMIGRATED this turn.
Read-only inventory identifies FOUR mobile producers without tile_work_dock:
company_base4x4, vehicle_factory4x4, maintenance_depot3x3, shipyard5x4. Their
respective output recipe counts are6,6,2,7. Resource-only facilities have no mobile
egress requirement. With current strict native rollout, these missing layouts
must be authored before actual consumer initialization/acceptance can succeed;
the reference fixture's882 passes do NOT prove that migration complete.

Next actual data work must preserve material/service/repair access, initial-kit
capacity and prospective prefix clearance at company_base, land/sea movement
classes, stable public views and all amounts/times/geography. Author sufficient
launches/exits/parking for the real starting kit and physical production, not a
single bay that makes founding impossible. Do not add an old ring fallback or
silently steer idle vessels in a host. Canonical berth/parking/full fleet and
offshore acceptance remain OPEN.

Other fixture callers that deliberately replace a mobile entity_output must
explicitly replace the corresponding lane's output identity too; no reader or
test factory may automatically infer that change. Wider custom geometry/corner/
clearance/fuel/topology/fault/stress matrices remain unproved. The earlier two
dock failures and combined transformation75/16 remain unresolved, not rerun or
declared green here.

Original audit remains221 requirements /209 recorded checked /12 open;
P9.acceptance.051 and full goal remain ACTIVE/OPEN. No canonical2.650 checkpoint
was earned. All older native artifacts and consumer binaries remain historical,
incompatible evidence; never rewrite their headers. No commit or push.

Final handoff: fresh original audit221/209 recorded checked/12 open; git diff
--check exit0 with35 existing line-ending warnings and no other findings; staged
paths0; no live bounded workers. Goal remains ACTIVE, not complete or blocked.
