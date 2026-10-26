using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpillResourceAccountingTests
    {
        internal const string PublicSamplesCase="Spill public samples: native and independent fixed query authorization and saved identity";
        internal const string PublicSampleFaultCase="Spill public samples: provider failure withholds prefix and preserves original fault";
        internal const string NativeUnfinishedCase="Spill disposal protocol: native unfinished effect cannot bypass earned machine time";
        internal const string IndependentUnfinishedCase="Spill disposal protocol: independent unfinished effect cannot bypass earned machine time";
        private static ISpatialLayerQuery<TileRect,Fix> Samples(SimGame game,int seat=0)
            =>game.Seat(seat).View.Feature<ISpatialLayerQuery<TileRect,Fix>>()
                ??throw new InvalidOperationException("actual selected fixed sample query is absent");
        private static void PublicSamples(Check c)
        {
            foreach(bool native in new[]{false,true}){
                var factory=IndependentTileWorkTests.Factory(nativeTiles:native,spillResources:true);var game=New(factory);
                var query=Samples(game);ulong hash=game.Hash();var rows=new List<Fix>{Fix.One};
                c.Equal(SpatialLayerReadStatus.Success,query.ReadLayer("sensor.spilled-oil",new TileRect(14,20,1,1),rows),"owner reads actual private layer through public contract");
                c.That(rows.Count==1&&rows[0].IsZero,"unallocated actual quantity is zero, not absent");
                c.That(ReferenceEquals(query,Samples(game)),"one contract read model per viewer");
                c.Equal(1,game.Seat(0).View.FeatureRegistrations.Count(r=>r.Contract==typeof(ISpatialLayerQuery<TileRect,Fix>)),"one explicit fixed query registration");
                rows.Add(Fix.One);c.Equal(SpatialLayerReadStatus.Unavailable,Samples(game,1).ReadLayer("sensor.spilled-oil",new TileRect(14,20,1,1),rows),"foreign private layer unavailable");
                c.Equal(0,rows.Count,"private refusal clears caller values");
                rows.Add(Fix.One);c.Equal(SpatialLayerReadStatus.Unavailable,query.ReadLayer("missing",new TileRect(-1,0,1,1),rows),"missing identity cannot leak geometry distinction");
                c.Equal(0,rows.Count,"missing refusal clears caller values");
                c.Equal(SpatialLayerReadStatus.InvalidRegion,query.ReadLayer("sensor.spilled-oil",new TileRect(-1,0,1,1),rows),"owned invalid geometry refuses");
                c.Equal(SpatialLayerReadStatus.ValueTypeMismatch,query.ReadLayer("sensor.screen",new TileRect(0,0,1,1),rows),"byte layer is not silently reinterpreted as a fixed layer");
                c.Equal(SpatialLayerReadStatus.Success,query.ReadLayer("sensor.spilled-oil",new TileRect(0,0,0,1),rows),"empty valid region is an empty success");
                c.Equal(0,rows.Count,"empty query returns no fabricated sample");
                c.Throws<ArgumentNullException>(()=>query.ReadLayer("missing",new TileRect(-1,0,1,1),null),"healthy caller null buffer remains an argument refusal");
                c.Equal(hash,game.Hash(),"all public sample reads are pure");
                var twin=factory.Load(game.Save());c.Equal(hash,twin.Hash(),"new query registration does not create stock or another saved owner");
                c.That(!ReferenceEquals(query,Samples(twin)),"saved world receives its own viewer query");
                c.Equal(SpatialLayerReadStatus.Success,Samples(twin).ReadLayer("sensor.spilled-oil",new TileRect(14,20,1,1),rows),"restored interface binding remains usable");
                c.That(rows.Count==1&&rows[0].IsZero,"restored selected provider sample remains exact");
                if(native){
                    var map=game.Seat(0).View.Feature<TileMapView>();
                    c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("sensor.spilled-oil",new TileRect(14,20,1,1),rows),"native facade delegates to same fixed contract");
                    c.Equal(hash,game.Hash(),"native facade introduces no second sample state");
                }
            }
        }
        // Explicit white-box malformed provider probes, never ordinary player logic.
        private static void PublicSampleFaults(Check c)
        {
            foreach(bool mutate in new[]{false,true}){
                var factory=IndependentTileWorkTests.Factory(spillResources:true);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var query=Samples(game);var layer=(SensorSampleLayer)TileSpatialGeometry.RequireSampleLayer(game.World,
                    (ITileSpatialSpace)game.World.Space,game.World.Content.Get<ISpatialLayerDefinition>("sensor.spilled-oil"),false);
                int calls=0;layer.BeforeSample=()=>{
                    calls++;if(calls!=2)return;
                    // The setter may itself sample during resource preflight. Inject once at the
                    // second queried sample, not recursively into that existing validation.
                    layer.BeforeSample=null;
                    if(mutate)layer.SetSample(new TileCoord(14,20),Fix.One);
                    else throw new InvalidOperationException("proof.original-fixed-sample-error");
                };
                var values=new List<Fix>{Fix.One};
                c.Equal(SpatialLayerReadStatus.Unavailable,Samples(game,1).ReadLayer("sensor.spilled-oil",new TileRect(14,20,2,1),values),"private caller cannot reach malformed sample callback");
                c.That(values.Count==0&&calls==0,"unauthorized query publishes no values and samples nothing");
                c.Throws<InvalidOperationException>(()=>query.ReadLayer("sensor.spilled-oil",new TileRect(14,20,2,1),values),"actual second sample failure refuses entire read");
                var first=game.Fault;c.That(first!=null&&calls==2,"actual callback reached exactly after first staged value");
                c.Equal(0,values.Count,"neither thrown error nor mutation publishes first staged sample");
                if(!mutate)c.That(first!=null&&first.Detail.Contains("proof.original-fixed-sample-error"),"original provider cause preserved");
                long time=game.Clock.StepIndex;game.Update();c.Equal(time,game.Clock.StepIndex,"provider fault stops sole clock");
                values.Add(Fix.One);c.Throws<InvalidOperationException>(()=>query.ReadLayer("missing",new TileRect(-1,0,1,1),values),"missing layer cannot mask original provider fault");
                c.Equal(0,values.Count,"faulted query clears retained caller values");
                c.Throws<InvalidOperationException>(()=>query.ReadLayer("missing",new TileRect(-1,0,1,1),null),"null buffer cannot replace original provider fault with an argument error");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted provider cannot publish healthy hash");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted provider cannot save partial observation");
                c.That(ReferenceEquals(first,game.Fault),"first cause survives later queries and update");
                c.Equal(idleHash,idle.Hash(),"fault leaves separate world unchanged");idle.Update();
                c.That(idle.Fault==null&&idle.Clock.StepIndex>0,"separate selected provider remains live");
            }
        }
        private static void UnfinishedDisposal(Check c,SimGame game,int order,bool native)
        {
            string id=native?"sensor.dispose-oil":"sensor.dispose-oil-independent";
            var effect=game.World.Content.Get<IInventoryWorkEffectDefinition>(id);ulong hash=game.Hash();
            c.Equal(typeof(Def),effect.GetType().BaseType,"native/external effect metadata depend on contracts");
            c.Equal(WorkEffectCodes.NotComplete,effect.Apply(game.World,game.World.Services.Get<WorkBoard>().Order(order),null).Blocker,
                "direct unfinished completion cannot bypass actual machine time");
            c.Equal(hash,game.Hash(),"unfinished protocol refusal changes no material or history");
        }
    }
}
