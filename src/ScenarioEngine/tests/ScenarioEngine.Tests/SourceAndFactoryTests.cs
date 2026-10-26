using System.Text;
using Counter.GameLayer;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;

namespace ScenarioEngine.Tests;

public sealed class SourceAndFactoryTests
{
    [Fact]
    public async Task ContractSourceLoadsAndGameFactoryUsesCommonLifecycle()
    {
        var h = new Harness();
        IScenarioContentService content = h.Content;
        var source = new ByteSource(Encoding.UTF8.GetBytes(Harness.Pack().ToJsonString()));
        var loaded = await content.LoadAsync(source, TestContext.Current.CancellationToken);
        Assert.True(loaded.IsReady);
        IGameFactory factory = new GameFactory(h.Factory, h.Registry);
        using var game = factory.Create(loaded.Scenarios[0]);
        Assert.IsAssignableFrom<GameBase>(game);
        game.Init(new(loaded.Scenarios[0], Harness.Options(), Harness.Frame(0)));
        game.Submit(Harness.Increment());
        Assert.True(game.Update(Harness.Input(1)).Committed);
    }

    [Fact]
    public async Task CancellationAfterSourceReadDoesNotRegisterContent()
    {
        var h = new Harness();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var source = new ByteSource(Encoding.UTF8.GetBytes(Harness.Pack().ToJsonString()), () => cancellation.Cancel());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Content.LoadAsync(source, cancellation.Token));
        Assert.Empty(h.Content.ListPlayable());
    }

    [Fact]
    public async Task SourceFailureIsReportedAndDoesNotRegisterContent()
    {
        var h = new Harness();
        var loaded = await h.Content.LoadAsync(new BrokenSource(), TestContext.Current.CancellationToken);
        Assert.False(loaded.IsReady);
        Assert.Contains(loaded.Report.Diagnostics, d => d.Code == "PACK_SOURCE_FAILED");
        Assert.Contains(h.Reporter.Notices, n => n.Code == "PACK_SOURCE_FAILED" && n.Exception is IOException);
        Assert.Empty(h.Content.ListPlayable());
    }

    [Fact]
    public void RemovalStopsNewLaunchesButRunningSessionKeepsItsDefinitions()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var session = h.Start(scenario);
        var removal = h.Content.RemoveFromCatalog(scenario.Identity.PackId, scenario.Identity.ContentDigest);
        Assert.True(removal.Removed);
        Assert.Empty(h.Content.ListPlayable());
        Assert.False(h.Content.TryResolve(scenario.Identity, out _));
        session.Enqueue(Harness.Increment());
        Assert.True(session.Step(Harness.Input(1)).Committed);
        Assert.Equal("PACK_NOT_INSTALLED", h.Content.RemoveFromCatalog(scenario.Identity.PackId, scenario.Identity.ContentDigest).Code);
    }

    [Fact]
    public void FailedAuditLeavesCatalogInstalled()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        h.Reporter.FailAudit = true;
        Assert.False(h.Content.RemoveFromCatalog(scenario.Identity.PackId, scenario.Identity.ContentDigest).Removed);
        Assert.Single(h.Content.ListPlayable());
    }

    [Fact]
    public void FactoryRejectsUnregisteredLayer()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        IGameFactory factory = new GameFactory(h.Factory, h.Registry);
        Assert.Equal("LAYER_UNAVAILABLE", Assert.Throws<ScenarioContractException>(() =>
            factory.Create(scenario with { Identity = scenario.Identity with { LayerId = "not_registered" } })).Code);
    }

    private sealed class ByteSource(byte[] bytes, Action? afterRead = null) : IScenarioPackSource
    {
        public Task<ReadOnlyMemory<byte>> ReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            afterRead?.Invoke();
            return Task.FromResult<ReadOnlyMemory<byte>>(bytes);
        }
    }
    private sealed class BrokenSource : IScenarioPackSource
    {
        public Task<ReadOnlyMemory<byte>> ReadAsync(CancellationToken cancellationToken) =>
            throw new IOException("Injected source outage.");
    }
}
