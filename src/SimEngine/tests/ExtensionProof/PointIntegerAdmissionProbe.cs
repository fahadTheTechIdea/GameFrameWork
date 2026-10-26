using Beep.SimEngine;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side negative admission only; never a successful layer write or gameplay in the driver.
    public sealed class PointIntegerAdmissionProbe : GameSystem
    {
        public override string Id=>"proof.point-integer-admission";
        public override int StateSchemaVersion=>1;
        public bool FractionRejected { get; private set; }
        public bool DuplicateRejected { get; private set; }
        public bool NoPartialWrite { get; private set; }
        public override void Initialize(World world)
        {
            var point=(PointSpace)world.Space;var before=new StateHasher();point.Sync(before);
            FractionRejected=point.WriteLayers(world,new PlayerId(0),new[]{
                (Layer:"corridor.depth",Point:Fix.FromInt(3),Value:Fix.Parse("1.5")),
                (Layer:"corridor.company",Point:Fix.FromInt(3),Value:Fix.Parse("0.5"))})==Codes.InvalidArgument;
            DuplicateRejected=point.WriteLayers(world,new PlayerId(0),new[]{
                (Layer:"corridor.company",Point:Fix.FromInt(3),Value:Fix.One),
                (Layer:"corridor.company",Point:Fix.FromInt(3),Value:Fix.FromInt(2))})==Codes.InvalidArgument;
            var after=new StateHasher();point.Sync(after);NoPartialWrite=before.Value==after.Value;
        }
        public override void Update(World world,TickInfo tick){}
    }
}
