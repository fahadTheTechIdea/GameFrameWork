# P11-D7 physical remembered observation refresh — scoped verification

2026-10-07. Canonical Beep.SimEngine only. Full goal/plan remain ACTIVE. No commit/push.

## Actual invocation ledger

1. `dotnet build Beep.SimEngine.slnx --no-restore`: exit0,0 warnings/errors,5.15s.
2. `dotnet run --no-build --project tests/Beep.SimEngine.Tests -- "Knowledge refresh: physical observations and durable chronology"`:
   exit0,84 checks passed/0 failed/0 aborted suites,423ms, first attempt.

ONE build, ONE runtime invocation, ONE distinct NEW bounded case. No repair/rerun, stale binary,
older/family/full/platform/OilFieldDays run. After green, gameplay/public proof/JSON/tests frozen;
only documents follow.102 older assertion files/166 actual build/save identity lines migrate and
compile ONLY. TechnologyTests codec instrumentation/layout migrates directly, NOT runtime rerun.
Two pre-existing `check.Equal` save assertions in AutomationTests/TransformerTests still133; they
compile only and remain explicitly unverified. Migrate on the next source slice, not a post-green edit.

## Actual ownership

Required `IKnowledgeProvider.Retention(subject)` returns explicit FirstLearned/LatestObservation;
zero/unknown enum throws, never guesses. SAME CompanyKnowledgeService sorted records/candidate
validation/Sync owns immutable payload, LearnedTick and ObservedTick. First observation initializes
both; refresh replaces SAME subject, preserves first time, timestamps actual recording through SAME
World/GameClock. Later same-value observation establishes new observation time. Permanent identical
record stays no-op; changed permanent payload refuses. Reads do not consult inventory or age state.

Technology and fixed authored facts are intrinsically permanent. Existing public StockKnowledgeProvider/
StockKnowledgeEffectDef resolve actual JSON StockKnowledgeSubjectDef (resource, first/latest retention),
not hard-coded subject semantics or another provider/stock table. Native authored positive machine
work/effect ancestry and existing WorkBoard/WorkSystem still own travel, inputs and completion.

Build2.230.0/save134/knowledge codec2/view2; stock semantic provider schema2; other owner/view layouts
unchanged. Direct migration, no legacy reader/compatibility mode. One unchanged GameClock SHA256
`339a39f516bf392c7aa6549ace1aa05af97aba49b719fe5e7507323376056e31`, sole construction World.cs:96.

## Exact bounded player/codec proof

Actual master lists knowledge-refresh-work.json AND technology.json for selected sim.technology;
shared knowledge.json/emergency-preemption content reused. Public extension compiles against public
contracts only. No host/test calculates gameplay progression or writes live stock/learned records.

Normal policy/order commands dispatch actual machine/timed work: stock20→12→4. First partial work
publishes no observation. Initial latest/permanent readings capture12 and native fixed fact earns through
the same DAG. Second partial activity already consumes inputs (actual stock4), but remembered latest
reading/time remain12/old; preview is pure. Completion publishes4, preserves original learning and
leaves held old payload/permanent reading/native first time unchanged. Partial first/second work saves
and twin continuation hashes agree. Cancelled timed repeat publishes no refresh; a completed ordinary
same-value repeat publishes a later observation time with actual stock unchanged. Exactly three records
remain, no duplicate observation history. Private/invalid viewer/explicit observer/isolated session/
immutable payload/saved final chronology checks pass.

Engine codec fault instrumentation (not a player grant) rejects negative/future/reversed timestamps,
refreshed permanent records, schema/data/duplicate/truncated payloads, preserving entire prior ledger.
Invalid/missing/empty/case-mismatched retention and missing subject/resource refs fail authoring.
This case does NOT runtime-prove every arbitrary provider's purity or invalid-enum implementation.

## Remaining scope and lifecycle review

Dynamic spatial sensors/exploration, actual remembered map-sample publication/refresh/sharing/expiry,
arbitrary provider/global mutation/privacy/CPU/fault/corruption/overflow/replay/lockstep/soak/allocation/
full acceptance and industrial rival policies remain open. Remembered stock snapshots do not imply
live sensor truth or complete fog-of-war. Historical D5/D6 runtime cases NOT rerun on this identity.

Lifecycle skill review preserved one knowledge/clock/work authority and explicit JSON/public contracts;
focused-only user/repo rule overrides skill full-regression suggestion. No OilFieldDays/host/demo/
Draw/platform/Tycoon/business gameplay edits. Full goal remains active; publish ONLY after full finish.

## Source-focused inventory

Final scoped hashes/local links and read-only diff check are recorded in
[the manifest](P11_OBSERVATION_REFRESH_SOURCE_MANIFEST.json). Historical reports/manifests unchanged.
This inventory is NOT a whole-repository audit or broad runtime acceptance.

364 unique inherited/scoped paths,63 Markdown documents,1,422 local links. Final read-only recheck:
0 broken links/0 hash mismatches; git diff --check exit0 (existing newline normalization warnings).
Sole unchanged clock/construction retained. No broad runtime gate or Git mutation used for inventory.
