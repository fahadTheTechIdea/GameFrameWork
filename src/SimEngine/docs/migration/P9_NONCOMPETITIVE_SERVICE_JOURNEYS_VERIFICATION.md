# P9-SJ1 — Noncompetitive dock candidate verification

Date2026-10-16. Engine2.646.0/save160/replay1. Focused selector and canonical
three-facility harbour completion verified. Full marine workflow/goal remain open;
the initial one-day wall-limit failure is retained, not erased by later progress.

## Implementation and correctness

Original WorkBoard.SelectDock is still the sole slot selector. Its existing
Check helper now evaluates only original guarded policy, claim, queue, vacancy,
cost and reach eligibility. Retained-slot selection still invokes a FULL original
DockJourney after successful eligibility, preserving preference over any new
cheaper slot. Ordinary candidates run the same eligibility first, then the
original Cheaper comparator against an ACTUALLY FEASIBLE incumbent. Only a losing
full journey is skipped. Every potentially better candidate and every necessary
fallback still receives the original full route/area/continuation validation.

There is no feasible incumbent merely because a cost is small; no incumbent means
no pruning. Original no-success Pending/refusal precedence remains. Actual chosen
arrival/duration, port/cargo/slot promises and real motion remain owned by the
existing engine paths. No scheduler, cache, second ranking function, parallel
claims, fabricated material, teleport, shorter work or broader timeout.

Build/current protocol identity advance together, save160/replay1 unchanged. No
old-header rewrite, compatibility path, business/platform/Draw or Oil logic edit.

## All focused attempts and builds

New literal:
`Dock candidate selection: full eligibility precedes pruning losing service journeys`.
Reuses DockTravelSelectionTests.Factory, original master-authored PointGame/
dock-cost-points.json and public contract PointMotion/PointDock providers. Test-only
witnesses throw if a losing far/tied SERVICE route is invoked. Other modes require
a blocked-cheap fallback, all-blocked/Pending precedence and mutation in a losing
candidate's still-required guarded COST callback. Reversed JSON slots and original
native saved twins exercise the same selector, not a mirror implementation.

| Attempt | Child | Assertions | Runtime / observer | Result |
| --- | --- | --- | --- | --- |
|New case baseline2.645|24020|0/1|233/369ms|exit1; actual losing service route invoked;untruncated |
|New case final2.646|30596|142/0|278/406ms|exit0;untruncated |
|Existing physical selection|35460|241/0|1725/1855ms|exit0;untruncated |
|Existing retained-claim admission first|24996|74/1|1389/1530ms|exit1; unconditional fault witness activated during initial acquisition |
|Retained-claim admission corrected|31620|77/0|1318/1454ms|exit0;untruncated |

Three distinct final framework cases total460/0. No count inflation from failed
or repeated attempts. New30s bound; existing physical/retained90s bounds; all5s
termination/10000chars. Exact existing names:

- `Dock travel selection: actual prospective cost precedes stable identity without displacing physical claims`
- `Dock replanning: pure alternatives, retained claims and typed holds`

Physical proof includes minute/half-minute, near/prospective/tied and reversed-slot
cases, real loading/travel/unloading/departure, exact stock/cargo, retained slot
commitments, private projections, native saves, isolated idle session and replay.
Retained proof covers static obstruction alternatives, actual policy retarget,
receipt/identity preservation, full no-alternative holds, finite competing service/
waiting slots and negative provider cutoff before retarget publication.

The74/1 failure is a stale NEGATIVE FIXTURE, not relaxed engine admission. It
enabled route-touch before acquiring its initial job, but both the old and new
selector call DockJourney on the first candidate without any incumbent. Native
initial acquisition correctly invokes that provider and faults. The proof now
authors route-touch-in-area: healthy acquisition precedes the later region-policy
change that activates the same real Entity.Touch failure. Full route/fault guards
are unchanged; no engine path was weakened. Other unconditional route-touch tests
retain their original probe. The142/0 and241/0 ran on the identical final production
core before this test-only witness correction and are not rerun/count-inflated.

Target Release --no-restore project builds: baseline5.35s clean, implementation
6.38s clean, negative fixture correction5.35s clean, actual consumer1.57s clean.
No full solution/profile/family regression or default full marine run.

## Canonical actual consumer boundary

Same original Gulf96x96/seed1/two humans/seat1 and authored construction-site docks.
No world search, geometry/stock/timing change or mandatory economy. Preparation
child35040 passed24/0 in7520ms/observer7619ms,exit0/untruncated. Minute11 gameplay
hashE7B4CC98D6977D89 matches the preceding docked builds exactly. One timing sample
does not establish an engine-wide speedup.

Native preparation:
`artifacts/marine/ac061003595b40b48a4c380097822acc/checkpoint.json` in actual Oil consumer;
game.sav SHA256AC710042D126AAAD7CEDF4799D9A7B9911C938555A8D96DBE59F7253F3132341.
First-day continuation session97946/child33492 used that real native artifact,
180s process cap/5s grace/18000chars, unchanged120s inner threshold checked after
native batches, one-day observation and original25-day harbour deadline. It FAILED
the inner wall limit:20/1,121848ms/observer121945ms,exit1,untruncated; diagnostic
120996ms. Native minute1331/hash2E9601E4F5E80E87, no fault. Depot clearing250/480;
shipyard erection0/3600 correctly waits for materials. Initial hauling jobs4/5
have finished; ordinary follow-up loads8/9 are29.333 and21.333. This actual minute1331
hash matches the earlier2.643 progressed checkpoint exactly; not a completed
harbour or passed one-day performance result.

Native checkpoint:
`artifacts/marine/394875dd527c47f397439748cef9f8e3/checkpoint.json`, game.sav115211bytes,
SHA256EF88A759F9399C3EEF047A9BD7700F70D5992F9BC968E3F7780D49F29FB1BB49.
Continuation session57137/child18448 proceeded from this real native save under
identical bounds, not a rerun of preparation or simplified fixture. It PASSED22/0,
120076ms/observer120177ms,exit0/untruncated. The inner simulation/checkpoint
observation completed119189ms: a complete game day1331→2771, hashB0BC66279EC02C20.
All construction hauling jobs had completed and both trucks were idle. Real crane
job11 had earned shipyard erection1207/3600; depot/grid erection waited for the
same busy crane rather than missing stock. Bulldozer clearing was complete.

Next native checkpoint:
`artifacts/marine/0ee157bf34f74532b1b01ebc3d884d30/checkpoint.json`, game.sav136009bytes,
SHA2563B703006A0B4537142384462357C46440F6D73694D6CD57CA03CD2064F664A16.
Session76119/child34644 continued from that actual native state with the SAME
bounds:22/0,33874ms/observer33974ms,exit0/untruncated, complete game day2771→4211,
hash23B54378AB09E6EA, shipyard erection2647/3600. Artifact
`artifacts/marine/b686f1032ffe416faea7c251c22f7c7a/checkpoint.json`, save SHA256
7649AEF7E7C7C261052CD0D70C827E3EB2CA76585F483CC07A69D55291C9F7E7.

Session39619/child30652 then passed22/0,41871ms/observer41969ms,exit0/untruncated,
complete game day4211→5651/hash94E7052DE4DFECFC. Shipyard site#10 had completed
and converted through the engine to actual shipyard#13; original order/job gone.
The same real crane physically departed and was working at depot service72.5,72.5,
earning415/1800. Grid still waits for that crane. No new machines or instant work.
Native artifact `artifacts/marine/680af8716f4943e992853c79b2f0847d/checkpoint.json`.
Save SHA256A43FA3335F678E7A0F9FE587DB21759162063F9FF18F732041DCDF228719A5AA.

Session2964/child26796 then passed19/0,47901ms/observer48005ms,exit0/untruncated,
full day5651→7091/hashAC1EFF126250C1BE, body/checkpoint47129ms. Depot site#11
completed as actual harbor_depot#14 and its order/job disappeared. The same crane
physically moved to grid service74.5,72.5 and earned26/960; no other project remains.
Checkpoint `artifacts/marine/d3d267e63d45477daff87bfb9e5e2176/checkpoint.json`.
Save SHA25633D54626B2F9330EC4EBDE8753F5B6ECE7151871A304A47F3F1A72D5A0501B59.

Final session27219/child8448 passed16/0,52647ms/observer52747ms,exit0/untruncated,
body/checkpoint51898ms. It reached minute8051/hash040E86B23A4560E2 and HARBOUR
COMPLETE, within original11+25-day deadline36011. Actual shipyard#13,
harbor_depot#14 and grid_connection#15 exist; all three sites were converted through
ordinary timed work, no construction orders/jobs remain. Grid is operating; the
same crane is physically moving home, not teleported. No additional machine,
resource, cash, shortened graph, changed fixture or timeout was introduced.

LATEST CURRENT native checkpoint:
`artifacts/marine/b1d60669869b41a382fab7f5dc4089b4/checkpoint.json`, verified_stage
harbour_complete/next_stage fleet, game.sav134888bytes SHA256
4D976C103F413F0F2C0A1965A1647295748A540EB80F1F6F7FBB5C61B90C7AF8.
No selected bounded-test worker remains after actual terminal polling/inventory.
All five successful native continuations used the same2.646 engine/content and
actual prior native save; no failed observation was restarted merely because
watching timed out. Every checkpoint received existing in-memory/disk hash checks.

The completed harbour does not imply the original full sea case passes: vessel
manufacturing, supplied marine survey, offshore pad/drilling, terminal delivery
and subsea construction remain pending. Original Play contains those public-input
steps, but checkpoint admission currently accepts ONLY harbour_execution. Next
extend the SAME MarineRun/native validation and checkpoint writer for the fleet
stage, reusing the original player sequence, not another driver or preparation.
The harbour_complete artifact MUST NOT be passed to the old harbour-only branch
or header-rewritten. Keep build2.646/content identity while adding test-only
continuation so this earned physical state remains usable.
The earlier20/1 observation remains failed; this later passed slice does not erase
it or establish universal performance/deadlock freedom/full harbour acceptance.

A stack sample on confirmed-live child18448 placed execution in actual Entity.Sync
inside dock geometry overlap/ClaimBlocker and retained ReplanDockBindings. This is
a call-site observation, not statistical attribution or authority to skip guards.

## Identities and limits

Core444-source path/hash-list digest:
632c279ac6a8dbfb806e7301f3f039caf8e32b515adcb877478942389744b555.
Core DLL SHA2563D7FD4BA688F87253264DFC267676C43CF3CC3F7BDF7DF7BF734F6AA85B158E0.
Framework tests DLL SHA256B939DCB99806C3F7E540474D46CE58B2EC007C0C5290369957FB24A234D518C9.
Consumer proof DLL SHA256F57AFFD7FCAFFEC51058D3EB86124B9FD6E3EE322D361B3A032EE0F70F4A290E.

Original221 requirements/210 recorded checked/11 open freshly reconciled against
every unchanged requirement/document checkbox, no unmatched entries; no original broad
gate closes. Harbour's three-facility physical path is complete, but the full
fleet/offshore workflow and broader performance/traffic/
fault/replay/lockstep/provider/soak gates remain open. Earlier native checkpoints
remain preserved/incompatible by explicit build identity, never converted.
No full/profile/family tests, business/platform/Draw work or Git staging/commit/push.
[Design](../../plans/enhancements/P9_NONCOMPETITIVE_SERVICE_JOURNEYS_DESIGN.md),
[preceding nested observation stage](P3_NESTED_SPATIAL_BASELINES_VERIFICATION.md).

Scoped handoff: four affected stage documents have no broken relative links or
trailing whitespace. Framework git diff --check exits0 with35 existing line-ending
warnings and no other findings. Framework and consumer indexes remain empty;
no selected bounded-test process remains. No executable edit followed final runs.
