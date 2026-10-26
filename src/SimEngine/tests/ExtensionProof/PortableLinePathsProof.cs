using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side observation proof only; no replacement construction, graph, progress or gameplay.
    public sealed class CorridorPathProbeDef : Def
    {
        public string Space { get; private set; }
        private string _path;
        public JsonValue Path => Json.Parse(_path);
        protected override void ReadCore(JsonReader reader)
        {
            Space = reader.String("space"); JsonValue path = reader.Array("path");
            if (path == null || path.Count == 0) throw reader.Error("explicit nonempty path required");
            _path = path.ToString();
        }
        protected override void ResolveCore(ContentResolver content) => content.Def<ISpatialSpaceDefinition>(Space);
    }
    public sealed class CorridorPathFacts
    {
        public JsonValue Path { get; }
        public string Identity { get; }
        public int Count => Path.Count;
        public CorridorPathFacts(JsonValue path, string identity) { Path = path; Identity = identity; }
    }
    public sealed class PointCorridorPathView
    {
        private readonly World _world;
        public PointCorridorPathView(World world) { _world = world; }
        public CorridorPathFacts Inspect(string id, JsonValue supplied = null)
        {
            var definition = _world.Content.Get<CorridorPathProbeDef>(id);
            var path = LineDeclaration.ReadPath(_world, definition.Space, supplied ?? definition.Path);
            return new CorridorPathFacts(LineDeclaration.WritePath(_world, definition.Space, path),
                LineDeclaration.PathIdentity(_world, definition.Space, path));
        }
    }
    public sealed class PortableLinePathsProofModule : GameModule
    {
        public override string Id => "proof.portable-line-paths";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement("points",Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<CorridorPathProbeDef>("corridor_path_probes");
        public override void Compose(WorldComposer composer)
        {
            foreach (var path in composer.Content.All<CorridorPathProbeDef>())
                LineDeclaration.ReadPath(composer.World,path.Space,path.Path);
            composer.AddViewFeature(Id,1,(world,viewer)=>new PointCorridorPathView(world));
        }
    }
}
