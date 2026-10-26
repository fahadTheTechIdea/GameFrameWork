using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpillResourceAccountingTests
    {
        internal const string IntegerSamplesCase="Tile layer contracts: shared integer fixed queries and saved native independent extrema";
        internal const string IntegerFaultCase="Tile layer contracts: malformed integer provider cannot truncate or publish a prefix";
        internal const string AliasDeclarationCase="View contract aliases: explicit primary declarations refuse invalid composition";
        private static GameFactory IntegerFactory(bool native,Action<WorldComposer> compose=null,bool regional=false)
            =>IndependentTileWorkTests.Factory(nativeTiles:native,spillResources:true,
                authorSpillResources:root=>root.Get("world_setup_profiles")[0].Get("includes").Add(JsonValue.Of("sensor.query-values")),
                additionalModule:new ProbeModule("proof.tile-layer-queries",new ModuleVersion(1,0,0),compose:compose),
                additionalDeclaration:"{\"id\":\"proof.tile-layer-queries\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[\"tile-query-types.json\"]}",
                additionalDocument:FrameworkProofContent.Read("tile-query-types.json"),
                authorVisibility:regional?root=>root.Get("knowledge_visibility_policies")[0].Get("rules").Add(JsonValue.Of("sensor.query-regions")):null);
        private static ISpatialLayerQuery<TileRect,int> Integers(SimGame game,int seat=0)
            =>game.Seat(seat).View.Feature<ISpatialLayerQuery<TileRect,int>>()
                ??throw new InvalidOperationException("selected integer query contract absent");
        private static void IntegerSamples(Check c)
        {
            foreach(bool native in new[]{false,true}){
                var factory=IntegerFactory(native);var game=New(factory);ulong hash=game.Hash();
                var integers=Integers(game);var fixedValues=Samples(game);var values=new List<int>{-1};var exact=new List<Fix>();
                c.That(ReferenceEquals(integers,fixedValues),"both contracts resolve the SAME per-viewer engine read model");
                c.That(ReferenceEquals(integers,game.Seat(0).View.Feature<TileLayerView>()),"typed contracts share their actual primary model, not fresh player views");
                var registration=game.Seat(0).View.FeatureRegistrations.Single(r=>r.Contract==typeof(ISpatialLayerQuery<TileRect,int>));
                c.That(registration.SourceId=="tile-layers.sensor.tiles"&&registration.SourceContract==typeof(TileLayerView),"immutable alias metadata names actual primary identity and contract");
                c.That(!ReferenceEquals(integers,Integers(game,1)),"another viewer receives its own query model");
                c.Equal(1,game.Seat(0).View.FeatureRegistrations.Count(r=>r.Contract==typeof(ISpatialLayerQuery<TileRect,int>)),"one explicitly declared integer contract");
                c.Equal(SpatialLayerReadStatus.Success,integers.ReadLayer("sensor.query-byte",new TileRect(1,1,3,1),values),"public byte samples project exactly to integers");
                c.That(values.SequenceEqual(new[]{255,17,0}),"byte maximum and unallocated zero are not clamped or omitted");
                c.Equal(SpatialLayerReadStatus.Success,integers.ReadLayer("sensor.query-int",new TileRect(1,1,3,2),values),"owned signed integer region is readable");
                c.That(values.SequenceEqual(new[]{int.MinValue,int.MaxValue,0,120000,-70000,0}),"signed extrema and large nonbyte values preserve exact row-major ordering");
                c.Equal(SpatialLayerReadStatus.Unavailable,Integers(game,1).ReadLayer("sensor.query-int",new TileRect(-1,0,1,1),values),"foreign private identity refuses before exposing geometry");
                c.Equal(0,values.Count,"private refusal clears all caller integers");
                c.Equal(SpatialLayerReadStatus.Unavailable,integers.ReadLayer("missing",new TileRect(-1,0,1,1),values),"unknown identity is indistinguishable from private");
                c.Equal(SpatialLayerReadStatus.ValueTypeMismatch,integers.ReadLayer("sensor.spilled-oil",new TileRect(14,20,1,1),values),"fixed layer is not rounded to integer samples");
                c.Equal(SpatialLayerReadStatus.ValueTypeMismatch,fixedValues.ReadLayer("sensor.query-int",new TileRect(1,1,1,1),exact),"integer layer is not silently retyped as fixed");
                c.Equal(SpatialLayerReadStatus.InvalidRegion,integers.ReadLayer("sensor.query-int",new TileRect(int.MaxValue,0,2,1),values),"wide envelope check precedes overflowing endpoint arithmetic");
                c.Equal(SpatialLayerReadStatus.InvalidRegion,integers.ReadLayer("sensor.query-int",new TileRect(0,0,-1,1),values),"negative extent refuses");
                c.Equal(SpatialLayerReadStatus.Success,integers.ReadLayer("sensor.query-int",new TileRect(0,0,0,1),values),"empty valid region succeeds without values");
                c.Equal(0,values.Count,"empty integer query returns no invented sample");
                c.Throws<ArgumentNullException>(()=>integers.ReadLayer("missing",new TileRect(-1,0,1,1),null),"healthy null integer buffer is an argument refusal");
                c.Equal(hash,game.Hash(),"all typed public queries preserve engine state");
                var twin=factory.Load(game.Save());c.Equal(hash,twin.Hash(),"authored byte/signed samples and typed bindings survive save/load");
                c.That(ReferenceEquals(Integers(twin),Samples(twin))&&!ReferenceEquals(integers,Integers(twin)),"saved world recreates one independent shared query owner");
                c.Equal(SpatialLayerReadStatus.Success,Integers(twin).ReadLayer("sensor.query-int",new TileRect(1,1,3,2),values),"restored integer contract reads original provider");
                c.That(values.SequenceEqual(new[]{int.MinValue,int.MaxValue,0,120000,-70000,0}),"saved extrema are exact, not initialization regrants");
                if(native){
                    c.Equal(SpatialLayerReadStatus.Success,game.Seat(0).View.Feature<TileMapView>().ReadLayer("sensor.query-int",new TileRect(1,1,3,2),values),"native facade delegates integer reads to shared owner");
                    c.That(values.SequenceEqual(new[]{int.MinValue,int.MaxValue,0,120000,-70000,0}),"native facade preserves the same signed range");
                }
                game.Update();twin.Update();c.Equal(game.Hash(),twin.Hash(),"native Update continues exact saved typed sample state");
            }
        }
        private sealed class AliasReadModel : IProbeFirst,IProbeSecond { }
        private static void AliasDeclarations(Check c)
        {
            var game=New(IntegerFactory(false));ulong original=game.Hash();
            var primary=game.Seat(0).View.FeatureRegistrations.Single(r=>r.Contract==typeof(TileLayerView));
            c.Throws<InvalidOperationException>(()=>new ViewFeatureRegistration("proof.alias.foreign",1,"proof.foreign",
                typeof(ISpatialLayerQuery<TileRect,int>),primary),"another module cannot claim an existing primary read model");
            c.That(game.Fault==null,"invalid declaration is not a failure of the already healthy session");
            c.Equal(original,game.Hash(),"rejected foreign alias leaves original world unchanged");
            foreach(string mode in new[]{"missing-primary","incompatible","duplicate-contract","duplicate-id","alias-primary"}){
                var initialized=new InitializeProbe();
                c.Throws<InvalidOperationException>(()=>New(IntegerFactory(false,composer=>{
                    composer.AddSystem(initialized,300);
                    if(mode=="missing-primary"){
                        composer.AddViewFeatureAlias<IProbeFirst,AliasReadModel>("proof.alias.first",1);return;
                    }
                    composer.AddViewFeature("proof.alias.root",1,(world,viewer)=>new AliasReadModel());
                    if(mode=="incompatible")composer.AddViewFeatureAlias<IDisposable,AliasReadModel>("proof.alias.bad",1);
                    else if(mode=="duplicate-id")composer.AddViewFeatureAlias<IProbeFirst,AliasReadModel>("proof.alias.root",1);
                    else {
                        composer.AddViewFeatureAlias<IProbeFirst,AliasReadModel>("proof.alias.first",1);
                        if(mode=="duplicate-contract")composer.AddViewFeatureAlias<IProbeFirst,AliasReadModel>("proof.alias.second",1);
                        else composer.AddViewFeatureAlias<IProbeSecond,IProbeFirst>("proof.alias.second",1);
                    }
                })),"invalid explicit view alias refuses "+mode);
                c.Equal(0,initialized.Initializations,"invalid alias refuses before any system Initialize "+mode);
            }
        }
        // Malformed provider instrumentation only. No normal player driver writes stock or samples.
        private static void IntegerFaults(Check c)
        {
            foreach(string mode in new[]{"byte-fraction","byte-negative","byte-overflow","int-fraction"}){
                var factory=IntegerFactory(false);var game=New(factory);var idle=New(factory);ulong idleHash=idle.Hash();
                string id=mode.StartsWith("byte",StringComparison.Ordinal)?"sensor.query-byte":"sensor.query-int";
                var layer=(SensorSampleLayer)TileSpatialGeometry.RequireSampleLayer(game.World,(ITileSpatialSpace)game.World.Space,
                    game.World.Content.Get<ISpatialLayerDefinition>(id),false);
                int reads=0;layer.SampleResultProbe=(tile,actual)=>++reads!=2?actual:
                    mode=="byte-negative"?Fix.FromInt(-1):mode=="byte-overflow"?Fix.FromInt(256):Fix.Half;
                var query=Integers(game);var values=new List<int>{42};
                c.Throws<InvalidOperationException>(()=>query.ReadLayer(id,new TileRect(1,1,2,1),values),"malformed sample faults instead of truncating "+mode);
                var first=game.Fault;c.That(first!=null&&reads==2,"converter validates actual second sample "+mode);
                c.Equal(0,values.Count,"valid first sample cannot escape as a published prefix "+mode);
                c.That(first!=null&&first.Message.Contains(mode.EndsWith("fraction",StringComparison.Ordinal)?"fractional sample":"outside its declared representation"),"actual representation violation is the original cause "+mode);
                var exact=new List<Fix>{Fix.One};c.Throws<InvalidOperationException>(()=>Samples(game).ReadLayer("missing",new TileRect(-1,0,1,1),exact),"other typed interface cannot mask first fault");
                c.Equal(0,exact.Count,"cross-contract fault clears retained output");
                c.Throws<InvalidOperationException>(()=>query.ReadLayer("missing",new TileRect(-1,0,1,1),null),"null integer buffer cannot mask original provider fault");
                long time=game.Clock.StepIndex;game.Update();c.Equal(time,game.Clock.StepIndex,"malformed integer stops sole clock");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"no healthy hash after invalid representation");
                c.Throws<InvalidOperationException>(()=>game.Save(),"no save after invalid representation");
                c.That(ReferenceEquals(first,game.Fault),"first fault remains stable across typed queries");
                c.Equal(idleHash,idle.Hash(),"separate world remains untouched");idle.Update();
                c.That(idle.Fault==null&&idle.Clock.StepIndex>0,"separate provider continues native Update");
            }
        }
    }
}
