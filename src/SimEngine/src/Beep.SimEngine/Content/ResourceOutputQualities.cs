using System;
using System.Collections.Generic;
using System.Numerics;

namespace Beep.SimEngine.Content
{
    public enum ResourceQualityRounding : byte { Exact, Floor, Ceiling }
    /// <summary>One authored output axis. Source null is a fixed grade; otherwise quantity-weighted consumed material.</summary>
    public interface IResourceOutputQualityRule
    {
        IResourceQualityDefinition Quality { get; }
        IResourceQualityDefinition Source { get; }
        Fix Constant { get; }
        ResourceQualityRounding Rounding { get; }
    }
    /// <summary>Shared JSON authoring for native and independently supplied recipes. No stock or execution state.</summary>
    public sealed class ResourceOutputQualityDeclaration : IResourceOutputQualityRule
    {
        private readonly string _quality, _source;
        public IResourceQualityDefinition Quality { get; private set; }
        public IResourceQualityDefinition Source { get; private set; }
        public Fix Constant { get; }
        public ResourceQualityRounding Rounding { get; }
        private ResourceOutputQualityDeclaration(string quality, JsonReader reader)
        {
            _quality = quality;
            if (reader.Has("source"))
            {
                _source = reader.String("source"); string rounding = reader.String("rounding");
                Rounding = rounding == "exact" ? ResourceQualityRounding.Exact : rounding == "floor" ? ResourceQualityRounding.Floor :
                    rounding == "ceiling" ? ResourceQualityRounding.Ceiling : throw reader.Error("quality rounding must be exact, floor or ceiling");
            }
            else { if (!reader.Has("constant")) throw reader.Error("output quality requires a constant or source"); Constant = reader.Fix("constant"); }
            reader.End(); // Also rejects constant+source and unknown fields.
        }
        public static IReadOnlyList<ResourceOutputQualityDeclaration> Read(JsonReader reader)
        {
            var result = new List<ResourceOutputQualityDeclaration>(); var rules = reader.Child("output_qualities");
            if (rules != null)
            {
                for (int i = 0; i < rules.Raw.Count; i++)
                { string id = rules.Raw.KeyAt(i); result.Add(new ResourceOutputQualityDeclaration(id, rules.Child(id))); }
                rules.End();
            }
            return result.AsReadOnly();
        }
        public void Resolve(ContentResolver content)
        { Quality = content.Def<IResourceQualityDefinition>(_quality); Source = _source == null ? null : content.Def<IResourceQualityDefinition>(_source); }
        public static bool Valid(ResolvedContent content, IResourceOutputQualityRule rule)
        {
            if (rule?.Quality == null || !ReferenceEquals(content.Find<IResourceQualityDefinition>(rule.Quality.Id), rule.Quality) ||
                !Enum.IsDefined(typeof(ResourceQualityRounding), rule.Rounding)) return false;
            if (rule.Source == null) return rule.Constant >= rule.Quality.Minimum && rule.Constant <= rule.Quality.Maximum &&
                UnitConversion.ValidateAmount(rule.Quality.Unit, rule.Constant) == UnitConversionStatus.Converted;
            return ReferenceEquals(content.Find<IResourceQualityDefinition>(rule.Source.Id), rule.Source) &&
                ReferenceEquals(rule.Source.Unit, rule.Quality.Unit);
        }
        /// <summary>Exact raw fixed-point accumulation; rounding applies once at the output axis unit.</summary>
        public static bool TryEvaluate(ResolvedContent content, IResourceOutputQualityRule rule, IReadOnlyList<ResourceLotAmount> consumed, out Fix value)
        {
            value = Fix.Zero;
            if (!Valid(content, rule)) return false;
            if (rule.Source == null) { value = rule.Constant; return true; }
            BigInteger numerator = BigInteger.Zero, denominator = BigInteger.Zero;
            foreach (var lot in consumed) if (lot.Resource == rule.Source.Resource)
            {
                var qualities = lot.Qualities(content);
                if (!lot.ValidMetadata(content) || qualities == null || !qualities.TryGetValue(rule.Source.Id, out Fix quality) || !lot.Amount.IsPositive) return false;
                numerator += (BigInteger)lot.Amount.Raw * quality.Raw; denominator += lot.Amount.Raw;
            }
            if (denominator.IsZero) return false;
            long quantum = rule.Quality.Unit.Discrete ? Fix.One.Raw : 1L;
            BigInteger quotient = BigInteger.DivRem(numerator, denominator * quantum, out BigInteger remainder);
            if (!remainder.IsZero)
            {
                if (rule.Rounding == ResourceQualityRounding.Exact) return false;
                if (rule.Rounding == ResourceQualityRounding.Floor && numerator.Sign < 0) quotient--;
                if (rule.Rounding == ResourceQualityRounding.Ceiling && numerator.Sign > 0) quotient++;
            }
            BigInteger raw = quotient * quantum;
            if (raw < long.MinValue || raw > long.MaxValue) return false;
            value = Fix.FromRaw((long)raw);
            return value >= rule.Quality.Minimum && value <= rule.Quality.Maximum && UnitConversion.ValidateAmount(rule.Quality.Unit, value) == UnitConversionStatus.Converted;
        }
    }
}
