using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Content
{
    /// <summary>Single strict authoring/resolution path for native quantity per rational period.</summary>
    public sealed class ResourceRateSpec
    {
        private readonly List<(string Resource, Fix Quantity, long Numerator, long Denominator, DurationUnit Unit)> _rows =
            new List<(string, Fix, long, long, DurationUnit)>();
        private ResourceRateSpec() { }
        public static ResourceRateSpec Read(JsonReader reader, string key)
        {
            JsonValue array = reader.Array(key) ?? throw reader.Error("explicit " + key + " rate array required; author [] for none");
            var result = new ResourceRateSpec(); var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                var row = new JsonReader(array[i], reader.Where + "." + key + "[" + i + "]");
                string resource = row.String("resource"); Fix quantity = row.Fix("quantity");
                var period = row.Child("period") ?? throw row.Error("explicit rate period required");
                long numerator = period.Long("numerator"), denominator = period.Long("denominator");
                DurationUnit unit = JsonReader.TimeUnit(period.String("unit"), period); period.End(); row.End();
                if (!quantity.IsPositive || numerator <= 0 || denominator <= 0 || !seen.Add(resource))
                    throw row.Error("unique resource, positive quantity and rational period required");
                result._rows.Add((resource, quantity, numerator, denominator, unit));
            }
            result._rows.Sort((a, b) => string.CompareOrdinal(a.Resource, b.Resource));
            return result;
        }
        public IReadOnlyList<IResourceRate> Resolve(ContentResolver content)
        {
            var rates = new List<IResourceRate>();
            foreach (var row in _rows)
            {
                try { rates.Add(new ResourceRate(content.Def<IResourceContentDefinition>(row.Resource), row.Quantity, row.Numerator, row.Denominator, row.Unit)); }
                catch (ArgumentException ex) { throw content.Fail(ex.Message); }
            }
            return rates.AsReadOnly();
        }
    }
}
