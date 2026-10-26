using System;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Sole stable identity grammar for authored engine/module contracts.</summary>
    public static class ContractIdentity
    {
        public static void Validate(string id, int schemaVersion, string kind)
        {
            ValidateId(id, kind);
            if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion), "a " + kind + " schema version is positive");
        }

        public static void ValidateId(string id, string kind)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("a " + kind + " needs a stable id", nameof(id));
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '.' || c == '_' || c == '-'))
                    throw new ArgumentException("invalid " + kind + " id '" + id + "'", nameof(id));
            }
        }
    }
}
