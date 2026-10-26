# P3-NB1 — Nested spatial baseline verification

Date2026-10-16. Engine2.645.0/save160/replay1. Scoped observer implementation
verified; no original broad acceptance gate or full-goal completion is claimed.

## Implementation and ownership

EntityObservation remains the only pure callback observation owner. The existing
World now holds one internal nullable active SpatialScope as synchronous stack
scratch. ReadProvider saves the previous frame, constructs/verifies and activates
the current spatial scope, and restores the previous value in finally, including
construction failure, callback throw, verification failure and swallowed faults.
There is no static/thread-local/session-crossing store or saved observation state.

For an actually enclosing same-world/same-space pure read, SpatialScope reuses
the enclosing original ProviderScope baseline. It STILL reads/guards the actual
revision getter with a fresh complete codec hash and STILL performs fresh full
codec/revision verification after every callback. Original actor/provider/target/
clock/random/identity facts are unchanged. A changed enclosing revision is also
refused before a child callback. Manual commit/refusal scopes do not activate a
pure-read frame, preserving legitimate earned effects' existing commit rules.

This is not a cached successful result, version-only check, stale previous-read
hash, mutation exemption or second observer. The expected baseline is shared only
within the current call stack; actual verification reads remain fresh. No public
API, state codec, clock, save layout, geometry, work duration, stock or business
rule changed. Engine build/protocol fixture advance together; old headers are
not rewritten and no compatibility path was added.

## Baseline, builds and focused results

New literal:
`Spatial observation baselines: nested full verification cleanup and session isolation`.
It is registered by the existing SharedSpatialObservationTests owner and uses the
existing master-authored PointGame/PointSpace. Proof-only Sync/revision hooks
count actual codec calls or inject a bad getter; they own no gameplay or saved state.

| Attempt | Child | Assertions | Runtime / observer | Outcome |
| --- | --- | --- | --- | --- |
|2.644 baseline|33620|59/4|210/338ms|exit1; nine rather than seven codec calls twice; corrupt parent prefix still entered/returned from child |
|2.645 initial focused proof|12624|63/0|218/348ms|exit0; untruncated |
|2.645 expanded final proof|7576|101/0|215/346ms|exit0; untruncated |
|Existing native tile callback fault stage|31828|48/0|365/495ms|exit0; untruncated |
|Existing layered physical navigation/save case|32620|48/0|2574/2704ms|exit0; untruncated |

Three distinct final framework cases total197/0. Earlier intermediate63 checks
are not added again. New/fault cases use30s caps; layered physical case90s; each
has5s termination grace and10000 output chars. No broad family/profile/full run.

Exact existing names:

- `Tile condition contracts: callback fault stage`
- `Layered navigation: authored roads, depth gates and physical saved work`

Final new proof covers sibling and recursive intervals, actual fresh hashes at
all nested boundaries, legal between-read mutation, fresh subsequent roots,
independent native save/load, manual-scope separation, interleaved worlds,
persisted payload and revision-only corruption, corrupt parent prefixes, revision
getter mutation, clock changes, random draws, original exceptions, caught inner
faults, stopped Update/save and frame removal on every tested exit. Original
private invalidation counter is injected only in framework fault tests, not
duplicated as gameplay state. Physical case executes actual native routing,
road/depth changes, reroute, arrival, earned work and restored continuation.

The counted three-interval example changes nine full codec reads to seven (two
redundant initial captures removed); getter and callback verifications remain.
This local count is not an engine-wide throughput or timing claim.

Target Release --no-restore builds: baseline6.61s clean, implementation7.18s clean,
expanded proof5.00s clean, actual consumer1.70s clean. No solution regression run.

## Actual canonical consumer boundary

Original Gulf96x96/seed1/two humans/seat1 fixture and authored construction-site
docks; no search for a smaller/easier world, resource grant, shortened work,
different geometry, extra economic module or changed deadline.

Preparation child9004/session4824 passed24/0 in9726ms/observer9828ms,
exit0/untruncated. Minute11 gameplay hashE7B4CC98D6977D89 matches prior docked
builds exactly. Single observed prepare time, not a statistical speedup claim.

Native artifact:
`artifacts/marine/84b247fc6fde4dbd86779799c5363778/checkpoint.json` in actual Oil consumer;
game.sav SHA256342A2639E15C4D6326D5180EBFC2034D3C9E736D713A6784DF409259FA60732A.
First-day continuation session53000/child32640 used that same actual native save,
180s process cap/5s grace/18000 chars, unchanged120s inner threshold after native
batches, one-day observation and original25-day harbour deadline. Terminal result
FAILED the inner wall limit:20/1,134200ms/observer134300ms,exit1,untruncated;
diagnostic133345ms. No engine fault. Native minute1151/hash5A756513744DEE83:
shipyard clearing complete, depot clearing70/480, truck5 at actual shipyard service
73.5,78.5 with16.666 loaded/haul4 phase3, truck6 moving in new haul6. Shipyard
erection still0/3600 and correctly waits for real materials. This is physical
progress, not a passed one-day or completed harbour test.

LATEST CURRENT native checkpoint:
`artifacts/marine/a5f4cca24f574297bd7384e8681e43cf/checkpoint.json`, stage
harbour_progress/next harbour_execution, game.sav65264bytes SHA256
B25E6444F557B857589D0F96BE7F9763DD6041AA53EF8F4F16F91EBDD8EF8BE9.
No active bounded worker remains after terminal polling and process inventory.
Previous2.644 first observation reached1031 under its unchanged threshold; these
different terminal intervals are not equal-work samples or a global speedup claim.

Next inspect the existing WorkBoard.SelectDock candidate loop: its Check helper
executes a complete DockJourney before the outer loop discards losing candidates
with the original Cheaper comparator. This is analogous remaining work to the
already-corrected departure loop, but is NOT changed or verified by P3-NB1. Preserve
retained claims, guarded eligibility/cost and all necessary route/fallback checks.
Continue actual canonical physical work from the current native checkpoint;
never relax bounds, shrink work, silently change the fixture or rewrite old headers.

## Identities and limits

Core444-source path/hash-list digest:
3f1787efa8f046a5160200bf40224af950645929ef0a3361b7882d38c01eb743.
Core DLL SHA256C7351F4228B0781A78825644DAC9BE2E6C40FBA58D69B4E809D8218A80067B41.
Framework test DLL SHA256B6AD85C542EB7C5613B8FF3E64034CA47161A9414263ADA1BEA68F2C2C78C736.
Consumer proof DLL SHA256789AFF0DAED0C2B553B051AD5AEBF1CE0D906468247B68234D65B3CBCB66B93A.
Source search shows the only production active-frame writers in the original
ReadProvider; World stores the one scratch slot, not a second observation owner.

Full canonical harbour/fleet/offshore workflow, broad fault/provider/private CLR/
replay/lockstep/soak/performance matrices remain open. Original221 requirements,
210 recorded checked/11 open, freshly reconciled against every unchanged requirement
and actual document checkbox with no unmatched entry; no original closure. No Oil content/gameplay,
business, renderer/platform/Draw change, full/profile/family test or Git staging/
commit/push. Earlier native saves preserved and incompatible by build identity.
See [design](../../plans/enhancements/P3_NESTED_SPATIAL_BASELINES_DESIGN.md) and
[preceding dock stage](P9_DOMINATED_DOCK_JOURNEYS_VERIFICATION.md).

Scoped handoff: both new stage documents have no broken relative links or trailing
whitespace. Framework git diff --check exits0 with35 existing line-ending warnings,
no other findings. Framework and actual consumer indexes remain empty. Confirmed
terminal process inventory contains no selected bounded-test worker.
