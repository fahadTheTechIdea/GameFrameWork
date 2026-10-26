# P9-A13.2 — JSON-authored automatic history evidence

Historical checkpoint. [Current M103 automatic-reference observation](P9_PLAN_REFERENCE_OBSERVATION_VERIFICATION.md#current-m103-first-failures-and-full-component-owner-observation--2026-10-23)
passes its extracted54-check case without running the old broad physical helper. This A13.2
source manifest and results are not rewritten as current digests or whole-history proof.

2026-10-07 working-tree checkpoint, not clean-HEAD attribution or full regression.
Build beep-simengine-2.182.0; save100; board34; work system31; work view62;
native recurring component7/public5; construction view4. The full framework goal remains active.

## Implementation and ownership

The optional sim.plan-history module requires an explicit scenario policy from master-listed
JSON. AuthoredPlanHistoryPolicyDef implements the public IPlanHistoryPolicy contract; the
public-only ExtensionProof module supplies a distinct JSON-backed PlanHistoryPolicyDef through
the same composition contract. No default policy, directory discovery, alternative executor,
duplicate history ledger, platform adapter or module-owned loop was introduced.

WorkBoard alone stores the selected actual content reference, resolved minimum age and saved
native-ID fairness cursor. Its existing Sync owns policy identity/cursor and actual terminal
PlanEntry.EndedTick timestamps. EndedTick is a coordinate of the session's sole GameClock,
not another clock. Cancellation/settlement records actual end time; creation time is not age.
Saved policy mismatch, invalid cursor and future/inverted terminal chronology fail explicitly.
Current identities migrate directly; no legacy save reader or timestamp fallback exists.

Transient cohorts reference existing PlanEntry records: actual recurring CycleHead, manual
company/group, or an ungrouped singleton. Terminal age uses the latest actual member end time.
Newest terminal cohorts are retained per company with stable identity tie-breaks. After native
settlement/pruning, WorkSystem asks the same board to scan at most the authored cohort budget
in native head-ID round-robin order, including protected/live cohort visits. The cursor persists
through save/load without another sequence or cleanup timer.

Whole candidates reuse A13.1 explicit retirement admission. Removing blocked whole cohorts and
rechecking the safe set preserves ancestry into newly excluded cohorts. Live source history,
native disposition, controller cursors/replies and module references always override age/count
policy. Referenced records may remain indefinitely: this is not a hard history/world-size cap.
Ordinary explicit retirement remains available independently of automatic age/minimum policy.
Completed structures, resources, native IDs and earned physical work are not deleted or refunded.

IPlanBoard and WorkView expose detached immutable policy/cohort views. Company-private views
explain live, young, newest and actual reference blockers without advancing time/cursor or
pruning records. Eligibility is reference/policy admission, not a promise that the current
scan budget selects a cohort in this Update. Recursive reference admission explicitly refuses;
a transient finally-cleared guard prevents overflow, not another saved authority.

Tests submit player commands and normal updates, then inspect/assert state. Public proof JSON
authors actual spatial presence, enabled work permission and its required stopped state for
machine self-inspection. The existing work-permit implementation is reused; no instant work or
test-side policy/retirement logic is provided. Private internal probes are limited to corruption
and deliberately adversarial provider behavior.

## Verification actually run

Final implementation build: dotnet build Beep.SimEngine.slnx --no-restore; zero warnings/errors.
Only these two exact existing suites were run, with their full names as runner filters:

| Exact suite | Passed | Failed | Runtime |
|---|---:|---:|---:|
| Recurring templates: authored groups, private admission and cycle ownership |559|0|1108ms|
| Recurring templates: physical projects, cancellation and saved continuation |448|0|4683ms|

1,007 final assertions, zero final failures or aborted suites. Existing A13.1/A12/A11/A10/A8/A7
callbacks remain included. A9 and separate controller/operation/incident/construction/replay/
other suites compile only, not rerun. No third, family-filtered or full regression gate ran.
Documentation reconciliation did not change runtime inputs after those final implementation gates.

Commands used:

```powershell
dotnet build Beep.SimEngine.slnx --no-restore
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: authored groups, private admission and cycle ownership"
dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build -- "Recurring templates: physical projects, cancellation and saved continuation"
```

### Direct coverage

- Required/missing/invalid/extra JSON fields and explicit policy identity; absent module does
  not prune, while explicit safe retirement still works. Native/public JSON-backed policies
  resolve through the same board; no fabricated fallback.
- Long-lived pending intent remains live. Age starts at actual cancellation/completion, not
  creation; same-clock pause freezes time and scan cursor. Private immutable diagnostics and
  unchanged hashes prove the covered reads do not mutate authoritative state.
- Per-company newest cohorts and fixed-point ancestry into excluded history remain intact.
  Explicit retirement may override automatic minimum count, but not actual references.
- Budget1 visits remove at most one cohort, possibly zero when protected. A permanently live
  older head cannot starve eligible later cohorts. Partial saved cursors continue identically;
  native IDs remain monotonic. Corrupt cursor/end chronology refuse.
- Two dependent physical projects consumed8 of20 material units and produced two structures.
  Whole graph history retired only after the latest actual completion aged; structures and
  remaining12 units were preserved. Saved twins agreed before/after retirement.
- Native/public machine self-inspection sources each completed20 actual cycles. With authored
  budget1, at most two plan records remained in this fixture: latest referenced history plus
  at most one terminal cohort awaiting its fair scan. This is a fixture-specific bounded proof,
  not a general maximum for all providers or policies.
- Native/public frozen two-entry template cycles retired as whole old cycles, preserving exact
  new ancestry and source head; save/load retained no orphan cycle heads. Public provider-held
  expired history remained protected; recursive diagnostics failed without changing hash/time/
  cursor/history. Fresh/interleaved sessions used separate board/cursor/clock state.
- Original A13.1 provider/controller reply/cursor/disposition/canonical/private retirement
  callbacks ran inside these same suites; separate controller suites did not run.

### Earlier failing checks and corrections

The first contract run had552 passes and1 failure: a fixture expected exactly one retirement
although the one-cohort budget visited a protected live head. The assertion now correctly
requires at most one retirement and separately proves eventual fair progress.

The first physical run had418 passes and1 failure/aborted suite: self-inspection was unreachable
because its intent-only source had no authored spatial presence. The fixture now supplies actual
point presence. The bounded diagnostic wait exposed this failure without hanging the gate.

The second physical run had425 passes and3 failures including an abort: it incorrectly expected
instant one-record history, and the public source lacked its required actual work permit. The
fixture now allows the honest one-cohort fair-scan backlog and explicitly authors enabled permit
and its required stopped state. The existing permit definition was declared in the public point
proof module. No production fallback, scheduling shortcut or time rule was added for these fixes.

Intermediate repaired gates passed553 contracts and434 physical assertions. Subsequent public
policy, whole native/public template and recursive reference proofs produced the final559/448
counts above. Earlier failure counts are not erased or added to the final passing total.

## Source inventory and structural checks

[Source manifest](P9_AUTOMATIC_PLAN_HISTORY_SOURCE_MANIFEST.json) records functional paths,
87 mechanical current build/save/board metadata migrations, unchanged ownership/time/content
dependencies and current documents with SHA-256 hashes. Overlapping paths occur once in the
hashed source inventory. Older manifests remain historical checkpoints, not current digests.
Most mechanically migrated suites are compile-only evidence.

Structural checks verify every listed file/digest, relative links in the new stage/evidence,
HTML parsing, scoped whitespace/diff checks, current identity migration and the sole GameClock
construction/Sync ownership. GameClock itself remains unchanged from the preceding checkpoint.
These checks are not runtime/full-regression acceptance.

## Review and remaining gates

The lifecycle skill aligned phased design, implementation, focused proof and the single tracker;
its generic full-regression recommendation is superseded by the user's exact-suite-only rule.
Single board/state codec/update owner is preserved. Public policy substitution and optional
reference contracts use the existing extension registries; the public proof compiles without
internal access. Hosts/demos do not own gameplay decisions.

The cohort visit budget bounds selected cohort work, not total CPU/allocation: deriving cohorts,
newest selection and reference checks still read the board and may allocate. Global purity of
arbitrary provider/session mutations, large-history performance, replay/lockstep, fuzz/soak,
active workflow revision, wider provider coverage and full framework acceptance remain open.
No OilFieldDays/Tycoon/host/demo/platform/Unity/Godot/Draw changes or full-suite claims.
