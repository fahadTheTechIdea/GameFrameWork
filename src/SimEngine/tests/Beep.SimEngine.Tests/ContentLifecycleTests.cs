using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class ContentLifecycleTests
    {
        internal static void Register()
        {
            TestRunner.Add("Content lifecycle: single authoring owner, publication and authored metadata", Contracts);
            TestRunner.Add("Content lifecycle: shared prototypes, physical sessions and saved continuation", Physical);
        }
        private static JsonReader Reader() => new JsonReader(Json.Parse("{\"value\":17}"), "lifecycle probe");
        private static SimGame New(GameFactory factory, bool probe = false)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 607 };
            request.Mods.Add(probe ? "proof.lifecycle" : "proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static GameFactory ProbeFactory(Action<ContentTypes> declarations = null)
        {
            var module = new ProbeModule("proof.lifecycle", new ModuleVersion(1, 0, 0), content: types =>
            {
                types.Section<LifecycleProbeDef>("lifecycle_defs");
                types.Component<LifecycleProbeComponentDef>("lifecycle_probe", 1);
                declarations?.Invoke(types);
            });
            var definitions = Json.Parse(PointGame.Definitions);
            definitions.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var text = new System.Text.StringBuilder(); definitions.WriteCanonical(text);
            return PointGame.Factory(definitions: text.ToString(), additionalModule: module,
                moduleDeclaration: "{\"id\":\"proof.lifecycle\",\"version\":\"1.0.0\",\"documents\":[\"lifecycle.json\"]}",
                moduleDocument: "{\"lifecycle_defs\":[{\"id\":\"authored.probe\",\"value\":17}],\"entities\":[{\"id\":\"authored.machine\",\"kind\":\"entity\",\"components\":{\"lifecycle_probe\":{\"value\":17}}},{\"id\":\"authored.probe-worksite\",\"kind\":\"entity\",\"tags\":[\"authored\"],\"components\":{\"lifecycle_probe\":{\"value\":23}}}]}" );
        }
        // Reflection only inventories existing typed catalog aliases; it creates no registry or gameplay.
        private static HashSet<Def> Definitions(ResolvedContent content)
        {
            var result = new HashSet<Def>();
            var method = typeof(ResolvedContent).GetMethod(nameof(ResolvedContent.All));
            foreach (var assembly in new[] { typeof(Def).Assembly, typeof(LifecycleProbeDef).Assembly })
                foreach (var type in assembly.GetTypes())
                    if (type != typeof(Def) && !type.ContainsGenericParameters && typeof(Def).IsAssignableFrom(type))
                        foreach (Def definition in (IEnumerable)method.MakeGenericMethod(type).Invoke(content, null)) result.Add(definition);
            return result;
        }
        private static void Frozen(Check c, ResolvedContent content)
        {
            var defs = Definitions(content); c.That(defs.Count > 20, "actual master-selected catalog contains core and extension content");
            foreach (Def definition in defs)
            {
                var resolver = new ContentResolver(content, definition);
                c.That(definition.IsFrozen, "published definition is frozen: " + definition);
                c.Throws<ContentException>(() => definition.Read(Reader()), "published reader cannot run again: " + definition);
                c.Throws<ContentException>(() => definition.Resolve(resolver), "published resolver cannot run again: " + definition);
                c.Throws<ContentException>(() => definition.ValidateResolved(resolver), "published linked validator cannot run again: " + definition);
                if (definition is EntityDef entity)
                    foreach (ComponentDef part in entity.Components)
                    {
                        c.That(part.IsFrozen && ReferenceEquals(part.Owner, entity), "published component has frozen actual ownership");
                        c.Throws<ContentException>(() => part.Read(Reader()), "published component read refuses");
                        c.Throws<ContentException>(() => part.Resolve(resolver), "published component resolution refuses");
                        c.Throws<ContentException>(() => part.Validate(entity), "published component validation refuses");
                    }
            }
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "content lifecycle has explicit current identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old save reader remains");
            foreach (var type in new[] { typeof(Def), typeof(ComponentDef) })
                foreach (string method in new[] { "Read", "Resolve", type == typeof(Def) ? "ValidateResolved" : "Validate" })
                    c.That(!type.GetMethod(method).IsVirtual, "one engine entry owns authoring permissions: " + type.Name + "." + method);
            var factory = ProbeFactory(); var game = New(factory, true); var content = game.World.Content;
            ulong hash = game.Hash(); Frozen(c, content); c.Equal(hash, game.Hash(), "frozen authoring refusals do not alter live session");
            var authored = content.Get<LifecycleProbeDef>("authored.probe");
            c.Equal(1, authored.Reads, "external authored reader runs once"); c.Equal(1, authored.Resolves, "external resolution runs once");
            c.Equal(1, authored.Validations, "external linked validation runs once"); c.Equal(17, authored.Value, "authored value remains intact");
            foreach (string id in new[] { "authored.machine", "authored.probe-worksite" })
            {
                var entity = content.Get<EntityDef>(id); var part = entity.Component<LifecycleProbeComponentDef>();
                c.Equal(1, part.Reads, "authored part is read exactly once"); c.Equal(1, part.Resolves, "authored part uses ordinary resolution");
                c.Equal(1, part.Validations, "authored part uses ordinary sibling validation");
                c.Equal(id == "authored.probe-worksite" ? 23 : 17, part.Value, "one authored value survives publication");
                c.Equal("lifecycle_probe", part.Key, "authored metadata uses actual declared codec");
                c.That(part.StateCodec != null && part.StateCodec.SchemaVersion == 1, "authored metadata binds captured state identity");
                c.Throws<ContentException>(() => ContentAttachmentProbe.Attach("steal", "proof.lifecycle", EntityKind.Generic, "proof", "proof", null, part), "published component cannot be attached to another entity");
                c.That(ReferenceEquals(entity, part.Owner), "failed attachment cannot steal prior ownership");
                c.Throws<ContentException>(() => entity.SetComponents(Array.Empty<ComponentDef>()), "frozen entity layout cannot be replaced");
                c.Throws<ContentException>(() => { part.Key = "other"; }, "frozen component key cannot change");
                c.Throws<ContentException>(() => { part.Slot = 100; }, "frozen slot cannot change");
                c.Throws<ContentException>(() => { part.Owner = null; }, "frozen ownership cannot change");
                c.Throws<ContentException>(() => { part.StateCodec = null; }, "frozen codec cannot change");
                c.Throws<ContentException>(() => { part.OperationSchema = null; }, "frozen operation metadata cannot change");
                c.Throws<ContentException>(() => { part.SignalSchema = null; }, "frozen signal metadata cannot change");
            }
            c.Throws<ContentException>(() => { authored.Id = "other"; }, "frozen definition identity cannot change");
            c.Throws<ContentException>(() => { authored.Module = "other"; }, "frozen ownership cannot change");
            c.Throws<ContentException>(() => { authored.Index = 100; }, "frozen table index cannot change");
            c.Throws<ContentException>(() => { authored.AuthoringPath = "other"; }, "frozen provenance cannot change");
            c.Equal(hash, game.Hash(), "metadata mutation refusals leave gameplay state unchanged");
            Failures(c, content);
            var unowned = LifecycleProbeComponentDef.ReadProbe();
            c.Throws<ContentException>(() => ContentAttachmentProbe.Attach("duplicate", "proof.lifecycle", EntityKind.Generic, "proof", "proof", null, unowned, unowned), "duplicate component attachment refuses before ownership changes");
            c.That(unowned.Owner == null, "duplicate preflight retains unowned component");
            // Negative internal admission cannot reuse published content in another catalog.
            var otherCatalog = new ResolvedContent(new[] { "proof.lifecycle" }, 0);
            c.Throws<ContentException>(() => otherCatalog.Add(content.Get<EntityDef>("authored.probe-worksite")), "published authored content cannot join another catalog");
            c.Equal(hash, game.Hash(), "rejected cross-catalog admission cannot alter original session");
            var areas = New(IncidentPreparednessTests.Fixture()); Area(c, areas); NativeAreas(c);
        }
        private static void Failures(Check c, ResolvedContent content)
        {
            var malformed = new LifecycleProbeDef();
            c.Throws<ContentException>(() => malformed.Read(new JsonReader(Json.Parse("{\"value\":17,\"unknown\":1}"), "strict lifecycle")), "strict whole-reader validation belongs to authoring lifecycle");
            c.Throws<ContentException>(() => malformed.Resolve(new ContentResolver(content, malformed)), "unknown-key failure cannot bypass authored validation");
            var malformedPart = new LifecycleProbeComponentDef();
            c.Throws<ContentException>(() => malformedPart.Read(new JsonReader(Json.Parse("{\"value\":17,\"unknown\":1}"), "strict component")), "strict component reader invalidates failed authoring");
            c.Throws<ContentException>(() => malformedPart.Resolve(new ContentResolver(content, malformed)), "failed component cannot skip JSON validation on retry");
            var wrongOwner = LifecycleProbeComponentDef.ReadProbe();
            var trueOwner = ContentAttachmentProbe.Attach("true.owner", "proof.lifecycle", EntityKind.Generic, "proof", "proof", null, wrongOwner);
            wrongOwner.Resolve(new ContentResolver(content, trueOwner));
            c.Throws<ContentException>(() => wrongOwner.Validate(null), "invalid sibling validation refuses inside lifecycle");
            c.Throws<ContentException>(() => wrongOwner.Validate(trueOwner), "failed sibling validation cannot retry with a different owner");
            c.Throws<ContentException>(() => wrongOwner.Freeze(), "failed sibling validation cannot publish");
            for (int phase = 0; phase < 3; phase++) foreach (bool swallow in new[] { false, true })
            {
                var def = new LifecycleProbeDef(); var resolver = new ContentResolver(content, def);
                Action invalid = () => { if (swallow) { try { def.Read(Reader()); } catch (ContentException) { } } else throw new ContentException("injected authoring fault"); };
                if (phase == 0) def.OnRead = invalid;
                else { def.Read(Reader()); if (phase == 1) def.OnResolve = invalid; else { def.Resolve(resolver); def.OnValidate = invalid; } }
                c.Throws<ContentException>(() => { if (phase == 0) def.Read(Reader()); else if (phase == 1) def.Resolve(resolver); else def.ValidateResolved(resolver); }, "throw/swallowed recursive def authoring poisons outer lifecycle");
                def.OnRead = def.OnResolve = def.OnValidate = null;
                int calls = def.Reads + def.Resolves + def.Validations;
                c.Throws<ContentException>(() => def.Read(Reader()), "failed definition cannot be reauthored");
                c.Throws<ContentException>(() => def.Resolve(resolver), "failed definition cannot be resolved");
                c.Throws<ContentException>(() => def.ValidateResolved(resolver), "failed definition cannot be validated");
                c.Throws<ContentException>(() => def.Freeze(), "failed definition cannot be published");
                c.Throws<ContentException>(() => { def.Id = "retry"; }, "failed metadata cannot be rewritten");
                c.Equal(calls, def.Reads + def.Resolves + def.Validations, "failed callbacks never run again");

                var part = new LifecycleProbeComponentDef();
                var entity = ContentAttachmentProbe.Attach("fresh", "proof.lifecycle", EntityKind.Generic, "proof", "proof", null, part);
                resolver = new ContentResolver(content, entity);
                invalid = () => { if (swallow) { try { part.Resolve(resolver); } catch (ContentException) { } } else throw new ContentException("injected component fault"); };
                if (phase == 0) part.OnRead = invalid;
                else { part.Read(Reader()); if (phase == 1) part.OnResolve = invalid; else { part.Resolve(resolver); part.OnValidate = invalid; } }
                c.Throws<ContentException>(() => { if (phase == 0) part.Read(Reader()); else if (phase == 1) part.Resolve(resolver); else part.Validate(entity); }, "throw/swallowed recursive component authoring poisons lifecycle");
                part.OnRead = part.OnResolve = part.OnValidate = null; calls = part.Reads + part.Resolves + part.Validations;
                c.Throws<ContentException>(() => part.Read(Reader()), "failed component cannot be reauthored");
                c.Throws<ContentException>(() => part.Resolve(resolver), "failed component cannot be resolved");
                c.Throws<ContentException>(() => part.Validate(entity), "failed component cannot be validated");
                c.Throws<ContentException>(() => part.Freeze(), "failed component cannot be published");
                c.Equal(calls, part.Reads + part.Resolves + part.Validations, "failed component callbacks never run again");
            }
            var runtimeMetadata = new LifecycleProbeDef(); var runtimeResolver = new ContentResolver(content, runtimeMetadata);
            c.Throws<ContentException>(() => runtimeMetadata.Freeze(), "unresolved runtime metadata cannot publish");
            c.Throws<ContentException>(() => runtimeMetadata.ValidateResolved(runtimeResolver), "unresolved metadata cannot validate");
            runtimeMetadata.Resolve(runtimeResolver); runtimeMetadata.ValidateResolved(runtimeResolver); runtimeMetadata.Freeze();
            c.That(runtimeMetadata.IsFrozen && runtimeMetadata.Reads == 0 && runtimeMetadata.Resolves == 1, "noncatalog runtime metadata follows original resolve/validate/freeze lifecycle without fabricated JSON");
        }
        private static void Area(Check c, SimGame game)
        {
            var owner = game.World.Services.Get<IOperatingAreas>(); var company = new PlayerId(0);
            var original = (ISpatialRegionGeometry)owner.Resolve(company, "reserve.home"); var prototype = game.World.Content.Get<ISpatialRegionDefinition>("reserve.home");
            c.That(original is Def d && d.IsFrozen, "initial runtime geometry freezes before observation");
            string prior = original.ShapeJson;
            var command = new SetOperatingAreaCommand { Name = "player.lifecycle", Template = "reserve.home", ExpectedRevision = -1, ShapeJson = "{\"minimum\":0,\"maximum\":4}" };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "area preview uses same frozen prototype authoring");
            c.Equal(hash, game.Hash(), "geometry preview does not alter catalog or session");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary command creates engine-owned shape");
            var shape = (Def)owner.Resolve(company, command.Name);
            c.That(shape.IsFrozen && !ReferenceEquals(shape, prototype), "new geometry uses same lifecycle, not shared prototype mutation");
            c.Throws<ContentException>(() => shape.Read(Reader()), "runtime shape cannot reauthor");
            c.Throws<ContentException>(() => shape.Resolve(new ContentResolver(game.World.Content, shape)), "runtime shape cannot re-resolve");
            c.Equal(prior, original.ShapeJson, "retained original geometry remains immutable");
            c.That(owner.Resolve(new PlayerId(1), command.Name) == null, "runtime area remains company-private");
            foreach (string malformed in new[] { "{\"minimum\":0,\"maximum\":4,\"space\":\"other\"}", "{\"minimum\":0,\"maximum\":4,\"id\":\"overwrite\"}", "{\"minimum\":0,\"maximum\":4,\"unknown\":1}" })
                c.Equal(OperatingAreaCodes.InvalidShape, game.Seat(0).Preview(new SetOperatingAreaCommand { Name = "invalid.lifecycle", Template = "reserve.home", ExpectedRevision = -1, ShapeJson = malformed }), "shape cannot replace identity/space or hide unsupported authoring");
        }
        private static void Step(SimGame game, SimGame twin)
        {
            game.Update(); twin.Update();
            if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("content lifecycle physical fault: " + game.Fault + "/" + twin.Fault);
        }
        private static void NativeAreas(Check c)
        {
            var factory = RouteOperatingAreaTests.Tiles();
            var game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 607 });
            Frozen(c, game.World.Content);
            var areas = game.World.Services.Get<IOperatingAreas>();
            foreach (var entry in new[] {
                new[] { "route.all", "player.rectangle", "{\"x\":16,\"y\":16,\"width\":8,\"height\":8}" },
                new[] { "route.detour", "player.paint", "{\"tiles\":[{\"x\":16,\"y\":16},{\"x\":17,\"y\":16}]}" } })
            {
                var initial = (ISpatialRegionGeometry)areas.Resolve(new PlayerId(0), entry[0]);
                string original = initial.ShapeJson;
                c.That(((Def)initial).IsFrozen, "native runtime prototype geometry is frozen");
                var command = new SetOperatingAreaCommand { Name = entry[1], Template = entry[0], ExpectedRevision = -1, ShapeJson = entry[2] };
                ulong hash = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(command), "native shape preview shares original provider validation");
                c.Equal(hash, game.Hash(), "native preview has no publication side effects");
                c.That(TestKit.Order(game, 0, command).Accepted, "player authors native rectangular/painted geometry");
                var geometry = (Def)areas.Resolve(new PlayerId(0), entry[1]);
                c.That(geometry.IsFrozen, "native shape freezes through shared authoring lifecycle");
                c.Throws<ContentException>(() => geometry.Read(Reader()), "native runtime shape cannot be reauthored");
                c.Equal(original, initial.ShapeJson, "native original remains unchanged");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native runtime shapes save/restore deterministically");
                c.That(((Def)twin.World.Services.Get<IOperatingAreas>().Resolve(new PlayerId(0), entry[1])).IsFrozen, "native restored shape is frozen");
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool native in new[] { false, true }) foreach (bool policy in new[] { false, true }) foreach (bool worker in new[] { false, true })
            {
                var factory = IncidentPreparednessTests.Fixture(native, policy, worker);
                var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
                c.That(ReferenceEquals(game.World.Content, isolated.World.Content), "sessions share one frozen resolved catalog");
                ulong fingerprint = game.World.Content.Fingerprint; Area(c, game);
                Order order = Raise(c, game); c.That(order.State != OrderState.Done, "authored activity requires physical game time");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual response claims and runtime shapes restore");
                var content = game.World.Content;
                c.That(ReferenceEquals(content, twin.World.Content), "load shares published content instead of reauthoring");
                long time = game.Clock.StepIndex; ulong paused = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(time, game.Clock.StepIndex, "one paused game clock prevents workflow progress"); c.Equal(paused, game.Hash(), "pause cannot mutate shared definitions or physical claims");
                game.Clock.Resume(); twin.Clock.Resume();
                for (int i = 0; i < 300 && order.State != OrderState.Done; i++) Step(game, twin);
                if (order.State != OrderState.Done)
                    throw new InvalidOperationException("response timeout at game update " + game.Clock.StepIndex + ", order=" + order.State + ", blocker=" + order.Blocker);
                c.Equal(Fix.FromInt(4), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "physical machine work consumes four real inputs once");
                c.That(game.Clock.StepIndex > time, "movement and work complete only through sole game clock");
                c.Equal(game.Hash(), twin.Hash(), "saved physical sessions remain deterministic");
                c.Equal(untouched, isolated.Hash(), "another session sharing prototypes remains untouched");
                c.Equal(fingerprint, content.Fingerprint, "updates and geometry authoring never change shared catalog fingerprint");
                c.That(ReferenceEquals(game.Clock, game.World.Clock) && !ReferenceEquals(game.Clock, isolated.Clock), "exactly one clock per session, no process-global time");
                c.That(((Def)twin.World.Services.Get<IOperatingAreas>().Resolve(new PlayerId(0), "player.lifecycle")).IsFrozen, "restored runtime shape follows publication lifecycle");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed response checkpoints keep same current state");
            }
        }
    }
}
