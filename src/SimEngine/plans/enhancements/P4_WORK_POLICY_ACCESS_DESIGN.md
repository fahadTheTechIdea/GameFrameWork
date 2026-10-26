# P4-WP1 — Work policy authority and public group controls

## 0. Problem
WorkPolicyRules/WorkView/single handlers currently omit current sight; group previews can disclose hidden policy settings. The existing group operation is engine-internal to hosts except named fleets.

## 1. Goals
1. One existing policy owner checks healthy/live/actual/owned/current-sight admission before settings/provider callbacks.
2. Hidden/foreign/missing targets return the same private refusal in typed previews and actual commands.
3. Expose original group admission/commit via commands and immutable views without named-fleet dependency or another policy store.
4. Prove a hidden member cannot mutate a visible prefix, while policy pause/resume preserves real cargo/job/time and saved continuation.

## 2. Design
CheckConfigure gains required player authority, with provider-level checks retained in the same partial class. ReadObserved becomes the internal shared observed read and uses the existing fail-stop observation owner. PreviewGroup withholds settings for private members and shares CheckConfigure. The view and command handler delegate. Add one group command/handler in WorkPolicyCommands.cs, register it in WorkModule, and forward WorkView.PreviewPolicyGroup to the original selection evaluator.

## 3. API
Breaking CheckConfigure(World,PlayerId,Entity,WorkPolicySettings), new ConfigureWorkPolicyGroupCommand and PreviewPolicyGroup. Work view100-to101, engine2.511-to2.512; save154 and physical codecs unchanged. No compatibility entry point.

## 4. Files
Existing WorkPolicies/WorkPolicySelection/WorkPolicyCommands/WorkModule/WorkView/identity/protocol; new thin exact driver and tracker. Older foreign assertions migrate mechanically only if affected; their cases are not run.

## 5. Errors
NotFound precedes private metadata/settings; malformed group shapes remain InvalidArgument. All members preflight before commit; original provider violations fail-stop. No rollback, teleport, stock injection or completion fallback.

## 6. Compatibility
Development-only direct revision; no legacy shim.

## 7. Verification
One Release build and new exact bounded case, only same-case repairs if needed. Native authored point policies/sight/resource fixtures drive public normal Update. Verify visible/hidden single/group parity, real pending saved inputs, no visible prefix, disabled loaded physical movement, resume/delivery, saved twins/replay/isolation and physical manufacture.

## 8. Risks
Normal simulation progress may continue during refusal; verify ownership/material/assignment invariants rather than freezing legitimate work. Local pause is not another saved clock.

## 9. Non-goals
Other work controls/policy families, broad P4/privacy/provider/performance/full closure, Tycoon/Oil/platform/Draw and final full regression.

## 10. Order
Revise original owners; add public group forwarding; author physical thin case; build/run; update scoped evidence and master, with broad gates open.

## 11. Scoped result

FINAL Release288/0/0 in2363ms, PID38492/Exited0. Native minute/half actual hauling with hidden pending inputs/no visible prefix, policy pause with advancing time/retained job/cargo, resume/delivery/twins/replay/isolation and physical manufacture pass. FIRST286/0/0 and added retained-job coverage build are preserved in [evidence](../../docs/migration/P4_WORK_POLICY_ACCESS_VERIFICATION.md). Core2.512.0/save154/work101, original221/193checked/28open unchanged/additional244. No executable edits after FINAL; current source/input/link/gate metadata refreshed. Other work controls, policy families, provider/fault/performance/global/full goal and final full regression remain open.

