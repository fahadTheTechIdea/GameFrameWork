using System.Collections.Generic;

namespace Beep.SimEngine
{
    /// <summary>
    /// One line of an inspect panel: a content-id key (a host localises it), an optional subject (an
    /// item or def id), a value and, when it is a gauge, a maximum. Numbers and ids only.
    /// </summary>
    public struct Facet
    {
        public Code Key;
        public string Subject;
        public Fix Value;
        public Fix Max;

        public Facet(Code key, Fix value, string subject = null, Fix max = default)
        {
            Key = key;
            Value = value;
            Subject = subject;
            Max = max;
        }

        public override string ToString() => Key + (Subject != null ? "[" + Subject + "]" : "") + "=" + Value + (Max.IsPositive ? "/" + Max : "");
    }
}

namespace Beep.SimEngine.Kernel
{
    /// <summary>
    /// A component that contributes lines to its entity's inspect panel. It decides what a viewer who
    /// does not own the entity may see (a rival sees a tank, not what is in it).
    /// </summary>
    public interface IInspectable
    {
        void Inspect(World world, PlayerId viewer, IList<Facet> into);
    }
}
