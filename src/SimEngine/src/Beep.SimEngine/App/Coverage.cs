using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;

namespace Beep.SimEngine.App
{
    /// <summary>
    /// Watches a game through its public feeds and records which of the game's features were exercised:
    /// every entity def that appeared, every command that was accepted, every kind of news. Compared with
    /// the game's catalogue, it says how much of the game a run (a demo, a soak, a playtest) really used.
    /// Generic: it knows nothing about any particular game.
    /// </summary>
    public sealed class CoverageTracker : AppComponent
    {
        private readonly SortedSet<string> _defs = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> _commands = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedSet<string> _news = new SortedSet<string>(StringComparer.Ordinal);
        private readonly List<CommandResult> _results = new List<CommandResult>();
        private readonly List<Notification> _notes = new List<Notification>();
        private long[] _resultCursors = new long[0];
        private long _newsCursor;
        private SimGame _game;

        public CoverageTracker() { UpdateOrder = 1000; }

        public IReadOnlyCollection<string> DefsSeen => _defs;
        public IReadOnlyCollection<string> CommandsAccepted => _commands;
        public IReadOnlyCollection<string> NewsSeen => _news;

        public override void Update(AppFrame frame, IReadOnlyList<InputAction> actions) => Observe(App.Game);

        /// <summary>Records what the game has shown since the last call. Callable without an app, for batch runs.</summary>
        public void Observe(SimGame game)
        {
            if (game == null) return;
            if (!ReferenceEquals(game, _game))
            {
                _game = game;
                _resultCursors = new long[game.SeatCount];
                _newsCursor = 0;
            }
            IReadOnlyList<EntityView> ents = game.Snapshot.Entities;
            for (int i = 0; i < ents.Count; i++)
                if (game.Observer.TryGetEntity(ents[i].Id, out EntityView v)) _defs.Add(v.Def);
            for (int s = 0; s < game.SeatCount; s++)
            {
                _results.Clear();
                game.Seat(s).ReadResults(ref _resultCursors[s], _results);
                for (int i = 0; i < _results.Count; i++) if (_results[i].Accepted) _commands.Add(_results[i].Command);
            }
            _notes.Clear();
            game.ReadAllNotifications(ref _newsCursor, _notes);
            for (int i = 0; i < _notes.Count; i++) _news.Add(_notes[i].Kind.Id);
        }

        /// <summary>The catalogue entries (entity defs) and command ids never exercised.</summary>
        public void Missing(SimGame game, List<string> defs, List<string> commands)
        {
            defs.Clear();
            commands.Clear();
            var catalogue = new List<CatalogueEntry>();
            game.Observer.CopyCatalogue(catalogue);
            for (int i = 0; i < catalogue.Count; i++) if (!_defs.Contains(catalogue[i].Id)) defs.Add(catalogue[i].Id);
            IReadOnlyList<string> ids = game.Observer.CommandIds;
            for (int i = 0; i < ids.Count; i++) if (!_commands.Contains(ids[i])) commands.Add(ids[i]);
        }
    }
}
