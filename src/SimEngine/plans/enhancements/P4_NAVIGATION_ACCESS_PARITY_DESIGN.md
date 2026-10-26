# P4-NP1 — Private navigation admission parity

## 0. Problem statement
MotionView.PreviewNavigation lacks current sight and healthy-entry checks. MotionNavigationContracts.PreviewGroup accepts an alternate foreign refusal used only by the view; ordinary single/group commands can change hidden owned motion. This violates the original P4.014 single-evaluator/private-projection requirement.

## 1. Goals
1. Use the original navigation contract owner for actual live, owned, current-sight admission before profile/provider metadata.
2. Typed/ordinary single/group previews and executed private refusals must agree; hidden entries disclose no selection.
3. A group refusal commits no visible prefix; permitted route changes retain actual cargo/job/travel ownership.
4. Prove ordinary physical delivery, saved hidden inbox/travel, replay, retained facts and isolated sessions.

## 2. Design
MotionNavigationContracts.CheckConfigure gains required player authority and delegates provider checks within the same owner. Existing Configure retains its internal engine-level provider validation. Remove the view-only foreign-refusal overload. PreviewGroup gates healthy updates, validates canonical target identity, and uses the same CheckConfigure for every member. MotionView/handlers delegate without another evaluator. Observer single diagnosis uses the actual owner and cannot grant command privilege.

## 3. API surface
Breaking CheckConfigure(World,PlayerId,Entity,string); one PreviewGroup signature. No compatibility accessor. Motion view12-to13 and engine2.510-to2.511; save154 and physical codecs unchanged.

## 4. Files to change
Original MotionNavigation.cs/MotionNavigationCommands.cs/MotionView.cs/MotionModule.cs/EngineInfo owner, current protocol, new thin exact case/registration/profile and tracker. Two older foreign-refusal assertions migrate mechanically; their cases are not run.

## 5. Error handling
Hidden/missing/dead/foreign single targets refuse NotFound before profile validation. Group shape errors remain explicit. A hidden member has null selection and no prefix commit. Existing provider observation/commit violations remain fail-stop, never rollback or instant travel.

## 6. Compatibility
Development-only direct revision; no legacy/compatibility shim.

## 7. Verification
One solution build and one new bounded exact case; only same-case repairs if needed. Reuse native file/master fleet fixtures and the existing engine-side visibility module under explicit scenario selection; no parallel motion/resource/state owner or gameplay in the driver. The combined minute/half-minute native physical proof has an explicit300second process bound after two90second attempts reached the bounded deadline; the second proved actual loaded departure and hidden parity before timing out. Every physical wait retains its512Update bound. Conservation is observed every eight calls and at boundaries, not claimed at every update.

## 8. Risks
Atomic admission does not freeze legitimate ongoing physical progress. Assert unchanged selected profiles/cargo accounting while allowing normal time to advance. Pause and speed are local pacing, not extra saved clocks.

## 9. Out of scope
Other command families, full P4/privacy/provider/stress closure, Tycoon/Oil/platform/Draw, full regression and goal completion.

## 10. Execution order
1. Revise original admission and delete alternate refusal mapping.
2. Author thin current physical fleet proof with actual hidden commands.
3. Build and execute the one new exact case.
4. Record outcomes and current-source audit; leave compound/global gates open.

## 11. Scoped result

FINAL Release354/0/0 in123372ms, PID43440/Exited0, native minute/half loaded departure/delivery, private saved inbox/refusals, actual finite stock, twins/replay/isolation. Three earlier Debug timeouts are retained in [evidence](../../docs/migration/P4_NAVIGATION_ACCESS_PARITY_VERIFICATION.md); no Debug/performance/full regression claim. Core2.511.0/save154/motion13, original221/193checked/28open unchanged/additional243. No executable edits after FINAL; metadata audit refreshed. Other command/private/provider/global/full goal gates and authorized final full regression remain open.

