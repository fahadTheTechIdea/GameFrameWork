using System;
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
    internal static partial class SpillResourceAccountingTests
    {
        internal const string RegionChangeCase="Regional change protocol: native independent owners scopes batches and saved identity";
        internal const string RegionChangeFaultCase="Regional change protocol: malformed provider publishes no cursor or regions";
        private static ISpatialRegionChangeQuery<TileRect> Changes(SimGame game,int seat=0)
            =>game.Seat(seat).View.Feature<ISpatialRegionChangeQuery<TileRect>>()
                ??throw new InvalidOperationException("selected regional change contract absent");
        private static void UnchangedRegions(Check c,SimGame game,int seat,SpatialRegionCursor cursor)
        {
            var value=Changes(game,seat).ReadChanges(cursor);
            c.That(value.Status==SpatialRegionChangeStatus.Success&&!value.FullInvalidation&&value.ChangedRegions.Count==0&&value.Cursor.Version==cursor.Version,
                "unchanged/private-hidden world publishes no invalidation, timing or location");
        }
        private static void ChangedAt(Check c,SimGame game,SpatialRegionCursor cursor,TileCoord tile,int seat=0)
        {
            var value=Changes(game,seat).ReadChanges(cursor);
            c.That(value.Status==SpatialRegionChangeStatus.Success&&!value.FullInvalidation&&value.Cursor.Version>cursor.Version&&value.ChangedRegions.Any(r=>r.Contains(tile)),
                "actual committed sample has a provider-owned incremental region at its viewer revision");
        }
        private static void RefusedRegions(Check c,SpatialRegionChanges<TileRect> value)
            =>c.That(value.Status==SpatialRegionChangeStatus.InvalidCursor&&value.Cursor==null&&!value.FullInvalidation&&value.ChangedRegions.Count==0,
                "foreign query cursor exposes no provider baseline or geometry");
        // Raw owner/knowledge/sample checks are explicitly provider protocol instrumentation.
        // Physical normal-play checks reuse Run and only query these detached engine contracts.
        private static void RegionChangeProtocol(Check c)
        {
            var original=new List<TileRect>{new TileRect(1,2,1,1)};
            var retained=SpatialRegionChangeSet<TileRect>.Incremental(3,original);original.Clear();
            c.Equal(1,retained.ChangedRegions.Count,"source facts copy supplied geometry, not retained provider lists");
            c.Throws<NotSupportedException>(()=>((IList<TileRect>)retained.ChangedRegions).Clear(),"source fact geometry is readonly");
            c.Throws<ArgumentOutOfRangeException>(()=>SpatialRegionChangeSet<TileRect>.Full(-1),"source facts require nonnegative actual version");
            c.Throws<ArgumentNullException>(()=>SpatialRegionChangeSet<TileRect>.Incremental(0,null),"missing regional geometry cannot become empty success");
            foreach(bool native in new[]{false,true}){
                var factory=IntegerFactory(native,regional:true);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var query=Changes(game);var foreignQuery=Changes(game,1);var initial=query.ReadChanges();var foreign=foreignQuery.ReadChanges().Cursor;
                c.That(initial.Status==SpatialRegionChangeStatus.Success&&initial.FullInvalidation&&initial.ChangedRegions.Count==0,"initial baseline signals full without enumerating world");
                c.That(ReferenceEquals(query,game.Seat(0).View.Feature<SpatialRegionChangeView<TileRect>>()),"declared interface and primary share actual projection query");
                c.That(game.Seat(0).View.Feature<ISpatialRegionChangeSource<TileRect>>()==null,"mutable source is not a public read-model feature");
                UnchangedRegions(c,game,0,initial.Cursor);
                var provider=(ISpatialLayerProvider)game.World.Space;
                var source=(ISpatialRegionChangeSource<TileRect>)game.World.Space;
                var layer=(ITileWritableSampleLayer)provider.SpatialLayers.Single(l=>l.Id=="sensor.query-byte");
                var first=new TileCoord(1,1);var hidden=new TileCoord(2,1);
                layer.SetSample(hidden,Fix.FromInt(18));UnchangedRegions(c,game,0,initial.Cursor);UnchangedRegions(c,game,1,foreign);
                LearnRegion(c,game,0,"sensor.query-first-known");var learned=query.ReadChanges(initial.Cursor);
                c.That(learned.FullInvalidation&&learned.Cursor.Version>initial.Cursor.Version,"actual scope grant requires full region baseline reset");
                UnchangedRegions(c,game,1,foreign);
                var baseline=learned.Cursor;
                ((ITileSampleMutationSpace)game.World.Space).ApplySamples(new[]{
                    new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(layer.Id),hidden,Fix.FromInt(19)),
                    new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(layer.Id),first,Fix.FromInt(200))});
                ChangedAt(c,game,baseline,first);UnchangedRegions(c,game,1,foreign);
                var delta=query.ReadChanges(baseline);
                if(!native)c.That(delta.ChangedRegions.SequenceEqual(new[]{new TileRect(first,1,1)}),"independent mixed batch contains only visible changed sample, not hidden neighbor");
                var after=delta.Cursor;layer.SetSample(hidden,Fix.FromInt(20));layer.SetSample(first,Fix.FromInt(200));
                UnchangedRegions(c,game,0,after);UnchangedRegions(c,game,1,foreign);
                for(int i=0;i<4;i++)layer.SetSample(first,Fix.FromInt(201+i));
                c.Equal(1,query.ReadChanges(after).ChangedRegions.Count,"repeated same-cell changes retain one latest invalidation, not a second event history");
                Send(c,game,new ForgetKnowledgeCommand{Policy="sensor.query-forget-first"});
                var revoked=query.ReadChanges(after);c.That(revoked.FullInvalidation,"actual scope revocation resets prior regional facts");
                layer.SetSample(first,Fix.FromInt(205));UnchangedRegions(c,game,0,revoked.Cursor);
                var old=query.ReadChanges().Cursor;foreign=foreignQuery.ReadChanges().Cursor;
                provider.AssignOwner(game.World,"sensor.query-int",new PlayerId(1));
                c.That(query.ReadChanges(old).FullInvalidation&&foreignQuery.ReadChanges(foreign).FullInvalidation,"ownership transfer resets both affected viewer scopes");
                old=query.ReadChanges().Cursor;foreign=foreignQuery.ReadChanges().Cursor;
                var stock=(ITileWritableSampleLayer)provider.SpatialLayers.Single(l=>l.Id=="sensor.query-int");
                stock.SetSample(new TileCoord(1,2),Fix.FromInt(9));stock.SetSample(new TileCoord(2,1),Fix.FromInt(10));
                UnchangedRegions(c,game,0,old);ChangedAt(c,game,foreign,new TileCoord(1,2),1);
                if(!native)c.That(foreignQuery.ReadChanges(foreign).ChangedRegions.SequenceEqual(new[]{new TileRect(2,1,1,1),new TileRect(1,2,1,1)}),"independent geometry uses stable row-major keys, not command arrival order");
                RefusedRegions(c,query.ReadChanges(foreign));RefusedRegions(c,Changes(idle).ReadChanges(old));
                var values=new List<int>();var page=Pages(game,1).ReadLayerPage("sensor.query-int",new TileRect(first,1,1),new SpatialLayerPageRequest(0,1),values);
                RefusedRegions(c,foreignQuery.ReadChanges(page.Cursor));
                c.Throws<ArgumentOutOfRangeException>(()=>source.ReadRegionChanges(new PlayerId(0),-1),"raw source refuses invalid numerical baseline");
                ulong hash=game.Hash();query.ReadChanges(old);c.Equal(hash,game.Hash(),"regional diagnostic reads remain pure");
                var twin=factory.Load(game.Save());c.Equal(hash,twin.Hash(),"regional metadata adds no save authority");
                RefusedRegions(c,Changes(twin).ReadChanges(old));
                var twinBase=Changes(twin,1).ReadChanges().Cursor;UnchangedRegions(c,twin,1,twinBase);
                foreign=foreignQuery.ReadChanges().Cursor;
                var twinStock=(ITileWritableSampleLayer)((ISpatialLayerProvider)twin.World.Space).SpatialLayers.Single(l=>l.Id==stock.Id);
                stock.SetSample(new TileCoord(1,2),Fix.FromInt(11));twinStock.SetSample(new TileCoord(1,2),Fix.FromInt(11));
                ChangedAt(c,game,foreign,new TileCoord(1,2),1);ChangedAt(c,twin,twinBase,new TileCoord(1,2),1);
                c.Equal(game.Hash(),twin.Hash(),"fresh restored derived history preserves authoritative twin state");
                game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"regional-query save twins continue canonical Update");
                c.Equal(idleHash,idle.Hash(),"interleaved session has no shared invalidation or sample state");
                c.That(initial.ChangedRegions.Count==0&&learned.FullInvalidation,"retained prior source/query results stay detached");
            }
        }
        private static void RegionChangeFaults(Check c)
        {
            foreach(string mode in new[]{"null","version","unchanged-full","throw","mutate"}){
                var factory=IntegerFactory(false);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var query=Changes(game);var baseline=query.ReadChanges().Cursor;var source=(SensorTileSpace)game.World.Space;
                // Keep the actual read model before fault injection. Creating an uncached feature
                // correctly refuses at its factory boundary before a read method can own a buffer.
                var pages=Pages(game);
                if(mode=="null")source.ChangeResultProbe=facts=>null;
                else if(mode=="version")source.ChangeResultProbe=facts=>SpatialRegionChangeSet<TileRect>.Incremental(facts.Version+1,Array.Empty<TileRect>());
                else if(mode=="unchanged-full")source.ChangeResultProbe=facts=>SpatialRegionChangeSet<TileRect>.Full(facts.Version);
                else source.BeforeReadChanges=world=>{
                    if(mode=="throw")throw new InvalidOperationException("proof.original-regional-error");
                    ((ITileWritableSampleLayer)source.SpatialLayers.Single(l=>l.Id=="sensor.query-int")).SetSample(new TileCoord(1,2),Fix.FromInt(12));
                };
                SpatialRegionChanges<TileRect> result=null;
                c.Throws<InvalidOperationException>(()=>result=query.ReadChanges(baseline),"malformed source stops regional publication "+mode);
                var fault=game.Fault;c.That(fault!=null&&result==null,"no regions or fresh cursor escape source failure "+mode);
                if(mode=="throw")c.That(fault.Detail.Contains("proof.original-regional-error"),"original regional callback error retained");
                c.Throws<InvalidOperationException>(()=>query.ReadChanges(),"initial cursor shortcut cannot mask terminal source fault");
                c.Throws<InvalidOperationException>(()=>query.ReadChanges(new SpatialChangeScope().Capture(0)),"foreign cursor shortcut cannot mask terminal source fault");
                var values=new List<int>{42};c.Throws<InvalidOperationException>(()=>pages.ReadLayerPage("missing",default,default,values),"other retained typed view cannot mask terminal source fault");
                c.Equal(0,values.Count,"faulted typed query clears retained output");
                long time=game.Clock.StepIndex;game.Update();c.Equal(time,game.Clock.StepIndex,"regional fault stops sole clock");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted regional source cannot publish healthy hash");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted regional source cannot save partial result");
                c.That(ReferenceEquals(fault,game.Fault),"first regional fault remains original owner");
                c.Equal(idleHash,idle.Hash(),"source fault isolated from another session");idle.Update();c.That(idle.Fault==null,"other session remains playable");
            }
            var owner=New(IntegerFactory(false));var initialized=new InitializeProbe();
            c.Throws<InvalidOperationException>(()=>New(IntegerFactory(false,compose:composer=>{
                composer.AddSystem(initialized,300);
                SpatialRegionChangeView<TileRect>.Compose(composer,(ISpatialRegionChangeSource<TileRect>)owner.World.Space,"proof.foreign-regions");
            })),"another world's source cannot compose a view for this selected space");
            c.Equal(0,initialized.Initializations,"foreign source declaration refuses before Initialize");
        }
    }
}
