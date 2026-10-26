using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Bind an existing optional charge to the original activity start, never copy its price or receipts.</summary>
    public sealed class ContentActionActivityBindingDef : Def, IActivityStartRule
    {
        private string _activity, _charge;
        public IAuthoredActivityDefinition Activity { get; private set; }
        public IContentActionChargeDefinition Charge { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { _activity = reader.String("activity"); _charge = reader.String("charge"); }
        protected override void ResolveCore(ContentResolver content)
        {
            Activity = content.Def<IAuthoredActivityDefinition>(_activity); Charge = content.Def<IContentActionChargeDefinition>(_charge);
            if (!ReferenceEquals(Activity, Charge.Target)) throw content.Fail("activity charge must reference its actual activity target");
            ContentDependencies = Array.AsReadOnly(new IContentDefinition[] { Activity, Charge });
            _activity = null; _charge = null;
        }
        public Code Check(World world, PlayerId company, Entity target)
            => world.Services.Get<IBusinessContentCharges>().Read(company, Charge.Action, Charge.Target.Id, out _);
        public Code Commit(World world, PlayerId company, Entity target)
            => world.Services.Get<IBusinessContentCharges>().Charge(company, Charge.Action, Charge.Target.Id);
    }
}
