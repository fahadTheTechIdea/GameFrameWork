# P2-F3 — Entity update boundaries verification

2026-10-06; [stage](../../plans/enhancements/P2_ENTITY_UPDATE_BOUNDARIES_DESIGN.md),
[sole tracker](../../plans/enhancements/MASTER_TRACKER.md).

## Authority and identity

Build `beep-simengine-2.168.0`, save95. Runtime gameplay system/service/component/view layouts
remain unchanged. This directly changes internal entity context construction and observation
ownership; there is no compatibility overload, old observation facade or second state authority.

Exactly one GameClock remains authoritative per session. Its source is unchanged:
SHA256 `339A39F516BF392C7AA6549ACE1AA05AF97ABA49B719FE5E7507323376056E31`.
Update indices and entity state timestamps derive from that clock. Transient issued/completed
coordinates are update permissions, never independently advancing timers or saved gameplay time.

World issues contexts bound to the actual entity only during its entity-update phase. Default,
foreign, stale, cross-entity, recursive and repeated same-update calls refuse. Recursive kernel
advancement latches the existing first-fault owner. State callbacks stop immediately after any
nested terminal failure, including one swallowed by a provider. Later sources, later entities,
completion callbacks and clock advancement do not continue from a partial failed update.

EntityObservation is the sole migrated scoped guard. It observes actual entity identity, version,
owner, liveness, state/state-since, next identity and clock identity/update/pause/speed. Entity state
reads use stack-held capture/verify without allocating a per-source delegate or hashing the entire
world. Existing standard/view callers use the same owner. Invalid content-state references fail
instead of substituting another state. First-fault diagnostics preserve nested initialization errors.

## Exact final gate

```powershell
dotnet build Beep.SimEngine.slnx
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Entity boundaries: issued updates, scoped observations and first-fault cutoff"
dotnet run --project tests/Beep.SimEngine.Tests --no-build -- "Entity boundaries: authored states, physical updates and saved continuation"
```

- Build: zero warnings/errors; MSBuild elapsed 3.73 seconds.
- Issued updates/scoped observations/first-fault cutoff: 366 passed, zero failed, 110ms.
- Authored states/physical updates/saved continuation: 149 passed, zero failed, 499ms.
- Total: 515 assertions in two exact suites; zero final failures or aborted suites.
- Only these two names ran, including repair reruns. No family/full regression was run.
- 30 standard/view observation helper-owner/caller files directly migrated.
- 82 older metadata files and three older context/diagnostic expectations migrate and compile only;
  those suites were not rerun. These counts are file sets, not independent scenarios.

## Behavioral proof

Public-only authored probes cover seven fault modes, nine scoped mutation injections, foreign
state references, stale/default/foreign-session contexts and repeated invocation of an entity
already completed in the active update. Failures preserve the original diagnostic boundary,
refuse save/hash of partial updates, clear transient permissions and cannot retry clock advancement.
Healthy snapshots retained before failure remain unchanged; fault reporting stays immutable.

Healthy providers select states by existing priority and stable identity tie-breaking. Each source
runs once per issued update. Unchanged states retain their clock-derived state-since coordinate.
Snapshot observation does not drive callbacks. Saved twins continue hash-identically through
interleaved updates, pause and isolated sessions.

Eight native/public incident, policy and worker compositions execute the existing physical response
DAG. They save during active work, resume deterministically, consume four real target inputs once
and finish inside bounded update loops with failure diagnostics. Completed checkpoints restore.
The existing private work-policy observation uses the same migrated guard without changing hashes.
Tests author content, submit player commands, drive Game.Update and inspect engine results; no
response scheduler or gameplay implementation was added outside the engine.

## Repairs and scope

Initial suite attempts aborted because the proof JSON was not declared as an embedded resource.
The project now explicitly embeds it; no missing-file fallback was added. Proof composition creates
a fresh instrumentation system per session. State observation uses scalar scope capture rather than
per-source closures. These repairs preceded the final build and two green suite runs.

The [source manifest](P2_ENTITY_UPDATE_BOUNDARIES_SOURCE_MANIFEST.json) records 126 current source,
fixture and project fingerprints, including the unchanged clock. It is scoped evidence, not a claim
that the dirty checkout is clean or that earlier suites passed on the current identity.

All 126 fingerprints matched after recording. Nine relative stage/evidence links resolved. The
scoped tracked diff whitespace check passed. Source searches found neither the removed observation
owner nor the former World-only context constructor. These are source/document checks, not extra
runtime suites.

Global/private provider mutation, other-entity mutation outside these scoped facts, full protocol,
replay/fuzz/soak/allocation matrices and broader P2/P3/framework acceptance remain open. Earlier
content lifecycle evidence is preceding evidence and was not rerun in this slice. No Oil/Tycoon,
host/demo, platform adapter, rendering or Draw implementation was changed.
