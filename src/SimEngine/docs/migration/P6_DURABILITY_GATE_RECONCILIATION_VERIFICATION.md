# P6-I8 original durability gate reconciliation — focused evidence

Date2026-10-15. Engine2.605.0/save159/replay1 unchanged. This stage's five exact
final cases417/0, combined with unchanged I7 parallel/control evidence470/0, prove
the named original P6.acceptance.009. Only that original checkbox changes.
Neither the full framework/Oil regression nor the full industrial goal is achieved.

## Original requirement, not a reduced replacement

Checked sentence retained verbatim:

“Save/load/continue, replay, lockstep and interleaved-session isolation gates pass.”

The preceding original save/replay section explicitly names self-description,
ordered copied commands, results/checkpoints, corrupt/mismatched/unsupported data,
travel, partial delivery, parallel activity, reservation contention, paused control,
incident response and pending next-step effects. Evidence was mapped to those
actual requirements, not just successful case names or aggregate pass counts.

## Coverage matrix

| Named requirement | Inspected implementation/proof and authoritative runtime evidence |
|---|---|
| Actual saved module/content/time/build/command identity | CanonicalReplayTests.Protocol describes embedded save, immutable module list, initial/final hashes/time/counts and wrong-factory refusal; StateCodecTests.CurrentNativeProtocol checks independent authored build159/codec expectations; current75/92 |
| Ordered copied admissions/results and every-step checkpoints | Protocol exercises saved initial/pending final inputs, mutated source command after submit, controllers/Updated callbacks and concurrent admission; changed result/hash/checkpoint ordering/duplicate inputs with valid resealed checksum refuse; current75 |
| Corruption, unsupported schema, wrong identity | Canonical protocol checksum/truncation/trailing data/unsupported replay schema/wrong factory/limits refuse with no unverified Game; native protocol independently reseals retired save format and observes explicit rejection; current75/92 |
| Declaration identity before payload restore | Drift changes service/system/component schemas, identities, order/schedule and missing provider; all refuse before initialization/payload callbacks; current30 |
| Extension callback failure boundary | ReplayFailureBoundaryTests.Faults exercises13 actual compose/restore/load/hash/command construct/decode/copy/update/bad-payload modes, typed failure coordinates/provenance and source/other-session isolation; current141 |
| Save during physical travel | CanonicalReplayTests.Native checkpoints real loaded cargo/movement, pause/multi-step work and completed native construction; Points checkpoints mod travel/earned private learning; current75 |
| Save during actual partial delivery | Strengthened NetworkedModDurabilityTests saves received amount>0/<8 plus residual cargo in unload toil3, reattaches two peers and finishes without a new haul; current79 |
| Parallel activity and scarce reservations | I7 ParallelAcquisitionTests restores distinct workers, feasible all/any branches, consumed material, actual target/worker tools and exact reservation structs; blocked contender has no acquired prefix;337/0 on identical current core/content |
| Paused control and pending next-step action | I7 PlanetXControlResponseTests restores pending false/true decisions, original due, unapplied target and native pause; LastAppliedAt equals due and doesn't repeat;133/0 on identical current core/content. Current ReplayFailureBoundaryTests independently verifies recorded save-origin pending action continuation |
| Incident response active-stage saves | Current replay-failure case records machine/material/tool work and continuation through episode completion; networked mod saves blocked episode, reattaches and checkpoints productive repair;141/79 |
| Same native codecs, final hash and results | All physical cases restore through GameFactory.Load; CanonicalReplayTests.Verify requires actual/expected/final hashes and step; current resealed result mismatch fails;75. Networked mod requires four final peers agree and verifies each replay segment;79 |
| Shuffled lockstep ordering | Canonical replay has two actual local human seats, reversed packets and peer-identical recorder bytes;75. Networked mod has delayed/reversed multi-packet transport, real stalls, checksum comparisons and preserved sequences7/8;79 |
| Genuinely advancing session isolation | Networked mod advances restored peer pair while originals network-pause at fixed hash/time, resumes and compares common committed epochs, then independently finishes project/incident;79. Other failure cases prove bad verification can't poison source or another live game |
| Presentation excluded from authority | Replay recorder remains an observation owner, snapshots stay detached, saves restore canonical native state rather than transport/render caches; recording limits don't fault/cap game progression. Current cases retain exact hash/results across reload/replay despite different publication/transport pacing |

I7 is linked rather than silently rerun: [all I7 attempts](P6_CONTENDED_CONTROL_SAVES_VERIFICATION.md).
Its two source files/content were unchanged during I8. Runtime engine C# files
are unchanged across I5–I8: freshly observed441-file combined SHA256 is
`aaa21d3297f20d16a0386876eeaf76df498490b18bddf8517018c7ba5bb0ad82`, matching
the pre/post I5 baseline. This is scoped evidence continuity, not a fresh audit of
all historical checks or a claim that only version strings establish equivalence.

## Partial-delivery implementation

No new game/fixture or delivery implementation. Existing NetworkedModDurabilityTests
now waits for original endpoint stock strictly between0 and8 with live residual
loaded cargo in unload toil3. Both native peers save at that committed epoch.
Two new SimGame instances load those saves and reattach native NetSession.

The restored games still satisfy the partial-delivery facts; they receive no
replacement hauling command. Original and saved branches complete unload/home
through existing work/handling/transaction/motion owners, align actual game-clock
epochs and compare final authoritative hashes. Their peer recording streams are
byte-identical and independently replay from the partial save with0 new commands.
Existing full initial9-command, mid-load4-command and blocked-incident2-command
replays, active isolation, actual build/recovery and native sequence assertions
remain. Driver still only submits inputs, paces Update and forwards native packets.

## All attempts and current exact results

The four existing identity/refusal cases were inspected and executed against the
current built artifact before the isolated network-test addition. Their source,
content and runtime core did not change afterward:

| Exact case | Passed/failed | Case ms | PID / exit / bounded elapsed ms |
|---|---:|---:|---|
| Canonical replay: physical native and mod checkpoints with shuffled lockstep inputs |75/0|12336|5204 /0/12479|
| Replay failure boundary: extension callbacks and active process control recovery |141/0|8280|6128 /0/8426|
| Current native protocol: authored expectations and physical selected-role checkpoints |92/0|2440|26332 /0/2577|
| State codecs: declaration drift refuses before payload restoration |30/0|72|19452 /0/207|

Canonical session12296 was polled to terminal, not restarted on yield.
No failed baseline attempt. After the partial-delivery addition:

`dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`

Clean4.78s, terminal exit0, zero warnings/errors before runtime.

| Changed exact case | Passed/failed | Case ms | PID / exit / bounded elapsed ms |
|---|---:|---:|---|
| Networked colony mod: shuffled peers reattach partial saves and preserve active-session replay |79/0|2670|25092 /0/2809|

Five final literals417/0; no failure, abort, timeout or truncation in I8. All use
the built test DLL with `--bounded-exact`,60000ms timeout and5000ms termination
timeout; output limits3000/6500 chars. No executable edits after final. No full,
profile, broad family or other historical case ran. The adjacent I7's470 are
separate evidenced runs, not added to a fictitious same-run full regression count.

## Remaining original scope

This closes P6.acceptance.009 only, not P6's other requirements. Broad standalone/
Oil profiles, exact whole-source ownership/thin-client audits, remaining domain
parity and P1–P12 implementation items, all-optional-modules simultaneously active,
general fault/fuzz/soak/performance/network stress matrices and full industrial
objective/rival/emergency acceptance remain separate open requirements.

Save reattachment remains at admitted/answered committed checkpoints; external
packets and unadmitted proposals are not claimed to persist in simulation saves.
No arbitrary-provider security guarantee or new compatibility loader is claimed.
No engine implementation, content, native schema, Oil/Tycoon rule, platform/Draw,
host/demo gameplay, staging, commit or push change in this increment.

Original bookkeeping delta is221/197 recorded checked/24tracked open, from one
additional original checkbox. This is not a fresh all197 completion audit. Full
goal remains active and unachieved; full regression is still not authorized/run.
