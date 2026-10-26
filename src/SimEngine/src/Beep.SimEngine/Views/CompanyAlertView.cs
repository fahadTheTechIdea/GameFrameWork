using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class CompanyAlertView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal CompanyAlertView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private Code Access(PlayerId company)
        {
            // Cached views must reject the original terminal state before ordinary shortcuts.
            _world.RequireHealthyUpdate();
            return !_world.Players.IsSeat(company) ? CompanyAlertCodes.NotCompany
                : _viewer == PlayerId.None ? Code.None : !_world.Players.IsSeat(_viewer) ? Codes.NotSeated
                : _viewer != company ? Codes.NotOwned : Code.None;
        }
        public Code PreviewAlert(PlayerId company, CompanyAlertSettings settings)
            => !Access(company).IsNone ? Access(company) : _world.Services.Get<ICompanyAlertPolicy>().CheckAlert(company, settings);
        public Code TryReadAlert(PlayerId company, out CompanyAlertSnapshot snapshot)
        {
            snapshot = default;
            if (!Access(company).IsNone) return Access(company);
            snapshot = _world.Services.Get<ICompanyAlertPolicy>().ReadAlert(company); return Code.None;
        }
        public Code TryReadReserve(PlayerId company, WorkerRoleId role, out CompanyWorkerReserveSnapshot snapshot)
        {
            snapshot = default;
            if (!Access(company).IsNone) return Access(company);
            return _world.Services.Get<ICompanyWorkerReservePolicy>().ReadReserve(company, role, out snapshot);
        }
        public void CopyAlerts(IList<CompanyAlertSnapshot> destination)
        {
            destination.Clear();
            _world.RequireHealthyUpdate();
            if (_viewer != PlayerId.None && !_world.Players.IsSeat(_viewer)) return;
            for (int i = 0; i < _world.Players.Count; i++)
                if (_viewer == PlayerId.None || _viewer.Value == i) destination.Add(_world.Services.Get<ICompanyAlertPolicy>().ReadAlert(new PlayerId(i)));
        }
        public Code TryReadReserveCoverage(PlayerId company, out CompanyWorkerReserveCoverage coverage)
        {
            coverage = null;
            if (!Access(company).IsNone) return Access(company);
            return _world.Services.Get<ICompanyWorkerReservePolicy>().ReadCoverage(company, out coverage);
        }
    }
}
