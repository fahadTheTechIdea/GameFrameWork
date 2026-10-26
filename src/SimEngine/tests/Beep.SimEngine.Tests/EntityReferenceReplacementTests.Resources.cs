using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class EntityReferenceReplacementTests
    {
        private const string ResourceIsolationCase = "Reference commit: unrelated material and claim-sequence changes cannot escape typed rebinding";
        private const string ResourceCodecCase = "Resource observation: canonical stock claims and sequence exclude auxiliary component state";
        private const string CombinedResourceCase = "Reference resources: stock-bearing reference owner completes physical saved rebinding without false resource changes";
        private static void CombinedResources(Check c)
        {
            var factory = Factory(edit: root => {
                var entities = root.Get("entities");
                for (int i = 0; i < entities.Count; i++)
                    if (entities[i].Get("id").Text == "reference")
                        entities[i].Get("components").Set("point_stock", entities[0].Get("components").Get("point_stock").Clone());
            });
            var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            EntityId owner = Find(game, "reference").Id, source = Find(game, "source").Id;
            var stock = game.World.Entities.Get(owner).Get<IInventory>();
            Fix opening = stock.Stock[0].Amount;
            ulong Resources() { var writer = new StateHasher(); stock.WriteResourceState(writer); return writer.Value; }
            ulong resources = Resources();
            Send(c, game, new SetReplacementReferenceCommand { Owner = owner, Target = source });
            Send(c, game, new PostOrderCommand { Plan = "upgrade", Target = source });
            var saved = factory.Load(game.Save());
            c.Equal(game.Hash(), saved.Hash(), "stock-bearing reference owner and pending physical work use original save codecs");
            int steps = 0;
            while (Reference(game, owner).Commits == 0 && steps++ < 20) { game.Update(); saved.Update(); }
            var made = Find(game, "source.next");
            c.That(steps <= 20 && game.Fault == null && saved.Fault == null, "ordinary engine updates finish the real staffed replacement");
            c.That(Reference(game, owner).Reference == made.Id && Reference(game, owner).Commits == 1,
                "reference owner on the stock-bearing entity commits once through the original coordinator");
            c.Equal(opening, stock.Stock[0].Amount, "legitimate parent entity invalidation does not change its material");
            c.Equal(resources, Resources(), "resource-only state excludes legitimate reference and parent version changes");
            c.Equal(game.Hash(), saved.Hash(), "saved physical twins retain identical actual material and references");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed combined capabilities restore through original codecs");
            c.Equal(untouched, isolated.Hash(), "another session remains unchanged");
        }
        private static void ResourceCodec(Check c)
        {
            var game = New(Factory()); var source = game.World.Entities.Get(Find(game, "source").Id);
            var stock = source.Get<SingleResourcePort>(); var resource = stock.Stock[0].Resource;
            ulong Resources() { var writer = new StateHasher(); stock.WriteResourceState(writer); return writer.Value; }
            ulong ComponentState() { var writer = new StateHasher(); stock.Sync(writer); return writer.Value; }
            Fix amount = stock.Amount(resource); ulong before = Resources(), component = ComponentState();
            stock.RecordService();
            c.Equal(before, Resources(), "actual auxiliary component state is not resource material or promises");
            c.That(component != ComponentState(), "same original component codec still persists its own auxiliary state");
            var owner = new ReservationOwner("proof.resource-observation", source.Id.Value);
            var token = stock.ReserveIn(owner, resource, Fix.One);
            c.That(before != Resources(), "actual owned claim changes canonical resource state");
            stock.ReleaseIn(owner, token);
            c.That(before != Resources(), "claim sequence remains observable after a real reserve/release cycle");
            c.That(stock.Reservations.Count == 0 && stock.Amount(resource) == amount, "cycle does not fake a material or retained claim delta");
            var reader = new StateReader(Array.Empty<byte>(), 0, 0);
            c.Throws<InvalidOperationException>(() => stock.WriteResourceState(reader), "observation contract cannot be repurposed as a restore entry");
            c.Equal(0, reader.Position, "reader refusal precedes all resource payload access");
            c.Equal(game.Hash(), Factory().Load(game.Save()).Hash(), "canonical resource and auxiliary state still use original saved codecs");
        }
        private static void ResourceIsolation(Check c)
        {
            foreach (string mode in new[] { "other-stock-commit", "other-claim-commit", "other-claim-cycle-commit" })
            foreach (bool loaded in new[] { false, true })
            {
                var factory = Factory(mode: mode, edit: root => {
                    var rows = root.Get("entities"); JsonValue stock = null, later = null;
                    for (int i = 0; i < rows.Count; i++) {
                        if (rows[i].Get("id").Text == "source") stock = rows[i].Clone();
                        if (rows[i].Get("id").Text == "reference") later = rows[i].Clone();
                    }
                    stock.Set("id", JsonValue.Of("reference.stock")); rows.Add(stock);
                    StartupContentAuthoring.Add(root, "reference.stock", "reference.stock", 0, Json.Parse("{\"space\":\"field.points\",\"position\":70}"));
                    later.Set("id", JsonValue.Of("reference.later")); later.Get("components").Get("replacement_reference").Set("mode", JsonValue.Of("normal"));
                    rows.Add(later); StartupContentAuthoring.Add(root, "reference.later", "reference.later", 0, null);
                });
                var game = New(factory); EntityId sourceId = Find(game, "source").Id;
                EntityId referenceId = Find(game, "reference").Id, laterId = Find(game, "reference.later").Id, otherId = Find(game, "reference.stock").Id;
                Send(c, game, new SetReplacementReferenceCommand { Owner = referenceId, Target = sourceId });
                Send(c, game, new SetReplacementReferenceCommand { Owner = laterId, Target = sourceId });
                if (loaded) game = factory.Load(game.Save());
                var world = game.World; var source = world.Entities.Get(sourceId); var unrelated = world.Entities.Get(otherId).Get<IInventory>();
                var resource = unrelated.Stock[0].Resource; Fix opening = unrelated.Amount(resource);
                var idle = factory.Load(game.Save()); ulong isolated = idle.Hash(); long time = game.Clock.StepIndex;
                Entity made = null; Exception failed = null;
                try { Upgrades.TryReplace(world, source, world.Content.Get<EntityDef>("source.next"), out made); }
                catch (Exception error) { failed = error; }
                string context = mode + "/loaded=" + loaded;
                c.Equal(mode == "other-stock-commit" ? opening - Fix.One : opening, unrelated.Amount(resource), "negative probe performed the actual original unrelated stock delta: " + context);
                c.Equal(mode == "other-claim-commit" ? 1 : 0, unrelated.Reservations.Count, "negative probe retained or released its real unrelated claim: " + context);
                c.That(failed is InvalidOperationException && game.Fault != null && made == null, "reference-only commit rejects unrelated resource or claim-sequence changes: " + context);
                c.Equal(0, world.Entities.Get(laterId).Get<ReplacementReference>().Commits, "later owner never commits after unrelated resource violation: " + context);
                c.Equal(time, game.Clock.StepIndex, "reference commit creates no second time advancement: " + context);
                if (game.Fault == null) throw new InvalidOperationException("unrelated resource mutation escaped replacement: " + context + ", made=" + made?.Id);
                c.Throws<InvalidOperationException>(() => game.Save(), "failed reference rebinding cannot publish a resumable save: " + context);
                c.Throws<InvalidOperationException>(() => game.Hash(), "failed reference rebinding cannot certify a healthy hash: " + context);
                c.Equal(isolated, idle.Hash(), "another session retains its exact material, claims and reference state: " + context);
            }
        }
    }
}
