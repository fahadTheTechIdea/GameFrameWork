# Scenario engine detailed design

## 1 Authoritative state and invariants

ScenarioEngine owns one SessionState. LostSoldierLayer owns its domain snapshot and exports read-only typed facts. Godot observes the native world and submits those observations through WorldFrame. Nodes never directly complete objectives, spend inventory or resolve an ending.

| State | Owner | Persisted |
| --- | --- | --- |
| Session/content/layer identity, lifecycle, tick | ScenarioEngine | Yes |
| Objective status and once-only transition keys | ScenarioEngine | Yes |
| Selected ending and campaign outcome ID | ScenarioEngine | Yes |
| Layer state, action progress and random streams | Selected layer | Yes |
| Current poses/perception facts | Layer after WorldFrame admission | Yes, accepted observations |
| Native paths, avoidance cache and animation blend | Godot | Rebuilt on load |
| Diagnostic timestamp and UI selection | Host services/UI | Separate settings/logs |

Invariants: stocks cannot be negative; entity IDs do not change during a session; a terminal objective cannot revert without restoring a checkpoint; an ending commits at most once; published events refer only to committed state; one writer advances the session.

## 2 Public C# boundary

The compiled source is the authoritative API shape:

- [Simulation contracts](../../src/ScenarioEngine.Contracts/Simulation.cs): IGame, IGameLayer, ILayerRuntime, IScenarioSimulation, commands, observations, state and tick results.
- [Definition contracts](../../src/ScenarioEngine.Contracts/Definitions.cs): identities, typed conditions, facts, zones, objectives, endings, registry/catalog and session factory.
- [Infrastructure contracts](../../src/ScenarioEngine.Contracts/Infrastructure.cs): content source, schema validator, save codec/store/service, diagnostics and notification sink.
- [Replay contracts](../../src/ScenarioEngine.Contracts/Replay.cs): portable tape, entry/result records, recorder, service and codec boundaries.
- [Shared definition rules](../../src/ScenarioEngine.Contracts/ScenarioDefinitionRules.cs): one graph/condition validation policy consumed by content loading and runtime creation.
- [Wire validation](../../src/ScenarioEngine.Contracts/WireJsonRules.cs): common UTF-8, nesting, duplicate-property and observation-batch limits.
- [GameBase](../../src/ScenarioEngine.Core/GameBase.cs): shared Init and Update lifecycle for every game subclass.
- [ScenarioEngine](../../src/ScenarioEngine.Core/ScenarioEngine.cs): transactional runtime and immutable snapshot commits.

Init receives a validated scenario, host-issued controllers, seed and initial WorldFrame. Update receives one TickInput. The layer runtime receives its prior immutable snapshot and returns a candidate state, command outcomes, events and world requests. The engine owns the commit; the layer cannot mutate a second hidden authoritative state.

The engine session factory resolves the exact layer version through IGameLayerRegistry rather than requiring hosts to construct concrete layer classes during each tick. Restore checks identity, graph, layer state, phase enumeration, controller membership, queue bounds, command payload schemas, admission fingerprints and receipt/sequence consistency. The standalone save API supports format 1 only; no previous-format migration is advertised.

The concrete first slice uses immutable collections, cloned JsonElement boundaries, registered payload schemas and an injectable IContractValidator. Domain state validation remains part of ILayerRuntime's behavioral contract. The Counter and Signal checks exercise substitution.

The current step context always carries 50 ms, the objective graph is static, and an ending closes the entire session. The zone-request seam is not yet an acknowledged persistent-world protocol. [Chapter 09](09_GAME_TYPE_EXTENSIBILITY.md) specifies proposed enhancements for turn/action/calendar time, scoped activity completion and persistent regions. Those capabilities are not implied by the generic interface names or the current test count.

## 3 Session state machine

| Current | Trigger | Next | Rule |
| --- | --- | --- | --- |
| Ready | Start after world ready | Running | Tick remains 0 |
| Running | Pause | Paused | Complete current tick first |
| Paused | Resume | Running | No wall-time catch-up |
| Running | Committed zone request | Transitioning | Freeze orders and world until replacement ready |
| Transitioning | Valid next-generation WorldFrame | Running | First new tick validates destinations and resumes |
| Running | Closing condition and matching ending | Ended | Resolve once and disable domain commands |
| Any live phase | Unexpected rule exception | Faulted | Last committed snapshot retained |
| Any | Dispose | Disposed | Disconnect host and reject future operations |

Saving is allowed at safe committed boundaries described in chapter 07. Ended may be saved for outcome continuity. Restoring creates a new session instance from a validated snapshot rather than mutating a faulted instance.

## 4 Command admission

Only host-issued controller IDs can submit commands. The host assigns a monotonic ingress sequence; native callbacks may not choose it. The engine:

1. Rejects unsupported phase, wrong generation, unknown operation, invalid payload, unauthorized actor, duplicate ID with different payload or excess queue size.
2. Accepts target ticks from currentTick + 1 through currentTick + 8. A late command returns COMMAND_LATE; no hidden retargeting.
3. Keeps at most 128 queued commands, 32 for one tick. Excess returns COMMAND_QUEUE_FULL.
4. Returns an existing receipt for an exact duplicate CommandId and payload; it never spends twice.
5. Retains receipts in the session snapshot. At 100,000 accepted IDs, end admission with an explicit session-limit diagnostic; no silent dedup eviction.
6. While paused, permits queueing for the next tick. CancelQueued checks controller ownership, marks an unexecuted receipt Cancelled and removes that pending command atomically; repeating it returns the existing receipt. It cannot cancel a layer action already started. No time advances.

Layer validation runs again at execution because resources, targets or wounds may have changed since preview. A UI estimate is not a reservation.

### Implemented wire admission policy

The same WireJsonRules policy validates live command payloads and restored queued payloads, before fingerprinting or invoking the JSON Schema provider. Payloads must be available JSON, at most 64 KiB in UTF-8 and depth 32, with no duplicate property names, including nested objects. Commands require an object root and a nonempty operation identifier of at most 128 characters. The layer's registered schema applies after these generic checks.

WorldFrameRules checks initial and tick frames before cloning payloads or invoking the layer. Each observation has a nonempty kind of at most 128 characters and the same JSON payload budget. A frame allows at most 256 observations and a 1 MiB aggregate budget, computed from UTF-8 payload/kind bytes plus a 256-byte allowance per observation. Sequence, tick, generation and registered observation schema are then checked by the runtime. A default observation collection, null observation, disposed JSON owner or invalid payload is refused with a stable code.

Invalid tick input leaves the previous snapshot and phase intact; the host can correct and resubmit the same next tick. Invalid initial frame shape raises ScenarioContractException before constructing the plugin runtime. These admission checks do not change the fault policy for unexpected exceptions inside domain execution.

## 5 Tick transaction

For tick T = committedTick + 1:

1. Verify monotonic T, WorldFrame.ForTick, generation and observation sequence continuity. Reject missing/duplicate/invalid frames before mutating state.
2. Fork/copy a candidate state from the previous commit.
3. Apply observations ordered by native observation sequence: pose, arrival, obstruction, line-of-sight and sound/evidence. Bound and type-check values.
4. Resolve prior timed-action completions in ascending action ID. An incapacitated actor cannot complete a treatment/repair later in the same tick.
5. Resolve player commands in ingress order, rechecking legal actor, state, cost and target.
6. Resolve an AI decision only for an actor whose decision pulse or relevant evidence-change event is present. Sort actors by stable ID.
7. Apply domain periodic costs and expire knowledge/timers using tick indices.
8. Project typed layer facts; evaluate objectives in stable topological order. Each objective transitions at most once this tick.
9. Apply objective consequences through registered layer handlers, project facts again, then evaluate closing and ending conditions.
10. Validate final invariants; commit snapshot, event sequence, journal input and rule-state hash as one in-memory transaction.
11. Return the immutable committed batch. Host applies world requests and notifies UI only after this point.

An exception at steps 2–10 discards candidate state/events and faults the session. Expected action rejection is an outcome, not an exception. Native movement between ticks is outside this transaction and is captured as input.

## 6 Action transaction and cancellation

Each accepted domain action has actionId, actorId, targetId, startTick, finishTick, resource reservation, interrupt policy and status. Reservation is owned by the layer. Free stock = stored stock minus reservations. Starting a repair reserves its tool; completion consumes it. Cancellation releases it unless the definition explicitly consumes a start cost. A rejected action changes no stock.

A cancel command in a tick where completion is already due arrives after timed completions by section 5; the UI must say already completed rather than refund. Incapacitation detected in the observation phase cancels the action first. This ordering prevents double-use of a medical kit.

## 7 Typed condition language

Generic conditions operate on registered facts; there is no scripting, reflection or expression eval.

~~~json
{
  "all": [
    {"fact": "lost_soldier.missionDelivered", "op": "eq", "value": true},
    {"fact": "lost_soldier.heat", "op": "lte", "value": 30}
  ]
}
~~~

Allowed nodes: all, any, not, or one comparison with fact/op/value. Comparison ops: eq, ne, lt, lte, gt, gte. Equality compares exact types. Ordering accepts integers only. Depth limit 8; total nodes 64; all/any contain 1–16 child nodes. No implicit string/number conversion. Missing fact, unregistered key or value outside declared range fails validation; a runtime mismatch faults with CONDITION_CONTRACT.

The engine supplies core.tick and core.objective.<id>.status. The layer supplies namespaced facts such as lost_soldier.heat. Fact projections are derived from authoritative state; the engine does not keep a second mutable inventory.

## 8 Objectives and effects

Objective statuses: Locked, Active, Completed, Failed, Abandoned.

Locked becomes Active when dependencies are satisfied and activation condition is true. Active evaluates failure first, then completion; simultaneous death/deadline and pickup therefore follows an authored failure condition. Completed/Failed/Abandoned are terminal. A fail-forward dependency may list a Failed/Abandoned objective explicitly as an accepted terminal state; failure does not automatically satisfy prerequisites.

Effects contain a registered type, idempotency key and typed arguments. Generic graph transitions create an effect key from objective ID and terminal transition. The selected layer applies costs/facts atomically and records the key. Effects cannot name a CLR class or mutate arbitrary state paths.

## 9 Ending resolution

Scenario closingCondition determines when endings can resolve. While it is false, an ending candidate never stops play. When true:

1. Evaluate conditions after objective effects and final fact projection.
2. Sort matching endings by descending integer priority, then stable ID for diagnostic order.
3. Equal-priority overlapping matches are rejected at validation when known; at runtime a top-priority tie faults with ENDING_AMBIGUOUS.
4. No matching ending produces ENDING_UNCOVERED and a recoverable diagnostic snapshot, never a fabricated success.
5. Commit one OutcomeId from session/scenario/ending identity. Repeated processing cannot apply the campaign delta twice.

The author must include a justified failure ending that covers terminal failure states; an unconditional success fallback is forbidden.

## 10 Randomness and hashes

Use a documented algorithm such as PCG32 with algorithm ID in the replay/save. Streams are derived using a stable SHA-256 encoding of session seed, domain stream name and entity ID. Never use .NET string.GetHashCode, wall time or dictionary iteration order. Store stream state/draw count and publish the algorithm test vectors before runtime acceptance.

Rule-state hash uses a specified canonical serializer: UTF-8, ordinal map-key ordering, integer numbers, explicit nulls, invariant enum names, no diagnostic UTC or rendering state. Hash algorithm is SHA-256. Content digest is over normalized manifest/file digests, not ZIP byte layout.

## 11 Determinism and simulator limits

Three distinct tests are required:

- Rule replay: identical content, seed, command trace AND recorded WorldFrames must yield identical state hashes and ending. This is the supported deterministic contract.
- Synthetic scenario simulation: authored movement/perception responses explore domain paths. Results demonstrate rule behavior in that test environment only.
- Native world acceptance: Godot headless or interactive execution uses actual navigation, collision and sensors. It verifies that routes are physically playable.

Do not claim a standalone simulator with a simple straight-line adapter proves native path reachability. Godot physics/navigation are not treated as a cross-platform deterministic service. A replay missing its observation journal returns REPLAY_WORLD_INPUT_MISSING.

## 12 Implemented portable replay

[ReplayService](../../src/ScenarioEngine.Replay/ReplayService.cs) records commands, cancellations, lifecycle calls and TickInput/WorldFrame data from a stable baseline. It stores a state hash and output hash after each operation. Verification compares result codes, hashes and operation position, returning the first divergent index.

State hashing remains integer-only and canonically orders maps/sets. Replay input/output integrity additionally supports rejected fractional JSON inputs so refusals can be reproduced without relaxing rule-state integer requirements. Format 1 declares canonical-session-sha256-v1 and pcg32-xsh-rr-v1. Unknown formats and altered payload digests are refused.

The recorder is bounded to 10,000 entries and 16 MiB, including capacity reserved for Dispose. It captures serializable wire inputs; unsupported in-process objects are rejected before execution. An exported JSON tape is implemented; continuous crash-safe journal segments remain planned.

## 13 Errors

| Code | Result | Recovery |
| --- | --- | --- |
| COMMAND_INVALID / COMMAND_DENIED | Rejected command, no effects | Correct payload/action |
| COMMAND_LATE / COMMAND_QUEUE_FULL | Admission refused | UI offers new order |
| WORLD_FRAME_GAP / WORLD_GENERATION | Step uncommitted | Host pauses; diagnose/rebuild world |
| WORLD_FRAME_INVALID / WORLD_FRAME_LIMIT / WORLD_OBSERVATION_INVALID | Input refused, phase and snapshot retained | Repair or reduce host input and retry the same tick |
| CONDITION_CONTRACT | Session fault, previous commit kept | Fix pack/code and restore |
| ENDING_AMBIGUOUS / ENDING_UNCOVERED | Session fault with diagnostic snapshot | Correct conditions and rerun |
| ENGINE_BUSY | Concurrent/reentrant step refused | Repair host ownership |
| INVARIANT_VIOLATION | Tick discarded | Preserve cause and prior commit |

No catch returns a fake StepResult.Committed = true. Exception and audit handling follow chapter 07.
