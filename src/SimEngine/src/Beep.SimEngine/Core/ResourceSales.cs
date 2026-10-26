namespace Beep.SimEngine
{
    /// <summary>One ephemeral participant in the existing synchronous inventory transaction.</summary>
    public interface IResourceInventoryCommit { void Commit(); }
    public interface IResourceSaleCommit : IResourceInventoryCommit { }
    public interface IResourceSaleSettlement
    {
        Code PrepareSale(ResourceInventoryIntent intent, out IResourceSaleCommit commit);
    }
}
