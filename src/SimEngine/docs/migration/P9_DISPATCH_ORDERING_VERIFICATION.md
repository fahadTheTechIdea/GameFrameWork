# P9-B4d — Authored dispatch ordering: focused verification

Date: 2026-10-08. Canonical repository: Beep.SimEngine.
Scope: ONE distinct NEW bounded case only; full goal/plan remains ACTIVE.
[Stage](../../plans/enhancements/P9_DISPATCH_ORDERING_DESIGN.md).
[Master tracker](../../plans/enhancements/MASTER_TRACKER.md).
[Self-excluded source manifest](P9_DISPATCH_ORDERING_SOURCE_MANIFEST.json).

## Outcome

Existing IWorkerDispatchPolicy/DispatchProfileDef now owns required typed Ordering, authored explicitly
as priority-first or starvation-first in the existing master-listed JSON catalogs. The original
WorkSystem.RankOffer stamps immutable derived offers; original CompareAssignments validates one common
ordering and retains ready/emergency precedence, original stable identity/cost ties and physical guards.
Native and independent public profiles share that same owner. No parallel queue, age counter, scheduler,
claim owner, clock, money, Draw or platform layer was introduced.

Build2.297.0/save143/replay1/work-view84. Persistent layouts unchanged; selected content identity carries
the authored policy. Prior native/public fair profiles explicitly author starvation-first; industrial
profiles explicitly author priority-first. Missing/null/invalid selected ordering is a content refusal,
not a compatibility/default reader. 164 older C# identity paths migrated compile-only.

Executable source/proof/tests/JSON frozen at **2026-10-08 12:27:01 UTC**, immediately after final green.
After freeze only documentation/manifest edits and read-only inspections were performed.

## All build and runtime attempts

Exactly TWO solution builds, each:
`dotnet build Beep.SimEngine.slnx --no-restore`

1. Clean, 0 warnings / 0 errors, 16.74 seconds. Initial live handle43753 was polled to completion,
   not restarted.
2. Clean, 0 warnings / 0 errors, 5.75 seconds, after the observed new pause-driver repair.

Exactly TWO attempts of the SAME ONE new case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Dispatch ordering: authored player priority, starvation and physical saved execution"`

1. **110 passed, 6 failed, 0 aborted**, 717 ms. All six assertions expected pause while the new
   driver called RunTicks(1), the documented tool fast-forward that intentionally ignores pause.
   The clock advanced one step and its authoritative state hash consequently changed in each of
   three physical profile stages. No product defect or old runtime case was inferred.
2. Repaired ONLY the observed new pause-driver line to call normal Game.Update on both sessions.
   **116 passed, 0 failed, 0 aborted**, 777 ms.

No older, family, full-suite, Oil, Tycoon, platform or stale runtime was executed.
No executable/content changes were made after the final green.

## Physical acceptance covered

- Required explicit native/public ordering contracts; missing/null/unknown JSON ordering refused before
  Update. Mixed/invalid derived ordering refuses comparison instead of substituting another profile.
- Real disabled worker creates an aged low-priority pending order; a younger high-priority order enters
  through normal preview/command admission. Existing age threshold determines actual starvation.
- Native/public priority-first selects player priority before that aged order. Explicit fair profile
  selects the actual oldest starved order first. Eligibility and emergency precede either setting.
- Ordinary enabled-work command assigns the actual machine through the original coordinator. Assignment
  does not create progress or remotely consume inputs. Machine physically reaches the remote point;
  its actual work consumes eight existing materials. Both jobs finish physically with exact final stock.
- A real incident response outranks ordinary priority int.MaxValue, travels and completes through
  existing response/work machinery and consumes only its explicitly authored inputs.
- Same-company detached queue facts are pure/private; the foreign seat cannot read them. Candidate
  projection retains engine-stamped effective priority, starvation facts and ordering.
- Save/load during aged waiting and physical execution, normal pause/resume, interleaved untouched session
  and continued twin-run hashes. Canonical command replay reproduces final physical state.
- Bounded stages permit at most1200 updates and throw clock/job/blocker diagnostics on a stall.
  Replay limits8MiB/5000 existing records; these are explicit verification bounds, not world-size caps.
- No IEconomy is selected: physical work/logistics do not require optional business rules.

## Remaining gates

No full P1-P12 acceptance claim. Broader provider policy mutability/purity, exhaustive external-source/
native-haul joint acquisition, fairness under sustained load, lockstep/shuffled admission, stress/soak/
allocation/CPU, complete spatial utility breadth, global hostile-provider observation, independent game
acceptance and Oil/domain migration gates remain open. Earlier reports remain historical, not rerun on
build2.297.0. Strict priority-first cannot guarantee low-priority progress against an endless stream of
higher-priority work; starvation-first is an explicit game-policy tradeoff.

No Git staging, commit or push. The user's publication instruction applies only after the FULL goal
and plan finish with required verification; a scoped successful slice does not satisfy that condition.

## Read-only source audit

The accompanying manifest inventories current and prior referenced paths with stable SHA256 hashes.
Its own path is excluded to avoid self-hashing; the prior P11-D16 manifest remains included.
1810 self-excluded paths,592 Markdown and3650 local links. Of359 prior runtime paths, exactly five
changed: four original dispatch contract/rank/view files and the build identity. Of330 prior authored
extension JSON files, exactly three existing catalogs changed; no new catalog was added. The sole
GameClock SHA256 remains35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1.
Actual git diff --check exited0 with31 pre-existing line-ending warnings; all18 existing phase rows
retain five data columns. No destructive operations or source rewrite followed the freeze.
