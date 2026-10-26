namespace Beep.SimEngine.Standard
{
    /// <summary>
    /// Optional monetary contract supplied by TycoonModule. Resource-only games omit it;
    /// disabled monetary rules do not use a fake ledger. Reasons are authored ledger categories.
    /// </summary>
    public interface IEconomy
    {
        bool CanAfford(PlayerId player, Fix amount);
        /// <summary>Takes the payment if the company can afford it; false (and nothing taken) otherwise.</summary>
        bool TryCharge(PlayerId player, Fix amount, Code reason);
        /// <summary>Takes a running cost even into debt (upkeep, wages).</summary>
        void Charge(PlayerId player, Fix amount, Code reason);
        void Credit(PlayerId player, Fix amount, Code reason);
        Fix Cash(PlayerId player);
    }

}
