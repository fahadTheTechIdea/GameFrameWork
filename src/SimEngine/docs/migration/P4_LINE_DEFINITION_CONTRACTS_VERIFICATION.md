# P4-G49 actual verification — 2026-10-09

Current2.722 update: [M67 material-versus-topology validation and all attempts](P3_SPATIAL_PUBLICATION_FAULTS_VERIFICATION.md#current-m67-spatial-publication-ownership-and-material-semantics--2026-10-22).
The same contract validator now rejects actual quantity channels for both line values and ownership
before Initialize, but retains Associated resource metadata. New exact16/0 and existing native/direct
physical222/0/two replays pass on captured final build6. No second line model or business change.
The2.392 results below are historical, not current API/runtime/full-regression certification.

Canonical Beep.SimEngine; core beep-simengine-2.392.0/save154/replay1.
[Design](../../plans/enhancements/P4_LINE_DEFINITION_CONTRACTS_DESIGN.md). Scoped authored corridor seam; full original goal/plan remain OPEN.

## Actual commands and attempts

```text
dotnet build Beep.SimEngine.slnx --no-restore
dotnet tests/Beep.SimEngine.Tests/bin/Debug/net8.0/Beep.SimEngine.Tests.dll --bounded-exact 'Line definitions: independent master JSON drives physical DAG corridors and plans' --timeout-ms 30000 --termination-timeout-ms 5000 --output-chars 30000
```

THREE builds exit0/zero warnings/errors:16.20s,7.13s,6.54s. First yielded session45455, same handle polled to terminal; later builds returned terminal directly. No live processes remain.

ONE NEW exact case, THREE attempts; no other runtime case:

- 22 passed1 failed0 aborted in865ms; PID29620 Exited1, observed1017ms.
- 160 passed2 failed0 aborted in1519ms; PID7288 Exited1, observed1666ms.
-FINAL164 passed0 failed0 aborted in1388ms; PID43256 Exited0, observed1523ms.

All used30,000ms execution/5,000ms owned termination/30,000-character output limits. No timeout/truncation/observer error. Only FINAL proves current sources.

First attempt exposed a proof observation mistake: completed orders leave the live-order view. The proof now uses original persistent PlanView terminal history, not a mutable Order reference. Second attempt's supply-empty negative was invalid: worker-fed requirements produce an empty cursor envelope by design. It now authors actual excess cursor supply and expects refusal. Shared validator also checks one-segment prototype node metadata/dependencies, covered by a same-count but unbound source-graph negative. No normal gameplay copied into proof/test.

## Actual scope

ILineDefinition replaces native metadata dependencies in networks, generated cursors, original LineService/topology/effects, plan catalog and infrastructure views. Native/direct definitions delegate sole LineDeclaration for metadata, path plans and templates. Original graph binder and actual work/claims/machine/resource/map/clock owners persist unchanged; no second builder/state/legacy reader.

Native/direct real construction chronology is identical. Three path tiles bind nine authored nodes; inspect/prepare run in parallel with terminal waiting on both and later tile roots waiting on the prior terminal. Machines travel, tools held without consumption, stock40 becomes38 after first install and34 after three. Only earned topology appears. Original partial/completed saves, twin continuation, pause, replay, isolation and detached/private facts work. Paused portable template plans remain intent only. A second partial corridor cancellation retains only its earned first tile, unspent material and stock32; cancelled state saves/restores.

Twenty-seven unused malformed authorings refuse before Initialize, including null/foreign layer, zero value, invalid medium/plan/graph, wrong supply mode/null/excess envelope/segment effect, missing/empty/unknown five references and extra legacy duration field. Two live Layer getters throw/pause the sole clock after a healthy step; selected admission guard faults and save/hash refuse. These are scoped provider/clock proofs, not global/private callback purity.

## Evidence boundaries

Selected378 executable inputs hashed AFTER final green at2026-10-09 19:05:17 UTC. Not a pre-run freeze/full source/binary/environment closure. No subsequent executable edits. Final audit rechecks selected hashes, actual source anchors, local links, unchanged original gate text/states/offsets and protected files.

Ports/line-effect authoring, non-tile networks, all plan/template/route/effect callbacks, owned/public-neutral topology, areas, congestion, promises, utility/global/private/recursion/performance/lockstep/fuzz/soak/full acceptance remain OPEN. Old cases compiled only; previous G48 network evidence is historical, not rerun on2.392.0. Original221 compound gates retain text/checkboxes. G19 unanswered; protected Tycoon/GameClock/Oil/hosts/platforms untouched. Nothing staged, committed or pushed.
