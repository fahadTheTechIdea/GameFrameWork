using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpatialPublicationFaultTests
    {
        internal const string Case = "Spatial publication: actual notification failure retains first fault and forbids healthy continuation";
        internal const string GuardCase = "Spatial resource guards: pure refusals stay healthy while scoped mutations retain first fault";
        internal const string DirectCase = "Spatial material publication: direct writes and generation retain first fault and actual stock";
        internal const string SiblingCase = "Spatial resource guards: sibling layer codec and revision mutations retain original fault";
        internal const string GetterCase = "Spatial observation: getter entry exit and initial codec cannot establish mutated baselines";
        internal const string CheckpointCase = "Spatial checkpoints: save hash and breakdown codecs are read-only and restore remains writable";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(GuardCase, Guards); TestRunner.Add(DirectCase, DirectNotifications); TestRunner.Add(SiblingCase, SiblingGuards); TestRunner.Add(GetterCase, GetterGuards); TestRunner.Add(CheckpointCase, CheckpointCodecs); TestRunner.Add(OperationCase, OperationAdmission); }
        private sealed class NotificationGuard : ISpatialResourceCellGuard
        {
            private readonly ISpatialResourceCellGuard _actual;
            internal readonly Exception Error = new InvalidOperationException("proof.spatial-notification-first-error");
            internal Action Notification;
            internal Action Preflight;
            internal Action OwnerNotification;
            internal Action OwnerPreflight;
            internal Action Availability;
            internal Action VersionRead, QuantityRead, CodecRead;
            internal int Calls;
            internal NotificationGuard(ISpatialResourceCellGuard actual) { _actual = actual; }
            public ResourceEndpointId Endpoint => _actual.Endpoint;
            public void WriteResourceState(IStateSync writer) => _actual.WriteResourceState(writer);
            public World World => _actual.World;
            public PlayerId Company => _actual.Company;
            public int Version { get { VersionRead?.Invoke(); return _actual.Version; } }
            public ISpatialLayer Layer => _actual.Layer;
            public Fix Quantity { get { QuantityRead?.Invoke(); return _actual.Quantity; } }
            public bool AllowsQuantity(Fix value) { bool result=_actual.AllowsQuantity(value); Availability?.Invoke(); return result; }
            public void PreflightQuantityChange(Fix value) { _actual.PreflightQuantityChange(value); Preflight?.Invoke(); }
            public void QuantityChanged() { Calls++; _actual.QuantityChanged(); if (Notification != null) Notification(); else throw Error; }
            public void PreflightOwnerChange() { _actual.PreflightOwnerChange(); OwnerPreflight?.Invoke(); }
            public void OwnerChanged() { _actual.OwnerChanged(); if(OwnerNotification!=null){Calls++;OwnerNotification();} }
            public Action StageSync(IStateSync sync, Fix quantity) { CodecRead?.Invoke(); return _actual.StageSync(sync, quantity); }
        }
        private static FieldInfo FindField(object owner, string name)
        {
            for (Type type = owner.GetType(); type != null; type = type.BaseType) {
                var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new InvalidOperationException("missing actual protocol owner field " + name);
        }
        private static object Field(object owner,string name)=>FindField(owner,name).GetValue(owner);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static void Guards(Check c)
        {
            foreach (bool native in new[] { false, true }) foreach (string mode in new[] { "refusal", "clock", "entity", "clock-throw", "entity-throw" }) {
                var factory = IndependentTileWorkTests.Factory(nativeTiles: native, extraction: true);
                var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                var definition = game.World.Content.Get<ISpatialLayerDefinition>("sensor.deposit");
                var samples = TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(game.World, (ITileSpatialSpace)game.World.Space, definition, false);
                var endpoint = game.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(game.World.Content.Get<ISpatialResourceCellDefinition>("sensor.cell"));
                var guard = new NotificationGuard((ISpatialResourceCellGuard)endpoint);
                int calls = 0;
                guard.Preflight = () => {
                    calls++;
                    if (mode.StartsWith("clock",StringComparison.Ordinal)) game.Clock.Pause();
                    if (mode.StartsWith("entity",StringComparison.Ordinal)) game.World.Entities.All[0].Touch();
                    if (mode == "refusal" || mode.EndsWith("-throw",StringComparison.Ordinal)) throw guard.Error;
                };
                var cells = (SortedDictionary<long, ISpatialResourceCellGuard>)Field(Field(samples,"_resourceCells"),"_cells");
                cells[((long)20 << 32) | 14] = guard;
                ulong before = game.Hash(); int version = endpoint.Version;
                var mutation = new[] { new TileSampleMutation(definition,new TileCoord(14,20),Fix.FromInt(10)) };
                Exception observed = null;
                try { ((ITileSampleMutationSpace)game.World.Space).ApplySamples(mutation); } catch (Exception error) { observed = error; }
                c.That(observed != null && calls == 1,"actual pure guard invocation refuses before publication");
                c.Equal(Fix.FromInt(6),endpoint.Amount(definition.Resource.Resource),"guard failure creates no requested material debit");
                c.Equal(version,endpoint.Version,"guard failure creates no endpoint version prefix");
                c.Equal(0,guard.Calls,"guard failure invokes no post-publication notification");
                if (mode == "refusal") {
                    c.That(ReferenceEquals(guard.Error,observed) && game.Fault == null,"ordinary unchanged refusal preserves exact exception and healthy session");
                    c.Equal(before,game.Hash(),"unchanged refusal retains original saved state");
                    c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"ordinary refusal remains resumable");
                }
                else {
                    c.That(game.Fault != null && game.Fault.Phase == "spatial.guard","scoped preflight mutation latches original world fault");
                    if (mode.EndsWith("-throw",StringComparison.Ordinal)) c.That(ReferenceEquals(guard.Error,observed),"mutation plus refusal preserves original thrown error instance");
                    else c.That(observed is InvalidOperationException && observed.Message.Contains("entity observation mutated"),"silent guard mutation violates original observation scope");
                    var first = game.Fault;
                    c.Throws<InvalidOperationException>(() => game.Save(),"mutating guard cannot save as healthy");
                    c.Throws<InvalidOperationException>(() => game.Hash(),"mutating guard cannot hash as healthy");
                    c.Throws<InvalidOperationException>(() => ((ITileSampleMutationSpace)game.World.Space).ApplySamples(mutation),"mutating guard is not retried");
                    c.Equal(1,calls,"faulted guard callback remains single invocation");
                    c.Equal(0,game.Update(),"mutating preflight stops further game updates");
                    c.That(ReferenceEquals(first,game.Fault),"first preflight fault remains stable");
                }
                c.Equal(isolated,idle.Hash(),"guard refusal or mutation does not leak to another session");
                idle.Update(); c.That(idle.Fault == null,"unrelated session still updates normally");
            }
            OwnershipGuards(c);
        }
        private static (GameFactory Factory, SimGame Game, SimGame Idle, ISpatialLayerProvider Space, NotificationGuard Guard, string LayerId)
            OwnershipProbe(string representation, bool stockPoint = false)
        {
            bool point=representation=="points";
            var factory=point?SpatialExtractionTests.Points():IndependentTileWorkTests.Factory(nativeTiles:representation=="native",extraction:true);
            var game=point?SpatialExtractionTests.Create(factory,true):New(factory);var idle=point?SpatialExtractionTests.Create(factory,true):New(factory);
            var space=(ISpatialLayerProvider)game.World.Space;string layerId=point?"point.deposit":"sensor.deposit";
            space.AssignOwner(game.World,layerId,new PlayerId(0));
            if(point&&stockPoint) {
                var result=((Beep.SimEngine.ExtensionProof.PointSpace)space).WriteLayer(game.World,new PlayerId(0),layerId,Fix.FromInt(10),Fix.FromInt(12));
                if(!result.IsNone)throw new InvalidOperationException("direct material protocol seed refused "+result);
            }
            var layer=game.World.Content.Get<ISpatialResourceCellDefinition>(point?"point.extract.cell":"sensor.cell");
            var endpoint=game.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(layer);
            var guard=new NotificationGuard((ISpatialResourceCellGuard)endpoint);
            var cells=(SortedDictionary<long,ISpatialResourceCellGuard>)Field(Field(guard.Layer,"_resourceCells"),"_cells");
            cells[point?Fix.FromInt(10).Raw:((long)20<<32)|14]=guard;
            return(factory,game,idle,space,guard,layerId);
        }
        private static ISpatialLayer Sibling(ISpatialLayerProvider space,bool point)
        {
            foreach(var layer in space.SpatialLayers)if(layer.Id==(point?"point.terrain":"sensor.screen"))return layer;
            throw new InvalidOperationException("actual sibling layer absent");
        }
        private static void ChangeSibling(Check c,SimGame game,ISpatialLayerProvider provider,string representation,bool hideRevision)
        {
            bool point=representation=="points";var sibling=Sibling(provider,point);var space=game.World.Space;int revision=space.ObservationVersion;
            object counter=representation=="independent"?(object)sibling:space;
            string field=point?"_observerVersion":"_version";object old=Field(counter,field);
            if(point)((Beep.SimEngine.ExtensionProof.PointSpace)provider).WriteLayer(game.World,PlayerId.None,sibling.Id,Fix.One,Fix.One);
            else ((ITileWritableSampleLayer)sibling).SetSample(new TileCoord(0,0),Fix.One);
            if(hideRevision) {
                FindField(counter,field).SetValue(counter,old);
                c.Equal(revision,space.ObservationVersion,"negative codec probe deliberately retains original spatial revision");
            }
        }
        private static void CheckpointCodecs(Check c)
        {
            // Explicit serializer contract/fault probe, not player-side simulation rules.
            foreach(string representation in new[]{"points","native","independent"})
                foreach(string entry in new[]{"save","hash","breakdown"})
                    foreach(string mode in new[]{"spatial","spatial-throw","hidden","hidden-throw","clock","entity","random","restore","throw","observe","healthy"}) {
                        var probe=OwnershipProbe(representation,true);var game=probe.Game;var guard=probe.Guard;bool point=representation=="points";
                        ulong before=game.Hash(),isolated=probe.Idle.Hash();Fix stock=guard.Quantity;int version=guard.Version,calls=0;
                        StateReader restore=null;
                        if(mode=="restore")SaveGame.ReadHeader(game.Save(),out restore);
                        var retainedEntity=game.World.Entities.All[0];
                        guard.CodecRead=()=>{
                            guard.CodecRead=null;calls++;
                            if(mode.StartsWith("spatial",StringComparison.Ordinal)||mode.StartsWith("hidden",StringComparison.Ordinal)) {
                                try{ChangeSibling(c,game,probe.Space,representation,mode.StartsWith("hidden",StringComparison.Ordinal));}
                                catch{if(mode.EndsWith("throw",StringComparison.Ordinal))throw guard.Error;throw;}
                                if(mode.EndsWith("throw",StringComparison.Ordinal))throw guard.Error;
                            }
                            if(mode=="clock")game.Clock.Pause();
                            if(mode=="entity")retainedEntity.Touch();
                            if(mode=="random")game.World.Random.NextULong();
                            if(mode=="restore")game.World.Sync(restore);
                            if(mode=="observe")c.That(game.Observer.Space.Space==game.World.Space.Id,"nested read-only projection remains permitted during encoding");
                            if(mode=="throw")throw guard.Error;
                        };
                        Exception observed=null;byte[] save=null;ulong? hash=null;IReadOnlyList<KeyValuePair<string,ulong>> parts=null;bool returned=false;
                        try {
                            if(entry=="save")save=game.Save();
                            else if(entry=="hash")hash=game.Hash();
                            else parts=game.HashBreakdown();
                            returned=true;
                        }catch(Exception error){observed=error;}
                        string context=representation+"/"+entry+"/"+mode;
                        c.That(calls==1&&game.World.ObservationCaptureDepth==0,"checkpoint callback once and capture scratch released "+context);
                        c.That(guard.Quantity==stock&&guard.Version==version,"checkpoint changes no actual endpoint material/book "+context);
                        if(mode=="healthy"||mode=="observe") {
                            c.That(returned&&observed==null&&game.Fault==null,"healthy original codec succeeds "+context);
                            c.Equal(before,game.Hash(),"healthy encoding leaves original state unchanged "+context);
                            c.Equal(before,probe.Factory.Load(save??game.Save()).Hash(),"normal restore can apply original complete state "+context);
                        }
                        else {
                            c.That(!returned&&observed!=null&&save==null&&!hash.HasValue&&parts==null,"failed encoding publishes no payload/hash/parts "+context);
                            c.That(game.Fault!=null&&game.Fault.Phase=="checkpoint.encode","ordinary encoding owns original terminal fault "+context);
                            if(mode.EndsWith("throw",StringComparison.Ordinal))c.That(ReferenceEquals(guard.Error,observed)&&game.Fault!=null&&game.Fault.Message==observed.GetType().Name+": "+observed.Message,"checkpoint retains original codec exception "+context);
                            if(mode=="restore")c.That(ReferenceEquals(retainedEntity,game.World.Entities.Get(retainedEntity.Id)),"reentrant restore refuses before replacing original entities "+context);
                            var sibling=Sibling(probe.Space,point);Fix actual;
                            if(point)actual=((SortedDictionary<long,Fix>)Field(sibling,"_samples")).TryGetValue(Fix.One.Raw,out Fix stored)?stored:Fix.Zero;
                            else actual=((ITileWritableSampleLayer)sibling).GetSample(new TileCoord(0,0));
                            c.Equal(Fix.Zero,actual,"checkpoint refuses spatial publication before a sibling prefix "+context);
                            var first=game.Fault;
                            c.Throws<InvalidOperationException>(()=>game.Hash(),"failed encoder cannot hash healthy "+context);
                            c.Throws<InvalidOperationException>(()=>game.Save(),"failed encoder cannot save healthy "+context);
                            c.That(game.Update()==0&&ReferenceEquals(first,game.Fault)&&calls==1,"first encoding fault stops Update/retry "+context);
                        }
                        c.Equal(isolated,probe.Idle.Hash(),"encoding remains session-local "+context);
                        probe.Idle.Update();c.That(probe.Idle.Fault==null,"unrelated encoder session remains playable "+context);
                    }
        }
        private static void GetterGuards(Check c)
        {
            // One-shot getter faults belong only to this explicit corrupt-provider protocol.
            foreach(string representation in new[]{"points","native","independent"})
                foreach(string entrance in new[]{"entry-quantity","exit-quantity","entry-version","codec"})
                    foreach(string mode in new[]{"spatial","spatial-throw","hidden","hidden-throw","healthy"}) {
                        var probe=OwnershipProbe(representation,true);var game=probe.Game;var guard=probe.Guard;bool point=representation=="points";
                        ulong before=game.Hash(),isolated=probe.Idle.Hash();Fix stock=guard.Quantity;int version=guard.Version;
                        int reads=0,methods=0;bool returned=false;Action getter=null;
                        getter=()=>{
                            reads++;guard.VersionRead=null;guard.QuantityRead=null;guard.CodecRead=null;
                            if(mode!="healthy") {
                                try { ChangeSibling(c,game,probe.Space,representation,mode.StartsWith("hidden",StringComparison.Ordinal)); }
                                catch { if(mode.EndsWith("throw",StringComparison.Ordinal))throw guard.Error;throw; }
                                if(mode.EndsWith("throw",StringComparison.Ordinal))throw guard.Error;
                            }
                        };
                        guard.Availability=()=>{methods++;if(entrance=="exit-quantity")guard.QuantityRead=getter;};
                        if(entrance=="entry-quantity")guard.QuantityRead=getter;
                        else if(entrance=="entry-version")guard.VersionRead=getter;
                        else if(entrance=="codec")guard.CodecRead=getter;
                        Exception observed=null;
                        try {
                            if(point)((ISpatialResourceQuantity<Fix>)guard.Layer).CheckWithdrawal(Fix.FromInt(10),Fix.One);
                            else ((ISpatialResourceQuantity<TileCoord>)guard.Layer).CheckWithdrawal(new TileCoord(14,20),Fix.One);
                            returned=true;
                        }catch(Exception error){observed=error;}
                        string context=representation+"/"+entrance+"/"+mode;
                        c.Equal(0,game.World.ObservationCaptureDepth,"baseline/codec capture always releases its session-local guard "+context);
                        c.Equal(1,reads,"one-shot getter/codec probe executes exactly once "+context);
                        c.That(guard.Company==new PlayerId(0)&&guard.Quantity==stock&&guard.Version==version&&guard.Calls==0,"getter probe changes no requested endpoint/material "+context);
                        if(mode=="healthy") {
                            c.That(returned&&observed==null&&game.Fault==null&&methods==1,"healthy getter retains original pure admission "+context);
                            c.Equal(before,game.Hash(),"healthy getter remains authoritative-state pure "+context);
                            c.Equal(before,probe.Factory.Load(game.Save()).Hash(),"healthy getter remains durably resumable "+context);
                        }
                        else {
                            var sibling=Sibling(probe.Space,point);Fix actualSibling;
                            if(point)actualSibling=((SortedDictionary<long,Fix>)Field(sibling,"_samples")).TryGetValue(Fix.One.Raw,out Fix stored)?stored:Fix.Zero;
                            else actualSibling=((ITileWritableSampleLayer)sibling).GetSample(new TileCoord(0,0));
                            bool duringCodec=entrance=="entry-version"||entrance=="codec";
                            c.Equal(duringCodec?Fix.Zero:Fix.One,actualSibling,"codec publication is refused before prefix; ordinary getter prefix is detected without rollback "+context);
                            c.That(!returned&&observed!=null&&game.Fault!=null,"mutating getter/initial codec cannot publish healthy admission "+context);
                            c.Equal(entrance=="exit-quantity"?1:0,methods,"no downstream guard dispatch after corrupt baseline "+context);
                            if(mode.EndsWith("throw",StringComparison.Ordinal))c.That(ReferenceEquals(guard.Error,observed)&&game.Fault!=null&&game.Fault.Message==observed.GetType().Name+": "+observed.Message,"getter/codec retains original thrown error "+context);
                            var first=game.Fault;
                            c.Throws<InvalidOperationException>(()=>game.Hash(),"mutating getter cannot hash healthy "+context);
                            c.Throws<InvalidOperationException>(()=>game.Save(),"mutating getter cannot save healthy "+context);
                            c.That(game.Update()==0&&ReferenceEquals(first,game.Fault),"getter first fault stops sole clock "+context);
                        }
                        c.Equal(isolated,probe.Idle.Hash(),"getter observation is session-local "+context);
                        probe.Idle.Update();c.That(probe.Idle.Fault==null,"unrelated getter session remains playable "+context);
                    }
        }
        private static void SiblingGuards(Check c)
        {
            // Explicit corrupt-provider protocol; never normal host/player resource mutation.
            foreach(string representation in new[]{"points","native","independent"})
                foreach(string operation in new[]{"availability","sample","owner","reserved-out","reserved-in"})
                    foreach(string mode in new[]{"refusal","spatial","spatial-throw","codec-only","codec-throw","healthy"}) {
                        var probe=OwnershipProbe(representation,true);var game=probe.Game;var guard=probe.Guard;bool point=representation=="points";
                        guard.Notification=()=>{};int calls=0;ulong isolated=probe.Idle.Hash();Fix opening=guard.Quantity;
                        IResourceOutlet endpoint=null;var requestOwner=new ReservationOwner("proof.sibling-guard",1);ReservationToken receipt=default;
                        bool reserved=operation.StartsWith("reserved-",StringComparison.Ordinal);
                        if(reserved) {
                            endpoint=game.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(game.World.Content.Get<ISpatialResourceCellDefinition>(point?"point.extract.cell":"sensor.cell"));
                            receipt=operation=="reserved-out"?endpoint.ReserveOut(requestOwner,guard.Layer.Resource.Resource,Fix.One):endpoint.ReserveIn(requestOwner,guard.Layer.Resource.Resource,Fix.One);
                        }
                        ulong before=game.Hash();int version=guard.Version;
                        ISpatialLayer sibling=Sibling(probe.Space,point);
                        Action changeSibling=()=>ChangeSibling(c,game,probe.Space,representation,mode.StartsWith("codec",StringComparison.Ordinal));
                        Action callback=()=>{
                            calls++;
                            if(reserved) {
                                c.Equal(Fix.One,(Fix)Field(endpoint,operation=="reserved-out"?"_committing":"_receiving"),"actual guard runs inside original reserved material publication interval");
                                using(var memory=new MemoryStream()) {
                                    var writer=new StateWriter(memory);
                                    c.Throws<CorruptStateException>(()=>guard.StageSync(writer,guard.Quantity),"active resource publication still refuses actual checkpoint serialization");
                                }
                            }
                            if(mode!="refusal"&&mode!="healthy")changeSibling();
                            if(mode=="refusal"||mode.EndsWith("throw",StringComparison.Ordinal))throw guard.Error;
                        };
                        if(operation=="availability")guard.Availability=callback;
                        else if(operation=="owner")guard.OwnerPreflight=callback;
                        else guard.Preflight=callback;
                        Action mutate;
                        if(operation=="owner")mutate=()=>probe.Space.AssignOwner(game.World,probe.LayerId,new PlayerId(1));
                        else if(reserved)mutate=operation=="reserved-out"?()=>endpoint.TakeReserved(requestOwner,receipt,Fix.One):()=>endpoint.ReceiveReserved(game.World.Content,requestOwner,receipt,Fix.One);
                        else if(point) {
                            var quantity=(ISpatialResourceQuantity<Fix>)guard.Layer;
                            mutate=operation=="availability"?()=>quantity.CheckWithdrawal(Fix.FromInt(10),Fix.One):()=>quantity.Withdraw(Fix.FromInt(10),Fix.One);
                        }
                        else {
                            var quantity=(ISpatialResourceQuantity<TileCoord>)guard.Layer;var location=new TileCoord(14,20);
                            mutate=operation=="availability"?()=>quantity.CheckWithdrawal(location,Fix.One):()=>quantity.Withdraw(location,Fix.One);
                        }
                        Exception observed=null;try{mutate();}catch(Exception error){observed=error;}
                        string context=representation+"/"+operation+"/"+mode;
                        if(mode=="healthy") {
                            c.That(observed==null&&game.Fault==null&&calls>0,"healthy original spatial guard succeeds "+context);
                            Fix delta=operation=="sample"||operation=="reserved-out"?-Fix.One:operation=="reserved-in"?Fix.One:Fix.Zero;
                            c.That(guard.Company==new PlayerId(operation=="owner"?1:0)&&guard.Quantity==opening+delta,"healthy requested owner/material change uses original endpoint "+context);
                            if(reserved)c.Equal(0,endpoint.Reservations.Count,"healthy original receipt consumed exactly once "+context);
                            c.Equal(game.Hash(),probe.Factory.Load(game.Save()).Hash(),"healthy guarded original state roundtrips "+context);
                        }
                        else {
                            c.That(observed!=null&&calls==1,"first invalid spatial guard refuses "+context);
                            c.That(guard.Company==new PlayerId(0)&&guard.Quantity==opening&&guard.Version==version&&guard.Calls==0,"guard refusal publishes no requested endpoint/material prefix "+context);
                            if(reserved)c.Equal(1,endpoint.Reservations.Count,"guard refusal retains exact original owned receipt "+context);
                            if(mode=="refusal") {
                                c.That(ReferenceEquals(guard.Error,observed)&&game.Fault==null,"unchanged refusal retains original cause and healthy world "+context);
                                c.Equal(before,game.Hash(),"unchanged refusal retains actual saved state "+context);
                            }
                            else {
                                Fix siblingValue=point?((SortedDictionary<long,Fix>)Field(sibling,"_samples"))[Fix.One.Raw]:((ITileWritableSampleLayer)sibling).GetSample(new TileCoord(0,0));
                                c.Equal(Fix.One,siblingValue,"explicit raw protocol confirms actual sibling mutation, not fabricated rollback "+context);
                                c.That(game.Fault!=null&&game.Fault.Phase=="spatial.guard","sibling mutation latches original guard fault "+context);
                                if(mode.EndsWith("throw",StringComparison.Ordinal))c.That(ReferenceEquals(guard.Error,observed)&&game.Fault!=null&&game.Fault.Message==observed.GetType().Name+": "+observed.Message,"mutating sibling refusal retains original thrown cause "+context);
                                var first=game.Fault;
                                c.Throws<InvalidOperationException>(()=>game.Hash(),"sibling mutation cannot hash healthy "+context);
                                c.Throws<InvalidOperationException>(()=>game.Save(),"sibling mutation cannot save healthy "+context);
                                c.That(game.Update()==0&&ReferenceEquals(first,game.Fault)&&calls==1,"sibling first fault stops Update/retry "+context);
                            }
                        }
                        c.Equal(isolated,probe.Idle.Hash(),"sibling guard fault remains session-local "+context);
                        probe.Idle.Update();c.That(probe.Idle.Fault==null,"unrelated sibling-guard session remains playable "+context);
                    }
        }
        private static void DirectNotifications(Check c)
        {
            // Explicit low-level contract probe, not a second gameplay driver or inventory owner.
            foreach(string representation in new[]{"points","native","independent"})
                foreach(string operation in representation=="native"?new[]{"set","withdraw","deposit","prepared-withdraw","prepared-deposit","fill"}
                    :new[]{"set","withdraw","deposit","prepared-withdraw","prepared-deposit"})
                    foreach(string mode in new[]{"throw","clock","entity","success"}) {
                        var probe=OwnershipProbe(representation,true);var game=probe.Game;var guard=probe.Guard;
                        ulong isolated=probe.Idle.Hash();Fix opening=guard.Quantity;int version=guard.Version;
                        bool deposit=operation.EndsWith("deposit",StringComparison.Ordinal);Action mutate,invalid;
                        if(representation=="points") {
                            var quantity=(ISpatialResourceQuantity<Fix>)guard.Layer;Fix location=Fix.FromInt(10);
                            invalid=()=>quantity.Withdraw(-Fix.One,Fix.Zero);
                            if(operation=="set") {
                                Fix after=quantity.CheckWithdrawal(location,Fix.One).StoredAfter;
                                mutate=()=>((Beep.SimEngine.ExtensionProof.PointSpace)probe.Space).WriteLayer(game.World,new PlayerId(0),probe.LayerId,location,after);
                            }
                            else if(operation.StartsWith("prepared-",StringComparison.Ordinal)) {
                                var prepared=deposit?quantity.PrepareDeposit(location,Fix.One):quantity.PrepareWithdrawal(location,Fix.One);
                                mutate=()=>prepared.Commit();
                            }
                            else mutate=deposit?()=>quantity.Deposit(location,Fix.One):()=>quantity.Withdraw(location,Fix.One);
                        }
                        else {
                            var quantity=(ISpatialResourceQuantity<TileCoord>)guard.Layer;var location=new TileCoord(14,20);
                            invalid=()=>quantity.Withdraw(new TileCoord(-1,0),Fix.Zero);
                            if(operation=="set") {
                                Fix after=quantity.CheckWithdrawal(location,Fix.One).StoredAfter;
                                mutate=()=>((ITileWritableSampleLayer)guard.Layer).SetSample(location,after);
                            }
                            else if(operation=="fill") {
                                byte after=checked((byte)(quantity.CheckWithdrawal(location,Fix.One).StoredAfter.Raw/Fix.One.Raw));
                                mutate=()=>((TileMap)game.World.Space).Layer<ByteLayer>(probe.LayerId).Fill((x,y)=>x==14&&y==20?after:(byte)0);
                            }
                            else if(operation.StartsWith("prepared-",StringComparison.Ordinal)) {
                                var prepared=deposit?quantity.PrepareDeposit(location,Fix.One):quantity.PrepareWithdrawal(location,Fix.One);
                                mutate=()=>prepared.Commit();
                            }
                            else mutate=deposit?()=>quantity.Deposit(location,Fix.One):()=>quantity.Withdraw(location,Fix.One);
                        }
                        if(mode=="clock")guard.Notification=()=>game.Clock.Pause();
                        if(mode=="entity")guard.Notification=()=>game.World.Entities.All[0].Touch();
                        if(mode=="success")guard.Notification=()=>{};
                        Exception observed=null;try{mutate();}catch(Exception error){observed=error;}
                        string context=representation+"/"+operation+"/"+mode;
                        c.That(guard.Quantity==opening+(deposit?Fix.One:-Fix.One)&&guard.Version==version+1&&guard.Calls==1,"actual direct material prefix exactly once "+context);
                        if(mode=="success") {
                            c.That(observed==null&&game.Fault==null,"healthy direct notification stays healthy "+context);
                            c.Equal(game.Hash(),probe.Factory.Load(game.Save()).Hash(),"healthy direct stock/book/cursor roundtrip "+context);
                        }
                        else {
                            c.That(observed!=null&&game.Fault!=null&&game.Fault.Phase=="spatial.mutation","direct failure latches original world fault "+context);
                            if(mode=="throw")c.That(ReferenceEquals(guard.Error,observed),"direct original exception retained "+context);
                            var first=game.Fault;
                            c.Throws<InvalidOperationException>(()=>game.Save(),"failed direct publication cannot save healthy "+context);
                            c.Throws<InvalidOperationException>(()=>game.Hash(),"failed direct publication cannot hash healthy "+context);
                            c.Throws<InvalidOperationException>(mutate,"failed direct publication cannot retry or no-op "+context);
                            c.Throws<InvalidOperationException>(invalid,"invalid direct shortcut cannot hide fault "+context);
                            c.That(game.Update()==0&&ReferenceEquals(first,game.Fault)&&guard.Calls==1,"direct first fault stops Update/retry "+context);
                        }
                        c.Equal(isolated,probe.Idle.Hash(),"direct publication remains session-local "+context);
                        probe.Idle.Update();c.That(probe.Idle.Fault==null,"unrelated session still plays "+context);
                    }
        }
        private static void OwnershipGuards(Check c)
        {
            foreach(string representation in new[]{"points","native","independent"})
                foreach(string mode in new[]{"refusal","clock","entity","clock-throw","entity-throw"}) {
                    var probe=OwnershipProbe(representation);var game=probe.Game;var guard=probe.Guard;int calls=0;
                    ulong before=game.Hash(),isolated=probe.Idle.Hash();int version=guard.Version;
                    int own=game.Seat(0).View.Space.Version,other=game.Seat(1).View.Space.Version;Fix quantity=guard.Quantity;
                    guard.OwnerPreflight=()=>{
                        calls++;
                        if(mode.StartsWith("clock",StringComparison.Ordinal))game.Clock.Pause();
                        if(mode.StartsWith("entity",StringComparison.Ordinal))game.World.Entities.All[0].Touch();
                        if(mode=="refusal"||mode.EndsWith("-throw",StringComparison.Ordinal))throw guard.Error;
                    };
                    Exception observed=null;
                    try{probe.Space.AssignOwner(game.World,probe.LayerId,new PlayerId(1));}catch(Exception error){observed=error;}
                    c.That(observed!=null&&calls==1,"actual ownership preflight refuses at first failing callback "+representation+"/"+mode);
                    c.That(guard.Company==new PlayerId(0)&&guard.Version==version&&guard.Quantity==quantity&&guard.Calls==0,"ownership preflight failure publishes no owner, endpoint, stock or notification prefix");
                    if(mode=="refusal") {
                        c.That(ReferenceEquals(guard.Error,observed)&&game.Fault==null,"pure ownership refusal preserves original exception and healthy world");
                        c.Equal(before,game.Hash(),"pure ownership refusal preserves authoritative saved state");
                        c.That(game.Seat(0).View.Space.Version==own&&game.Seat(1).View.Space.Version==other,"pure ownership refusal retains all viewer revisions");
                        c.Equal(before,probe.Factory.Load(game.Save()).Hash(),"pure ownership refusal remains resumable");
                        guard.OwnerPreflight=null;probe.Space.AssignOwner(game.World,probe.LayerId,new PlayerId(1));
                        c.That(guard.Company==new PlayerId(1)&&guard.Version==version+1&&guard.Quantity==quantity,"fresh healthy ownership transfer changes endpoint once without material duplication");
                        c.Equal(game.Hash(),probe.Factory.Load(game.Save()).Hash(),"healthy transferred cell uses original owner and book checkpoint");
                    }
                    else {
                        c.That(game.Fault!=null&&game.Fault.Phase=="spatial.guard","mutating ownership preflight retains sole original guard fault");
                        if(mode.EndsWith("-throw",StringComparison.Ordinal))c.That(ReferenceEquals(guard.Error,observed),"mutation plus ownership refusal retains original thrown cause");
                        var first=game.Fault;
                        c.Throws<InvalidOperationException>(()=>game.Save(),"mutating ownership guard cannot save as healthy");
                        c.Throws<InvalidOperationException>(()=>game.Hash(),"mutating ownership guard cannot hash as healthy");
                        c.Throws<InvalidOperationException>(()=>probe.Space.AssignOwner(game.World,probe.LayerId,new PlayerId(0)),"same-owner shortcut cannot hide guard fault");
                        c.That(game.Update()==0&&ReferenceEquals(first,game.Fault)&&calls==1,"ownership guard fault stops canonical Update and callback retries");
                    }
                    c.Equal(isolated,probe.Idle.Hash(),"ownership preflight failure is isolated from another session");
                    probe.Idle.Update();c.That(probe.Idle.Fault==null,"unrelated ownership session remains playable");
                }
        }
        private static void OwnershipNotifications(Check c)
        {
            foreach(string representation in new[]{"points","native","independent"})foreach(string mode in new[]{"throw","clock","entity"}) {
                bool point=representation=="points";
                var probe=OwnershipProbe(representation);var game=probe.Game;var idle=probe.Idle;var space=probe.Space;string layerId=probe.LayerId;
                ulong isolated=idle.Hash();var endpoint=probe.Guard;var guard=probe.Guard;
                guard.OwnerNotification=mode=="throw"?()=>throw guard.Error:mode=="clock"?()=>game.Clock.Pause():()=>game.World.Entities.All[0].Touch();
                int before=endpoint.Version;Exception observed=null;
                try{space.AssignOwner(game.World,layerId,new PlayerId(1));}catch(Exception error){observed=error;}
                c.That(observed!=null&&game.Fault!=null&&game.Fault.Phase=="spatial.mutation",representation+" ownership notification failure latches sole world publication fault "+mode);
                if(mode=="throw")c.That(ReferenceEquals(guard.Error,observed),"ownership preserves original thrown exception instance");
                c.That(endpoint.Company==new PlayerId(1)&&endpoint.Version==before+1&&guard.Calls==1,"explicit raw probe confirms real ownership publication began exactly once");
                var first=game.Fault;
                c.Throws<InvalidOperationException>(()=>game.Save(),"partial ownership cannot save as healthy");
                c.Throws<InvalidOperationException>(()=>game.Hash(),"partial ownership cannot hash as healthy");
                c.Throws<InvalidOperationException>(()=>space.AssignOwner(game.World,layerId,new PlayerId(1)),"same-owner no-op cannot hide original publication fault");
                if(point)c.Throws<InvalidOperationException>(()=>((Beep.SimEngine.ExtensionProof.PointSpace)space).SetAccess(true),"same-access no-op cannot hide original publication fault");
                c.That(game.Update()==0&&ReferenceEquals(first,game.Fault)&&guard.Calls==1,"failed ownership cannot continue or retry notification");
                c.Equal(isolated,idle.Hash(),"publication fault does not leak to another session");
                idle.Update();c.That(idle.Fault==null,"independent session remains playable");
            }
        }
        private static void Run(Check c)
        {
            foreach (bool native in new[] { false, true }) foreach (string mode in new[] { "throw", "clock", "entity" }) {
                var factory = IndependentTileWorkTests.Factory(nativeTiles: native, extraction: true);
                var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                var definition = game.World.Content.Get<ISpatialLayerDefinition>("sensor.deposit");
                var samples = TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(game.World, (ITileSpatialSpace)game.World.Space, definition, false);
                var endpoint = game.World.Services.Get<ISpatialResourceEndpoints>().ResolveCell(game.World.Content.Get<ISpatialResourceCellDefinition>("sensor.cell"));
                var guard = new NotificationGuard((ISpatialResourceCellGuard)endpoint);
                if (mode == "clock") guard.Notification = () => game.Clock.Pause();
                if (mode == "entity") guard.Notification = () => game.World.Entities.All[0].Touch();
                var bindings = Field(samples,"_resourceCells");
                var cells = (SortedDictionary<long, ISpatialResourceCellGuard>)Field(bindings,"_cells");
                long key = ((long)20 << 32) | 14; cells[key] = guard;
                var view = game.Seat(0).View; var foreign = game.Seat(1).View;
                var mutation = new[] { new TileSampleMutation(definition,new TileCoord(14,20),Fix.FromInt(10)) };
                Exception observed = null;
                try { ((ITileSampleMutationSpace)game.World.Space).ApplySamples(mutation); } catch (Exception error) { observed = error; }
                if (mode == "throw") c.That(ReferenceEquals(guard.Error,observed), "actual post-publication error instance is preserved");
                else c.That(observed is InvalidOperationException && observed.Message.Contains("entity observation mutated"), "nonthrowing clock/entity notification mutation is rejected");
                c.Equal(1,guard.Calls,"actual selected guard notification invoked once");
                c.That(game.Fault != null && game.Fault.Phase == "spatial.mutation" && observed != null && game.Fault.Message == observed.GetType().Name + ": " + observed.Message,
                    "sole world fault records the actual publication failure outside Update");
                var first = game.Fault;
                c.Equal(Fix.FromInt(5),endpoint.Amount(definition.Resource.Resource),"protocol probe confirms publication began before the notification error");
                c.Throws<InvalidOperationException>(() => game.Save(),"partial published state cannot save as healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(),"partial published state cannot hash as healthy");
                c.Throws<InvalidOperationException>(() => ((ITileSampleMutationSpace)game.World.Space).ApplySamples(mutation),"faulted mutation is not retried");
                c.Throws<InvalidOperationException>(() => ((ITileSampleMutationSpace)game.World.Space).ApplySamples(null),"first fault precedes invalid mutation shortcuts");
                foreach (var retained in new[] { view, foreign }) {
                    var rows = new List<Facet>();
                    c.Throws<InvalidOperationException>(() => retained.Inspect(default,rows),"faulted missing/private observation cannot publish healthy facts");
                    c.Throws<InvalidOperationException>(() => retained.Feature<KnowledgeView>().TryRead(new PlayerId(0),"sim.knowledge.facts","missing",out _),
                        "faulted private/empty knowledge cannot hide the mutation failure");
                }
                c.Equal(0,game.Update(),"faulted session performs no further update");
                c.That(ReferenceEquals(first,game.Fault) && guard.Calls == 1,"first fault remains stable and guard is never retried");
                c.Equal(isolated,idle.Hash(),"independent session receives no published values or fault");
                idle.Update(); c.That(idle.Fault == null,"unrelated session still updates normally");
            }
            OwnershipNotifications(c);
        }
    }
}
