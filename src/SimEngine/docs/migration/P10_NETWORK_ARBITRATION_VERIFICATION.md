# P10-A2d — authored successful-service network arbitration verification

2026-10-06, framework only. Scoped current-source evidence, not full P10/P1-P12 completion.
No Oil/Tycoon/host/demo/platform/Draw changes. Lifecycle guidance kept explicit staged scope,
one existing owner per responsibility and directly affected gates, not historical green claims.

## Source revision and single ownership

- Build **beep-simengine-2.127.0**, save **70**, **LineService schema4**. Infrastructure view4 and
  other owner payloads unchanged. No previous save/content reader or compatibility path.
- NetworkDef requires explicit flow_policy and resolves open NetworkFlowPolicyDef/INetworkFlowPolicy.
  Infrastructure registers native network_round_robin_policies. Other registered content sections
  supply derived policies without modifying the coordinator. Missing/wrong refs fail.
- NetworkFlowCandidate is detached immutable metadata: entity/company/resource/direction, actual
  due allowance/combined quantum/eligible duration. No mutable World, entity, clock or stock handle.
  Rank is signed64-bit, lower first; stable actual entity IDs resolve equal custom ranks.
- Native round-robin sorts after last actual successful service. Tick-modulo consumer order and
  fixed producer order are removed. Existing NetComponent saves sparse resource/direction winner/
  timestamp in the same graph owner/Sync, never another queue/inventory. Idle/full/dry/zero allowance/
  failed acquisition never rotates history. Changed component identity resets it; unchanged
  membership retains the same component/history. Removed winners remain valid historical facts.
- All legs earn eligibility and all policies rank before transfers. Participating entity/endpoint
  metadata/version/liveness guards surround callbacks. Rechecked actual stock/capacity and verified
  receipts commit stock. Only successful transfer updates history. Existing elapsed bindings/reset
  semantics remain authoritative; no banked unused allowance or separate stock/credit ledger.
- Sync rejects uncarried resources, duplicate/out-of-order keys, future/zero timestamps and invalid/
  never-created identities. Historical timestamps cannot imply current readiness or live membership.

## Exactly one game clock

Current source inspection: Hosting/GameClock controls stepping/pause/speed. Kernel/World.Step alone
advances authoritative current simulation time; SimGame.Tick/views read it. Core/GameTime is immutable
unit conversion. Network elapsed eligibility is work progress; FlowTurn.Tick is a historical last-
success timestamp from that same clock. Neither independently advances world time. The physical suite
proves two paused Updates leave current time, service history and state hash unchanged. This stage
introduced no clock, update loop or converter. A tick means a fixed step of the one game clock.

## Final gates

`dotnet build Beep.SimEngine.slnx --no-restore`

Success, **0 warnings/0 errors**, MSBuild **2.88s**. Both gates used this completed build.

| Exact suite | Passed | Failed | Final reported time |
|---|---:|---:|---:|
| Network arbitration: authored contracts, pure ranking and durable history | 28 | 0 | 474ms |
| Network arbitration: competing physical legs and saved fair continuation | 50 | 0 | 505ms |

**78 checks, two suites, zero failures/aborts.** Repair reruns kept these names. No third/family/full
suite ran. Bounded Update counts and actual fault diagnostics avoid stalled test workflows.

Contracts prove explicit native/public-only independent policy, custom lower-rank winner and equal-
rank stable tie; subquantum/success timestamp/no feed-through; private pure snapshots; save/hash;
future/duplicate/uncarried/never-created history corruption; missing/wrong/unknown JSON refusal;
participating mutation faults before receiving transfers and faulted checkpoints refuse.

Physical tests create a real one-unit pool, two-minute rate, four ports and a four-minute supply/
service cycle: the clock-residue alias shape. Each of two consumers receives ten actual opportunities;
competing native/independent producers each supply ten. Only actual twenty transfers reach the
receiver; native stock is conserved. Saved interleaved twins preserve winners/hashes before/after
contention, gate waits/resume and capacity waits. Pause uses the sole game clock. Unrelated topology
retains identity/history; changed membership resets it. Existing pure receipt-refusal probes cannot
steal turns or block another compatible leg. Explicit real inventory cleanup and actual despawn leave
a legitimate historical winner which saves/loads and continues without substitution or lost stock.

Initial proof assumptions were repaired, not runtime weakened: content references resolve at actual
NewGame, not creation of an unresolved GameFactory; mutation probes must target participating legs;
normal command helpers execute Update, so resumed elapsed assertions include it. A proof-only wrong
EntityId namespace was corrected to the actual public signature.

60 earlier build assertion files and58 save assertions migrate/compile only. Four existing JSON
network fixtures explicitly author/select native policies. An older codec offset probe now skips
empty-network elapsed cursor payloads. Previous rate/construction/topology/resource/process and
other suites were not rerun; compilation is not current behavioral proof for those gates.

## Completion limits

Native successful-service round-robin applies to repeatedly due compatible candidates in the same
actual component/resource/direction with real supply/capacity. Different budgets/quanta/rates, shared
capacity across resources, cross-network conflicts, runtime player policy editing, utility allocation/
solvers, generic spatial graphs, broader provider/global rollback/replay/fault/stress/soak/allocations/
full acceptance remain open. Not unconditional starvation freedom, global provider determinism or
transactional rollback proof. No size cap, fabricated default, legacy or fallback.

## SHA-256 source fingerprints

| File | SHA-256 |
|---|---|
| src/Beep.SimEngine/Standard/NetworkFlow.cs | 2BFD9AF209901C6C650A32683071D59B947CC9E8F16597CA52005867F148ADBF |
| src/Beep.SimEngine/Standard/NetworkFlowPolicies.cs | 8EB550C54383321C7F5795CDF9592FDDF89A2E60E13BFB81257AD36FF5FFF282 |
| src/Beep.SimEngine/Standard/Networks.cs | 97ECE552F3F301D578AC3A832A4ECBEBD050CB7A0D53CC36D9FD9F3FD3F7BC8A |
| src/Beep.SimEngine/Kernel/SaveGame.cs | 81DF24D1A0AD19EA388B89F358DC1B2EF7A3FDF639D35B4C360F42C8F08F0B64 |
| tests/Beep.SimEngine.Tests/NetworkArbitrationTests.cs | D5597DA314DB97B77609D7F79B955CFE50ABAD1115806A3842F7158A31F5EAAB |
| tests/ExtensionProof/content/network-arbitration.json | 8BA64FB302DAF19FA3364B0B314F93CBD6A4FE2D32B485B29FFD5A8D31C12EF5 |
