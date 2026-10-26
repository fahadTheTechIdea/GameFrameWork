using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class KnowledgeView
    {
        private readonly World _world; private readonly PlayerId _viewer;
        internal KnowledgeView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public void CopyKnown(IList<KnowledgeObservation> destination)
            => _world.Services.Get<ICompanyKnowledge>().CopyKnown(_viewer, destination);
        public bool TryRead(PlayerId company, string provider, string subject, out KnowledgeObservation observation)
            => _world.Services.Get<ICompanyKnowledge>().TryRead(_viewer, company, provider, subject, out observation);
        public Code ReadFreshness(PlayerId company, string provider, string subject, out KnowledgeFreshnessSnapshot snapshot)
            => _world.Services.Get<IKnowledgeFreshness>().ReadFreshness(_viewer, company, provider, subject, out snapshot);
        public Code PreviewShare(PlayerId recipient, string policy, out KnowledgeShareSnapshot snapshot)
            => _world.Services.Get<IKnowledgeSharing>().PreviewShare(_viewer, recipient, policy, out snapshot);
        public Code PreviewForget(string policy, out KnowledgeForgetSnapshot snapshot)
            => _world.Services.Get<IKnowledgeForgetting>().PreviewForget(_viewer, policy, out snapshot);
    }
}
