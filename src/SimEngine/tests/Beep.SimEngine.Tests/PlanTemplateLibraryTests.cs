using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static List<StoredPlanTemplateView> Library(SimGame game, int seat = 0)
        { var entries = new List<StoredPlanTemplateView>(); View(game, seat).CopyStoredPlanTemplates(entries); return entries; }
        private static CommandResult StoreTemplate(SimGame game, PlanTemplate template, string name, long id = 0, long revision = 0, int seat = 0)
            => TestKit.Order(game, seat, new StorePlanTemplateCommand { Template = id, ExpectedRevision = revision, Name = name, Value = template });
        private static CommandResult RemoveTemplate(SimGame game, long id, long revision, int seat = 0)
            => TestKit.Order(game, seat, new RemovePlanTemplateCommand { Template = id, ExpectedRevision = revision });
        private static CommandResult PlaceStored(SimGame game, long id, long revision, int at, string group, int seat = 0)
            => TestKit.Order(game, seat, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Stored(id, revision), Anchor = At(game, at), Group = group });

        private static void LibraryContracts(Check c)
        {
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            PlanTemplate original = View(game).AuthoredPlanTemplate("point.pair");
            ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            c.That(View(game).PreviewStorePlanTemplate(0, 0, "field.design", original).Ready, "library pure preview uses same provider validation");
            c.Equal(hash, game.Hash(), "storage preview advances no clock, job, stock or record");
            c.Equal(time, game.Clock.StepIndex, "library has no independent time");
            CommandResult created = StoreTemplate(game, original, "field.design");
            c.That(created.Accepted && created.Arg0 > 0 && created.Arg1 == 1, "create returns actual private ID and first revision");
            long id = created.Arg0; StoredPlanTemplateView retained = Library(game)[0];
            c.That(retained.Id == id && retained.Name == "field.design" && retained.Revision == 1, "immutable private library projection");
            c.Equal(original.ToJson().ToString(), retained.Template.ToJson().ToString(), "same JSON template value, not copied live execution");
            c.Throws<InvalidOperationException>(() => retained.Template.Entries[0].Arguments.Set("stock", JsonValue.Number("99")), "stored nested intent remains frozen");
            c.Equal(0, TemplatePlans(game).Count, "saving a design never creates plans or sites");
            c.Equal(0, game.World.Services.Get<WorkBoard>().Orders.Count, "saving a design never creates native execution");
            c.Equal(0, Library(game, 1).Count, "another company's library listing leaks no private designs");
            c.Equal(Codes.NotFound, View(game, 1).ReadStoredPlanTemplate(id, out StoredPlanTemplateView hidden), "private lookup returns no ownership oracle");
            c.That(hidden == null, "private lookup emits no template payload");
            c.Equal(Codes.NotFound, StoreTemplate(game, original, "stolen", id, 1, 1).Refusal, "foreign revision cannot update design");
            c.Equal(Codes.NotFound, RemoveTemplate(game, id, 1, 1).Refusal, "foreign remove refuses");
            c.Equal(Codes.NotFound, PlaceStored(game, id, 1, 60, "stolen", 1).Refusal, "foreign stored selection cannot place another design");
            c.That(StoreTemplate(game, original, "field.design", seat: 1).Accepted, "same name allowed in distinct private libraries");
            c.Equal(PlanCodes.TemplateNameConflict, StoreTemplate(game, original, "field.design").Refusal, "duplicate company name refuses without overwrite");
            foreach (string name in new[] { null, "", "bad name" })
                c.Equal(Codes.InvalidArgument, View(game).PreviewStorePlanTemplate(0, 0, name, original).Blocker, "explicit stable private name required");
            c.Equal(Codes.InvalidArgument, View(game).PreviewStorePlanTemplate(0, 1, "invalid", original).Blocker, "creation never silently updates a nonexistent revision");
            c.Equal(Codes.InvalidArgument, View(game).PreviewStorePlanTemplate(0, 0, "invalid", null).Blocker, "no fabricated template default");
            c.Equal(Codes.InvalidArgument, View(game).PreviewStorePlanTemplate(0, 0, "invalid", new PlanTemplate("other.space", original.Entries)).Blocker, "selected space mismatch refuses");
            var bad = new PlanTemplate(original.Space, new[] { original.Entries[0], new PlanTemplateEntry("late", "build:missing", original.Entries[1].Arguments, 1, true) });
            hash = game.Hash(); var badPreview = View(game).PreviewStorePlanTemplate(id, 1, "renamed", bad);
            c.That(badPreview.Blocker == Codes.UnknownDef && badPreview.Entries.Count == 2 && badPreview.Entries[0].Blocker.IsNone,
                "late missing provider returns per-entry diagnostics before entire library mutation");
            c.Equal(hash, game.Hash(), "late storage refusal preview leaves actual record unchanged");
            c.Equal(badPreview.Blocker, StoreTemplate(game, bad, "renamed", id, 1).Refusal, "storage preview and command refusal agree");
            c.That(Library(game)[0].Name == "field.design" && Library(game)[0].Revision == 1, "late refusal never commits rename or prefix");
            var single = new PlanTemplate(original.Space, new[] { original.Entries[0] });
            CommandResult updated = StoreTemplate(game, single, "renamed", id, 1);
            c.That(updated.Accepted && updated.Arg0 == id && updated.Arg1 == 2, "atomic update preserves actual ID and advances revision");
            c.That(retained.Name == "field.design" && retained.Revision == 1 && retained.Template.Entries.Count == 2, "retained detached view does not mutate with record");
            c.Equal(PlanCodes.TemplateRevisionConflict, StoreTemplate(game, original, "stale", id, 1).Refusal, "stale update never overwrites current revision");
            c.Equal(PlanCodes.TemplateRevisionConflict, View(game).PreviewRemovePlanTemplate(id, 1), "stale remove preview has typed blocker");
            c.Equal(PlanCodes.TemplateRevisionConflict, RemoveTemplate(game, id, 1).Refusal, "stale remove command parity");
            c.Equal(PlanCodes.TemplateRevisionConflict, View(game).PreviewPlanTemplateSelection(PlanTemplateSelection.Stored(id, 1), At(game, 12), "stale").Blocker,
                "placement preview resolves exact current revision inside engine");
            c.Equal(PlanCodes.TemplateRevisionConflict, PlaceStored(game, id, 1, 12, "stale").Refusal, "stale placement command commits no plans");
            hash = game.Hash(); c.That(View(game).PreviewPlanTemplateSelection(PlanTemplateSelection.Stored(id, 2), At(game, 12), "valid").Ready, "current selection uses existing whole placement preview");
            c.Equal(hash, game.Hash(), "selected placement preview is pure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "private identities/revisions/designs save through one board codec");
            foreach (PlanTemplateSelection selection in new[] { PlanTemplateSelection.Snapshot(original), PlanTemplateSelection.Stored(id, 2) })
            {
                var command = new PlacePlanTemplateCommand { Selection = selection, Anchor = At(game, 12), Group = "wire" };
                byte[] wire = DependencyWire(command); var round = new PlacePlanTemplateCommand(); round.Sync(new StateReader(wire, 0, wire.Length));
                c.Equal(Convert.ToBase64String(wire), Convert.ToBase64String(DependencyWire(round)), "one explicit selection wire round trip");
            }
            var storeWire = new StorePlanTemplateCommand { Template = id, ExpectedRevision = 2, Name = "wire", Value = original };
            byte[] bytes = DependencyWire(storeWire); var decoded = new StorePlanTemplateCommand(); decoded.Sync(new StateReader(bytes, 0, bytes.Length));
            c.Equal(Convert.ToBase64String(bytes), Convert.ToBase64String(DependencyWire(decoded)), "private editing wire round trip shares same template codec");
            c.Throws<ArgumentException>(() => PlanTemplateSelection.Stored(0, 1), "stored selection requires actual ID");
            c.Throws<ArgumentException>(() => PlanTemplateSelection.Stored(1, 0), "stored selection requires explicit positive revision");
            c.Throws<ArgumentNullException>(() => PlanTemplateSelection.Snapshot(null), "snapshot source requires explicit immutable value");
            BadTemplateSelection(c, 99, 1, 1); BadTemplateSelection(c, 2, 0, 1); BadTemplateSelection(c, 2, 1, 0);
            LibraryCorruption(c, game);
            c.That(RemoveTemplate(game, id, 2).Accepted, "player removes current private design");
            c.Equal(Codes.NotFound, View(game).ReadStoredPlanTemplate(id, out _), "deleted design disappears from immutable lookup");
            CommandResult replacement = StoreTemplate(game, original, "renamed");
            c.That(replacement.Accepted && replacement.Arg0 > id && replacement.Arg1 == 1, "same-name recreation never reuses retired ID");
            c.Equal(Codes.NotFound, PlaceStored(game, id, 2, 12, "retired").Refusal, "retired selection never selects a same-name replacement");
            LibraryQueuedRevision(c);
            GameFactory tileFactory = Colony.Factory(); SimGame tile = ResourceDispositionTests.New(tileFactory);
            JsonValue arguments = JsonValue.NewObject(); arguments.Set("placement", Content.Json.Parse("{\"space\":\"" + tile.World.Space.Id + "\",\"dx\":0,\"dy\":0}"));
            var tileDesign = new PlanTemplate(tile.World.Space.Id, new[] { new PlanTemplateEntry("tower", "build:tower", arguments, 1, true) });
            CommandResult native = StoreTemplate(tile, tileDesign, "native");
            c.That(native.Accepted && PlaceStored(tile, native.Arg0, 1, 10, "native").Accepted, "same library supports native tile and independent point placement contracts");
        }
        private static void BadTemplateSelection(Check c, byte kind, long id, long revision)
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); bool present = true; writer.Bool(ref present);
                var selected = (PlanTemplateSelectionKind)kind; writer.Enum(ref selected); writer.Long(ref id); writer.Long(ref revision);
                writer.Flush(); bytes = stream.ToArray();
            }
            c.Throws<CorruptStateException>(() => new PlacePlanTemplateCommand().Sync(new StateReader(bytes, 0, bytes.Length)), "corrupted selection kind/identity/revision refuses");
        }
        private static void LibraryCorruption(Check c, SimGame game)
        {
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            var entries = (IEnumerable)typeof(WorkBoard).GetField("_storedPlanTemplates", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
            foreach (object entry in entries)
            {
                foreach (var change in new[] { ("Id", (object)0L), ("Revision", (object)0L), ("Name", (object)"bad name") })
                {
                    FieldInfo field = entry.GetType().GetField(change.Item1, BindingFlags.Instance | BindingFlags.NonPublic); object old = field.GetValue(entry);
                    field.SetValue(entry, change.Item2);
                    try { c.Throws<CorruptStateException>(() => game.Save(), "sole library codec rejects invalid saved " + change.Item1); }
                    finally { field.SetValue(entry, old); }
                }
                break;
            }
        }
        private static void LibraryQueuedRevision(Check c)
        {
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory); PlanTemplate design = View(game).AuthoredPlanTemplate("point.pair");
            long id = StoreTemplate(game, design, "queued").Arg0;
            game.Seat(0).Submit(new StorePlanTemplateCommand { Template = id, ExpectedRevision = 1, Name = "queued", Value = design });
            game.Seat(0).Submit(new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Stored(id, 1), Anchor = At(game, 12), Group = "stale.inbox" });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Library(game)[0].Revision == 2 && Group(game, "stale.inbox").Count == 0, "queued stale selection refuses after earlier design update, not host selection time");
            c.Equal(game.Hash(), twin.Hash(), "saved private editing/selection inbox has identical command ordering");
            game.Seat(0).Submit(new RemovePlanTemplateCommand { Template = id, ExpectedRevision = 2 });
            game.Seat(0).Submit(new StorePlanTemplateCommand { Name = "queued", Value = design });
            game.Seat(0).Submit(new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Stored(id, 2), Anchor = At(game, 12), Group = "retired.inbox" });
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Library(game)[0].Id > id && Group(game, "retired.inbox").Count == 0, "queued retired selection cannot be rebound after delete/recreate");
            c.Equal(game.Hash(), twin.Hash(), "monotonic private counter survives saved delete/recreate inbox");
        }

        private static void LibraryPhysical(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            PlanTemplate design = View(game).AuthoredPlanTemplate("dependency.diamond"); long id = StoreTemplate(game, design, "physical").Arg0;
            game.Seat(0).Submit(new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Stored(id, 1), Anchor = At(game, 12), Group = "library.field" });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Group(game, "library.field").Count == 4 && TemplateCount(game, "point.tower-site") == 0, "stored selection creates real intent only while paused root blocks physical frontier");
            c.Equal(game.Hash(), twin.Hash(), "stored selection and private library survive saved inbox");
            foreach (SimGame current in new[] { game, twin })
            {
                c.That(View(current).CapturePlanTemplate("library.field", At(current, 12), out PlanTemplate captured).Ready, "player captures actual group into same portable library format");
                c.That(StoreTemplate(current, captured, "captured", id, 1).Accepted, "same owned library revises captured real graph intent");
                c.That(RemoveTemplate(current, id, 2).Accepted, "removing blueprint never deletes its submitted graph");
                EmergencyPreemptionTests.Configure(c, current); DependencyEdit(c, current, "library.field", false);
            }
            c.That(Group(game, "library.field")[3].State == PlanState.Active && Group(game, "library.field")[0].Order == 0,
                "native promotion starts only actual dependency root after source revision/removal");
            EditingReach(game, () => game.World.Services.Get<WorkBoard>().Order(Group(game, "library.field")[3].Order)?.Node("build").Progress.WholeTicks > 0,
                "library physical partial build", twin);
            c.Equal(game.Hash(), twin.Hash(), "interleaved stored-source physical work is deterministic");
            twin = factory.Load(game.Save()); ulong hash = game.Hash(); long time = game.Clock.StepIndex; game.Clock.Pause();
            for (int i = 0; i < 4; i++) game.Update();
            c.Equal(hash, game.Hash(), "one clock pause freezes real library construction and stock");
            c.Equal(time, game.Clock.StepIndex, "library has no auxiliary time advancement"); game.Clock.Resume();
            EditingReach(game, () => Group(game, "library.field").TrueForAll(p => p.State == PlanState.Done), "library dependency field completion", twin);
            c.Equal(4, TemplateCount(game, "point.tower"), "all saved design entries become real physical structures");
            c.Equal(Fix.FromInt(4), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "four builds consume sixteen finite units over game time");
            c.Equal(game.Hash(), twin.Hash(), "partial saved continuation matches native completed field");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "deleted source does not invalidate saved completed plan history");
            SimGame cancelled = ConstructionPlanTests.NewPoints(factory); id = StoreTemplate(cancelled, View(cancelled).AuthoredPlanTemplate("dependency.diamond"), "cancel").Arg0;
            CommandResult placed = PlaceStored(cancelled, id, 1, 12, "cancel");
            c.That(placed.Accepted && TestKit.Order(cancelled, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { placed.Arg0 + 3 }) }).Accepted,
                "same ordinary cancellation owns real prerequisite from stored design");
            c.That(RemoveTemplate(cancelled, id, 1).Accepted, "source removal never erases cancelled dependency history");
            c.That(Group(cancelled, "cancel")[1].Blocker == PlanCodes.DependencyCancelled && TemplateCount(cancelled, "point.tower-site") == 0,
                "dependent stored design work never pretends cancelled prerequisite completed");
            c.Equal(cancelled.Hash(), factory.Load(cancelled.Save()).Hash(), "cancelled source-independent graph has one durable owner");
        }
    }
}
