using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpillResourceAccountingTests
    {
        private const string SiteObservationCase="Spill site observation: actual target codecs guard direct admission and delayed commit";
        private static void SiteObservation(Check c)
        {
            foreach(bool native in new[]{false,true})
            {
                foreach(string mode in new[]{"none","refuse"})SiteContext(c,native,false,mode,true);
                foreach(bool commit in new[]{false,true})
                    foreach(string mode in new[]{"target","target-refuse","target-throw","clock","throw"})SiteContext(c,native,commit,mode,false);
                SiteContext(c,native,true,"refuse",false);
            }
        }
        private static void SiteContext(Check c,bool native,bool commit,string mode,bool healthy)
        {
            string label=(native?"native/":"independent/")+(commit?"commit/":"admission/")+mode;
            Console.WriteLine("  spill site "+label);
            var factory=IndependentTileWorkTests.Factory(nativeTiles:native,spillResources:true,
                authorSpillResources:json=>{
                    json.Remove("tile_resource_cells");
                    json.Get("entities")[1].Get("components").Set("spill_site_private_probe",Json.Parse("{}"));
                },additionalModule:new SpatialCellContentModule(),
                additionalDeclaration:"{\"id\":\"proof.spatial-cell-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\"}],\"documents\":[\"spill-site-observation.json\"]}",
                additionalDocument:FrameworkProofContent.Read("spill-site-observation.json"));
            var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            var source=game.World.Entities.Get(Id(game,"sensor.incident-source"));var target=game.World.Entities.Get(Id(game,"sensor.spill-site"));
            var raise=new RaiseIncidentCommand{Target=source.Id,Source=new Code("leak"),Kind=new Code("sensor.fire")};
            c.That(TestKit.Order(game,0,raise).Accepted,label+" starts from an actual player-raised source episode");
            var cell=game.World.Content.Get<BrokenTileCellDef>("sensor.spill-cell");
            var effect=game.World.Content.Get<SpatialResourceTransferEffectDef>("sensor.spill");
            var oil=Resource(game,"oil");var inventory=target.Get<IInventory>();var probe=target.Get<ExposurePrivateProbe>();
            var layer=((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single(x=>x.Id=="sensor.spilled-oil") as ITileSampleLayer;
            if(layer==null||probe==null)throw new InvalidOperationException("actual site/quantity/probe binding missing");
            ulong original=game.Hash();long before=game.Clock.StepIndex,published=game.Seat(0).Snapshot.Tick;
            cell.ArmSite(game.World,mode,commit);Exception actual=null;bool returned=false;Code result=Code.None;
            if(commit)
            {
                for(int i=0;i<120&&cell.SiteCalls==0&&game.Fault==null;i++)
                {before=game.Clock.StepIndex;published=game.Seat(0).Snapshot.Tick;game.Update();}
            }
            else
            {
                try{result=SpatialResourceTransferContracts.Check(game.World,effect,target,new ReservationOwner("proof.spill-site",target.Id.Value),out _);returned=true;}
                catch(Exception error){actual=error;}
            }
            c.Equal(1,cell.SiteCalls,label+" exercises the actual selected callback exactly once");
            if(cell.SiteCalls!=1)throw new InvalidOperationException(label+" did not reach actual site callback");
            c.Equal(mode.StartsWith("target",StringComparison.Ordinal)?1:0,probe.Value,label+" exact private payload diagnostic, never rollback");
            c.Equal(cell.VersionAtCall,target.Version,label+" payload instrumentation deliberately bypasses entity revision");
            c.Equal(Fix.FromInt(5),inventory.Amount(oil),label+" no material leaves inventory before predicate admission");
            c.Equal(Fix.Zero,layer.GetSample(new TileCoord(14,20)),label+" no spill quantity prefix publishes");
            c.Equal(isolated,idle.Hash(),label+" another actual session stays isolated");
            if(healthy)
            {
                c.That(returned&&actual==null&&game.Fault==null,label+" pure ready/refusal remains healthy");
                c.Equal(mode=="refuse"?new Code("proof.spill-site-refused"):Code.None,result,label+" original typed site result preserved");
                c.Equal(original,game.Hash(),label+" successful and refused pure reads alter no state");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),label+" healthy original owners remain durable");return;
            }
            c.That(game.Fault!=null&&(!returned||commit),label+" impure/failed callback cannot certify healthy execution");
            if(game.Fault==null)return; // Baseline reports actual escaped prefix; never reinterpret it as a pass.
            var first=game.Fault;
            if(mode.Contains("throw"))c.That(first.Message.Contains("proof.spill-site-first-error"),label+" preserves original provider exception");
            c.Equal(before,game.Clock.StepIndex,label+" failing callback earns no additional game time");
            c.Equal(published,game.Seat(0).Snapshot.Tick,label+" retains last healthy snapshot");
            c.Throws<InvalidOperationException>(()=>game.Save(),label+" cannot save failed state");
            c.Throws<InvalidOperationException>(()=>game.Hash(),label+" cannot certify failed state");
            c.Equal(0,game.Update(),label+" first fault stops future Update");
            c.That(ReferenceEquals(first,game.Fault),label+" first fault remains authoritative");
        }
    }
}
