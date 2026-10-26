using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional module-owned monetary annotation for the existing transformation definition.</summary>
    public interface ITransformationCostBinding : IContentDependencyDefinition
    {
        string TransformationId { get; }
        Fix Amount { get; }
    }
    public sealed class TransformationCostBindingDef : Def, ITransformationCostBinding
    {
        public string TransformationId { get; private set; }
        public Fix Amount { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            TransformationId = reader.String("transformation"); Amount = reader.Fix("amount");
            try { ContractIdentity.ValidateId(TransformationId, "transformation"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (!Amount.IsPositive) throw reader.Error("transformation cost must be positive");
        }
        protected override void ResolveCore(ContentResolver content)
            => ContentDependencies = Array.AsReadOnly(new IContentDefinition[] { content.Def<ITransformationDefinition>(TransformationId) });
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            var actual = content.Def<ITransformationDefinition>(TransformationId);
            if (actual.Cash != Amount) throw content.Fail("cost binding must match the actual compiled transformation annotation");
        }
    }
}
