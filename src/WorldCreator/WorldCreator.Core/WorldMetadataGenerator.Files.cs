using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldCreator.Core.Metadata;

public sealed partial class WorldMetadataGenerator
{
    public static async Task<T> ReadAsync<T>(string file,CancellationToken token=default)
    {
        await using var stream=File.OpenRead(file);
        if(stream.Length is <=0 or >16777216)throw new ArgumentException("METADATA_INPUT_SIZE_LIMIT");
        var options=WorldJson.CreateOptions();options.RespectRequiredConstructorParameters=true;
        options.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow;options.MaxDepth=32;
        return await JsonSerializer.DeserializeAsync<T>(stream,options,token) ?? throw new ArgumentException("METADATA_INPUT_REQUIRED");
    }
    public async Task<WorldMetadataDocument> GenerateFilesAsync(string recipeFile,string catalogFile,string newOutputDirectory,CancellationToken token=default)
    {
        var recipe=await ReadAsync<WorldMetadataRecipe>(recipeFile,token);
        var catalog=await ReadAsync<WorldMetadataCatalog>(catalogFile,token);
        var world=Generate(recipe,catalog,token);
        string target=Path.GetFullPath(newOutputDirectory),parent=Path.GetDirectoryName(target)!;
        if(Directory.Exists(target) || File.Exists(target))throw new IOException("METADATA_OUTPUT_EXISTS");
        Directory.CreateDirectory(parent);string staging=Path.Combine(parent,".metadata-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(staging);
        try
        {
            await Write("recipe.json",recipe);await Write("catalog.json",catalog);await Write("world-metadata.json",world);
            await Write("summary.json",new { world.SchemaVersion,world.Id,world.GameProfile,world.Scale,world.Coverage,world.RemainingScopes,
                Nodes=world.Nodes.Count,Connections=world.Connections.Count,
                Kinds=world.Nodes.GroupBy(n=>n.Kind).OrderBy(g=>g.Key,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Count()),
                world.RecipeSha256,world.CatalogSha256,Scope="Metadata generation only; no art, mockups, physics or game runtime required or implied." });
            await File.WriteAllTextAsync(Path.Combine(staging,"review.html"),Review(world,token),token);
            await new WorldMetadataSchematicExporter().WriteAsync(world,staging,token);
            token.ThrowIfCancellationRequested();Directory.Move(staging,target);return world;
            async Task Write<T>(string name,T value)=>await File.WriteAllTextAsync(Path.Combine(staging,name),JsonSerializer.Serialize(value,WorldJson.CreateOptions(true)),token);
        }
        finally
        {
            if(Directory.Exists(staging) && Path.GetDirectoryName(Path.GetFullPath(staging))==parent && Path.GetFileName(staging).StartsWith(".metadata-",StringComparison.Ordinal))Directory.Delete(staging,true);
        }
    }
    private static string Review(WorldMetadataDocument world,CancellationToken token)
    {
        string E(string? text)=>WebUtility.HtmlEncode(text ?? "");
        var b=new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>World metadata</title><style>body{font:16px system-ui;background:#13242c;color:#eaf1f2;margin:28px}a{color:#77ddd0}details{margin:8px 0 8px 20px;border-left:1px solid #49626e;padding:6px 12px}summary{cursor:pointer}pre{white-space:pre-wrap;overflow-wrap:anywhere}td,th{padding:8px;border-bottom:1px solid #49626e;text-align:left}</style>");
        b.Append($"<h1>{E(world.Id)}</h1><p>{E(world.GameProfile)} · {E(world.Scale)} · {world.Coverage} · {world.Nodes.Count} nodes · {world.Connections.Count} connections</p><p>Engine-independent metadata. Images and artwork are optional consumers, not generation inputs.</p>");
        if(world.RemainingScopes.Count>0)b.Append("<p>Scope remains: "+E(string.Join(", ",world.RemainingScopes))+"</p>");
        var children=world.Nodes.Where(n=>n.ParentId is not null).GroupBy(n=>n.ParentId!).ToDictionary(g=>g.Key,g=>g.ToArray());
        int shown=0;
        Node(world.Nodes.Single(n=>n.ParentId is null));
        if(world.Nodes.Count>shown)b.Append("<p>First 2000 nodes shown; all nodes are in the JSON.</p>");
        b.Append("<h2>Connections</h2><table><tr><th>Kind</th><th>From</th><th>To</th><th>Direction</th></tr>");
        // Large graphs stay in JSON; the review is bounded and explicitly says when truncated.
        foreach(var link in world.Connections.Take(500))b.Append($"<tr><td>{E(link.Kind)}</td><td>{E(link.FromId)}</td><td>{E(link.ToId)}</td><td>{(link.Bidirectional?"both":"forward")}</td></tr>");
        if(world.Connections.Count>500)b.Append("<tr><td colspan=\"4\">First 500 connections shown; all connections are in the JSON.</td></tr>");
        b.Append("</table><p><a href=\"world-metadata.json\">Complete metadata</a> · <a href=\"recipe.json\">Recipe</a> · <a href=\"catalog.json\">Reusable catalog</a> · <a href=\"summary.json\">Summary</a></p></html>");return b.ToString();
        void Node(WorldMetadataNode node)
        {
            token.ThrowIfCancellationRequested();
            if(shown>=2000)return;
            shown++;
            b.Append($"<details{(node.ParentId is null?" open":"")}><summary>{E(node.Kind)} · {E(node.Id)}</summary><p>{E(node.Purpose)}</p>");
            if(node.Position is not null)b.Append("<p>"+E(string.Join(", ",node.Position.Select(p=>p.ToString("R",System.Globalization.CultureInfo.InvariantCulture))))+" "+E(world.Space.Unit)+"</p>");
            if(node.StructureKey is not null)b.Append("<p>Catalog structure: "+E(node.StructureKey)+"</p>");
            if(node.Properties.Count>0)b.Append("<pre>"+E(JsonSerializer.Serialize(node.Properties,WorldJson.CreateOptions(true)))+"</pre>");
            if(children.TryGetValue(node.Id,out var nested))foreach(var child in nested)Node(child);
            b.Append("</details>");
        }
    }
}
