using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
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
        internal const string RegionRevisionCase="Spatial revision protocol: regional knowledge acquisition revocation and paged privacy";
        internal const string SharedPageCase="Tile page contracts: shared native independent typed cursors and saved identity";
        internal const string PageFaultCase="Tile page protocol: malformed provider withholds values cursor and original fault";
        private static ISpatialLayerPagedQuery<TileRect,int> Pages(SimGame game,int seat=0)
            =>game.Seat(seat).View.Feature<ISpatialLayerPagedQuery<TileRect,int>>()
                ??throw new InvalidOperationException("selected tile page contract absent");
        private static ISpatialLayerPagedQuery<TileRect,Fix> FixedPages(SimGame game)
            =>game.Seat(0).View.Feature<ISpatialLayerPagedQuery<TileRect,Fix>>()
                ??throw new InvalidOperationException("selected fixed page contract absent");
        private static void PageRefusal(Check c,SpatialLayerPageResult page,IList<int> values,SpatialLayerReadStatus status,string message)
        {
            c.Equal(status,page.Status,message);
            c.That(values.Count==0&&page.Cursor==null&&page.Count==0&&page.Offset==0&&page.NextOffset==0&&page.TotalCount==0,"refused page exposes no values, count, version or cursor");
        }
        private static void SharedPages(Check c)
        {
            foreach(bool native in new[]{false,true}){
                var factory=IntegerFactory(native);var game=New(factory);ulong hash=game.Hash();
                var query=Pages(game);var values=new List<int>();var region=new TileRect(1,1,3,2);
                c.That(ReferenceEquals(query,Integers(game))&&ReferenceEquals(query,Samples(game))&&ReferenceEquals(query,FixedPages(game)),"four explicit typed contracts share actual projection primary");
                var collected=new List<int>();SpatialLayerPageResult page=default;long offset=0;
                do {page=query.ReadLayerPage("sensor.query-int",region,new SpatialLayerPageRequest(offset,2,page.Cursor),values);
                    c.Equal(SpatialLayerReadStatus.Success,page.Status,"one shared bounded signed integer page");
                    c.That(page.Count==values.Count&&page.TotalCount==6&&page.NextOffset==offset+values.Count,"exact page counts and row-major offsets");
                    if(page.Status!=SpatialLayerReadStatus.Success)throw new InvalidOperationException("shared page refused "+page.Status);
                    collected.AddRange(values);offset=page.NextOffset;
                }while(page.HasMore);
                c.That(collected.SequenceEqual(new[]{int.MinValue,int.MaxValue,0,120000,-70000,0}),"bounded pages preserve authored signed extrema and sparse zeros");
                var cursor=page.Cursor;
                c.Equal(SpatialLayerReadStatus.Success,query.ReadLayerPage("sensor.query-int",region,new SpatialLayerPageRequest(6,int.MaxValue,cursor),values).Status,"terminal huge-capacity page allocates no unused buffer");
                c.Equal(0,values.Count,"terminal page is empty");
                foreach(var invalid in new[]{new SpatialLayerPageRequest(-1,1),new SpatialLayerPageRequest(7,1),new SpatialLayerPageRequest(0,0),default})
                    PageRefusal(c,query.ReadLayerPage("sensor.query-int",region,invalid,values),values,SpatialLayerReadStatus.InvalidPage,"invalid page envelope");
                PageRefusal(c,query.ReadLayerPage("sensor.query-int",new TileRect(1,1,2,2),new SpatialLayerPageRequest(0,1,cursor),values),values,SpatialLayerReadStatus.InvalidCursor,"changed region cannot reuse opaque cursor");
                PageRefusal(c,Pages(game,1).ReadLayerPage("sensor.query-byte",region,new SpatialLayerPageRequest(0,1,cursor),values),values,SpatialLayerReadStatus.InvalidCursor,"another viewer cannot reuse cursor");
                var other=New(factory);PageRefusal(c,Pages(other).ReadLayerPage("sensor.query-int",region,new SpatialLayerPageRequest(0,1,cursor),values),values,SpatialLayerReadStatus.InvalidCursor,"another session cannot reuse cursor");
                foreach(string missing in new[]{"missing","sensor.spilled-oil"})
                    PageRefusal(c,Pages(game,1).ReadLayerPage(missing,new TileRect(-1,0,1,1),default,values),values,SpatialLayerReadStatus.Unavailable,"missing and private identity refuse before page geometry");
                PageRefusal(c,query.ReadLayerPage("sensor.query-int",new TileRect(int.MaxValue,0,2,1),default,values),values,SpatialLayerReadStatus.InvalidRegion,"wide bounds refuse before endpoint arithmetic");
                PageRefusal(c,query.ReadLayerPage("sensor.spilled-oil",region,new SpatialLayerPageRequest(0,1),values),values,SpatialLayerReadStatus.ValueTypeMismatch,"fixed layer cannot be integer paged");
                c.Throws<ArgumentNullException>(()=>query.ReadLayerPage("missing",region,default,null),"healthy null page buffer is an argument error");
                c.Equal(hash,game.Hash(),"all page reads and refusals preserve original engine state");
                var twin=factory.Load(game.Save());c.Equal(hash,twin.Hash(),"pages add no saved sample or cursor authority");
                PageRefusal(c,Pages(twin).ReadLayerPage("sensor.query-int",region,new SpatialLayerPageRequest(0,1,cursor),values),values,SpatialLayerReadStatus.InvalidCursor,"restored projection requires its own fresh cursor");
                c.Equal(SpatialLayerReadStatus.Success,Pages(twin).ReadLayerPage("sensor.query-int",region,new SpatialLayerPageRequest(0,2),values).Status,"fresh restored page reads saved provider");
                c.That(values.SequenceEqual(new[]{int.MinValue,int.MaxValue}),"restored extrema stay exact");
                var exact=new List<Fix>();var fixedPage=FixedPages(game).ReadLayerPage("sensor.spilled-oil",new TileRect(14,20,1,1),new SpatialLayerPageRequest(0,1),exact);
                c.That(fixedPage.Status==SpatialLayerReadStatus.Success&&exact.SequenceEqual(new[]{Fix.Zero}),"fixed pages use same actual sample contract and sparse zero");
                if(native)c.Equal(SpatialLayerReadStatus.Success,game.Seat(0).View.Feature<TileMapView>().ReadLayerPage("sensor.query-int",region,new SpatialLayerPageRequest(0,2),values).Status,"native facade delegates shared page engine");
                game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"saved page-read worlds continue through canonical Update");
            }
        }
        // Explicit native/provider knowledge and sample protocol probes, not player-side discovery logic.
        private static void LearnRegion(Check c,SimGame game,int seat,string id)
        {
            var fact=game.World.Content.Get<KnowledgeFactDef>(id);
            c.Equal(Code.None,game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(seat),"sim.knowledge.facts",id,fact.Value),"original knowledge owner records actual authored regional fact");
            game.Update();if(game.Fault!=null)throw new InvalidOperationException(game.Fault.ToString());
        }
        private static void RegionRevisions(Check c)
        {
            foreach(bool native in new[]{false,true}){
                var factory=IntegerFactory(native,regional:true);var game=New(factory);var query=Pages(game);var values=new List<int>();
                var layer=(ITileWritableSampleLayer)((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single(l=>l.Id=="sensor.query-byte");
                var first=new TileCoord(1,1);var second=new TileCoord(2,1);var region=new TileRect(1,1,2,1);
                int a=Revision(game,0),b=Revision(game,1);layer.SetSample(first,Fix.FromInt(200));
                c.That(a==Revision(game,0)&&b==Revision(game,1),"unknown public sample writes invalidate neither company");
                PageRefusal(c,query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1),values),values,SpatialLayerReadStatus.Unavailable,"unobserved sample withholds page cursor");
                LearnRegion(c,game,0,"sensor.query-first-known");
                c.That(Revision(game,0)>a&&Revision(game,1)==b,"actual scope acquisition invalidates only the informed company");
                var page=query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1),values);
                c.That(page.Status==SpatialLayerReadStatus.Success&&page.HasMore&&values.SequenceEqual(new[]{200}),"authorized bounded slice succeeds even when next cell is unknown");
                var cursor=page.Cursor;a=Revision(game,0);b=Revision(game,1);
                layer.SetSample(second,Fix.FromInt(18));
                c.That(a==Revision(game,0)&&b==Revision(game,1),"hidden neighboring sample cannot invalidate known page stream");
                c.Equal(SpatialLayerReadStatus.Success,query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1,cursor),values).Status,"hidden mutation does not invalidate public readable cursor");
                PageRefusal(c,query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(1,1,cursor),values),values,SpatialLayerReadStatus.Unavailable,"hidden tail refuses before reporting freshness");
                layer.SetSample(first,Fix.FromInt(201));
                c.That(Revision(game,0)==a+1&&Revision(game,1)==b,"known sample write invalidates only its actual observer");
                PageRefusal(c,query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1,cursor),values),values,SpatialLayerReadStatus.Invalidated,"actual visible mutation requires explicit restart");
                LearnRegion(c,game,1,"sensor.query-second-known");
                a=Revision(game,0);b=Revision(game,1);layer.SetSample(second,Fix.FromInt(19));
                c.That(Revision(game,0)==a&&Revision(game,1)==b+1,"different company observes only its own known region");
                var fresh=query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1),values);
                Send(c,game,new ForgetKnowledgeCommand{Policy="sensor.query-forget-first"});
                PageRefusal(c,query.ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1,fresh.Cursor),values),values,SpatialLayerReadStatus.Unavailable,"revoked region hides values and cursor freshness");
                a=Revision(game,0);b=Revision(game,1);layer.SetSample(first,Fix.FromInt(202));
                c.That(a==Revision(game,0)&&b==Revision(game,1),"revoked scope stops future sample notifications");
                var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"saved knowledge scopes and original values share no extra revision codec");
                PageRefusal(c,Pages(twin).ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(0,1),values),values,SpatialLayerReadStatus.Unavailable,"restore cannot reacquire forgotten scope");
                c.Equal(SpatialLayerReadStatus.Success,Pages(twin,1).ReadLayerPage(layer.Id,region,new SpatialLayerPageRequest(1,1),values).Status,"saved other-company scope remains readable");
                c.That(values.SequenceEqual(new[]{19}),"restored authorized regional value remains exact");
                game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"scope-save twins continue through original Update");
            }
        }
        // Explicit malformed independent provider instrumentation, never a normal player workflow.
        private static void PageFaults(Check c)
        {
            foreach(string mode in new[]{"fraction","throw","mutate"}){
                var factory=IntegerFactory(false);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var layer=(SensorSampleLayer)((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single(l=>l.Id=="sensor.query-int");
                int reads=0;layer.SampleResultProbe=(tile,value)=>{
                    if(++reads!=2)return value;
                    if(mode=="throw")throw new InvalidOperationException("proof.original-page-error");
                    if(mode=="mutate"){layer.SetSample(new TileCoord(1,2),Fix.FromInt(11));return value;}
                    return Fix.Half;
                };
                var values=new List<int>{42};SpatialLayerPageResult? result=null;
                c.Throws<InvalidOperationException>(()=>result=Pages(game).ReadLayerPage(layer.Id,new TileRect(1,1,2,1),new SpatialLayerPageRequest(0,2),values),"second returned page sample must preserve actual provider fault "+mode);
                var fault=game.Fault;c.That(fault!=null&&reads==2,"page observes actual selected provider twice "+mode);
                c.That(values.Count==0&&!result.HasValue,"fault publishes neither first sample nor cursor "+mode);
                if(mode=="throw")c.That(fault.Detail.Contains("proof.original-page-error"),"original page callback cause is preserved");
                c.Throws<InvalidOperationException>(()=>Pages(game).ReadLayerPage("missing",default,default,null),"fault wins over null-buffer or missing-identity errors");
                var fixedValues=new List<Fix>{Fix.One};
                c.Throws<InvalidOperationException>(()=>FixedPages(game).ReadLayerPage("missing",default,default,fixedValues),"other page contract cannot mask first fault");
                c.Equal(0,fixedValues.Count,"cross-contract page fault clears output");
                long before=game.Clock.StepIndex;game.Update();c.Equal(before,game.Clock.StepIndex,"page fault stops sole game clock");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted page cannot publish healthy hash");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted page cannot save partial state");
                c.That(ReferenceEquals(fault,game.Fault),"first page fault remains unchanged");
                c.Equal(idleHash,idle.Hash(),"other session remains isolated from page faults");idle.Update();c.That(idle.Fault==null,"other session remains playable");
            }
        }
    }
}
