using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Another part of the existing account owner, not a separate ledger or persistent service.
    internal sealed partial class BusinessAccounts
    {
        private HashSet<string> _onceActionDefinitions;
        private readonly HashSet<(int Company, string Definition)> _chargedActions = new HashSet<(int, string)>();
        internal void ConfigureActionCharges(HashSet<string> definitions)
        {
            if (_onceActionDefinitions != null) throw new InvalidOperationException("content action charges configure once");
            _onceActionDefinitions = new HashSet<string>(definitions, StringComparer.Ordinal);
        }
        internal bool IsActionCharged(PlayerId company, string definition) => _chargedActions.Contains((company.Value, definition));
        internal Code ChargeAction(PlayerId company, string definition, Fix amount, Code reason, bool once)
        {
            if (once && (_onceActionDefinitions == null || !_onceActionDefinitions.Contains(definition))) return Codes.InvalidArgument;
            if (once && IsActionCharged(company, definition)) return Code.None;
            if (!TryCharge(company, amount, reason)) return Codes.CannotAfford;
            if (once) _chargedActions.Add((company.Value, definition));
            return Code.None;
        }
        private void SyncActionCharges(IStateSync sync)
        {
            var rows = new List<(int Company, string Definition)>(_chargedActions);
            rows.Sort((a, b) => a.Company != b.Company ? a.Company.CompareTo(b.Company) : StringComparer.Ordinal.Compare(a.Definition, b.Definition));
            int count = sync.Count(rows.Count); var restored = new HashSet<(int, string)>();
            int previousCompany = -1; string previousDefinition = null;
            for (int i = 0; i < count; i++)
            {
                PlayerId company = sync.Reading ? default : new PlayerId(rows[i].Company); string definition = sync.Reading ? null : rows[i].Definition;
                sync.Player(ref company); sync.String(ref definition);
                if (!IsAccount(company) || _onceActionDefinitions == null || definition == null || !_onceActionDefinitions.Contains(definition)
                    || company.Value < previousCompany || company.Value == previousCompany && StringComparer.Ordinal.Compare(previousDefinition, definition) >= 0
                    || !restored.Add((company.Value, definition))) throw new CorruptStateException("invalid saved content action charge receipt");
                previousCompany = company.Value; previousDefinition = definition;
            }
            if (sync.Reading) { _chargedActions.Clear(); foreach (var row in restored) _chargedActions.Add(row); }
        }
    }
}
