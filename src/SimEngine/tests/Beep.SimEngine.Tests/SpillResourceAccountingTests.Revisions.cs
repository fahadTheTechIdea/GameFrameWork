using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpillResourceAccountingTests
    {
        internal const string RevisionProtocolCase="Spatial revision protocol: native independent noops batches ownership and restore";
        private static int Revision(SimGame game,int seat)=>((TileWorldSnapshot)game.Seat(seat).View.Space).Version;
        // Explicit provider protocol checks, not a normal player driver. Physical play/replay above
        // uses only commands, Update and immutable views; these probes exercise raw provider writes.
        private static void RevisionProtocol(Check c)
        {
            foreach(bool native in new[]{false,true}){
                var factory=IntegerFactory(native);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                var provider=(ISpatialLayerProvider)game.World.Space;
                var privateLayer=(ITileWritableSampleLayer)provider.SpatialLayers.Single(l=>l.Id=="sensor.query-int");
                var publicLayer=(ITileWritableSampleLayer)provider.SpatialLayers.Single(l=>l.Id=="sensor.query-byte");
                var space=(ITileSampleMutationSpace)game.World.Space;
                var tile=new TileCoord(1,1);int owner=Revision(game,0),foreign=Revision(game,1);
                privateLayer.SetSample(tile,Fix.FromInt(7));
                c.Equal(owner+1,Revision(game,0),"one actual private sample advances only its owner");
                c.Equal(foreign,Revision(game,1),"single private write leaks no world invalidation");
                ulong hash=game.Hash();owner=Revision(game,0);
                privateLayer.SetSample(tile,Fix.FromInt(7));
                c.Equal(hash,game.Hash(),"same-value sample is not a saved mutation");
                c.Equal(owner,Revision(game,0),"same-value sample is not a visible mutation");
                c.Throws<ArgumentException>(()=>privateLayer.SetSample(tile,Fix.Half),"invalid integer sample refuses before publication");
                c.Equal(hash,game.Hash(),"refused scalar leaves original stock and revisions intact");
                c.Equal(owner,Revision(game,0),"refused scalar does not notify the owner");
                c.Equal(foreign,Revision(game,1),"refused scalar does not notify foreign players");
                space.ApplySamples(new[]{
                    new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(privateLayer.Id),tile,Fix.FromInt(8)),
                    new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(publicLayer.Id),tile,Fix.FromInt(12))});
                c.Equal(owner+2,Revision(game,0),"mixed batch advances one cursor per actual readable changed layer");
                c.Equal(foreign+1,Revision(game,1),"mixed batch reveals only the public layer change");
                owner=Revision(game,0);foreign=Revision(game,1);hash=game.Hash();
                c.Throws<ArgumentException>(()=>space.ApplySamples(new[]{
                    new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(privateLayer.Id),tile,Fix.FromInt(9)),
                    new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(publicLayer.Id),tile,Fix.FromInt(256))}),
                    "invalid later batch entry refuses the whole original publication");
                c.Equal(hash,game.Hash(),"refused batch publishes no stock prefix");
                c.That(owner==Revision(game,0)&&foreign==Revision(game,1),"refused batch publishes no viewer cursor prefix");
                space.ApplySamples(new[]{new TileSampleMutation(game.World.Content.Get<ISpatialLayerDefinition>(privateLayer.Id),tile,Fix.FromInt(8))});
                c.Equal(hash,game.Hash(),"no-op batch is a real unchanged original store");
                c.That(owner==Revision(game,0)&&foreign==Revision(game,1),"no-op batch invents no invalidation");
                provider.AssignOwner(game.World,privateLayer.Id,new PlayerId(1));
                c.Equal(owner+1,Revision(game,0),"ownership revocation invalidates old viewer exactly once");
                c.Equal(foreign+1,Revision(game,1),"ownership grant invalidates new viewer exactly once");
                var values=new List<int>();
                c.Equal(SpatialLayerReadStatus.Unavailable,Integers(game).ReadLayer(privateLayer.Id,new TileRect(tile,1,1),values),"revoked viewer loses values, not just metadata");
                c.Equal(SpatialLayerReadStatus.Success,Integers(game,1).ReadLayer(privateLayer.Id,new TileRect(tile,1,1),values),"new owner reads actual transferred layer");
                c.That(values.SequenceEqual(new[]{8}),"ownership never changes stock");
                owner=Revision(game,0);foreign=Revision(game,1);hash=game.Hash();
                provider.AssignOwner(game.World,privateLayer.Id,new PlayerId(1));
                c.Equal(hash,game.Hash(),"same-owner assignment is a saved no-op");
                c.That(owner==Revision(game,0)&&foreign==Revision(game,1),"same-owner assignment does not repeat notification");
                var twin=factory.Load(game.Save());c.Equal(hash,twin.Hash(),"derived cursors are not another saved authority");
                int restoredOwner=Revision(twin,1),restoredForeign=Revision(twin,0);
                var twinLayer=(ITileWritableSampleLayer)((ISpatialLayerProvider)twin.World.Space).SpatialLayers.Single(l=>l.Id==privateLayer.Id);
                privateLayer.SetSample(tile,Fix.FromInt(9));twinLayer.SetSample(tile,Fix.FromInt(9));
                c.That(Revision(game,0)==owner&&Revision(twin,0)==restoredForeign,"original and restored revoked viewers see no later write");
                c.That(Revision(game,1)==foreign+1&&Revision(twin,1)==restoredOwner+1,"original and fresh restored cursors each advance from their own baseline");
                c.Equal(game.Hash(),twin.Hash(),"transient viewer cursors do not disturb exact authoritative twin state");
                hash=game.Hash();for(int i=0;i<3;i++){Revision(game,0);Revision(game,1);}
                c.Equal(hash,game.Hash(),"Capture is pure and never refreshes a cursor on reads");
                c.Equal(0,game.World.Space.Capture(new PlayerId(2)).Version,"invalid seat gets no provider revision");
                c.Equal(game.World.Space.ObservationVersion,game.World.Space.Capture(PlayerId.None).Version,"explicit engine observer retains original provider-wide mutation guard");
                game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"saved sample ownership continues through sole native Update");
                c.Equal(idleHash,idle.Hash(),"other session has no shared viewer counters or sample stock");
            }
        }
    }
}
