using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IResourceSaleEligibility : IContentDependencyDefinition
    {
        void ValidateTarget(EntityDef target);
        Code Check(World world, Entity entity);
    }
    public interface IResourceSaleDefinition : IContentDependencyDefinition
    {
        EntityDef Target { get; }
        Bill Prices { get; }
        GameDuration Period { get; }
        GameDuration Offset { get; }
        Code Reason { get; }
        IResourceSaleEligibility Eligibility { get; }
    }
    public sealed class ResourceSaleDef : Def, IResourceSaleDefinition
    {
        private string _target, _eligibility;
        private BillSpec _prices;
        public EntityDef Target { get; private set; }
        public Bill Prices { get; private set; }
        public GameDuration Period { get; private set; }
        public GameDuration Offset { get; private set; }
        public Code Reason { get; private set; }
        public IResourceSaleEligibility Eligibility { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            _target = r.String("target"); _eligibility = r.String("eligibility");
            _prices = BillSpec.Read(r, "prices"); Reason = new Code(r.String("reason"));
            Period = new JsonReader(r.Object("period") ?? throw r.Error("sale period required"), r.Where + ".period").RationalDuration();
            Offset = new JsonReader(r.Object("offset") ?? throw r.Error("sale offset required"), r.Where + ".offset").RationalDuration();
            if (Period.IsZero || Reason.IsNone) throw r.Error("sale requires period and reason");
        }
        protected override void ResolveCore(ContentResolver c)
        {
            Target = c.Def<EntityDef>(_target); Prices = _prices.Resolve(c);
            Eligibility = c.Def<IResourceSaleEligibility>(_eligibility);
            if (!Target.HasCapability<IInventory>() || Prices.IsEmpty) throw new ContentException(Id + ": sale target requires inventory and explicit prices");
            var dependencies = new List<IContentDefinition> { Target, Eligibility };
            foreach (var line in Prices.Lines)
            {
                if (!line.Amount.IsPositive) throw new ContentException(Id + ": sale prices must be positive");
                dependencies.Add(c.Content.Resource(line.Resource));
            }
            ContentDependencies = dependencies.AsReadOnly();
        }
    }
    public interface IBusinessSalesReportDefinition : IContentDefinition
    {
        GameDuration Period { get; }
        GameDuration Offset { get; }
    }
    public sealed class BusinessSalesReportDef : Def, IBusinessSalesReportDefinition
    {
        public GameDuration Period { get; private set; }
        public GameDuration Offset { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            Period = new JsonReader(r.Object("period") ?? throw r.Error("report period required"), r.Where + ".period").RationalDuration();
            Offset = new JsonReader(r.Object("offset") ?? throw r.Error("report offset required"), r.Where + ".offset").RationalDuration();
            if (Period.IsZero) throw r.Error("report period must be positive");
        }
    }
    public readonly struct BusinessSaleFact
    {
        public const string EventId = "sim.tycoon.resource-sold";
        public PlayerId Company { get; }
        public EntityId Endpoint { get; }
        public ResourceId Resource { get; }
        public Fix Quantity { get; }
        public Fix Revenue { get; }
        public UnitId Currency { get; }
        public BusinessSaleFact(PlayerId company, EntityId endpoint, ResourceId resource, Fix quantity, Fix revenue, UnitId currency)
        { Company = company; Endpoint = endpoint; Resource = resource; Quantity = quantity; Revenue = revenue; Currency = currency; }
    }
    public readonly struct BusinessSalesSnapshot
    {
        public string ReportPolicy { get; }
        public UnitId Currency { get; }
        public Fix Revenue { get; }
        public Fix CurrentPeriodRevenue { get; }
        public Fix PreviousPeriodRevenue { get; }
        public BusinessSalesSnapshot(string policy, UnitId currency, Fix revenue, Fix current, Fix previous)
        { ReportPolicy = policy; Currency = currency; Revenue = revenue; CurrentPeriodRevenue = current; PreviousPeriodRevenue = previous; }
    }
    public interface IBusinessSales
    {
        Code ReadSales(PlayerId company, out BusinessSalesSnapshot snapshot);
        Code CopyPolicies(EntityId target, PlayerId viewer, IList<ResourceSaleView> destination);
    }
    public readonly struct ResourceSaleView
    {
        public string Id { get; }
        public Bill Prices { get; }
        public GameDuration Period { get; }
        public GameDuration Offset { get; }
        public Code Blocker { get; }
        internal ResourceSaleView(IResourceSaleDefinition d, Code blocker)
        { Id = d.Id; Prices = d.Prices; Period = d.Period; Offset = d.Offset; Blocker = blocker; }
    }
}
