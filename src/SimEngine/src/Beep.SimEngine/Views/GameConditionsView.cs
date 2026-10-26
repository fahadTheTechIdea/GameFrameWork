using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    /// <summary>Private pure queries through the sole condition guard; no result cache or game state.</summary>
    public sealed class GameConditionsView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal GameConditionsView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public Code CheckCondition(string id)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(_viewer)) return Codes.NotSeated;
            var condition = _world.Content.Find<IGameCondition>(id);
            if (condition == null) return Codes.UnknownDef;
            return GameConditionContracts.Read(_world, _world.ViewFor(_viewer), condition);
        }
    }
}
