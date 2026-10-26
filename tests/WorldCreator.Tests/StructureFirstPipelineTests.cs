using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using WorldCreator.Core;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;
using WorldCreator.Godot;

internal static class StructureFirstPipelineTests
{
    public static async Task RunAsync(string? cli = null, string? godotOutput = null)
    {
        string root = Path.Combine(Path.GetTempPath(), "worldcreator-structure-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var pipeline = new StructureFirstPipeline();
            var world = await new DesertWorldGenerator(TestCatalogs.Registry).GenerateAsync(MockupGenerationFixture.Create(64,64,1));
            string before = StructureFirstPipeline.WorldDigest(world);
            var spec = pipeline.Plan(world);
            var empty = await pipeline.BuildAsync(world, spec, root);
            Check(empty.CoverageComplete && spec.Scopes.Single(s => s.Kind == VisualScopeKind.Settlement).SettlementClass == SettlementClass.Town,
                "Town can use one complete settlement overview");
            Check(empty.StructureJobs.Count == 1 && empty.StructureJobs[0].InstanceCount == empty.Assembly.Instances.Count &&
                empty.MockupJobs.All(j => j.Status == "AwaitingStructureMasters"), "one reusable master job; concepts wait for masters");
            Check(empty.Assembly.Instances.Select(i => i.Placement.Id).ToHashSet().SetEquals(world.Objects.Where(o => o.VisualSource?.BuildingDefinitionId is not null).Select(o => o.Id)), "exact IDs retained");
            foreach (var item in empty.Assembly.Instances)
                Check(JsonSerializer.Serialize(item.Placement)==JsonSerializer.Serialize(world.Design!.Objects.Single(o=>o.Id==item.Placement.Id)), "unchanged position/footprint/provenance");
            var cityRules = world.Request.Design!.SettlementRules.Select(r => r with { Settlement = r.Settlement! with { Class = SettlementClass.City } }).ToArray();
            var city = world with { Request = world.Request with { Design = world.Request.Design with { SettlementRules = cityRules } } };
            var citySpec = pipeline.Plan(city); var cityDraft = await pipeline.BuildAsync(city, citySpec, root);
            Check(!cityDraft.CoverageComplete && cityDraft.ProductionIssues.Any(i => i.StartsWith("PARTIAL_SETTLEMENT_SAMPLE")) &&
                citySpec.Scopes.Any(s => s.Kind == VisualScopeKind.District), "small City model stays a district/sample, not complete city");
            var tooBig = await pipeline.BuildAsync(world, spec with { MaximumStructuresPerMockup = 1 }, root);
            Check(tooBig.ProductionIssues.Any(i => i.StartsWith("MOCKUP_SCOPE_REQUIRES_AUTHORED_SUBDIVISION")), "image budget requires explicit subdivision");
            var missingDistrict = spec.Scopes.Append(new StructureVisualScope("unplanned-settlement", "world", VisualScopeKind.Settlement,
                "future", SettlementClass.Town, [])).ToArray();
            Check(!(await pipeline.BuildAsync(world, spec with { Scopes = missingDistrict }, root)).CoverageComplete, "unplanned required scope prevents coverage rollup");
            await Reject(() => pipeline.BuildAsync(world, spec with { WorldSha256 = new string('0',64) }, root), "stale world");
            await Reject(() => pipeline.BuildAsync(world, spec with { Art = spec.Art.Concat(spec.Art).ToArray() }, root), "duplicate art mapping");
            await Reject(() => pipeline.BuildAsync(world, spec with { Art = [] }, root), "missing art mapping");
            var duplicateScope = spec.Scopes.Append(spec.Scopes.Last() with { Id = "duplicate" }).ToArray();
            await Reject(() => pipeline.BuildAsync(world, spec with { Scopes = duplicateScope }, root), "duplicate scope ownership");
            await Reject(() => pipeline.BuildAsync(world, spec with { Scopes = spec.Scopes.Select(s => s.Kind==VisualScopeKind.Settlement ? s with { ParentId="missing" } : s).ToArray() }, root), "invalid ancestry");
            var png = await File.ReadAllBytesAsync("tests/fixtures/mockup-atlas/closed.png");
            await File.WriteAllBytesAsync(Path.Combine(root,"source.png"),png);
            string imageHash = Convert.ToHexStringLower(SHA256.HashData(png));
            spec = spec with { Art = spec.Art.Select(e => e with { Master = new("source.png",imageHash) }).ToArray() };
            var candidate = await pipeline.BuildAsync(world,spec,root);
            Check(candidate.MockupJobs.All(j => j.Status=="ReadyForAgentImageCall") && candidate.ProductionIssues.Count>0,
                "verified candidate enables host image jobs but grants no production approval");
            await Reject(() => pipeline.BuildAsync(world, spec with { Art = spec.Art.Select(e => e with { Master = new("source.png",new string('0',64)) }).ToArray() },root), "source tampering");
            await Reject(() => pipeline.BuildAsync(world, spec with { Art = spec.Art.Select(e => e with { Master = new("../escape.png",imageHash) }).ToArray() },root), "path escape");
            var concept = new StructureScopeConcept(spec.Scopes.Single(s=>s.Kind==VisualScopeKind.Settlement).Id,"synthetic-town","v001",new("source.png",imageHash));
            var premature = pipeline.Plan(world) with { Concepts = [concept] };
            await Reject(() => pipeline.BuildAsync(world,premature,root), "concept before masters");
            spec = spec with { Concepts = [concept] };
            string atlasFile = Path.Combine(root,"base.atlas.json");
            var atlas = new JsonObject {
                ["schema_version"]="1.0",["image"]="source.png",["image_size"]=new JsonArray(16,16),["mask_color"]="#FF00FF",
                ["background_alpha"]=255,["pixels_per_meter"]=1,["frames"]=new JsonArray(new JsonObject {
                    ["name"]="base",["rect"]=new JsonArray(0,0,16,16),["pivot"]=new JsonArray(8,8),["source_sha256"]=imageHash,
                    ["anchors"]=new JsonObject { ["ground-nw"]=new JsonArray(2,3),["ground-ne"]=new JsonArray(14,3),["ground-se"]=new JsonArray(14,13),["ground-sw"]=new JsonArray(2,13) }
                })
            };
            await File.WriteAllTextAsync(atlasFile,atlas.ToJsonString());
            await File.WriteAllTextAsync(Path.Combine(root,"bundle.json"), """
                {"schema_version":"1.0","asset_id":"synthetic-warehouse","directions":["S"],
                "components":[{"id":"base","parent":null,"atlases":{"S":"base.atlas.json"}}],
                "sampled_poses":[{"direction":"S","normalized_time":0,"nodes":{"base":{"frame_index":0,"world_matrix_2d":[[1,0,0],[0,1,0],[0,0,1]]}}}]}
                """);
            ApprovedArtManifest Manifest() => new(1,"synthetic-warehouse","v001","test",ArtApprovalState.Approved,
                [new("base-sheet","source.png","base.atlas.json",imageHash,Hash(atlasFile),"base",["S"])],[]);
            spec = spec with { Art=spec.Art.Select(e=>e with { Production=new("synthetic-warehouse","v001","bundle.json","base","S","base-sheet","base") }).ToArray() };
            var allowed = new Authority(true); var denied = new Authority(false);
            var accepted = await pipeline.BuildProductionAsync(world,spec,root,new Registry(Manifest()),allowed,allowed);
            Check(accepted.Ready && accepted.Frames.Count==empty.Assembly.Instances.Count, "actual native bundle/hash/atlas/ground-calibration and existing assembler accept synthetic static structures");
            var unapproved = await pipeline.BuildProductionAsync(world,spec,root,new Registry(Manifest()),denied,denied);
            Check(!unapproved.Ready && unapproved.Frames.Count==0, "external approval authority rejects despite approved-looking input");
            atlas["allow_near_key"]=true; await File.WriteAllTextAsync(atlasFile,atlas.ToJsonString());
            Check(!(await pipeline.BuildProductionAsync(world,spec,root,new Registry(Manifest()),allowed,allowed)).Ready, "diagnostic atlas cannot become production");
            atlas.Remove("allow_near_key");
            atlas["frames"]![0]!["anchors"]!["ground-nw"] = new JsonArray(3,3);
            await File.WriteAllTextAsync(atlasFile,atlas.ToJsonString());
            var badGround = await pipeline.BuildProductionAsync(world,spec,root,new Registry(Manifest()),allowed,allowed);
            Check(!badGround.Ready && badGround.Issues.Any(i=>i.StartsWith("STRUCTURE_GROUND_CALIBRATION_MISMATCH")), "wrong calibrated ground cannot warp a structure into its site");
            atlas["frames"]![0]!["anchors"]!["ground-nw"]=new JsonArray(2,3); await File.WriteAllTextAsync(atlasFile,atlas.ToJsonString());
            accepted=await pipeline.BuildProductionAsync(world,spec,root,new Registry(Manifest()),allowed,allowed);
            string export=Path.Combine(root,"packet"); await pipeline.ExportAsync(world,spec,root,export);
            var restoredSpec=await StructureFirstPipeline.ReadJsonAsync<StructureFirstSpecification>(Path.Combine(export,"library.json"));
            var resumed=await pipeline.BuildAsync(world,restoredSpec,export);
            Check(resumed.Assembly.Instances.Count==empty.Assembly.Instances.Count && File.Exists(Path.Combine(export,"review.html")), "portable draft packet resumes using copied exact source bytes");
            string beforeOutput=Hash(Path.Combine(export,"instance-assembly.json"));
            await Reject(()=>pipeline.ExportAsync(world,spec,root,export),"existing output refusal");
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();
            await Reject(()=>pipeline.ExportAsync(world,spec,root,Path.Combine(root,"cancelled"),cancelled.Token),"cancellation");
            Check(beforeOutput==Hash(Path.Combine(export,"instance-assembly.json")) && !Directory.Exists(Path.Combine(root,"cancelled")),"failure/cancellation preserve output");
            Check(StructureFirstPipeline.WorldDigest(world)==before,"all geometry and source data remain unchanged");
            if(godotOutput is not null) await new GodotStructureLayerExporter().ExportAsync(accepted,godotOutput);
            if(cli is not null)
            {
                string worldFile=Path.Combine(root,"world.json"), specFile=Path.Combine(root,"library.json");
                await File.WriteAllTextAsync(worldFile,JsonSerializer.Serialize(world,WorldJson.CreateOptions()));
                await File.WriteAllTextAsync(specFile,JsonSerializer.Serialize(spec,WorldJson.CreateOptions()));
                Check((await Run("structure-pipeline","plan",worldFile,Path.Combine(root,"planned.json")))==0,"CLI plan");
                Check((await Run("structure-pipeline","build",worldFile,specFile,root,Path.Combine(root,"cli-packet")))==0,"actual CLI packet");
                Check((await Run("structure-pipeline","build",worldFile,specFile,root,Path.Combine(root,"cli-packet")))==1,"actual CLI rejected-output preservation");
                string audit=await File.ReadAllTextAsync(Path.Combine(root,".gameframework/operations/audit.jsonl"));
                Check(audit.Contains("world.structure-pipeline.succeeded") && audit.Contains("world.structure-pipeline.failed"),"shared audit covers success and failure");
                async Task<int> Run(params string[] arguments)
                {
                    var info=new ProcessStartInfo("dotnet"){WorkingDirectory=root,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
                    info.ArgumentList.Add(cli); foreach(var argument in arguments)info.ArgumentList.Add(argument);
                    using var process=Process.Start(info)!; var stdout=process.StandardOutput.ReadToEndAsync(); var stderr=process.StandardError.ReadToEndAsync();
                    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    try { await process.WaitForExitAsync(timeout.Token); } catch(OperationCanceledException){process.Kill(true);throw;}
                    _=await stdout; _=await stderr; return process.ExitCode;
                }
            }
            Console.WriteLine("PASS: structures-first Town/City scope coverage, master-before-concept jobs, exact C# instance assembly, persistence/CLI/audit, external approvals/native bundle/ground calibration, negative inputs and cancellation (synthetic production art).");
        }
        finally
        {
            if(Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase) && Path.GetFileName(root).StartsWith("worldcreator-structure-tests-",StringComparison.Ordinal)) Directory.Delete(root,true);
        }
    }
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static async Task Reject(Func<Task> action,string message)
    { try { await action(); } catch(Exception ex) when(ex is ArgumentException or IOException or OperationCanceledException or JsonException) { return; } throw new Exception("Missing rejection: "+message); }
    private static void Check(bool ok,string message){if(!ok)throw new Exception("Structure pipeline: "+message);}
    private sealed class Authority(bool approved):IGameArtsApprovalAuthority
    { public Task<bool> IsApprovedAsync(string assetId,string revision,CancellationToken token=default)=>Task.FromResult(approved); }
    private sealed class Registry(ApprovedArtManifest manifest):IApprovedArtRegistry
    { public Task<ApprovedArtManifest?> ResolveAsync(string assetId,CancellationToken token=default)=>Task.FromResult<ApprovedArtManifest?>(manifest.AssetId==assetId?manifest:null); }
}
