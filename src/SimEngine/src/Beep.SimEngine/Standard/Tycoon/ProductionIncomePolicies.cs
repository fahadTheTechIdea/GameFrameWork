using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct ProductionIncomeDecision
    {
        public Code Refusal { get; }
        public Bill Eligible { get; }
        public ProductionIncomeDecision(Code refusal, Bill eligible) { Refusal = refusal; Eligible = eligible; }
    }
    public interface IProductionIncomePolicy
    {
        string Id { get; }
        ProductionIncomeDecision Evaluate(ResourceGenerationContext context);
    }
    public interface IProductionIncomePolicyDefinition : IContentDefinition
    {
        IProductionIncomePolicy Create(World world);
    }
    /// <summary>One primary-only authoring and evaluation algorithm for native and independent policies.</summary>
    public sealed class PrimaryProductionIncomeDeclaration
    {
        private IReadOnlyList<string> _causes;
        public IReadOnlyList<string> Causes => _causes;
        public void Read(JsonReader r)
        {
            if (r.String("input_rule") != "primary_only") throw r.Error("primary income requires explicit primary_only input_rule");
            if (!r.Has("causes")) throw r.Error("income causes must be explicitly authored, including []");
            string[] causes = r.Strings("causes"); Array.Sort(causes, StringComparer.Ordinal);
            for (int i = 0; i < causes.Length; i++)
                if (string.IsNullOrWhiteSpace(causes[i]) || i > 0 && causes[i] == causes[i - 1])
                    throw r.Error("income causes require nonempty unique identities");
            _causes = Array.AsReadOnly(causes);
        }
        public IProductionIncomePolicy Create(World world, IProductionIncomePolicyDefinition definition)
        {
            if (world == null || !(definition is Def actual) || !actual.IsFrozen ||
                !ReferenceEquals(world.Content.Find<IProductionIncomePolicyDefinition>(definition.Id), definition))
                throw new InvalidOperationException("income policy requires its frozen selected session definition");
            return new PrimaryIncome(definition, this);
        }
        private sealed class PrimaryIncome : IProductionIncomePolicy
        {
            private readonly IProductionIncomePolicyDefinition _definition;
            private readonly PrimaryProductionIncomeDeclaration _declaration;
            public PrimaryIncome(IProductionIncomePolicyDefinition definition, PrimaryProductionIncomeDeclaration declaration)
            { _definition = definition; _declaration = declaration; }
            public string Id => _definition.Id;
            public ProductionIncomeDecision Evaluate(ResourceGenerationContext context)
            {
                if (!context.Consumed.IsEmpty) return new ProductionIncomeDecision(Code.None, Bill.Empty);
                foreach (string cause in _declaration.Causes)
                    if (cause == context.Cause.Id) return new ProductionIncomeDecision(Code.None, context.Produced);
                return new ProductionIncomeDecision(Code.None, Bill.Empty);
            }
        }
    }
    /// <summary>Explicit primary-generation rewards. Manufacturing/recycled-input rewards need another authored policy.</summary>
    public sealed class PrimaryProductionIncomeDef : Def, IProductionIncomePolicyDefinition
    {
        private readonly PrimaryProductionIncomeDeclaration _declaration = new PrimaryProductionIncomeDeclaration();
        public IReadOnlyList<string> Causes => _declaration.Causes;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        public IProductionIncomePolicy Create(World world) => _declaration.Create(world, this);
    }
}
