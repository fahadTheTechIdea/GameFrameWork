# P2-F6 — command-provider startup admission

## 0. Problem and requirement

P2.acceptance.004 requires duplicate owners, missing providers, dependency cycles and order conflicts to fail before Initialize. Kernel/Services.cs ServiceContainer.ValidateComposition currently visits registered services, systems and world-generation stages, but omits frozen CommandRegistry handlers. An externally registered handler declaring ICompositionPreparation/ICompositionValidator is never called unless separately aliased as a service. Missing provider/binding refusal can consequently be deferred until runtime or silently omitted.

## 1. Goals

Include every frozen registered command handler in the existing preparation/validation pass before any system Initialize, new-game generation or saved payload initialization. Prepare all participants before validating any. Preserve reference-identity deduplication for handlers also bound under service contracts. Test actual command-only bindings, alias bindings, early refusals and valid physical gameplay through public commands.

## 2. Design and API

Modify ServiceContainer.ValidateComposition(World, IEnumerable<GameSystem>, IEnumerable<WorldGenStage>, IReadOnlyList<CommandRegistry.Entry>) to collect command handlers using its original Add identity deduplicator. World.FinishComposition supplies Commands.Entries only after original registry freeze. Existing order services→systems→stages remains unchanged; previously omitted command-only handlers are appended in captured command registration order. ICompositionValidator and ICompositionPreparation comments clarify registered command participation. No new interface, registry, validator owner, persistent state or fallback.

Engine behavior identity2.418.0; save154/replay1 and all payload schemas unchanged. Current identity revised directly; no compatibility readers.

## 3. Files and authored proof

Modify Kernel/Services.cs, World.cs and current Build/protocol metadata. Add non-friend StartupCommandBindingsProof registration-only metadata/handlers, explicit ten-module master/add-on JSON and one new thin test driver. Extend existing Planet X factory only with explicit authoring inputs, retaining original recipes/resources/world/work/logistics. Module probe handlers throw if Check/Apply is invoked: they test startup admission only and never implement positive gameplay.

JSON authors required resource, optional service alias participation and negative preparation/validation/required-provider/registration modes. Native physical production remains authoritative; driver only submits actual commands, reads public facts, advances the sole clock and saves/replays/asserts.

## 4. Error behavior and verification

Missing handler provider/binding, thrown preparation/validation and attempts to register after freeze refuse before Initialize. Original registration collisions remain explicit. Aliases prepare/validate once, all preparations precede all validation. Valid command-only handlers validate on both new/load, without re-running generation or granting stock. Bounded ordinary hauling/smelting proves unchanged physical progression/conservation/saved twin/replay/isolation.

ONE NEW exact bounded case only; same-case repairs permitted. Compile solution once at completed slice; repairs target its project. No old/family/full/Oil/Tycoon/platform runtime. This focused requirement-matched change does not close full P2.004 or arbitrary module/binding/ownership/lifecycle matrices; original221 requirement words and checkbox state remain intact.

## 5. Execution and limits

1. Extend original frozen-provider collection, not another validation path.
2. Add explicit authored fixture and one bounded public-input driver with negative startup instrumentation.
3. Compile and run only its exact case; retain every attempt.
4. Freeze current executable inputs and reconcile master/evidence/anchors.

Protected G19 scope remains pending and its three sources stay unchanged. Sole GameClock untouched. No hosts/demos gameplay, Oil, business behavior, renderer, adapter, Draw, Unity/Godot, compatibility or duplicate owner. No staging/commit/push before complete full goal AND plan.
