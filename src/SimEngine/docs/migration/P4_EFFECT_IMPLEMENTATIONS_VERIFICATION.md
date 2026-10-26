# P4-G39 — Direct effect implementations and worker subject safety

2026-10-09. Core beep-simengine-2.377.0/save154/replay1. Full P1-P12 remains OPEN.

## Actual migration

WorkEffectDef and PhysicalWorkEffectDef are deleted, not renamed/wrapped. Replacement, inventory,
recovery, infrastructure line, knowledge observation, spatial extraction and condition definitions
implement existing IPhysicalWorkEffect; construction and incident completion implement IWorkEffect.
Public stock/retention/mark/completion proofs and old compile-time lookups use the same contracts.
Callback plumbing delegates to the original PhysicalWorkEffectContracts algorithm and existing
execution/resource/knowledge/incident owners. No new coordinator, queue, clock, catalog or codec.

IConditionEffect exposes source/unit/change and completed-worker subject metadata. Shared strict
ConditionEffectDeclaration reads/resolves the current JSON fields; ConditionEffectContracts validates
actual metadata/worker stage and delegates changes to original ConditionContracts. Native target
condition implementation is a Def; WorkerConditionEffectDef specializes only WorkerSubject metadata.
The direct independent Def/IConditionEffect uses the same declaration/helper without native inheritance.
There is no scheduler type switch, guessed yield, implicit repair or legacy authoring translation.

During physical subject resolution, the original order target/provider/map scope now additionally
observes the actual ActivityNodeExecution.CompletedBy worker scalar/Entity.Sync. Identity remains in
that existing node record; no remembered subject ledger or second worker lookup authority. Pure
checks and refused commits continue to observe the resolved actual subject. Mutation faults rather
than pretending to undo it. Arbitrary unrelated entities/services are outside this scoped guard.

## Compilation and source inspection

First --no-restore solution build14.53s failed with four removed-type references in three historical
test files (CargoRecoveryTests, ResourceDispositionTests, ResponseTests). Their structural/alias
lookups now use Def and IPhysicalWorkEffect/IWorkEffect; no runtime logic or production shortcut.
Final clean build6.29s, zero warnings/errors. Framework, public-only extension assembly, historical
cases and thin quickstart compile. Historical/family/full/Oil/Tycoon/platform cases were not run.

Source search over framework and ExtensionProof has zero references to either deleted base. Runtime
assembly inspection in the new case confirms both old types absent. This proves removal, not all
semantics of every migrated physical category. Old test fixtures retaining World access are not the
new player path and are not claimed to complete the host-boundary migration.

## ONE NEW exact bounded case

```text
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact "Worker effect contracts: earned subject changes and scoped faults" --timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 50000
```

FIRST/FINAL155 passed/0 failed/0 aborted,827ms; PID14840 exited0, observed990ms.
One runtime invocation, untruncated, no observer error or timeout. Two build invocations total.
231 selected executable/content inputs frozen with SHA2562026-10-09 14:25:05 UTC before execution.
No executable changes afterward. Selected identity is not a complete dependency closure.

## Runtime coverage

- Actual native and direct independent condition-effect aliases/units, existing WorkBoard authority,
no Tycoon module. Explicit opening stock20 in source plus3 in worker; actual condition10 in both.
- Ordinary player policy/PostOrder commands, real machine movement, positive eight-minute staffed
progress, two consumed worker materials and one held tool. Partial work cannot damage equipment.
- Incomplete worker-subject preview returns typed not-found, foreign preview refuses ownership,
all reads preserve healthy hash. Completed worker selection uses actual earned graph identity.
- Partial resource/tool/work save, sole-clock pause, saved twins and real condition damage10 to4.
The order target remains10, so the effect does not confuse source with the completed worker.
- Original condition timestamp, later updates never repeat damage, private condition view and changed
worker codec reload. Real eight-minute repair restores4 to10; held tool is unchanged, total stock21.
- Retained immutable node facts, completed reload, interleaved session isolation and normal replay.
- Normal cancellation during partial work causes no condition change and no refund of two spent
inputs; original commitments settle and save normally.
- Thirteen invalid authoring cases fail before Initialize: source/kind/unit/amount/quantum/unknown
key, unused invalid unit, final-worker effect, timed-worker node and unrelated parallel machine work.
- Five actual independent callback faults after physical work: private completed-worker codec changes
without Touch during resolution, pure check, and refused commit; refused commit Touch; commit exception.
Each reaches the actual worker once, latches work.effect, withholds that Update's time completion,
cannot resume/hash/save. No world rollback or compensating repair/material change is claimed.

## Remaining scope

Only target/completed-worker subject scopes are exercised. Other subjects/services, malformed custom
condition metadata, every native target effect, replacement/recovery/extraction/infrastructure and
knowledge migrated-runtime matrices, broader retry/utility/network composition, global accounting,
corruption/lockstep/fuzz/soak/allocations/performance and full original compound gates remain unproved.
Metadata and source-only migration are distinct from actual runtime verification.

Original221 gate IDs/text/checkboxes are unchanged; line offsets refresh only. Earlier G38 and all
older focused results are historical, not rerun on2.377.0. G19 authority remains unanswered.
GameClock source and excluded Tycoon files/Oil/hosts/demos/platforms/Draw unchanged. No staging,
commit or push before the full goal and original plan are complete and verified.
