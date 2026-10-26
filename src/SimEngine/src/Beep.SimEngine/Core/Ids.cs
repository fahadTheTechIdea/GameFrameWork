using System;

namespace Beep.SimEngine
{
    /// <summary>A world entity's stable id. Ids are never reused within a game; 0 means none.</summary>
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        public readonly int Value;
        public EntityId(int value) { Value = value; }
        public static readonly EntityId None = default;
        public bool IsValid => Value > 0;
        public static bool operator ==(EntityId a, EntityId b) => a.Value == b.Value;
        public static bool operator !=(EntityId a, EntityId b) => a.Value != b.Value;
        public bool Equals(EntityId o) => Value == o.Value;
        public override bool Equals(object o) => o is EntityId e && e.Value == Value;
        public override int GetHashCode() => Value;
        public int CompareTo(EntityId o) => Value.CompareTo(o.Value);
        public override string ToString() => "#" + Value;
    }

    /// <summary>A company seat. Seats are numbered from 0; <see cref="None"/> (-1) is nobody, e.g. the map itself.</summary>
    public readonly struct PlayerId : IEquatable<PlayerId>, IComparable<PlayerId>
    {
        public readonly int Value;
        public PlayerId(int value) { Value = value; }
        public static readonly PlayerId None = new PlayerId(-1);
        public bool IsValid => Value >= 0;
        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
        public bool Equals(PlayerId o) => Value == o.Value;
        public override bool Equals(object o) => o is PlayerId p && p.Value == Value;
        public override int GetHashCode() => Value;
        public int CompareTo(PlayerId o) => Value.CompareTo(o.Value);
        public override string ToString() => Value < 0 ? "nobody" : "p" + Value;
    }

    /// <summary>
    /// An resource kind (crude, diesel, steel...). Interned from content at load: the integer is the resource's
    /// index in the content set, so the same content gives the same ids on every machine. 0 means none.
    /// </summary>
    public readonly struct ResourceId : IEquatable<ResourceId>, IComparable<ResourceId>
    {
        public readonly int Value;
        public ResourceId(int value) { Value = value; }
        public static readonly ResourceId None = default;
        public bool IsValid => Value > 0;
        public static bool operator ==(ResourceId a, ResourceId b) => a.Value == b.Value;
        public static bool operator !=(ResourceId a, ResourceId b) => a.Value != b.Value;
        public bool Equals(ResourceId o) => Value == o.Value;
        public override bool Equals(object o) => o is ResourceId i && i.Value == Value;
        public override int GetHashCode() => Value;
        public int CompareTo(ResourceId o) => Value.CompareTo(o.Value);
        public override string ToString() => "resource" + Value;
    }

    /// <summary>Stable authored identity, not an enum or a content-table position. Default means no worker.</summary>
    public readonly struct WorkerRoleId : IEquatable<WorkerRoleId>, IComparable<WorkerRoleId>
    {
        public string Value { get; }
        public bool IsValid => Value != null;
        public static readonly WorkerRoleId None = default;

        public WorkerRoleId(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("a worker role needs an identity", nameof(value));
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (!(ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9' || ch == '_' || ch == '-' || ch == '.'))
                    throw new ArgumentException("worker role identities use lowercase letters, digits, underscore, hyphen or dot", nameof(value));
            }
            Value = value;
        }

        public bool Equals(WorkerRoleId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object other) => other is WorkerRoleId role && Equals(role);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(WorkerRoleId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public static bool operator ==(WorkerRoleId a, WorkerRoleId b) => a.Equals(b);
        public static bool operator !=(WorkerRoleId a, WorkerRoleId b) => !a.Equals(b);
        public override string ToString() => Value ?? "none";
    }

    /// <summary>
    /// A stable, content-authored identifier for a code the engine reports — a refusal, a notification
    /// kind, a blocker. Hosts localise it; the engine never hands out prose.
    /// </summary>
    public readonly struct Code : IEquatable<Code>
    {
        public readonly string Id;
        public Code(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("a code needs an id", nameof(id));
            Id = id;
        }
        public static readonly Code None = default;
        public bool IsNone => Id == null;
        public static bool operator ==(Code a, Code b) => string.Equals(a.Id, b.Id, StringComparison.Ordinal);
        public static bool operator !=(Code a, Code b) => !(a == b);
        public bool Equals(Code o) => this == o;
        public override bool Equals(object o) => o is Code c && this == c;
        public override int GetHashCode() => Id == null ? 0 : Id.GetHashCode();
        public override string ToString() => Id ?? "none";
    }
}
