using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Original bindings are references; endpoint books alone own actual reusable commitments.
    public sealed partial class Transformer
    {
        public IReadOnlyList<TransformationPortLink> ToolSources => new List<TransformationPortLink>(_toolSources).AsReadOnly();
        private IReadOnlyList<ResourceReservation> ToolClaims(World world)
            => TransformationContracts.ToolRecords(world, this, _toolSources);
        private TransformationToolsSnapshot ReadTools(World world)
        {
            var records = ToolClaims(world); var held = new List<ResourceAmount>();
            foreach (var record in records) held.Add(new ResourceAmount(record.Resource, record.Amount));
            return new TransformationToolsSnapshot(_toolSources, records, new Bill(held));
        }
        private void ReleaseTools(World world)
        {
            var records = ToolClaims(world);
            if (records.Count != 0 && !world.Services.Get<IResourceTransaction>().ReleaseClaims(new ResourceClaimReleaseIntent(Entity.Owner, records, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed)
                throw new InvalidOperationException("transformation tool release changed after verified admission");
        }
    }

    public static partial class TransformationContracts
    {
        internal static IReadOnlyList<ResourceReservation> ToolRecords(World world, ITransformerTools provider,
            IReadOnlyList<TransformationPortLink> sources)
        {
            var result = new List<ResourceReservation>(); var endpoints = new HashSet<ResourceEndpointId>();
            ReservationOwner owner = provider.WorkInProgressOwner;
            if (!owner.IsValid) return result.AsReadOnly();
            foreach (var source in sources)
                if (endpoints.Add(source.Endpoint))
                {
                    ResourceClaimSnapshot claims = world.Services.Get<IResourceTransaction>().ReadClaims(
                        provider.Entity.Owner, source.Endpoint, owner);
                    if (!claims.Ready) throw new CorruptStateException("original reusable tool endpoint lost actual access: " + claims.Status);
                    result.AddRange(claims.Records);
                }
            return result.AsReadOnly();
        }
        internal static bool SameToolFacts(TransformationToolsSnapshot left, TransformationToolsSnapshot right)
        {
            if (!SamePortLinks(left.Sources, right.Sources) || !SameBill(left.Held, right.Held) || left.Receipts.Count != right.Receipts.Count) return false;
            for (int i = 0; i < left.Receipts.Count; i++)
            {
                var a = left.Receipts[i]; var b = right.Receipts[i];
                if (!a.Token.Equals(b.Token) || !a.Owner.Equals(b.Owner) || a.Resource != b.Resource ||
                    a.Amount != b.Amount || a.Direction != b.Direction) return false;
            }
            return true;
        }
        private static void ValidateTools(World world, ITransformer provider, TransformationSnapshot state, ITransformationDefinition head)
        {
            TransformationToolsSnapshot facts = state.Tools;
            if (facts.Held == null || facts.Sources == null || facts.Receipts == null) throw new CorruptStateException("missing explicit tool facts");
            if (!(provider is ITransformerTools tools))
            {
                if (facts.Sources.Count != 0 || facts.Receipts.Count != 0 || !facts.Held.IsEmpty || head != null && !head.Tools.IsEmpty)
                    throw new CorruptStateException("provider without tool commitments reports reusable material");
                return;
            }
            int version = provider.Entity.Version; bool active = state.Wip.Execution > 0;
            var sources = tools.ToolSources;
            int count = active ? head.ToolPorts.Count : 0;
            if (sources == null || sources.Count != count || !SamePortLinks(sources, facts.Sources))
                throw new CorruptStateException("reusable original tool bindings differ from actual active execution");
            for (int i = 0; i < count; i++)
            {
                var source = sources[i]; var route = head.ToolPorts[i];
                if (source.Resource != route.Resource || source.Port != route.Port || !source.Endpoint.IsValid || source.Endpoint == state.Wip.Endpoint)
                    throw new CorruptStateException("reusable tool binding differs from its authored route");
            }
            Bill expected = active ? head.Tools : Bill.Empty;
            var records = ToolRecords(world, tools, sources);
            var amounts = new List<ResourceAmount>(); var resources = new HashSet<ResourceId>();
            foreach (var record in records)
            {
                ResourceEndpointId endpoint = default;
                foreach (var source in sources) if (source.Resource == record.Resource) endpoint = source.Endpoint;
                if (!record.Owner.Equals(tools.WorkInProgressOwner) || record.Direction != ReservationDirection.Outbound ||
                    record.Token.Endpoint != endpoint || !resources.Add(record.Resource) ||
                    record.Amount != expected.AmountOf(record.Resource))
                    throw new CorruptStateException("execution's actual tool record differs from original authored commitments");
                amounts.Add(new ResourceAmount(record.Resource, record.Amount));
            }
            var actual = new TransformationToolsSnapshot(sources, records, new Bill(amounts));
            if (records.Count != expected.Count || !SameBill(actual.Held, expected) || !SameToolFacts(facts, actual))
                throw new CorruptStateException("missing, extra or fabricated actual reusable tool commitment");
            if (records.Count != 0 && !world.Services.Get<IResourceTransaction>().EvaluateClaimReceipts(provider.Entity.Owner, records).Ready)
                throw new CorruptStateException("reusable tools do not have actual valid native receipts");
            if (provider.Entity.Version != version) throw new InvalidOperationException("tool provider mutated a pure material read");
        }
    }
}
