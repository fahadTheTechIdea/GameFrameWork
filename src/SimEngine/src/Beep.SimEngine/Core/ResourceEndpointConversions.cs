using System.Collections.Generic;

namespace Beep.SimEngine
{
    /// <summary>One stock owner can consume owned inputs and produce into that same measured buffer atomically.
    /// Pure vectors grant no authority; mutation requires exact current outbound receipts. No capacity-credit ledger.</summary>
    public interface IResourceConversionEndpoint : IResourceOutlet
    {
        CapacityCheck CheckConversion(IReadOnlyList<ResourceAmount> consumed, IReadOnlyList<ResourceAmount> produced);
        /// <summary>Preflight the whole set before mutation. Preserve unrelated promises and unconsumed receipt remainders.
        /// A provider violation is terminal; callers verify complete stock/books before publishing success.</summary>
        void ConvertReserved(ReservationOwner owner, IReadOnlyList<ResourceConversionInput> inputs, Bill produced, IReadOnlyList<ResourceLotAmount> lots = null);
    }
}
