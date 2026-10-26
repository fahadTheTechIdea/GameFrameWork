using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class ResourceFlowView
    {
        private readonly IResourceFlowJournal _journal;
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal ResourceFlowView(World world, PlayerId viewer)
        { _world = world; _journal = world.Services.Get<IResourceFlowJournal>(); _viewer = viewer; }
        /// <summary>Foreign-company refusal reveals neither facts nor sequence watermarks.</summary>
        public ResourceFlowPage Read(PlayerId company, long cursor)
        {
            _world.RequireHealthyUpdate();
            return _viewer.IsValid && company != _viewer ? new ResourceFlowPage(ResourceFlowReadStatus.NotOwned)
                : EntityObservation.ReadProvider(_world, _journal, null, () => Disclose(_journal.Read(company, cursor)));
        }
        public ResourceFlowSummary ReadSummary(PlayerId company)
        {
            _world.RequireHealthyUpdate();
            return _viewer.IsValid && company != _viewer ? new ResourceFlowSummary(ResourceFlowReadStatus.NotOwned)
                : EntityObservation.ReadProvider(_world, _journal, null, () => _journal.ReadSummary(company));
        }
        public ResourceFlowPage ReadWindow(PlayerId company, long fromTick, long throughTick)
        {
            _world.RequireHealthyUpdate();
            return _viewer.IsValid && company != _viewer ? new ResourceFlowPage(ResourceFlowReadStatus.NotOwned)
                : EntityObservation.ReadProvider(_world, _journal, null, () => Disclose(_journal.ReadWindow(company, fromTick, throughTick)));
        }
        private ResourceFlowPage Disclose(ResourceFlowPage page)
        {
            foreach (var entry in page.Entries)
            {
                var fact = entry.Item;
                if (!_world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(fact.Resource)) || !ResourceLotSelection.Visible(_world, _viewer, fact.Selection))
                    return new ResourceFlowPage(ResourceFlowReadStatus.Undisclosed);
                foreach (var lot in fact.Lots) if (!ResourceLotSelection.LotVisible(_world, _viewer, lot))
                    return new ResourceFlowPage(ResourceFlowReadStatus.Undisclosed);
            }
            return page;
        }
        /// <summary>Evaluate an existing JSON profile at the sole current game time, without host window/filter rules.</summary>
        public Code ReadAuthoredThroughput(PlayerId company, string definitionId, out ResourceFlowObservation observation)
        {
            observation = null; _world.RequireHealthyUpdate();
            if (_viewer != PlayerId.None && company != _viewer) return Codes.NotOwned;
            var definition = _world.Content.Find<IResourceFlowQueryDefinition>(definitionId);
            if (definition == null || !_world.ViewFor(_viewer).CanSeeContent(definition)) return Codes.NotFound;
            var query = EntityObservation.ReadProvider(_world, definition, null,
                () => ResourceFlowQueryDeclaration.Create(definition, company, _world.Time, _world.Tick), observeTargetState: true);
            observation = ReadThroughput(query); return Code.None;
        }
        /// <summary>Measured committed output only; an expired window never reports partial or invented throughput.</summary>
        public ResourceFlowObservation ReadThroughput(ResourceFlowQuery query)
        {
            _world.RequireHealthyUpdate();
            if (query == null) throw new System.ArgumentNullException(nameof(query));
            if (_viewer != PlayerId.None && query.Company != _viewer)
                return ResourceFlowObservation.Refused(query,ResourceFlowReadStatus.NotOwned);
            if (!ResourceLotSelection.Visible(_world, _viewer, query.Selection))
                return ResourceFlowObservation.Refused(query, ResourceFlowReadStatus.Undisclosed);
            return EntityObservation.ReadProvider(_world,_journal,null,()=>{
                // Entire retained window is withheld if it would disclose hidden lot/axis metadata.
                var window = Disclose(_journal.ReadWindow(query.Company, query.FromTick, query.ThroughTick));
                if (window.Status == ResourceFlowReadStatus.Undisclosed) return ResourceFlowObservation.Refused(query, window.Status);
                var value=_journal.ReadThroughput(query);
                if (value==null || !ReferenceEquals(value.Query,query))
                    throw new System.InvalidOperationException("flow observation must name its actual immutable query");
                return value;
            });
        }
    }
}
