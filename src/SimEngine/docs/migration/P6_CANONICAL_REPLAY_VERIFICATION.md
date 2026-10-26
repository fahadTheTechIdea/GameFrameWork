# P6-R1 — Canonical engine replay verification

Date2026-10-08; canonical Beep.SimEngine only. Build2.281.0/save140/replay schema1.
[Stage](../../plans/enhancements/P6_CANONICAL_REPLAY_DESIGN.md),
[tracker](../../plans/enhancements/MASTER_TRACKER.md),
[source inventory](P6_CANONICAL_REPLAY_SOURCE_MANIFEST.json).

## Outcome and ownership

The actual missing recorder/verifier from original P6 is implemented. SimGame.RecordReplay returns
IReplayRecorder, ReplayVerifier implements IReplayVerifier. These are engine utilities, not harness
gameplay. One recorder observes actual copied/stamped admissions through the existing inbox lock;
local player/controller inputs record once, engine-generated decisions regenerate, network proposals
record only when NetSession actually admits them. NetSession transport remains unchanged.

Recorder embeds the initial Save including pending input. Existing saved build/content/module/version/
time/space/game-mode/codec declarations remain authoritative. Actual command registry ID/tag/owner
identity accompanies this save; no registry or executable codec is copied. Command.Sync is the payload
codec. Each completed original game step records authoritative hash and exact LastTickResults.
Final current time/hash/pending input is required before successful publication.

Verifier validates checksum/schema/envelope/order before composing, restores through GameFactory.Load,
injects stamped input into the same inbox, and runs original RunTicks(1) per contiguous checkpoint.
All checkpoints, results and final pending state must match. Failure returns detached typed record/
step/hash diagnostics and no partially verified game. ReplayDescription is detached readonly metadata.
Recording is privileged saved-session observation, not a company-private player knowledge API.

During recording, TickOnce and copied input/checkpoint observation use the SAME inbox Gate, ensuring
threaded input cannot sneak between drain/completion and a recorded checkpoint. Admission step is
separate from original issue step. Updated observer input is recorded after the settled checkpoint.
No authoritative page/work/stock state, second dispatcher/queue/time counter or gameplay loop added.

Explicit ReplayLimits byte/record budgets apply only to artifacts. Observation exhaustion records
Failure and refuses Finish; gameplay does not acquire a world-size cap or fabricated completion.
Dispose/failed Finish release ownership. Initial identity must fit the explicit budget before attachment.
Local pacing/pause/frame alpha and network packet buffers are not saved authoritative game state;
replay follows actual completed steps. This is logical simulation replay, not packet/video capture.

## Every build/runtime attempt

1. `dotnet build Beep.SimEngine.slnx --no-restore`:ONE clean build13.99s,0 warnings/errors,exit0.
   Started live session92071 after10.0027451s; polled ONCE (terminal completion0.6677795s tool).
2. ONE exact NEW bounded case FIRST runtime:75 passed0 failures/aborts2163ms;4.0832772s tool,exit0.

Exact runtime command:
`dotnet run --project tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-build --
"Canonical replay: physical native and mod checkpoints with shuffled lockstep inputs"`

Source/proof/tests/JSON frozen immediately after FIRST green.147 identity paths (EngineInfo+146 older
fixtures) compile-only; no old/family/full/platform/stale runtime. No JSON content changes, save140/
all existing persistent fields and sole GameClock unchanged. One new registered case only.

Read-only preparation errors: guessed LockstepTests/PointGame/PlayerControllers paths absent; rg
located actual owners. PowerShell wildcard path passed to rg was invalid; used actual directory.
Combined large skill/reference outputs truncated; selected missing skill portions re-read to EOF.
These did not change state and were not build/runtime failures. ModuleVersion metadata and delegate
binding were aligned to actual contracts before the first build; no runtime repair or fallback.

## Exact case coverage

- Diagnostic command-codec fixture: starting with saved pending input, payload-copy protection,
  attached input script and Updated callback, final paused pending inputs, exact resumed continuation.
  These wire probes are not proof of physical construction; separate ordinary gameplay below is.
- Detached actual build/time/content/module identity and readonly module metadata; independent replay.
- Corruption/truncation and correct-checksum unsupported schema/trailing/duplicate/noncontiguous
  records refused. Deliberate state checkpoint mismatch reports actual record/step/hash. Exact result
  tampering fails replay.results even when state hash matches. Wrong game factory and artifact limits refuse.
- Recording exhaustion does not fault/cap game; incomplete Finish refuses. Dispose and failed Finish
  release recording ownership. Concurrent24 inputs observed exactly once and replayed to original hash.
- Native ordinary ConstructCommand starts a site; actual moving state/loaded physical hauling; cargo save
  twins/pause/speed2 frames; delivered structure consumes30 of50 steel. Every completed step reproduced.
- Public non-tile ordinary policy/activity: partial productive work leaves knowledge hidden; replay from
  initial state matches partial progress. A new recording starting at its saved partial state finishes
  original activity without resubmission, consuming8 supplies and revealing knowledge only to its company.
- Two ordinary lockstep peers, different local seats/delay2/checksum1, reversed outgoing packet delivery:
  both live games converge, buffered proposals record zero admissions, actual streams record exactly2
  commands. Their replay bytes are identical and both reconstruct original final state.

Physical-stage failure bounds:2000 Updates with order/blocker/time/fault snapshot; concurrent producer
wait10000ms. No timeout occurred. These are not process watchdog, provider-hang, CPU or allocation proof.

## Review and remaining gates

Scoped handoff:841 canonical paths/176 Markdown/2416 local links/0 broken/0 duplicate paths;
18 phase rows valid;two new HTML help references exist. Actual git diff --check exit0 with31
existing LF/CRLF warnings.179 prior-covered engine files:only SimGame/input observer hooks and
build identity changed, plus one new Replay utility file.215 authored JSON files unchanged.
Sole GameClock SHA unchanged. This is scoped source evidence, not whole-plan/regression proof.

SRP: recorder observes, format decodes detached artifacts, verifier validates through original engine.
OCP/DIP: commands/modules/behaviors use existing contracts, no concrete gameplay scheduler switch.
ISP: replay optional. LSP: public recorder/verifier contracts have explicit refusal semantics; no legacy
format reader, instant work fallback, renderer or host rules. Lifecycle skill organized stage/handoff.

Focused replay requirement implemented; broader provider exceptions/checkpoint-fault matrix, arbitrary
content/codec/unknown-owner mutations, nonspatial process/incident/automation/parallel-DAG acceptance,
full saved-network-queue transport reconstruction, watchdog/fuzz/soak/allocation and Oil integration
remain OPEN. No broad runtime is authorized by this focused stage. Full P1–P12 goal remains ACTIVE;
commit/push waits for whole plan AND goal completion and verification.
