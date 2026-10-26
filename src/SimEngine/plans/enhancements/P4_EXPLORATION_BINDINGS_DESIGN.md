# P4-G42 — Contract-owned contact exploration bindings

## 0. Problem statement

`Standard/Exploration.cs` exposes exploration runtime contracts but still resolves a concrete
`ContactExplorationProfileDef`, and its reusable component requires `ContactExplorationDef`.
An independently authored provider cannot configure the original unfinished-dwell owner through
contracts. This contradicts the original contract/content extension requirement.

## 1. Goals

1. Native and independently registered JSON definitions bind the same original contact component.
2. One strict profile reader and one binding reader resolve actual frozen metadata; no profile copy,
   alternative scheduler, knowledge ledger, clock or compatibility accessor.
3. Validate all loaded profile metadata and actual selected source bindings before initialization.
4. Prove physical non-tile contact, retained/reset interruption, private earned observations,
   pause/save/replay/session isolation through commands and immutable views.
5. Scoped metadata observations use existing clock and actual queried-entity codec guards; prove
   actual clock-mutation refusal at the registered composition callback. Wider live getter/private
   codec fault matrices remain source-only/open.

## 2. Design

Add `IContactExplorationProfileDefinition` and `IContactExplorationDefinition` beside the existing
contracts. `ContactExplorationProfileDeclaration` owns explicit canonical entries and sampling-state
references. `ContactExplorationDeclaration` owns the named source, named sensor and profile reference.
Native definitions delegate these declarations. The existing `ContactExploration` derives `Component`
and selects its actual definition through the contract. Its existing unfinished progress codec is
unchanged. Transient validated call facts hold actual entry/state references, never authoritative state.

`ContactExplorationContracts` validates frozen owned aliases, canonical nonempty entries, actual
regions/subjects/state, first-learned retention, duration representability and source/sensor/presence
agreement. All loaded profiles receive metadata validation; only actual source bindings require the
selected world's space. Existing observation guards wrap metadata and actual queried entity codecs.

## 3. API surface

Public immutable profile/binding contracts and shared JSON declarations; original `IExplorationSource`,
`ExplorationView`, operation/work commands, source update permission and sole knowledge ledger retained.

## 4. Files to change

| Action | File | Approximate lines | Risk |
|---|---|---|---|
| Modify | `src/Beep.SimEngine/Standard/Exploration.cs` | 180 | high |
| Modify | `src/Beep.SimEngine/Standard/ExplorationModule.cs` | 8 | medium |
| Modify | `src/Beep.SimEngine/Kernel/SaveGame.cs` | 1 | low |
| New | `tests/ExtensionProof/ExplorationBindingsProof.cs` | 100 | medium |
| New | `tests/ExtensionProof/content/exploration-bindings.json` | 35 | medium |
| New | `tests/Beep.SimEngine.Tests/ExplorationBindingTests.cs` | 220 | high |
| Modify | Existing proof factory/resource registration/test registration | 20 | medium |

## 5. Error handling matrix

| Condition | Result |
|---|---|
| Missing/duplicate profile/entry/reference, invalid state/duration/interruption | Content refusal |
| Incompatible source sensor/presence/space/retention | Content refusal before Initialize |
| Outside contact region or disabled sensor | Existing typed blocker and authored reset/retain |
| Foreign company | Existing NotFound/no private source data |
| Metadata observation mutates scoped clock/queried entity codec | Fault, no healthy continuation/save/hash |

## 6. Backward compatibility

Development-only direct migration. No legacy definition dependency or fallback. Build2.380.0,
save154/replay1; the existing contact component payload/order and other schemas remain unchanged.

## 7. Verification

One NEW exact bounded case: `Exploration bindings: physical point discovery and saved interruption`.
Build the solution when implementation is ready; repair/rerun only this same new case if needed.
Record every build/run. Freeze executable inputs after green, then update evidence/master/audit.
No historical/family/full/Oil/Tycoon/platform runtime; full original compound gates remain open.

## 8. Risks

Metadata guards are scoped, not rollback or hostile-code sandboxing. Original region authoring bases,
remote/cell-wide exploration, utility consumption and broad provider/fault/soak gates are not closed
by this binding migration. Fact retention belongs solely to the existing company knowledge ledger.

## 9. Out of scope

Tycoon/excluded files, OilFieldDays, hosts/demos/renderers/adapters, Draw, full regression and publication
before the full goal and plan are finished. G19 scope question remains unanswered.

## 10. Execution order

Read owners → migrate shared declarations/contracts → bind independent authored definitions → thin
physical player driver → build → one exact case → freeze → evidence and existing tracker refresh.

## 11. Status

Scoped implementation verified: FINAL193 checks/0 failures/0 aborted,960ms. Four clean builds and
four invocations of the SAME new exact case are recorded in the
[evidence](../../docs/migration/P4_EXPLORATION_BINDINGS_VERIFICATION.md). First two runs21/1 identify
an authored mobile endpoint without a dock; the second adds diagnostic facts only. Authored service/
waiting/departure slots and an accessible outside site retain collision rules; third run191/0.
Final review repairs selected null-entry profile refusal and adds its negative case; fourth193/0.
FINAL253 selected executable inputs frozen2026-10-09 15:23:09UTC; no executable edits afterward.
Full original P1-P12 and wider region/provider/global/authorized regression gates remain OPEN.
