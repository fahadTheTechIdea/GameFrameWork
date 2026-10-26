# P9-C12 — Named fleet work-policy controls: scoped verification

2026-10-08, canonical Beep.SimEngine only.
[Stage](../../plans/enhancements/P9_FLEET_WORK_POLICIES_DESIGN.md).
Full P1–P12 remains ACTIVE/incomplete; publish only after FULL verified goal AND plan finish.

## Implementation and ownership

Existing WorkPolicyRules becomes partial with whole selection in WorkPolicySelection.cs, not another
policy service. PreviewGroup owns canonical copied unique member IDs, actual seat/ownership, original
CheckConfigure and immutable current WorkPolicySettings facts. Actual IWorkPolicy must belong to its
worker; original provider-codec observation checks pure CheckPolicy/Read. Foreign entries disclose no
settings. Original Configure skips exact same settings and verifies changed version/exact settings/
owner/job/session identity when committing. ConfigureGroup preflights ALL before stable commits;
normal refusal changes no prefix. Violated provider commit latches original world fault, no rollback.
This is not hostile-mod whole-world sandboxing or a simultaneous reservation transaction.

MachineFleetService shares existing named control admission with navigation and delegates to that
original rule owner. Records remain only company/name/profile/canonical membership, codec1. No fleet
work-settings overlay, automatic override, job/cargo/claim/home/route/time copy. Current membership,
not a host's stale selection or retained preview, decides who receives normal control. Removing
metadata or a member never cancels the member's live job or discards its policy/cargo/receipts.

One sender-stamped ConfigureFleetWorkPolicyCommand uses original WorkPolicySettings Sync and inbox
copying; result returns actual target/changed counts. FleetView2 supplies private immutable per-entry
blockers/settings, concealing foreign existence while commands retain ownership refusals. Strict
WorkPolicyDef JSON is still the sole authored settings model. Public proof module reuses existing
independent ProofWorkPolicy; no duplicate provider, scheduler or harness gameplay implementation.

Actual native/public policies call original dispatch.RefreshPolicy. Disabled live work pauses with
worker/cargo/reservations retained; reopening respects native current roles/areas/emergency/
continuation gates and takes physical handling/travel/unload time. Policy is not explicit player
held control; Game.Update resolves each actual entity's authored state. No teleport, instant transfer,
material refund or completion grant. Fleet work does not require money/business.

Build2.267.0/save140/FleetView2; fleet service1/WorkView80/existing physical payloads unchanged.
Original sole GameClock unchanged. No Oil/Tycoon business source/hosts/demos/Draw/platform edits.

## Every build/runtime attempt

All builds: `dotnet build Beep.SimEngine.slnx --no-restore`.

| Attempt | Exit | Build seconds | Warnings/errors | Observed repair |
|---|---|---|---|---|
| 1 | 1 | 6.18 | 0/1 | NEW fixture used JsonValue.Of boolean constructor for numeric fault2; use original JSON numeric parser |
| 2 | 0 | 3.35 | 0/0 | None |
| 3 | 0 | 3.32 | 0/0 | NEW assertion below corrected to read the authored EntityStateDef.State instead of definition ID |

Only runtime command, TWO invocations of SAME ONE exact NEW bounded case:
`dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Fleet work policies: whole admission, physical cargo pause and saved mixed-provider delivery"`.

| Attempt | Exit | Passed | Failed/aborted | Milliseconds | Observed repair |
|---|---|---|---|---|---|
| 1 | 1 | 110 | 1/0 | 897 | NEW fixture expected survey.stopped (definition ID), actual authored state code is stopped; test-only repair |
| 2 | 0 | 111 | 0/0 | 767 | None |

Engine/public proof/JSON unchanged after first runtime; only erroneous state assertion changed.
All source/public proof/tests/JSON frozen after final green; docs only afterward.131 older strict
build identities compile ONLY; original native factory changes module/master/document assembly,
not gameplay, and its previous runtime case is NOT rerun. No old/family/full/platform/stale runs.
Each physical stage1500 Update bound emits actual clock/jobs/worker/cargo/blockers/positions on stall.
Observed767ms is not CPU/allocation budget. Every failed invocation recorded, not hidden by final green.

## Proven scope

- Actual master-selected native JSON/public component ownership and unchanged original persistence.
- Native plus independent public IWorkPolicy in one real fleet; missing policy member refuses all.
- Canonical detached immutable current member/settings facts; native valid prefix unchanged on refusal.
- Malformed selection, invalid seat, foreign settings privacy, null/unknown role/area, empty named fleet.
- Pending named command copied from caller, saved inbox/membership/owner state continues identically.
- All fields retained: enabled, priority77, haul roles, source-only area, not interruptible,
  emergency eligible, ordinary assignment mode. Same-value command changes zero/no provider touch.
- Original area gate blocks automatic acquisition and stock transfer despite actual released carriers.
- Enlarged area admits original jobs; ONLY actual released native/public current members execute them.
- Positive partial loading, whole policy disable pauses actual jobs and keeps original worker/
  source/cargo/destination claim identities and loaded quantities. Entity Update settles authored state.
- Saved partial policy/cargo, equal explicit local Pause on both sessions, no physical advancement
  while disabled even as game time advances. Reopen emergency-only policy retains real ordinary work.
- Loaded physical departure, unchanged worker/destination commitment, saved travelling continuation.
- Edited current membership targets native policy99 only; removed public member retains own policy21.
- Metadata removal during delivery leaves original work/materials alive; actual timed deliveries
  finish16 at destination+4 source=20 total, real cargo empty, saved hashes identical.
- Independent sessions/public initial defaults and optional fleet-module omission.
- Five strict original work-policy document errors refuse; one actual public CheckPolicy mutation
  detected by scoped observation faults session and refuses a healthy save.

## Open and not claimed

Fleet-wide home/fuel/service/load controls/formation/corridors, every policy/provider/fault/corruption/
hidden-content/privacy/whole-world purity/interdependent commit matrix, malicious-mod rollback,
emergency incident end-to-end, all operating areas/non-tile/no-work worlds, replay/lockstep/shuffled
commands/fuzz/soak/CPU/allocations/full P9/full engine/P1–P12 acceptance. Runtime111 is not regression.
All fields retention is not a complete industrial-emergency or competitive priority scheduling proof.

## Source checks

Selected files/hashes and every invocation recorded in
[source manifest](P9_FLEET_WORK_POLICIES_SOURCE_MANIFEST.json). Scope is not full-repository acceptance;
prior historical source reports unchanged, manifest's own hash excluded.

Scoped698 paths/143 Markdown/2146 local links: zero broken links/duplicate selected paths.
Actual git diff --check exited0 with existing LF/CRLF warnings. Original GameClock SHA256
339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31 unchanged. Final readonly
hash/phase-row-shape results recorded in manifest. These source checks are not runtime acceptance.
