using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure authored intent into the sole optional alert owner and its normal command.</summary>
    public sealed class CompanyAlertControlActionDeclaration
    {
        public static readonly Code Capability = new Code("company.alert");
        private string _command, _owner;
        private CompanyAlertSettings _enabled, _disabled;
        private IReadOnlyList<IContentDefinition> _dependencies;
        public string CommandId => _command;
        public string CommandOwner => _owner;
        public ControlTargetKind TargetKind => ControlTargetKind.Company;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _dependencies;
        public void Read(JsonReader reader)
        {
            _command = reader.String("command"); _owner = reader.String("owner");
            try { ContractIdentity.ValidateId(_command, "alert command"); ContractIdentity.ValidateId(_owner, "alert command owner"); }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
            _enabled = Read(reader, "enabled"); _disabled = Read(reader, "disabled");
        }
        private static CompanyAlertSettings Read(JsonReader parent, string key)
        {
            JsonReader child = parent.Child(key) ?? throw parent.Error("explicit " + key + " alert settings required");
            CompanyAlertSettings result = CompanyAlertSettings.Read(child); child.End(); return result;
        }
        public void Resolve(ContentResolver resolver)
        {
            var definitions = new List<IContentDefinition>();
            foreach (var settings in new[] { _enabled, _disabled })
            {
                IContentDefinition profile = resolver.Def<ICompanyAlertProfileDefinition>(settings.Profile);
                if (!definitions.Contains(profile)) definitions.Add(profile);
                foreach (var reserve in settings.WorkerReserves)
                {
                    IContentDefinition role = resolver.Def<IWorkerRoleDefinition>(reserve.Role.Value);
                    if (!definitions.Contains(role)) definitions.Add(role);
                }
            }
            definitions.Sort((a, b) => { int order = string.CompareOrdinal(a.Id, b.Id); return order != 0 ? order : string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName); });
            _dependencies = definitions.AsReadOnly();
        }
        public void ValidateComposition(World world, string id)
        {
            if (!world.Services.TryGet<ICompanyAlertPolicy>(out var alerts) || !ReferenceEquals(alerts.World, world)
                || !world.Commands.TryGet(CommandId, out var command) || command.Type != typeof(ConfigureCompanyAlertCommand)
                || command.Owner != CompanyAlertModule.ModuleId || CommandOwner != command.Owner)
                throw new ContentException(id + ": alert action requires actual selected company alert owner and normal command");
            for (int i = 0; i < world.Players.Count; i++)
                if (!alerts.CheckAlert(new PlayerId(i), _enabled).IsNone || !alerts.CheckAlert(new PlayerId(i), _disabled).IsNone)
                    throw new ContentException(id + ": authored alert settings must bind actual roles/profiles/dispatch scopes");
        }
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
        {
            destination.Clear();
            if (entity == EntityId.None && world.Players.IsSeat(company) && world.Services.Has<ICompanyAlertPolicy>()) destination.Add(Capability);
        }
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
        {
            if (target.Kind != TargetKind || target.Entity != EntityId.None || target.Capability != Capability) return AutomationCodes.InvalidSettings;
            if (action.Unit != new Code("unit.boolean")) return AutomationCodes.UnitMismatch;
            if (action.Target != Fix.Zero) return Codes.InvalidArgument;
            return world.Services.Get<ICompanyAlertPolicy>().CheckAlert(company, action.Enabled ? _enabled : _disabled);
        }
        public Command CreateCommand(ControlActionRequest request) => new ConfigureCompanyAlertCommand
        { Company = request.Company, Settings = request.Action.Enabled ? _enabled : _disabled };
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => CheckBinding(world, company, target, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination) => target;
    }

    public sealed class CompanyAlertControlActionPolicyDef : Def, IControlActionPolicy
    {
        private readonly CompanyAlertControlActionDeclaration _declaration = new CompanyAlertControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }
}
