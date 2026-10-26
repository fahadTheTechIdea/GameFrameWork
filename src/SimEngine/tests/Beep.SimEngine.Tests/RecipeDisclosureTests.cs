using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Authored files and ordinary engine player commands/views/Update/save/replay only.
    internal static class RecipeDisclosureTests
    {
        internal const string Case = "Recipe disclosure: authored resource unit and result knowledge gate metadata and ordinary manufacturing";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Capability = new Code("contract.process");
        internal static GameFactory Factory(bool direct, string hidden, bool half,
            Action<JsonValue> setupEdit = null, Action<JsonValue> recipeEdit = null, string knowledgeExtraDocument = null, Action<JsonValue> knowledgeEdit = null,
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null, Action<JsonValue> scenarioEdit = null, Action<JsonValue> worldEdit = null)
        {
            var setup = Json.Parse(FrameworkProofContent.Read("capacity-contracts.json"));
            setup.Set("transformations", JsonValue.NewArray());
            setupEdit?.Invoke(setup);
            var recipes = Json.Parse(FrameworkProofContent.Read("transformations-contracts.json"));
            recipes.Get("independent_transformations")[0].Get("duration").Set("numerator", JsonValue.Number("4"));
            recipeEdit?.Invoke(recipes);
            if (!direct) { recipes.Set("transformations",recipes.Get("independent_transformations")); recipes.Remove("independent_transformations"); }
            var knowledge = Json.Parse(FrameworkProofContent.Read("recipe-disclosure.json"));
            knowledge.Get("knowledge_visibility_policies")[0].Set("rules",Json.Parse("[\"recipe.hide-"+hidden+"\"]"));
            knowledgeEdit?.Invoke(knowledge);
            var modules = new List<GameModule> {new CapacityContractModule(new InitializeProbe()), new TransformationContractModule(),
                new AutomationKnowledgeVisibilityModule()};
            if (extraModules != null) modules.AddRange(extraModules);
            const string declarations = "[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]},{\"id\":\"proof.transformation-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformations-contracts.json\"]},{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"recipe-disclosure.json\"]}]";
            var manifest = Json.Parse(declarations);
            if (extraDeclarations != null)
            { var additional = Json.Parse(extraDeclarations); for (int i = 0; i < additional.Count; i++) manifest.Add(additional[i].Clone()); }
            var documents = new Dictionary<string,string>{{"capacity-contracts.json",setup.ToString()},{"transformations-contracts.json",recipes.ToString()},{"recipe-disclosure.json",knowledge.ToString()}};
            if (extraDocuments != null) foreach (var pair in extraDocuments) documents.Add(pair.Key, pair.Value);
            if (knowledgeExtraDocument != null)
            {
                manifest[2].Get("documents").Add(JsonValue.Of(knowledgeExtraDocument));
                documents.Add(knowledgeExtraDocument, FrameworkProofContent.Read(knowledgeExtraDocument));
            }
            return ConstructionPlanTests.Points(extraModules:modules, extraDeclarations:manifest.ToString(), extraDocuments:documents,
                startupProfiles:new[]{"contract.capacity-start"},
                scenarioEdit:scenario=>{foreach(var module in modules)scenario.Get("modules").Add(JsonValue.Of(module.Id));
                    scenario.Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId));
                    scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId,Json.Parse("{\"policy\":\"recipe.visibility\"}")); scenarioEdit?.Invoke(scenario);},
                worldEdit:root=>{root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy",JsonValue.Of("contract.carrier"));
                    root.Get("resources")[0].Set("capacity",Json.Parse("{\"unit.capacity.count\":1}"));
                    if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30")); worldEdit?.Invoke(root);});
        }
        private static TransformationManagementView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<TransformationManagementView>();
        private static void Until(SimGame game,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<512&&!done();i++)Step(game,twin);
            if(!done())throw new InvalidOperationException(stage+" exceeded512Updates at="+game.Clock.StepIndex+" process="+Process(game).Phase+":"+Process(game).Blocker+":"+Process(game).Progress.WholeTicks);
        }
        private static void Hidden(Check c,SimGame game)
        {
            var view=View(game);var actor=Entity(game,"contract.factory");ulong hash=game.Hash();
            c.That(!view.TransformationsOf(actor,Capability).Contains("contract.make"),"whole hidden recipe omitted from engine menu");
            c.That(view.ReadExecutionPreview(actor,Capability,"contract.make",out var preview)==Codes.UnknownDef&&preview==null,"hidden execution preview has no partial metadata");
            c.That(view.ReadInputPorts(actor,Capability,"contract.make",out var ports)==Codes.UnknownDef&&ports.Count==0,"hidden port details withheld");
            c.That(view.ReadToolConsumptionRates(actor,Capability,"contract.make",out var rates)==Codes.UnknownDef&&rates.Count==0,"hidden rate/unit details withheld");
            c.That(view.TransformationEntityOutput("contract.make")==null&&view.PreviewEntityOutput(actor,Capability,"contract.make").Output==null,"hidden output identity withheld");
            var command=new EnqueueTransformationCommand{Transformer=actor,Capability=Capability,Transformation="contract.make"};
            c.Equal(Codes.UnknownDef,game.Seat(0).Preview(command),"ordinary command agrees with shared disclosure");
            c.Equal(Codes.UnknownDef,view.PreviewEnqueue(actor,Capability,"contract.make",1,false,EntityId.None),"management preview agrees");
            c.That(Process(game).QueueCount==0&&hash==game.Hash(),"all hidden reads/previews leave queue, stock, RNG and sole clock unchanged");
        }
        internal static void Physical(Check c,bool direct,string hidden,bool half,Action<Check,SimGame,bool> metadataCheck=null)
        {
            var factory=Factory(direct,hidden,half);var game=ConstructionPlanTests.NewPoints(factory);var idle=ConstructionPlanTests.NewPoints(factory);ulong idleHash=idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"discovery/manufacturing needs no business module");
            var limits=new ReplayLimits(4*1024*1024,4000);var recording=game.RecordReplay(limits);
            Hidden(c,game);metadataCheck?.Invoke(c,game,false);c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"hidden authored visibility survives save/load");
            // A refused command also uses the engine, not a test-side disclosure decision.
            int sequence=game.Seat(0).Submit(new EnqueueTransformationCommand{Transformer=Entity(game,"contract.factory"),Capability=Capability,Transformation="contract.make"});
            Step(game);long cursor=0;var results=new List<CommandResult>();game.Seat(0).ReadResults(ref cursor,results);
            c.That(results.Single(x=>x.Sequence==sequence).Refusal==Codes.UnknownDef&&Process(game).QueueCount==0,"hidden execution creates no queue prefix");
            Enable(c,game);
            Order(c,game,new PostOrderCommand{Plan="recipe.discover",Target=Entity(game,"source")});
            var work=game.Seat(0).View.Feature<WorkView>();
            var twin=factory.Load(game.Save());
            Until(game,twin,()=>View(game).TransformationsOf(Entity(game,"contract.factory"),Capability).Contains("contract.make"),"physical timed discovery");
            c.That(work.TryReadActivityCompletion("activity:recipe.discover",Entity(game,"source"),out var completion)&&completion.CompletedTick>completion.CreatedTick,"only completed physical work earns content visibility");
            metadataCheck?.Invoke(c,game,true);
            var view=View(game);var actor=Entity(game,"contract.factory");ulong hash=game.Hash();
            c.That(view.ReadExecutionPreview(actor,Capability,"contract.make",out var retained)==Code.None&&retained.Inputs.AmountOf(Resource(game))==Fix.FromInt(3),"known recipe returns exact authored input");
            c.Equal("contract.result",view.TransformationEntityOutput("contract.make"),"known result identity");
            c.Equal(hash,game.Hash(),"known projections remain pure");
            c.That(View(game,1).ReadExecutionPreview(actor,Capability,"contract.make",out var foreign)==Codes.NotFound&&foreign==null,"foreign company still has no processor metadata");
            Order(c,game,new EnqueueTransformationCommand{Transformer=actor,Capability=Capability,Transformation="contract.make"});
            Until(game,null,()=>Process(game).Progress.WholeTicks>0,"real supplied productive manufacture");
            var partial=Process(game);twin=factory.Load(game.Save());game.Clock.Pause();twin.Clock.Pause();long at=game.Clock.StepIndex;ulong paused=game.Hash();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==paused,"one clock pauses physical work");game.Clock.Resume();twin.Clock.Resume();
            Until(game,twin,()=>Process(game).QueueCount==0,"earned manufactured result");
            c.That(game.Seat(0).Snapshot.Entities.Count(x=>x.Def=="contract.result")==1&&partial.Progress.WholeTicks<partial.Required,"one real timed result, retained partial detached");
            c.That(Stock(game,"source")==Fix.FromInt(16)&&Stock(game,"contract.factory")==Fix.One&&Stock(game,"machine")==Fix.Zero,"physical four-unit supply consumes three and retains reusable tool");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"known completed state restores");
            c.Equal(idleHash,idle.Hash(),"interleaved undiscovered session stays isolated");
            c.That(new ReplayVerifier().Verify(factory,recording.Finish(),limits).Success,"refusal/discovery/supply/manufacturing command replay");
            c.That(retained.Current.QueueCount==0,"retained idle preview stays detached");
        }
        private static ResourceId Resource(SimGame game)
        {if(!game.Seat(0).View.TryFindResource("supply",out var resource))throw new InvalidOperationException("missing visible supply");return resource;}
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual changed engine build");
            foreach(bool direct in new[]{false,true})foreach(string hidden in new[]{"resource","quantity","capacity","result"})Physical(c,direct,hidden,hidden=="quantity"||hidden=="capacity");
        }
    }
}

