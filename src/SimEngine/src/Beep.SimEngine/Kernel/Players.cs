using System;
using System.Collections.Generic;

namespace Beep.SimEngine
{
    /// <summary>Who plays a seat.</summary>
    public enum Controller : byte
    {
        /// <summary>A person (or a client acting for one) sends the seat's orders.</summary>
        Human = 0,
        /// <summary>The engine's own company AI plays the seat, through the same commands.</summary>
        Autopilot = 1
    }

    /// <summary>
    /// A news item for one company (or everyone): a structure finished, a well ran dry, an objective
    /// was met. Codes and numbers only; a host turns them into words, icons or sounds.
    /// </summary>
    public struct Notification
    {
        public long Sequence;
        public long Tick;
        public Code Kind;
        /// <summary>The company it is about; <see cref="PlayerId.None"/> for news every seat sees.</summary>
        public PlayerId Player;
        /// <summary>A second company involved (a rival that took the lead).</summary>
        public PlayerId Other;
        public EntityId Entity;
        /// <summary>A content id the news refers to (an item, an entity def, an objective).</summary>
        public string Subject;
        public Fix Amount;

        public bool VisibleTo(PlayerId viewer) => !Player.IsValid || Player == viewer || Other == viewer || !viewer.IsValid;

        public override string ToString() => "tick " + Tick + " " + Kind + (Subject != null ? " " + Subject : "") + (Entity.IsValid ? " " + Entity : "");
    }
}

namespace Beep.SimEngine.Kernel
{
    /// <summary>The seats of one game: who controls each, and the start slot each was given.</summary>
    public sealed class PlayerTable
    {
        private readonly List<Controller> _controllers = new List<Controller>();
        private readonly List<int> _slots = new List<int>();

        public int Count => _controllers.Count;

        public PlayerId this[int i] => new PlayerId(i);

        public bool IsSeat(PlayerId p) => p.Value >= 0 && p.Value < _controllers.Count;

        public Controller ControllerOf(PlayerId p) => _controllers[p.Value];

        public int SlotOf(PlayerId p) => _slots[p.Value];

        internal void Add(Controller controller, int slot)
        {
            _controllers.Add(controller);
            _slots.Add(slot);
        }

        internal void SetController(PlayerId p, Controller c) => _controllers[p.Value] = c;

        internal void Sync(IStateSync s)
        {
            s.Section("players");
            int n = s.Count(_controllers.Count);
            if (s.Reading) { _controllers.Clear(); _slots.Clear(); }
            for (int i = 0; i < n; i++)
            {
                Controller c = s.Reading ? Controller.Human : _controllers[i];
                s.Enum(ref c);
                int slot = s.Reading ? 0 : _slots[i];
                s.Int(ref slot);
                if (s.Reading) { _controllers.Add(c); _slots.Add(slot); }
            }
        }
    }

    /// <summary>
    /// A bounded feed that any number of readers follow with their own cursor (a sequence number), so
    /// no reader consumes another's items. Old items fall off the end; a reader that fell behind sees
    /// that it missed some. Presentation feeds are not hashed; persistent owners explicitly call Sync.
    /// </summary>
    public sealed class FeedRing<T>
    {
        private readonly T[] _items;
        private readonly long[] _seqs;
        private long _next = 1;
        private int _head;
        private int _count;

        public FeedRing(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _items = new T[capacity];
            _seqs = new long[capacity];
        }

        /// <summary>Sequence of the newest item (0 when empty).</summary>
        public long Latest => _next - 1;

        /// <summary>Sequence of the oldest item still held.</summary>
        public long Oldest => _count == 0 ? _next : _seqs[Index(_count)];
        public bool TryReadOldest(out FeedEntry<T> entry)
        {
            if (_count == 0) { entry = default; return false; }
            int index = Index(_count); entry = new FeedEntry<T>(_seqs[index], _items[index]); return true;
        }
        private int Index(int back) => (int)(((long)_head - back + _items.Length) % _items.Length);

        public long Append(T item)
        {
            long seq = _next;
            _next = checked(_next + 1);
            _items[_head] = item;
            _seqs[_head] = seq;
            _head = _head == _items.Length - 1 ? 0 : _head + 1;
            if (_count < _items.Length) _count++;
            return seq;
        }

        /// <summary>Copies items after <paramref name="cursor"/> that pass <paramref name="filter"/>; moves the cursor to the newest.</summary>
        public int CopySince(ref long cursor, IList<T> dst, Func<T, bool> filter = null)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            return VisitSince(ref cursor, (seq, item) => dst.Add(item), filter);
        }

        public int CopyEntriesSince(ref long cursor, IList<FeedEntry<T>> dst)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            return VisitSince(ref cursor, (seq, item) => dst.Add(new FeedEntry<T>(seq, item)), null);
        }

        private int VisitSince(ref long cursor, Action<long, T> copy, Func<T, bool> filter)
        {
            int copied = 0;
            for (int k = _count; k >= 1; k--)
            {
                int idx = Index(k);
                if (_seqs[idx] <= cursor) continue;
                if (filter == null || filter(_items[idx])) { copy(_seqs[idx], _items[idx]); copied++; }
            }
            cursor = Latest;
            return copied;
        }

        /// <summary>Optional persistence through the owning service's single state codec.</summary>
        public void Sync(IStateSync s, SyncElement<T> element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            int capacity = _items.Length; s.Int(ref capacity);
            if (capacity != _items.Length) throw new CorruptStateException("feed capacity differs from authored policy");
            long next = _next; s.Long(ref next);
            int count = s.Count(_count);
            if (next <= 0 || count < 0 || count > capacity || next <= count || count != Math.Min(next - 1, capacity))
                throw new CorruptStateException("invalid feed sequence/count");
            for (int i = 0; i < count; i++)
            {
                int index = s.Reading ? i : Index(count - i);
                long sequence = s.Reading ? 0 : _seqs[index]; s.Long(ref sequence);
                if (sequence != next - count + i) throw new CorruptStateException("noncontiguous feed sequence");
                T item = s.Reading ? default : _items[index]; element(s, ref item);
                if (s.Reading) { _seqs[i] = sequence; _items[i] = item; }
            }
            if (s.Reading)
            {
                Array.Clear(_items, count, capacity - count); Array.Clear(_seqs, count, capacity - count);
                _next = next; _count = count; _head = count == capacity ? 0 : count;
            }
        }

        internal long NextSequence => _next;
    }

    public readonly struct FeedEntry<T>
    {
        public long Sequence { get; }
        public T Item { get; }
        public FeedEntry(long sequence, T item) { Sequence = sequence; Item = item; }
    }
}
