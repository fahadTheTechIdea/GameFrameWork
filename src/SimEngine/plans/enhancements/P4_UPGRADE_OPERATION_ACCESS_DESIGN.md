# P4-UE1 — Accepted upgrade operating eligibility

## 0. Problem
EntityReplacementEffectDef.CheckStart/CheckSubject reuse CheckIntent, so accepted physical replacement can fail after prototype metadata is revoked. The new operating capability must preserve actual access rules, not become a blanket bypass.

## 1. Goals
1. Existing replacement effect owns one identity/access evaluator for player intent versus accepted physical operation.
2. Physical start/completion use IContentOperationAccess; new requests retain IContentAccess/metadata gates.
3. Actual authored access restrictions deterministically stop and resume replacement through existing retry policy.
4. Geometry, owner, material consumption, entity identity and completion history remain original owners.
5. Prove minute/half hidden upgrade completion, ordered rule waits, saved twins/replay/isolation and no premature replacement.

## 2. Design
CheckIntent delegates private CheckTarget(playerIntent:true); CheckStart uses the same identity checks with operating eligibility, then original Upgrades.Check. Physical CheckSubject/CommitSubject remain unchanged. Require both capabilities in existing effect validation. No new replacement, history, stock, reservation or policy state.

The proof also exposed missing effect/replacement target in original EntityActivityDef disclosure; its resolved immutable alias list now includes both, shared by the existing activity catalog and all work views. Composite initialization probes now use an owner-scoped access-module ID, preserving kernel duplicate-ID rejection.

JSON authors replacement/activity/materials/visibility/forgetting and retryable real access blockers. Existing independent access-rule module supplies saved rules and ordinary control commands; no rule implementation in test driver. Existing recipe fixture gets only generic explicit module/document/scenario authoring parameters.

## 3. API
No new public production method; Core2.519-to2.520, save154/replay1 and all view/state schemas unchanged. No compatibility shim.

## 4. Files
Original WorkEffects/EntityActivities/SaveGame/protocol; JSON-only recipe helper; existing access probe identity; explicit embedded documents, new thin driver/profile/stage/evidence/master. Reuse existing access module/rule/command/content-policy owner.

## 5. Errors
Hidden new intent remains UnknownDef. Actual ordered rule refusal remains its typed code and authored wait; no ignored blocker or guessed target. Replacement identity/stock/placement guards remain original.

## 6. Compatibility
Development-only direct owner revision.

## 7. Verification
One Release completed-slice build and bounded new exact case; same-case repairs only. Actual construction precedes timed upgrade; metadata revocation plus real A/B restrictions, saved waiting completion, resume once, material/geometry/history checks and replay. No full/family/older suites.

## 8. Risks
Do not switch initial player admission to operation eligibility. Rule toggles are ordinary engine commands; retained snapshots/history must not follow later mutations. Tests cannot manufacture stock or completion.

## 9. Non-goals
All actual technology/restrictive-basis/fault/other physical workflows/ports/provider/lockstep/soak/performance/global acceptance, Tycoon/Oil/platform/Draw. Broad gates remain open.

## 10. Order
Revise original replacement access; author JSON and explicit module composition; build initial facility and accepted upgrade via players; revoke metadata/enable real ordered blockers; save/retry/unblock; verify physical once-only result and refresh tracker.

## 11. Scoped result

FINAL Release188/0/0 in3695ms/PID15496/Exited0; first0/1 and second180/8 attempts retained. Solution10.06s/focused8.41s/10.25s clean. Four minute/half cases prove real construction/accepted hidden upgrades, actual ordered A/B waits and authored retries, saved twins/resume once/material/geometry/history/replay/isolation. Core2.520.0/save154, schemas unchanged; original221/193checked/28open unchanged/additional252. Profile-info720/0runtime; no executable edits after FINAL. [Evidence](../../docs/migration/P4_UPGRADE_OPERATION_ACCESS_VERIFICATION.md). Broad policy/provider/effect/fault/performance/global/full goal/final regression remain open.

