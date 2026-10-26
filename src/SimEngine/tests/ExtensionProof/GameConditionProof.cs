using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public pure conditions, independent of any controller or objective coordinator.</summary>
    public class GameConditionProofModule : GameConditionsModule
    {
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<ElapsedGameConditionDef>("elapsed_conditions");
            types.Component<DialOperationDef>("strategy_dial", 1);
        }
    }
    /// <summary>Unrelated public content condition over the actual clock; no controller/system changes.</summary>
    public sealed class ElapsedGameConditionDef : GameConditionDef
    {
        private GameDuration _after;
        protected override void ReadCore(JsonReader reader)
            => _after = (reader.Child("after") ?? throw reader.Error("explicit duration required")).RationalDuration();
        public override void ValidateBinding(World world, PlayerId company)
        {
            try { _ = world.Time.Ticks(_after); }
            catch (OverflowException) { throw new ContentException(Id + ": condition duration exceeds actual session representation"); }
        }
        public override Code Check(GameConditionContext context)
            => context.View.Tick < context.View.SimulationTime.Ticks(_after) ? new Code("proof.strategy.grace") : Code.None;
    }

}

