using System.Reflection;
using System.Runtime.Loader;

namespace WorldCreator.Core.Planning;

/// <summary>Explicitly selected trusted code only. Assembly dependency isolation is not a security sandbox.</summary>
public sealed class SettlementCatalogPlugin : IDisposable
{
    private readonly PluginContext context;
    public IReadOnlyList<ISettlementCatalogProvider> Providers { get; }
    public SettlementCatalogPlugin(string assemblyPath,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        string path=Path.GetFullPath(assemblyPath);
        if (!File.Exists(path) || Path.GetExtension(path)!=".dll") throw new ArgumentException("CATALOG_PLUGIN_PATH_INVALID");
        context=new(path);
        try
        {
            var assembly=context.LoadFromAssemblyPath(path);
            var providers=new List<ISettlementCatalogProvider>();
            foreach (var type in assembly.GetTypes().Where(t=>!t.IsAbstract && !t.IsInterface && typeof(ISettlementCatalogProvider).IsAssignableFrom(t))
                         .OrderBy(t=>t.FullName,StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                providers.Add((ISettlementCatalogProvider)(Activator.CreateInstance(type) ?? throw new ArgumentException("CATALOG_PLUGIN_ENTRY_INVALID")));
            }
            if (providers.Count==0) throw new ArgumentException("CATALOG_PLUGIN_PROVIDER_MISSING");
            Providers=providers.AsReadOnly();
        }
        catch { context.Unload(); throw; }
    }
    public void Dispose() => context.Unload();
    private sealed class PluginContext(string path) : AssemblyLoadContext(isCollectible:true)
    {
        private readonly AssemblyDependencyResolver resolver=new(path);
        protected override Assembly? Load(AssemblyName name)
        {
            var shared=typeof(ISettlementCatalogProvider).Assembly;
            if (name.Name==shared.GetName().Name)
            {
                if (!AssemblyName.ReferenceMatchesDefinition(name,shared.GetName())) throw new ArgumentException("CATALOG_PLUGIN_CONTRACT_MISMATCH");
                return shared;
            }
            var dependency=resolver.ResolveAssemblyToPath(name);
            return dependency is null?null:LoadFromAssemblyPath(dependency);
        }
        protected override nint LoadUnmanagedDll(string name)
        {
            var dependency=resolver.ResolveUnmanagedDllToPath(name);
            return dependency is null?0:LoadUnmanagedDllFromPath(dependency);
        }
    }
}
