# P2-F7: pre-initialization composition acceptance

## 0. Problem statement

The original P2 acceptance gate for duplicate owners, missing providers, dependency cycles and order conflicts remains open. Existing composition checks belong to `GameDefinition`, `ContentTypes`, `ContentSet`, `ServiceContainer`, `CommandRegistry`, `EventBus`, `WorldComposer` and `World.AddSystem`; no new registry or validation pass is needed. P2-F6 verified registered command participants, but not the combined declaration/file/registry matrix on current source.

## 1. Goals

1. Verify code and master-file ownership/dependency refusals before any Initialize, generation or gameplay callback.
2. Verify service, command, event, view, codec, system and generation identity/order conflicts through their existing owners.
3. Verify missing declared providers, invalid schedules and frozen late registrations without substituting providers.
4. Verify valid aliases and actual non-tile physical production through public commands, views, Update, the sole clock, saves and replay at two authored time quanta.
5. Change only the original acceptance checkbox if the complete bounded case passes. Keep wider P2/P1-P12, regression and publication gates open.

## 2. Design

Extend the existing `StartupCommandBindingsModule` negative registration modes. They exercise actual composition registries and collect lifecycle counters; they implement no positive gameplay. Extend `PlanetXProductionTests.Factory` with optional master JSON editing, preserving one responsible factory and the existing explicit file list. Add one ordinary acceptance driver using the original Planet X recipes, stock, machines, ports and point-space provider.

## 3. API surface

No production API, codec, content schema or engine build change. Test-only JSON modes and an optional pure factory authoring callback.

## 4. Files to change

| Action | File | Risk |
|---|---|---|
| Modify | `tests/ExtensionProof/StartupCommandBindingsProof.cs` | low |
| Modify | `tests/Beep.SimEngine.Tests/PlanetXProductionTests.cs` | low |
| New | `tests/Beep.SimEngine.Tests/CompositionAcceptanceTests.cs` | low |
| Modify | `tests/Beep.SimEngine.Tests/Program.cs` | low |
| Modify | existing tracker, audit and checkpoint headers | low |

## 5. Error handling

Declaration/registry conflicts use the existing exceptions; malformed master/content uses ContentException. Assert the expected refusal and zero lifecycle prefix. Invalid schedule uses ArgumentException. No exceptions are swallowed to continue composition. Physical waits are bounded with a snapshot diagnostic.

## 6. Compatibility

No legacy or compatibility implementation. Original owners and payloads remain authoritative.

## 7. Verification

One solution compile, then ONE NEW exact case through `--bounded-exact`, timeout60000ms, termination5000ms, output30000 characters. Same-case repairs only. No older, family, full, Oil, Tycoon or platform runtime. No executable edits after the final pass; freeze changed/new selected inputs and refresh literal original gate anchors.

## 8. Risks

Negative instrumentation is not proof of a second gameplay implementation. Valid production must execute only original owners. Captured module getters and schema facts are existing behavior, not permission to mutate live gameplay. A narrow composition gate is not a claim of whole-engine completion.

## 9. Out of scope

Oil consumers, adapters, Draw, business/Tycoon, arbitrary mod sandboxing, provider CLR purity, performance, full regression and publication before the full goal/plan finish.

## 10. Execution order

1. Add test-only declaration/master/registry refusal coverage using existing owners.
2. Add saved, physically supplied production and replay proof; compile once and run only the new exact case.
3. Record all attempts, freeze source, update evidence/tracker/audit and verify metadata without more runtime.

## Verified outcome

FIRST/FINAL250/0/0,5303ms; actual process Exited0, untruncated/no observer error. The original P2 composition gate is covered, not all P2 or full P1-P12. Three compile attempts retained; final targeted compile5.91s clean, full solution not rerun. No production source change. [Evidence](../../docs/migration/P2_COMPOSITION_ACCEPTANCE_VERIFICATION.md). Read-only closure:1381 source/13 quickstart anchors,684 frozen executable inputs with zero drift,221 literal gates matched,2633 paths/918 Markdown/8177 links with no broken targets,18 valid master rows, no driver boundary violations or staged files; protected clock/Tycoon hashes unchanged. No executable edits after FINAL and no publication before the full goal/plan.
