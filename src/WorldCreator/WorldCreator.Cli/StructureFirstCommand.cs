using WorldCreator.Core;

namespace WorldCreator.Cli;

/// <summary>Thin CLI adapter; capability and publication belong to the pipeline.</summary>
internal static class StructureFirstCommand
{
    public static async Task<string> RunAsync(string[] arguments, CancellationToken token)
    {
        var pipeline = new StructureFirstPipeline();
        if (arguments.Length == 4 && arguments[1] == "plan")
        {
            var world = await StructureFirstPipeline.ReadJsonAsync<WorldDocument>(arguments[2], 536870912, token);
            await StructureFirstPipeline.SaveSpecificationAsync(pipeline.Plan(world, token), arguments[3], token);
            return "Saved structure jobs and scale-aware scope plan; city defaults are partial district samples.";
        }
        if (arguments.Length == 6 && arguments[1] == "build")
        {
            var world = await StructureFirstPipeline.ReadJsonAsync<WorldDocument>(arguments[2], 536870912, token);
            var specification = await StructureFirstPipeline.ReadJsonAsync<StructureFirstSpecification>(arguments[3], token: token);
            var result = await pipeline.ExportAsync(world, specification, arguments[4], arguments[5], token);
            return $"Exported {result.Assembly.Instances.Count} exact C# structure instances; coverage complete: {result.CoverageComplete}; production blockers: {result.ProductionIssues.Count}.";
        }
        throw new ArgumentException("Expected structure-pipeline plan <world.json> <new-library.json> OR structure-pipeline build <world.json> <library.json> <trusted-asset-root> <new-output-dir>.");
    }
}
