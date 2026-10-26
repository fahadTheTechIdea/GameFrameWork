using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureCompanyAlertCommand : Command, ICompanyControlCommand
    {
        public PlayerId Company;
        public PlayerId TargetCompany => Company;
        public CompanyAlertSettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.Player(ref Company); bool present = Settings != null; sync.Bool(ref present);
            if (present) Settings = CompanyAlertSettings.Sync(sync, Settings); else if (sync.Reading) Settings = null;
        }
    }
    internal sealed class ConfigureCompanyAlertHandler : CommandHandler<ConfigureCompanyAlertCommand>
    {
        protected override Code Check(World world, ConfigureCompanyAlertCommand command)
            => command.Player != command.Company ? Codes.NotOwned : world.Services.Get<ICompanyAlertPolicy>().CheckAlert(command.Company, command.Settings);
        protected override void Apply(World world, ConfigureCompanyAlertCommand command, CommandContext result)
            => world.Services.Get<ICompanyAlertPolicy>().ConfigureAlert(command.Company, command.Settings);
    }
}
