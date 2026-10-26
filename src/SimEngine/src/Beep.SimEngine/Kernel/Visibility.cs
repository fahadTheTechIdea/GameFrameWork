using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>
    /// Who may see which entity. A game that has no fog binds nothing and every seat sees everything. A
    /// fog module binds one: player views, inspect panels and each seat's render snapshot then show a
    /// company only what it can see. Exactly PlayerId.None is the observer. Typed definition visibility
    /// also gates normal content admission before capability/recipe details. Reading is pure; visibility
    /// does not own learned facts, execution progress or resource state.
    /// </summary>
    public interface IVisibility
    {
        bool CanSee(World world, PlayerId viewer, Entity entity);
        bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition);
    }

    /// <summary>Optional pure sample authorization on the SAME selected visibility owner.
    /// Keys are stable opaque identities of current observation scopes, not learned state or versions.</summary>
    public interface ISpatialSampleVisibility
    {
        bool CanSeeSample(World world, PlayerId viewer, ISpatialLayerDefinition layer, SpatialSnapshot sample);
        void CopyObservationKeys(World world, PlayerId viewer, ISpatialLayerDefinition layer, IList<string> destination);
    }
}
