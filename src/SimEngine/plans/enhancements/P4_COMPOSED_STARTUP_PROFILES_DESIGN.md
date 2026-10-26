# P4-N2 — Composed native startup profiles

Historical checkpoint; not rerun after P4-N3. [Sole native point startup](P4_NATIVE_POINT_STARTUP_DESIGN.md) now removes the remaining duplicate point executor. Dated limits below are preserved as historical evidence; full plan remains open.

Status: scoped implementation/46-check verification complete; remaining PointStart migration and full plan open. [Evidence](../../docs/migration/P4_COMPOSED_STARTUP_PROFILES_VERIFICATION.md).

## Problem and goals

`WorldSetupProfileDef` in `src/Beep.SimEngine/Standard/WorldSetup.cs` currently compiles only its direct entry list. Point placement already implements `ISpatialPlacementIntent` in `tests/ExtensionProof/PlanProof.cs`; the remaining migration needs explicit cross-document startup composition, not another geometry API or startup executor.

Extend the existing profile owner so a JSON root profile can include other profiles. Shared included profiles and their actual entries execute once, dependencies remain authoritative, and invalid references/cycles fail before system initialization. Keep the existing `WorldSetupStage`, `EntitySetupDef`, spatial placement provider, entity store, resource initialization and one game clock.

## Design and contracts

Add `IWorldSetupProfileDefinition` over the existing immutable compiled `Graph`. `WorldSetupProfileDef` implements this and `IDependencyNode`; its required JSON `includes` array supplies profile edges. `entries` continues to supply actual startup definitions. Resolve exact references, then validate after all definitions resolve. Collect the reachable profiles iteratively and pass them to the existing sole `DependencyGraph<T>` topology compiler—no second cycle/topological algorithm. Union actual entry definition aliases by stable ID, then compile the same existing entry graph. A diamond references one actual profile/entry, never a copied entity intent. Duplicate direct entries or duplicate include edges refuse rather than being silently normalized.

Each profile declares `includes`, including an explicit empty array. Migrate all current native JSON profiles and the one inline authoring fixture; no legacy reader/default. Stage execution accepts the profile contract but retains its existing generation pipeline and result/home validation. No saved field/layout change; bump engine build to2.329.0, retain save154/replay1 and native/provider state schemas.

## Files and errors

Modify WorldSetup.cs, EngineInfo identity in Kernel/SaveGame.cs, protocol fixture, existing native-profile JSON documents, and the inline cancellation-profile authoring. Add a master-listed external point proof with root/branch/shared startup definitions split between files, using explicit authored placement distinct from prototype opening position. Its thin driver sends ordinary work and haul commands and observes public facts.

Missing included profile, self/indirect include cycles, repeated include/direct-entry identity, missing required field, and entry dependency errors are content failures before Initialize. Existing spatial bounds/provider/company checks retain their actual generation lifecycle; no atomic world-generation guarantee is invented.

## Verification and risks

Compile once the completed slice is ready. Run ONE NEW bounded exact case (15-second process bound,3-second termination bound), repairing/rerunning only that case. Prove once-only diamond startup, cross-file home dependencies, exact point placement, definition-order independence, initialization refusal, real timed travel/work/hauling, saved twins/no regrant, and replay. Existing tests/profiles are source/compile checked, not runtime revalidated. A reachable shared profile must not create duplicate IDs or grants; the proof checks actual public entity/stock identity.

## Execution order and exclusions

1. Extend the existing profile/graph contracts and migrate required authoring.
2. Author the composed external point scenario and explicit master document list.
3. Implement the public-command/view driver; build and run only its new exact case.
4. Update master/audit/evidence with actual outcomes and remaining duties.

The legacy `PointStart` executor/content migration remains an explicit next step; this prerequisite does not claim all point fixtures migrated. No consumer/Oil/Tycoon behavior, platform, Draw, second clock, loader, registry or scheduler changes. Full P1–P12 remains open; no commit/push until full goal AND plan finish and are verified.
