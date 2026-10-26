# P7-D6 original creation allocator phase verification

## Outcome

Engine **2.619.0 / save159 / replay1**. Public signatures and original payload codecs unchanged. Original EntityStore owns identity allocation, candidate cleanup, registration and the new transient exclusion phase. No second allocator, scheduler, fault store, saved phase state or compatibility path.

Three selected literal finals: **612 +780 +88 =1480 passed, zero failures**. Original P7.017 and the full goal remain open. Bookkeeping remains221 original requirements /200 recorded checked /21 tracked open, not a fresh global audit.

442 core C# path/content SHA256: `800b2e6f72551035f403a1fd9d5c2b376f7bb72c4febd5e017f10327846cb9bc`. Earlier checkpoints are historical unless rerun here. No financial/host/platform/Draw work, full/profile/family runtime or Git publication.

## Reproduced failure

On2.618 a local candidate and a child created by its configure/OnSpawn/OnCreate callback both used the unchanged next ID. The child could register and return success, then the parent collided in the native dictionary after its callbacks had already run. The unfinished phase could also publish a save/hash containing only the old registered entities.

The new proof completed on the old engine with **348 passed /264 failed**. Valid post-registration initialization children already worked and remain protected against an overly broad nested-Spawn ban.

## Production design implemented

`EntityStore._creating` is a per-store transient protocol phase beside original `_replacing`, not simulation data. Create refuses an overlapping creation/irreversible replacement before child construction/callbacks/allocator mutation. It holds the phase from candidate construction through registration and releases it in finally on success, constructor failure, callback failure, cleanup failure or registration failure.

World.Spawn starts capability Initialize only after EntityStore.Create returns and releases the phase. An initializer can therefore create a real child with its own ID and original callbacks. Allocation was not moved earlier and failed candidates do not consume substitute IDs.

Original Replace also refuses while local creation is active, before candidate allocation. Original EntityStore.Sync refuses while local creation or irreversible replacement is unfinished. It never serializes the transient phase/candidate. No new overload or alternate creation path was added.

Original World first-fault and per-callback health checks handle a nested public Spawn/CommitCreation refusal whether a provider propagates or catches it. Existing original exception/cleanup behavior remains intact.

## Focused proof

Literal: `Creation allocation: local reentrancy and checkpoints refuse while initialized children retain distinct identities`.

It reuses the existing master-listed cleanup fixture, module composition, actual point provider and per-instance diagnostic creation/tail probes. The same JSON file now explicitly declares a nonspatial child. No gameplay decisions, spawn scheduler, separate identity book or static mutable hook was added to the tests.

- **48 negative allocation contexts:** parent Spawn/CommitCreation × child Spawn/CommitCreation × new/restored × configure/OnSpawn/OnCreate × caught/propagated refusal. The actual nested call refuses before child allocation; neither result escapes; one attempt, unchanged next ID, dead/unregistered parent candidate, no later callbacks/success facts, original creation fault and no healthy save/hash. Another restored session actually advances.
- **8 healthy initialized-child contexts:** same parent/child surfaces and new/restored sessions. The parent registers first, its initializer creates the child, IDs are exactly N/N+1 with allocator N+2, both original callback chains run once and native save/load hashes match.
- **12 handled checkpoint contexts:** new/restored × parent surface × configure/OnSpawn/OnCreate. Save and hash refuse the unregistered candidate. Catching these pure diagnostic refusals leaves the session healthy; the parent completes once and completed save/load works. No fake fault or rollback is manufactured to satisfy the assertion.

This is protocol/provider instrumentation, not full public-player acceptance or arbitrary CLR sandbox proof. In particular, post-registration initializer checkpointing is not tested or claimed safe by the local-phase guard.

## Selected regression evidence

- Original cleanup proof **780/0** still preserves exact primary exception/immutable first fault through secondary cleanup errors, candidate death and cleanup-triggered creation refusal.
- Original timed entity result proof **88/0** retains actual native WIP/tools, capacity/placement waits, repeat/cancel, sequential/independent results, native point/tile rollout and provider failures.

## All observed attempts

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj --no-restore -v minimal`.

One `--bounded-exact` literal per child, timeout60000 /termination5000. Baseline output6000; finals4500.

| Attempt | Result | Process evidence |
|---|---|---|
| Test/content proof build on2.618 |Clean5.23s|zero warnings/errors,exit0; original production allocator still unchanged.|
| New proof reproduction |348/264,446ms|pid16984,exit1,outer583ms;stdout truncated6000,stderr untruncated. Whole matrix/final counts returned; complete individual failure transcript not claimed.|
| Production phase/build2.619 |Clean7.33s|zero warnings/errors,exit0; original save159/replay1 schemas.|
| Allocation final |**612/0**,657ms|pid5172,exit0,outer917ms,untruncated.|
| Cleanup final |**780/0**,604ms|pid23072,exit0,outer801ms,untruncated.|
| Timed result final |**88/0**,877ms|pid23456,exit0,outer1065ms,untruncated.|

No executable edits after these finals. All processes terminal; no live-handle restarts. Scoped whitespace check passed.

## Remaining work

Original P7.017 remains open for otherwise healthy creation source/map purity, constructor/factory callback cutoff, replacement preparation reentrancy, post-registration initializer checkpoints or nested updates, coupled conversion callbacks, remaining disposition consumers, topology/accounting breadth and stress/performance. Create-versus-irreversible-replacement and Replace-versus-local-Create exclusion are implemented in the original owner but were not independently negatively instrumented here. These results are not full allocator/mod/lifecycle coverage. No staging/commit/push until full goal and plan completion.
