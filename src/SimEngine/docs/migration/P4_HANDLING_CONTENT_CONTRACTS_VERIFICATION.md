# P4-G16 — Physical cargo-handling content contract evidence

Date:2026-10-09. Scoped implementation proof, not full P1–P12 closure. No publication.

## Implementation and ownership

IResourceHandlingProfile now extends IContentDefinition. TransporterDef, BoundCargoDef and
ReserveTransporterDef resolve actual profiles through that contract, never the native base index.
ResourceHandlingDeclaration contains the original explicit resource-row reader, native basis validation,
reference resolver and exact quantity-ratio duration calculation. Native AuthoredResourceHandlingDef
and independent Def implementation delegate it. Existing SlotHandlingDef directly implements the
contract; its distinct authored slot duration calculation is retained, not copied into coordinators.
Old identity assertions now check actual contract alias and the ordinary Def reread refusal.

Original HaulHandling.Check accepts the actual World and always uses its sole Time. Lookup through
ITransporter.Handling and CheckHandling callback are guarded through EntityObservation.ReadProvider
with the actual carrier. Content identity must alias this session's resolved immutable definition.
Invalid/default callback facts, throws and clock mutation latch a real fault, not an admission/default.
Original Earned search uses that same guarded Check for every cumulative native prefix.
Logistics preview/physical execution and WorkBoard restore/view calculations pass their existing world.

Job.HandlingProgress/Loaded/phase, native stock/promises/receipts, movement/docks/work eligibility and
entity.Update remain the sole original authorities. No copied rate, saved ETA, host timing, shadow cargo,
parallel progress/inventory/resource catalog/clock, instant fallback, new state codec or compatibility reader.

The master-listed handling-contracts.json supplies direct linear and stepped-slot profiles.
HandlingContractModule is content composition/diagnostics only. FaultHandlingDef deliberately misbehaves
as an engine-side provider: throws, returns default facts, or casts the already-public clock owner to
pause it. No clock-control API was added to the read-only interface or framework for this adversary.
Normal player workflow is thin public views/commands/clock/save/replay; it writes no raw world state.

## Observed builds and ONE exact bounded case

`dotnet build Beep.SimEngine.slnx --no-restore` in canonical Beep.SimEngine:

- First7.48s: zero warnings/one new-proof compile error; IGameClock correctly has no Pause method.
  Proof corrected to use the existing public GameClock owner for deliberate impurity; no core API change.
- Clean8.11s: zero warnings/errors.
- Clean6.25s: zero warnings/errors after source review migrated two existing public transporter
  consumers and older native identity assertions in place.

Only `Handling content contracts: independent JSON rates, physical prefixes and guarded callbacks`
ran; bounded timeout30000ms/termination2000ms/output24000; all physical stages at most400 updates.

- FIRST275 passed/0 failed/0 aborted in803ms; childPID16744 exit0/observed1021ms.
- FINAL same case275 passed/0 failed/0 aborted in612ms; childPID10256 exit0/observed759ms.
  No stdout/stderr truncation or observer error. No stale binary, test-owned acquisition or raw progress.

The same new case was rerun because actual public content consumers changed after the first green.
No historical, family, full, Oil, Tycoon or platform runtime suite ran.

## Scope proven

Actual owned direct profile/source aliases and Initialize once. Normal worker-policy and haul commands
preview purely and allocate the original job. Worker physically approaches sourcepoint20 with empty
cargo; partial native quantity is received only after earned service.
Linear basis2 at3-minute loading/2-minute unloading produces exact12/8-step requirements for8 units.
Existing independent slot profile produces10/6-step requirements for two4-unit slots.
Both use original cumulative prefix evaluation and actual cargo/source/destination stock.

Views report current source/destination/resource/held/handled/remaining/progress/full duration privately;
retained loading facts remain detached through delivery. Loading pause under normal disabled worker
policy retains actual prefix/progress/position. Sole-clock pause freezes all stock/jobs/time/hash.
Saved partial loading, paused service, loaded trip, partial unloading and completed delivery continue
identically in twins. Loaded cargo is not instant destination stock; unloading requires real arrival
atpoint10 and positive service. Every observed normal update conserves exactly20 original units.
Final source12/destination8/cargo0; real travel adds elapsed time beyond handling totals.
Normal policy/haul/clock replay and fresh interleaved sessions agree.
An explicitly selected30-second clock makes the same8-unit linear load require24 steps.

Fifteen early authoring/binding refusals before Initialize: missing/empty rows, zero/fractional basis,
duplicate resource rows, missing resource, missing load duration, zero/negative duration, invalid
denominator/unit, unused malformed resource, missing/null/wrong-kind selected handler.
Three adversarial actual duration callbacks (clock pause, throw, default) fail preview, latch the
original session fault and cannot produce a resumable save. No compensating resource mutation is added.

## Protocol and freeze

Build beep-simengine-2.348.0/save154/replay1/work-view95/logistics-view40.
All state/view schemas unchanged. Sole GameClock unchanged.
Final executable freeze:2026-10-09 05:43:39UTC,15 source/content/project/protocol inputs with SHA256.
No post-final executable edits. Older proof/native branches compile only; their historical runtime
results are explicitly not revalidated on this build.

## Read-only completion reconciliation

All221 original acceptance IDs/phase/document/requirement/checkbox values retained;152 source line
offsets updated.149 historical checked/72 unchecked is not a completion percentage.
Twelve current status cells updated; all18 master rows remain valid five-column records.
Metadata:648 source SHA anchors/13 quickstart anchors with zero mismatches;2126 file paths,
711 Markdown documents/5413 local links with zero broken links. Diff check exit0;34 historical CRLF
warnings, no errors. New thin-player boundary scan reports no raw World/resource/progress mutation.
All15 frozen executable inputs rechecked unchanged.
GameClock SHA35d3fba57cf7fe665ee319a5e2f35e00f0704eccdb75c00a424882584eea43c1 unchanged.
Older runtime entries explicitly not revalidated on2.348.0; only final275 case is current runtime evidence.
No staged files. These are read-only metadata checks, not runtime suites.

## Remaining scope

Arbitrary provider/foreign identity/fault breadth, resource/unit/quantum permutations, endpoint removal,
cancel/recovery/stranding, shared physical docks/fleet contention, complete distributed accounting,
capacity-policy concrete boundaries, lockstep/soak/allocation/performance and full P4/P7/P9/P1–P12
gates remain OPEN. This is not a claim of global purity or a CLR security/CPU sandbox.
No Oil changes, business expansion, mandatory money/budget, Draw/adapters/Unity/Godot, demo/harness
gameplay, staging/commit/push. Full goal remains active.

[Stage](../../plans/enhancements/P4_HANDLING_CONTENT_CONTRACTS_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[completion inventory](../../plans/enhancements/COMPLETION_AUDIT.json).
