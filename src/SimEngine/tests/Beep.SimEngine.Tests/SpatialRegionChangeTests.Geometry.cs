using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpatialRegionChangeTests
    {
        internal const string GeometryCase="Spatial geometry policy: actual access commands invalidate scoped world page and regional views";
        internal const string GeometryProtocolCase="Spatial geometry protocol: atomic revision preflight and stale material preparation";
        internal const string OwnershipCase="Spatial ownership protocol: composed revisions scope transfer and stale material publication";
        internal const string OwnershipWorkflowCase="Spatial ownership: physical discovery player transfer scoped pages and saved continuation";
        private static GameFactory GeometryFactory(bool visibility)
        {
            if(visibility)return RegionalDiscoveryTests.Points();
            var root=Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            root.Get("scenarios")[0].Set("default_seats",JsonValue.Number("2"));
            return PointGame.Factory(root.ToString(),FrameworkProofContent.Read("point-layers.json"));
        }
        private static SimGame GeometryNew(GameFactory factory,bool visibility)
            =>visibility?ConstructionPlanTests.NewPoints(factory):factory.NewGame(new NewGameRequest{
                Scenario="points",Seed=1403,Seats={Controller.Human,Controller.Human}});
        private static void GeometryRun(Check c)
        {
            // Reuse actual physical knowledge acquisition; no fabricated visibility or player grant.
            PointPhysical(c,Geometry);
            var factory=GeometryFactory(false);Geometry(c,factory,GeometryNew(factory,false));
        }
        private static void Geometry(Check c,GameFactory factory,SimGame game)
        {
            var mine=game.Seat(0).View.Feature<PointLayerView>();var other=game.Seat(1).View.Feature<PointLayerView>();
            var observer=game.Observer.Feature<PointLayerView>();
            var query=game.Seat(0).View.Feature<ISpatialRegionChangeQuery<PointRegion>>();
            var foreign=game.Seat(1).View.Feature<ISpatialRegionChangeQuery<PointRegion>>();
            var observe=game.Observer.Feature<ISpatialRegionChangeQuery<PointRegion>>();
            var before=query.ReadChanges().Cursor;var beforeOther=foreign.ReadChanges().Cursor;var beforeObserver=observe.ReadChanges().Cursor;
            int a=mine.Version,b=other.Version,o=observer.Version;
            var region=new PointRegion(Fix.One,Fix.One,1);var values=new List<Fix>();
            var page=mine.ReadLayerPage("point.terrain",region,new SpatialLayerPageRequest(0,1),values);
            c.Equal(SpatialLayerReadStatus.Success,page.Status,"actual known public sample supplies a retained page");
            if(page.Status!=SpatialLayerReadStatus.Success)throw new InvalidOperationException("geometry page fixture is not observed");
            Submit(c,game,new SetPointAccessCommand{Open=false});
            c.That(!((PointWorldSnapshot)game.Seat(0).View.Space).AccessOpen&&!((PointWorldSnapshot)game.Seat(1).View.Space).AccessOpen,"actual public geometry fact closes for both companies");
            c.Equal(a+1,mine.Version,"geometry change advances existing owner cursor exactly once");
            c.Equal(b+1,other.Version,"public geometry change advances actual foreign cursor exactly once");
            c.Equal(o+1,observer.Version,"public geometry change advances original observer guard exactly once");
            c.That(query.ReadChanges(before).FullInvalidation&&foreign.ReadChanges(beforeOther).FullInvalidation&&observe.ReadChanges(beforeObserver).FullInvalidation,"all affected regional streams explicitly invalidate changed geometry");
            var stale=mine.ReadLayerPage("point.terrain",region,new SpatialLayerPageRequest(0,1,page.Cursor),values);
            c.Equal(SpatialLayerReadStatus.Invalidated,stale.Status,"retained page cannot silently cross actual geometry change");
            c.That(values.Count==0&&stale.Cursor==null&&stale.TotalCount==0,"invalidated page publishes no value, new cursor or count");
            var closed=query.ReadChanges().Cursor;var closedOther=foreign.ReadChanges().Cursor;
            a=mine.Version;b=other.Version;o=observer.Version;
            Submit(c,game,new SetPointAccessCommand{Open=false});
            c.That(a==mine.Version&&b==other.Version&&o==observer.Version,"same geometry command is not another spatial mutation");
            Unchanged(c,query.ReadChanges(closed),closed,"same access state retains incremental-empty stream");
            Unchanged(c,foreign.ReadChanges(closedOther),closedOther,"same access state cannot invent foreign invalidation");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"closed geometry saves original state without derived cursor authority");
            c.That(!((PointWorldSnapshot)twin.Seat(0).View.Space).AccessOpen,"restore preserves actual closed geometry, not constructor default");
            var twinQuery=twin.Seat(0).View.Feature<ISpatialRegionChangeQuery<PointRegion>>();
            Refused(c,twinQuery.ReadChanges(closed),"old geometry cursor cannot cross restored stream");
            int twinVersion=twin.Seat(0).View.Space.Version;
            Submit(c,game,new SetPointAccessCommand{Open=true});Submit(c,twin,new SetPointAccessCommand{Open=true});
            c.Equal(a+1,mine.Version,"reopening advances original existing revision");
            c.Equal(twinVersion+1,twin.Seat(0).View.Space.Version,"reopening advances restored existing revision from its own baseline");
            c.That(((PointWorldSnapshot)game.Seat(0).View.Space).AccessOpen&&((PointWorldSnapshot)twin.Seat(0).View.Space).AccessOpen,"ordinary reopening changes actual geometry in both worlds");
            c.Equal(game.Hash(),twin.Hash(),"closed-save/reopen twins preserve original engine state");
            game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"geometry changes retain canonical Update continuation");
        }
        // Counter exhaustion and raw quantity preparation are explicit provider protocol probes.
        // Reflection never appears in the module or normal player workflow above.
        private static void Counter(PointSpace space,string field,int value)
            =>typeof(PointSpace).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(space,value);
        private static void OwnCounter(PointSpace space,int seat,int value)
        {
            var records=(IDictionary)typeof(PointSpace).GetField("_privateVersions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(space);
            var record=records[new PlayerId(seat)]??throw new InvalidOperationException("actual viewer baseline absent");
            record.GetType().GetField("Version",BindingFlags.Instance|BindingFlags.Public).SetValue(record,value);
        }
        private static void OwnershipRun(Check c)
        {
            PointPhysical(c,Ownership);
            var factory=GeometryFactory(false);var game=GeometryNew(factory,false);
            Submit(c,game,new AssignPointLayerCommand{Layer="point.deposit",Owner=new PlayerId(0)});
            Submit(c,game,new WritePointLayerCommand{Layer="point.deposit",Position=Fix.One,Value=Fix.FromInt(7)});
            Ownership(c,factory,game);
        }
        private static void Ownership(Check c,GameFactory factory,SimGame game)
        {
            // Normal player workflow: the original physical helper earned the scoped knowledge.
            var mine=game.Seat(0).View.Feature<PointLayerView>();var other=game.Seat(1).View.Feature<PointLayerView>();
            var observer=game.Observer.Feature<PointLayerView>();
            var retained=mine.Layers.Single(l=>l.Id=="point.deposit");
            bool otherKnows=other.Layers.Any(l=>l.Id=="point.terrain");
            var own=mine.ReadChanges().Cursor;var foreign=other.ReadChanges().Cursor;var observed=observer.ReadChanges().Cursor;
            var region=new PointRegion(Fix.One,Fix.One,1);var values=new List<Fix>();
            var page=mine.ReadLayerPage("point.deposit",region,new SpatialLayerPageRequest(0,1),values);
            c.Equal(SpatialLayerReadStatus.Success,page.Status,"actual known private stock supplies retained ownership page");
            c.Equal(Fix.FromInt(7),values.Single(),"original actual sample survives before ownership transfer");
            Submit(c,game,new AssignPointLayerCommand{Layer="point.deposit",Owner=new PlayerId(1)});
            c.Equal(own.Version+1,mine.Version,"old owner loses known scope exactly once");
            c.Equal(foreign.Version+(otherKnows?1:0),other.Version,"new owner gains scope only when actual knowledge permits it");
            c.Equal(observed.Version+1,observer.Version,"actual ownership change advances original observer once");
            c.That(mine.ReadChanges(own).FullInvalidation,"old owner's regional stream invalidates actual revocation");
            if(otherKnows)c.That(other.ReadChanges(foreign).FullInvalidation,"informed new owner gets explicit scope invalidation");
            else Unchanged(c,other.ReadChanges(foreign),foreign,"ownership alone reveals no unknown activity or sample");
            c.That(!mine.Layers.Any(l=>l.Id=="point.deposit")&&retained.Owner==new PlayerId(0),"revoked metadata disappears while retained snapshot stays immutable");
            var stale=mine.ReadLayerPage("point.deposit",region,new SpatialLayerPageRequest(0,1,page.Cursor),values);
            c.That(stale.Status!=SpatialLayerReadStatus.Success&&values.Count==0&&stale.Cursor==null,"revoked page cannot disclose retained private samples");
            c.Equal(otherKnows?SpatialLayerReadStatus.Success:SpatialLayerReadStatus.Unavailable,
                other.ReadLayer("point.deposit",region,values),"new ownership never bypasses content/sample knowledge");
            if(otherKnows)c.Equal(Fix.FromInt(7),values.Single(),"new informed owner observes same real stock without copying it");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"actual transfer roundtrips original saved owner and samples");
            c.Equal(new PlayerId(1),twin.Observer.Feature<PointLayerView>().Layers.Single(l=>l.Id=="point.deposit").Owner,"restored actual layer has transferred owner");
            Refused(c,twin.Seat(0).View.Feature<PointLayerView>().ReadChanges(own),"pre-transfer cursor cannot cross restored stream");
            game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"saved ownership twins continue through sole engine Update");
        }
        private static void OwnershipProtocol(Check c)
        {
            // Explicit provider protocol: exhausted counters are not normal player setup.
            foreach (string exhausted in new[]{"observer","new-owner","previous-owner","composed-new","composed-previous"}) {
                var factory=GeometryFactory(false);var game=GeometryNew(factory,false);var space=(PointSpace)game.World.Space;
                space.AssignOwner(game.World,"point.deposit",new PlayerId(1));
                space.AssignOwner(game.World,"point.deposit",new PlayerId(0));
                var layer=space.SpatialLayers.Single(l=>l.Id=="point.deposit");
                // Capture both actual private records before instrumenting their derived counters.
                space.SetAccess(false);space.SetAccess(true);
                if(exhausted=="observer")Counter(space,"_observerVersion",int.MaxValue);
                else if(exhausted=="new-owner")OwnCounter(space,1,int.MaxValue);
                else if(exhausted=="previous-owner")OwnCounter(space,0,int.MaxValue);
                else {Counter(space,"_publicVersion",1);OwnCounter(space,exhausted=="composed-new"?1:0,int.MaxValue-1);}
                ulong hash=game.Hash();
                space.AssignOwner(game.World,"point.deposit",new PlayerId(0));
                c.Equal(hash,game.Hash(),"same-owner no-op does not consume exhausted revisions "+exhausted);
                c.Throws<OverflowException>(()=>space.AssignOwner(game.World,"point.deposit",new PlayerId(1)),"ownership exhaustion refuses before publication "+exhausted);
                c.Equal(new PlayerId(0),layer.Owner,"refused transfer retains actual previous owner "+exhausted);
                c.Equal(hash,game.Hash(),"refused transfer retains authoritative payload "+exhausted);
                c.That(game.Fault==null,"numerical admission refusal leaves original world healthy "+exhausted);
                c.Equal(hash,factory.Load(game.Save()).Hash(),"refused ownership remains durably unchanged "+exhausted);
            }
            var f=GeometryFactory(false);var g=GeometryNew(f,false);var source=(PointSpace)g.World.Space;
            source.AssignOwner(g.World,"point.deposit",new PlayerId(0));
            var quantity=(ISpatialResourceQuantity<Fix>)source.SpatialLayers.Single(l=>l.Id=="point.deposit");
            var pending=quantity.PrepareDeposit(Fix.One,Fix.FromInt(2));
            source.AssignOwner(g.World,"point.deposit",new PlayerId(1));ulong transferred=g.Hash();
            c.Throws<InvalidOperationException>(()=>pending.Commit(),"prepared material cannot cross an actual ownership transfer");
            c.Equal(Fix.Zero,quantity.ReadQuantity(Fix.One).Amount,"stale ownership preparation publishes no material prefix");
            c.Equal(transferred,g.Hash(),"stale material refusal preserves current transferred state");
            c.That(quantity.PrepareDeposit(Fix.One,Fix.FromInt(2)).Commit().Committed,"fresh material preparation uses current transferred owner");
            c.Equal(Fix.FromInt(2),quantity.ReadQuantity(Fix.One).Amount,"fresh material publishes exact quantity once");
            var sf=GeometryFactory(true);var sg=GeometryNew(sf,true);var sp=(PointSpace)sg.World.Space;
            OwnCounter(sp,1,int.MaxValue);var before=sg.Seat(1).View.Space.Version;
            sp.AssignOwner(sg.World,"point.deposit",new PlayerId(1));
            c.That(sg.Fault==null&&sg.Seat(1).View.Space.Version==before,"unknown private ownership does not consume an unaffected exhausted viewer counter");
            c.That(!sg.Seat(1).View.Feature<PointLayerView>().Layers.Any(l=>l.Id=="point.deposit"),"ownership alone grants no undiscovered layer definition");
            // Explicit knowledge instrumentation tests the pure prospective-owner contract;
            // the normal workflow above still earns all knowledge through timed physical work.
            var known=GeometryNew(sf,true);var knownSpace=(PointSpace)known.World.Space;
            knownSpace.AssignOwner(known.World,"point.deposit",new PlayerId(0));
            var fact=known.World.Content.Get<KnowledgeFactDef>("field.observed");
            foreach(int seat in new[]{0,1})c.Equal(Code.None,known.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(seat),"sim.knowledge.facts",fact.Id,fact.Value),"explicit protocol records actual authored scope");
            known.Update();int oldVersion=known.Seat(0).View.Space.Version,newVersion=known.Seat(1).View.Space.Version;
            var actualLayer=knownSpace.SpatialLayers.Single(l=>l.Id=="point.deposit");var plannedKeys=new List<string>();ulong beforePlan=known.Hash();
            SpatialLayerAccess.CopyObservationKeys(known.World,actualLayer,new PlayerId(1),plannedKeys,new PlayerId(1));
            c.That(plannedKeys.Count>0&&actualLayer.Owner==new PlayerId(0)&&known.Hash()==beforePlan,"prospective ownership scopes are pure and do not authorize current ownership");
            knownSpace.AssignOwner(known.World,"point.deposit",new PlayerId(1));
            c.That(known.Seat(0).View.Space.Version==oldVersion+1&&known.Seat(1).View.Space.Version==newVersion+1,"known scoped transfer invalidates both affected viewers exactly once");
            c.That(!known.Seat(0).View.Feature<PointLayerView>().Layers.Any(l=>l.Id=="point.deposit")&&known.Seat(1).View.Feature<PointLayerView>().Layers.Any(l=>l.Id=="point.deposit"),"prepared known scopes match actual revocation and acquisition");
            OwnCounter(knownSpace,1,int.MaxValue);beforePlan=known.Hash();
            c.Throws<OverflowException>(()=>knownSpace.AssignOwner(known.World,"point.deposit",new PlayerId(0)),"known old-owner exhaustion refuses before scoped transfer");
            c.That(actualLayer.Owner==new PlayerId(1)&&known.Hash()==beforePlan&&known.Fault==null,"scoped numerical refusal preserves actual owner and healthy saved state");
        }
        private static void GeometryProtocol(Check c)
        {
            foreach(bool visible in new[]{false,true}){
                var factory=GeometryFactory(visible);
                foreach(string exhausted in visible?new[]{"observer","viewer"}:new[]{"observer","public","composed-viewer"}){
                    var game=GeometryNew(factory,visible);var space=(PointSpace)game.World.Space;
                    if(exhausted=="observer")Counter(space,"_observerVersion",int.MaxValue);
                    else if(exhausted=="public")Counter(space,"_publicVersion",int.MaxValue);
                    else if(exhausted=="viewer")OwnCounter(space,1,int.MaxValue);
                    else {
                        ((ISpatialLayerProvider)space).AssignOwner(game.World,"point.deposit",new PlayerId(0));
                        Counter(space,"_publicVersion",1);OwnCounter(space,0,int.MaxValue-1);
                    }
                    ulong hash=game.Hash();int mine=game.Seat(0).View.Space.Version,other=game.Seat(1).View.Space.Version;
                    space.SetAccess(true);c.Equal(hash,game.Hash(),"same-state no-op tolerates an exhausted unchanged counter "+exhausted);
                    c.Throws<OverflowException>(()=>space.SetAccess(false),"affected counter exhaustion refuses before geometry publication "+exhausted);
                    c.That(space.AccessOpen&&game.Fault==null,"pure preflight refusal changes no geometry and is not a partial-world fault "+exhausted);
                    c.That(mine==game.Seat(0).View.Space.Version&&other==game.Seat(1).View.Space.Version,"failed geometry preflight retains all viewer baselines "+exhausted);
                    c.Equal(hash,game.Hash(),"failed geometry preflight retains authoritative state "+exhausted);
                    c.Equal(hash,factory.Load(game.Save()).Hash(),"derived counter exhaustion does not corrupt saved actual geometry "+exhausted);
                }
            }
            var scopedFactory=GeometryFactory(true);var scoped=GeometryNew(scopedFactory,true);var scopedSpace=(PointSpace)scoped.World.Space;
            Counter(scopedSpace,"_publicVersion",int.MaxValue);int before=scoped.Seat(0).View.Space.Version;
            scopedSpace.SetAccess(false);
            c.Equal(before+1,scoped.Seat(0).View.Space.Version,"unused unscoped-public counter cannot reject valid scoped geometry change");
            c.That(!scopedSpace.AccessOpen&&scoped.Fault==null,"only actually affected scoped counters participate");
            var f=GeometryFactory(false);var g=GeometryNew(f,false);var source=(PointSpace)g.World.Space;
            ((ISpatialLayerProvider)source).AssignOwner(g.World,"point.deposit",new PlayerId(0));
            var quantity=(ISpatialResourceQuantity<Fix>)source.SpatialLayers.Single(l=>l.Id=="point.deposit");
            var pending=quantity.PrepareDeposit(Fix.One,Fix.FromInt(2));c.That(pending.Check.Ready,"original quantity owner prepares a real exact write");
            source.SetAccess(false);ulong closed=g.Hash();
            c.Throws<InvalidOperationException>(()=>pending.Commit(),"prepared material cannot commit across changed geometry revision");
            c.Equal(Fix.Zero,quantity.ReadQuantity(Fix.One).Amount,"stale preparation publishes no material prefix");
            c.Equal(closed,g.Hash(),"stale geometry/material refusal retains current authoritative state");
            c.That(quantity.PrepareDeposit(Fix.One,Fix.FromInt(2)).Commit().Committed,"fresh actual material preparation uses current geometry revision");
            c.Equal(Fix.FromInt(2),quantity.ReadQuantity(Fix.One).Amount,"fresh raw protocol write commits exactly once");
            c.That(g.Fault==null,"stale-before-publication refusal leaves original session healthy");
        }
    }
}
