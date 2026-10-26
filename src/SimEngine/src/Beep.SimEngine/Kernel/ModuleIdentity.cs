using System;
using System.Globalization;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Exact code/content contract version. No ranges or compatibility substitution.</summary>
    public readonly struct ModuleVersion : IEquatable<ModuleVersion>
    {
        public readonly int Major, Minor, Patch;
        public ModuleVersion(int major, int minor, int patch)
        {
            if (major < 0 || minor < 0 || patch < 0) throw new ArgumentOutOfRangeException(nameof(major));
            Major = major; Minor = minor; Patch = patch;
        }
        public static bool TryParse(string text, out ModuleVersion version)
        {
            version = default;
            if (text == null) return false;
            string[] parts = text.Split('.');
            if (parts.Length != 3) return false;
            var numbers = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (parts[i].Length == 0 || (parts[i].Length > 1 && parts[i][0] == '0')) return false;
                for (int k = 0; k < parts[i].Length; k++) if (parts[i][k] < '0' || parts[i][k] > '9') return false;
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i])) return false;
            }
            version = new ModuleVersion(numbers[0], numbers[1], numbers[2]);
            return true;
        }
        public bool Equals(ModuleVersion other) => Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        public override bool Equals(object obj) => obj is ModuleVersion other && Equals(other);
        public override int GetHashCode() => unchecked((Major * 397 ^ Minor) * 397 ^ Patch);
        public override string ToString() => Major.ToString(CultureInfo.InvariantCulture) + "." + Minor.ToString(CultureInfo.InvariantCulture) + "." + Patch.ToString(CultureInfo.InvariantCulture);
        public static bool operator ==(ModuleVersion a, ModuleVersion b) => a.Equals(b);
        public static bool operator !=(ModuleVersion a, ModuleVersion b) => !a.Equals(b);
    }

    public readonly struct ModuleRequirement
    {
        public string Id { get; }
        public ModuleVersion Version { get; }
        public ModuleRequirement(string id, ModuleVersion version)
        {
            ValidateId(id); Id = id; Version = version;
        }
        internal static void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "engine") throw new ArgumentException("a module needs a non-reserved stable id", nameof(id));
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '_')
                    throw new ArgumentException("invalid module id '" + id + "'", nameof(id));
            }
        }
    }
}
