# P11-B3 — Contract-based objective content

Status: scoped implementation verified on2.333.0/save154/replay1; ONE NEW exact bounded case79/0/0 in1043ms. [Evidence](../../docs/migration/P11_OBJECTIVE_CONTENT_CONTRACTS_VERIFICATION.md). Full P1–P12 remains open.

## Scope and ownership

Use IObjectiveDefinition, IObjectivePredicate and IObjectiveEffect throughout the existing ObjectiveSystem and native content resolution. Independent JSON providers must not inherit ObjectiveDef, ObjectivePredicateDef or ObjectiveEffectDef. Keep the same objective enrollment, whole-pass decisions, deadlines, private views, effect checks, terminal states and saved codec. No second scheduler, store, graph compiler, timer, loader or outcome owner.

Native objective prerequisites and logical predicate children resolve actual contract aliases. ObjectiveSystem validates contract dependency graphs, actual owned/unambiguous definition/predicate/effect aliases and positive deadlines before Initialize. Selected provider binding validation remains in that owner. All observation/check callbacks retain the existing purity guard; Apply remains the sole effect commitment callback.

## Implementation stages

1. Replace concrete resolver, graph, entry and enumeration dependencies with existing contracts. Preserve native JSON grammar and state layout.
2. Declare independent objective, forwarding predicate and gated notification effect in an extension module. Use explicit master-listed objective-contracts.json and reuse existing construction/objective content rather than duplicating catalogs.
3. Add ONE NEW public player driver: real construction consumes hauled materials over game time; mixed native/external prerequisites activate in later passes; saved pending effects wait for a second physical structure; effects settle once; private facts, deadlines/dependency failure, saved twins, replay and session isolation stay authoritative.
4. Reject missing/wrong/duplicate/cyclic references, invalid enrollments and deadline declarations before Initialize. Compile and run only this new exact bounded case; repair/rerun only that case.
5. Record actual evidence and refresh master tracker/audit without changing original acceptance requirements or claiming full completion.

## Protocol and limits

Core identity2.333.0/save154/replay1. ObjectiveSystem schema1 and existing physical work/clock formats unchanged. No old-save compatibility reader. Historical cases compile only unless separately authorized. Broader mod malicious-provider, purity, corrupt-state, stress, lockstep, all other stages and full regression remain open. No business/Tycoon expansion, Oil consumer, platform/Draw/adapter work or second clock. Staging/commit/push only after the FULL goal AND plan genuinely finish and are verified.
