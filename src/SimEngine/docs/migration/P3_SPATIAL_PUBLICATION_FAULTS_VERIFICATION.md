# P3-S37 — Spatial publication failure verification

## Current M67: spatial publication ownership and material semantics — 2026-10-22

Native **2.722.0/save161/replay2**, master HEAD `cb61483ab81404c11944931aedb4703ba694edb5`
plus preserved existing work. [Existing publication design](../../plans/enhancements/P3_SPATIAL_PUBLICATION_FAULTS_DESIGN.md),
[line content owner](../../plans/enhancements/P4_LINE_DEFINITION_CONTRACTS_DESIGN.md),
[active C3 matrix](../../plans/enhancements/P7_OWNED_RESERVATIONS_DESIGN.md#current-c3-audit-m62).
Original221 requirements remain210 source-recorded checked/11open, not full current-runtime
certification. No original gate closes from this selected source/caller/physical qualification.

### Two observed defects and original owners

1. Old2.721 native quantity-withdrawal notification can call Hash successfully before publication
   completes: exact baseline2/2, `escaped=True`. The committed quantity/revision prefix is real;
   the missing admission boundary, not invented stock, is the defect. Other old-image combinations
   were not separately reproduced. The existing SpatialMutation owner is now a per-world intrinsic
   service implementing ISpatialMutation/ISimulationOperationBoundary. World binds it once under
   engine ownership before module composition; its existing frozen provider index observes it.
   It uses the same Kernel SimulationOperation nesting primitive on its own instance. Commit holds
   that scope through publish, notifications, fact verification and final health check. Existing
   first-fault phase spatial.mutation remains. Native map and independent tile/point providers use
   the contract; the static Commit(World,Action) entry is deleted, not retained as a shim.
2. Compiled caller review found corridor Stamp/Erase writes lacked a material-versus-topology guard.
   The baseline accepts all four native/direct quantity topology/owner variants and initializes
   systems:8/8. This is an admission reproduction, not a claimed runtime material-creation demo.
   Existing LineDefinitionContracts.ValidateMetadata now rejects ResourceMeasure.Quantity for
   both line topology and owner layers, before Initialize. Resource-linked Associated layers remain
   valid; they carry descriptive metadata, not material quantities. No geometry-specific duplicate
   validator, second line/resource model or broader ban on resource-linked layers.

Five native files change: Kernel SpatialMutation/World/SaveGame, Map TileMap and Standard
LineDefinitions. No new native source; all450 remain. SpatialMutation retains its original pure
Check/Facts helpers and actual World fault owner. Its operation counter is transient, not a saved
phase, resource balance, journal, clock or provider-value cache. No new content defaults or JSON
switch can disable intrinsic publication integrity. This is engine protocol, like initialization
and checkpoint admission—not game-specific business logic. Codecs161/replay2 and physical durations
are unchanged. Independent providers need no native inheritance and now call the same contract.

### Finite spatial material caller matrix

The existing read-only compiled audit now additionally records known quantity/sample/publication/
owner contracts and selected generic MapLayer field stores. It checks the same exact DLL/PDB/source
manifest; unfamiliar quantity-interface methods fail, and unknown resource methods still fail.
Final:450sources/450PDB checksums,2926types,12893methods with IL,344302instructions,
**132calls =111 earlier resource calls +21 spatial calls**,14 inventory/book primitive field writes,
8 selected generic-layer field stores,0unknown/0unmapped/0indirect sites.

| Actual spatial call group | Native source/count | Accounting disposition |
| --- | --- | --- |
| Intrinsic publication construction | Kernel/World1 | One per-world engine protocol owner; not material creation or a module default. |
| Native value/quantity preparation and publication | Map/TileMap8 | SetSample delegates Set; quantity wrappers prepare/commit actual writes; owner/batch/value publications use one service. Low-level primitives are not player extraction or attributed production. |
| Physical quantity endpoints and binding | Standard/SpatialResources7 | One binding plus original reserved/unreserved deposit/withdraw routes; actual work/incident effects build ResourceMovementIntent through the existing transaction owner. No extra stock/book. |
| Authored world startup | Standard/WorldSetup2 | Owner/sample setup only from original WorldSetupStage.Generate before gameplay; initial map quantities are opening state, not fabricated runtime production facts. |
| Corridor stamp/removal | Map/TileLineGeometry2 | Batch writes topology/owner data. New shared line metadata gate forbids actual quantity channels; Associated links remain valid. |
| Staffed terrain work | Standard/TileTerrainWorkEffects1 | Existing declaration AND live admission already reject quantity channels; no material debit/credit is hidden as terrain work. |

Every one of132 typed calls has one disposition in the machine report. The8 generic-layer field
stores include constructor backing structures, generation flag, staged sample/fill publication,
checkpoint restore and a static sampler delegate. They are **not eight material transfers** or all
collection/array element writes. Existing MapLayer owns those in-place writes as well. This census
does not certify arbitrary CLR behavior, all future providers, external modules' unexecuted paths,
whole-session opening-balance equations or every semantic quantity mutation. No new journal was
introduced. Existing physical extraction/spill/line tests below qualify actual current owner routes.

### Exact execution and retained evidence

[Attempts/images](../../artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/attempts.json),
[compiled census](../../artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/call-audit-2.log),
[caller dispositions](../../artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/caller-dispositions.json),
[frozen inputs](../../artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/final-inputs.json),
[structural checks](../../artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/structural-checks.json),
[original requirements](../../artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/original-gates-current.json).

| Latest selected literal | Passed/failed | Suite/observer ms | PID/exit | Captured image |
| --- | --- | --- | --- | --- |
| Spatial operation admission: unfinished quantity and ownership publication cannot checkpoint or advance time | 4086/0 | 7275/7459 | 24456/0 | build4 |
| Spatial publication: actual notification failure retains first fault and forbids healthy continuation | 174/0 | 755/941 | 12544/0 | build4 |
| Spatial extraction: timed work, blockers and saved physical continuation | 86/0 | 2161/2342 | 27000/0 | build4 |
| Spill cargo interruption: native and independent saved load returns through authored recovery | 856/0 | 10597/10787 | 22988/0 | build4; two public replays |
| Line material semantics: topology and ownership metadata cannot overwrite resource quantities | 16/0 | 339/597 | 24760/0 | build6 |
| Line definitions: independent master JSON drives physical DAG corridors and plans | 222/0 | 2957/3215 | 10084/0 | build6; two public replays |

Latest **5440/0/four actual public replays**, across captured build4/build6 images—not one final-image
regression. The four spatial scopes5202/0 were not rerun after the line-definition-only guard and its
tests changed; their Kernel/publication/map and independent provider sources remain unchanged.
Final line scopes238/0 use build6. New324 contexts cover native/independent tile/point quantity and
owner writes, tile batch and native Fill, new/restored sessions, handled/propagated failures and nine
checkpoint/clock entries. They verify real published prefixes, exact revisions/private cursors,
nested publication permission, before-payload restore refusal, no clock budget/update publication,
first fault, saved twins, resumed Update and isolated sessions. These are explicitly negative
low-level provider probes, not host-owned gameplay or public acceptance. Public replays are separate.

Six targeted builds: one failed7.83seconds because a new test passed a string where TileSampleMutation
requires ISpatialLayerDefinition; five clean6.93/11.12/6.81/10.46/9.98seconds. Nine literal runtimes
retain the old hash2/2 and line admission8/8 failures plus intermediate3276/0 before extra nested-scope
assertions. Both compiled audits succeed; first precedes the line guard (344284instructions), second
is final344302. No earlier green reran after unrelated test-only/line-only edits; no bound or expected
physical quantity was reduced. Existing324 contexts remain bounded by the original process watchdog.

Final native `FB1D841ED6577951D7C1CA8FAF5A95BD46FBFE48C9531111C8D4BEB4A86FBE87`;
proof `991851615CB8945E720D81FE0CC6268F12BE3E0FB1C0C5C943180E8C7274ACD5`;
tests `CABDC6A78787BA502A01F60F458EC1963897A95D7606F8DCC41736DF4E0CA668`.
Earlier CCD507... native and build2/3/4/5 captures retain their actual results, not relabelled final.

Build: `dotnet build tests/Beep.SimEngine.Tests/Beep.SimEngine.Tests.csproj -c Release --no-restore -v quiet`.
Each literal runs the tests DLL with `--bounded-exact`,
`--timeout-ms 60000 --termination-timeout-ms 5000 --output-chars 30000`.
Audit: `docs/migration/AuditResourceMutationCalls.ps1 -BuildInputs artifacts/optional-module-durability/3a7c1dd0f9ff4a8eb704f30794f3cff3/build-6-inputs.json`.

### Self-review and next action

Local self-review, not independent review. World first-fault, typed results, original resource flow
facts/private views and notifications remain the affected operational owners. [Wider operational/
assurance gaps](P9_HOME_APPROACH_INTENT_VERIFICATION.md#operational-services-and-affected-event-coverage)
remain open. No swallowed exception, fabricated rollback, clock, logger, inventory or new allocation
budget claim. No population/combat/business/platform behavior. Next close the existing finite typed
reference-owner matrix and remaining classified accounting/stage-stress evidence, selecting only
actual missing literal checks. Original P7.017/P7.019 and other broad gates remain open. No full/
profile/family/soak even final, consumer/Oil/marine, Tycoon business changes, Draw or staging/commit/push.
Full goal remains active.

## Historical 2.587 evidence — not current API or current runtime certification

Engine beep-simengine-2.587.0/save157/replay1. Payloads unchanged. No Oil/Tycoon/platform/host rules change, full/profile/family run or Git publication.

## Implementation

SpatialMutation.Commit is a stateless publication boundary delegating to the sole World fault owner. Native and independent batch writers enter it after whole-set pure admission succeeds. Publication/notification exceptions latch spatial.mutation and rethrow the original exception; no rollback, retry, separate fault state or invented healthy checkpoint. Independent mutation gates the existing world fault before null/invalid arguments or preflight callbacks.

Negative protocol probes replace the actual embedded resource-cell guard with a forwarding wrapper only for the test. It delegates all original quantity/book/codec behavior, performs the original QuantityChanged and throws one known exception. No alternative resource store, receipt coordinator or positive gameplay is implemented in the probe.

## All attempts

1. Targeted implementation/test compile: exit0, zero warnings/errors,8.20s.
2. New exact publication-fault case:30 passed/2 failed,377ms; PID36040 exit1/521ms. Both worlds were correctly faulted and all continuation checks passed, but the test expected a bare error message instead of WorldFault's existing exception-type-prefixed message. Exact expected formatting corrected; production error behavior unchanged.
3. Targeted expectation repair compile: exit0, zero warnings/errors,5.71s.
4. FINAL direct publication fault:32/0,500ms; PID36028 exit0/677ms.
5. FINAL affected atomic whole-set refusal/commit/receipt case:56/0,570ms; PID36624 exit0/753ms.
6. FINAL independent physical extraction/optional survey:89/0,2402ms; PID17000 exit0/2577ms.
7. FINAL native visibility-enabled extraction/optional survey:92/0,4094ms; PID20536 exit0/4292ms.

All outputs untruncated and all owned processes terminal. Four distinct exact cases269 successful checks; preliminary30 is not double-counted. No executable edits after these finals.

## Proven scope

Actual native/independent publication begins and the selected guard notification throws once. The identical error instance escapes and the original world fault records the exact type/message/phase. The partial published resource state cannot save/hash as healthy. Later valid/null mutation attempts refuse before callback retries; private/missing entity and knowledge queries cannot hide the first fault. Update returns0, fault identity remains stable and an interleaved session remains healthy and unchanged.

Ordinary preflight refusals still preserve all requested values and remain healthy. Existing physical extraction, pause, partial saves, private learned readings, optional survey and command replay pass on both providers. These cases do not prove all mutation/notification APIs.

## Remaining boundaries

Nonthrowing clock or unrelated state mutation during notification, hostile pure guard callbacks, other direct mutation paths, allocation failures and complete recovery/lifecycle/utility/provider matrices remain open. Terminal exception handling is not arbitrary transactional rollback. Physical stocked dismantling/recovery and global lockstep/fuzz/soak/allocation/full acceptance remain open; no original compound checkbox closes.
