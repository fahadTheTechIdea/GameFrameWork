# P7-R4 reference-only commit verification

Engine2.615.0/save159/replay1. No new state, view schema, registry, persistence or
reference coordinator. Independent protocol expectation advances with the engine.

## Existing owners revised

Upgrades.Relocate retains original whole-vector resource movement. Afterward it
captures original source/target EntityObservation facts with version changes allowed,
selected-space scope and detached participant spatial snapshots. Each registered
IEntityReplacementReferenceOwner may update its references and touch parent versions;
after EACH callback, original identity/owner/state/clock/random/space facts verify,
resource invariants verify, and physical snapshots must still match. A first bad
owner cannot proceed into a later owner's callback.

The old post-loop stock/claim check is extracted into one VerifyRelocatedStock used
before and during rebinding. Actual endpoint identity/company, empty receipt books
(including supplier-only books), empty retired-source stock and exact target vector
are checked through original live entity/provider observation. No second stock total
or synthetic receipt ledger is introduced.

Original PlayerView.ViewOf spatial capture and EntityView.Matches detached-shape
comparison are MOVED to EntityObservation.ReadSpatial/MatchesSpatial. Views and
replacement now use the same capture admission and reflexive/symmetric/stable
comparison; no copy of the validator remains. Null/type/space behavior and original
errors are retained. Shape providers remain contract-based, with no point/tile
branch in production code or rendering/platform dependency.

Checking only the spatial service is insufficient: the independent PointPresence
can change position without changing PointSpace's persisted state. Comparing its
detached native shape catches that case while allowing legitimate reference fields
in a provider's original codec to change.

## Negative instrumentation and every attempt

Existing ReplacementReference accepts immutable JSON negative commit modes. It
first performs its legitimate own reference/counter update, then optionally changes
pause, speed, selected-space access, random stream (including draw-and-restore),
source/target position, target stock or target inbound claims. No new persisted
probe field/codec or gameplay policy is added. A second ordinary reference owner
provides the observable cutoff assertion.

1. Reproduction build: affected project --no-restore -v minimal, exit0, zero
   warnings/errors,5.71s.
2. New exact case baseline232passed/92failed,455ms; child34108 exit1/594ms.
   Its5500char stdout WAS truncated. It showed silent clock/space/random/geometry
   mutation and later-owner execution even when stock/claim violations eventually
   faulted. Baseline also contained18 bad fixture assumptions that initial history
   must be empty; native initialization facts legitimately exist. Those assumptions
   were corrected to captured prior count/watermark, not classified as engine bugs.
3. Correction build: exit0, zero warnings/errors,9.22s. It initially returned live
   exec session20622, which was polled to terminal exit0 BEFORE any runtime began.
   No restart was inferred from the observation yield.

Final runtimes:

| Exact literal | Checks | Suite ms | Child / outer ms |
| --- | --- | --- | --- |
| Reference commit: clock geometry stock and claims stay invariant across saved replacement | 324/0 | 448 | 15356 exit0/588 |
| Entity replacement references: physical homes independent owners refusals and saved continuation | 96/0 | 494 | 27356 exit0/634 |
| System replacement references: declared order aliases pure refusal and saved physical continuation | 72/0 | 471 | 14460 exit0/597 |
| Network replacement references: slow connected push pull timing and saved physical upgrades | 408/0 | 3235 | 34752 exit0/3371 |
| Snapshot deltas: visible facts, physical mod movement and saved publication baselines | 72/0 | 2816 | 32680 exit0/2953 |

Every runtime used --bounded-exact, timeout60000ms, termination5000ms, final
output4000chars. All final stdout/stderr untruncated; all children terminal. Final
focused total972/0, not a full/profile/family run. No older unrelated test reruns.

## Evidence and limits

The324-check direct-contract case covers9modes x new/restored =18combinations. Public
seat commands establish actual references; direct TryReplace then probes the engine
contract boundary without relying on another enclosing update guard. Every mutation
faults; the later owner's counter stays0; prior native resource relocation is real
and not rolled back; source/target identities remain actual interrupted participants;
save/hash/live history refuse; prior publication and detached history survive; first
fault stays stable and an independent loaded session still advances.

Ordinary timed component96, aliased/ordered system72 and connected slow network408
cases prove legitimate reference updates, physical timing, saved continuity and
actual native stock/resource cursors still work. Snapshot72 proves the moved shared
capture/comparison implementation retains visible delta/publication behavior. These
are selected supported cases, not exhaustive verification of every reference owner.

The boundary is after each REGISTERED owner callback. An owner's private nested
algorithm remains responsible for its own contract; this is not an instruction-level
sandbox. Unrelated third-party entity/service state is not universally observed.
P7.017 still requires broader incident/cargo disposition/reference-owner audit,
conversion/spill/topology accounting and hostile/stress evidence. Original221/
200recorded checked/21tracked open remains unchanged bookkeeping, not a fresh audit
of all200. The full goal is not complete.

442core C# path/hash digest:
`222fa20283f1b8954caac0cc73e78283d90c4318d918c447ac2d9f43daa5af72`.
Focused diff-whitespace check exit0, only ordinary LF/CRLF warning. Source search
confirms entity snapshot capture/comparison validator text has one original owner
in EntityObservation, no duplicate in PlayerView. No Oil/Tycoon/host application/
platform/Draw edits or Git publication. Hosting/PlayerView is an ENGINE-owned
projection; its validator was moved into the kernel, not implemented in a host.
