using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    public static partial class AuthoredConstructionWorkflowTests
    {
        private const string ClearanceCase = "Construction clearance: pending and built facility access survives neighbouring placement";
        private static void Clearance(Check c)
        {
            foreach (bool built in new[] { false, true })
            {
                var entities = Json.Parse(FrameworkProofContent.Read("authored-construction-entities.json"));
                entities.Get("entities")[1].Get("components").Set("tile_work_dock", Json.Parse(
                    "{\"work_roles\":[\"survey\"],\"resources\":[],\"slots\":[{\"id\":\"service\",\"kind\":\"service\",\"x\":-1,\"y\":0},{\"id\":\"exit\",\"kind\":\"departure\",\"x\":-2,\"y\":0}]}"));
                var startup = Json.Parse(FrameworkProofContent.Read("authored-construction-startup.json"));
                if (built)
                {
                    startup.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("start.tower"));
                    startup.Get("entity_setups").Add(Json.Parse("{\"id\":\"start.tower\",\"after\":[\"start.builder\"],\"entity\":\"proof.tower\",\"company\":0,\"placement\":{\"space\":\"survey.tiles\",\"x\":16,\"y\":10},\"home\":null}"));
                }
                var factory = Factory(entities: entities.ToString(), startup: startup.ToString());
                var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                var site = built ? null : Build(c, game, "proof.tower");
                var twin = factory.Load(game.Save());
                foreach (var session in new[] { game, twin })
                {
                    ulong before = session.Hash(); long time = session.Clock.StepIndex;
                    foreach (int x in new[] { 15, 14 })
                    {
                        var blocked = new ConstructCommand { Def = "proof.shelter", Priority = 60,
                            Placement = TilePlacement.Arguments("survey.tiles", new TileCoord(x, 10)) };
                        c.Equal(Codes.Blocked, session.Seat(0).Preview(blocked),
                            "blocking neighbour cannot occupy " + (built ? "built" : "future") + " service/departure tile");
                    }
                    var clear = new ConstructCommand { Def = "proof.shelter", Priority = 60,
                        Placement = TilePlacement.Arguments("survey.tiles", new TileCoord(16, 11)) };
                    c.Equal(Code.None, session.Seat(0).Preview(clear), "unrelated adjacent ground remains available");
                    c.Equal(before, session.Hash(), "clearance preview reserves no extra space, stock or work");
                    c.Equal(time, session.Clock.StepIndex, "clearance preview advances no game time");
                    c.That(session.Fault == null, "ordinary placement refusal stays healthy");
                    var rows = new List<EntityView>(); session.Seat(0).View.CopyEntities(new PlayerId(0), rows);
                    c.Equal(built ? 1 : 0, rows.Count(row => row.Def == "proof.tower"), "clearance does not fabricate completed construction");
                    c.Equal(built ? 0 : 1, rows.Count(row => row.Def == "proof.tower-site"), "pending construction remains authoritative");
                }
                c.Equal(game.Hash(), twin.Hash(), "saved twin derives identical existing/future clearance");
                EntityId identity = game.World.Entities.NextIdentity;
                var refused = TestKit.Order(game, 0, new ConstructCommand { Def = "proof.shelter", Priority = 60,
                    Placement = TilePlacement.Arguments("survey.tiles", new TileCoord(15, 10)) });
                c.That(!refused.Accepted && refused.Refusal == Codes.Blocked, "actual inbox command matches clearance preview refusal");
                c.Equal(identity, game.World.Entities.NextIdentity, "refused placement allocates no site identity");
                c.That(Find(game, "proof.shelter-site") == null, "refused command creates no blocking site");
                c.That(game.Fault == null, "ordinary blocked command does not fault the engine");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "post-refusal state remains a native resumable checkpoint");
                if (!built)
                {
                    c.That(TestKit.Order(game, 0, new CancelConstructionCommand { Site = site.Id }).Accepted, "ordinary cancellation releases pending geometry");
                    var released = new ConstructCommand { Def = "proof.shelter", Priority = 60,
                        Placement = TilePlacement.Arguments("survey.tiles", new TileCoord(15, 10)) };
                    c.Equal(Code.None, game.Seat(0).Preview(released), "cancelled site leaves no ghost clearance ledger");
                    c.That(TestKit.Order(game, 0, released).Accepted, "ordinary command can use clearance after actual cancellation");
                    c.That(Find(game, "proof.shelter-site") != null, "released ground receives one real pending site, not a completed structure");
                }
                c.Equal(isolated, idle.Hash(), "another session receives no placement reservations or state");
            }
        }
    }
}
