using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // An independently authored engine strategy. Decisions use only the ordinary seat-scoped API.
    public sealed class ControllerReferenceReplacementModule : PlayerControllersModule
    {
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<ReplacementControllerDef>("replacement_controllers");
        }
    }
    public sealed class ReplacementControllerDef : PlayerControllerDef
    {
        private string _target, _replacement, _worker, _activity, _mode;
        private EntityDef _targetDef, _replacementDef, _workerDef;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader);
            _target = reader.String("target"); _replacement = reader.String("replacement");
            _worker = reader.String("worker"); _activity = reader.String("activity"); _mode = reader.String("mode");
            if (_mode != "normal" && _mode != "deny" && _mode != "mutate-check" && _mode != "throw-commit" && _mode != "foreign-trap")
                throw reader.Error("explicit controller replacement mode required");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _targetDef = content.Def<EntityDef>(_target); _replacementDef = content.Def<EntityDef>(_replacement);
            _workerDef = content.Def<EntityDef>(_worker);
            _ = content.Def<IAuthoredActivityDefinition>(_activity);
        }
        public override IPlayerController Create() => new Strategy(this);
        private sealed class Strategy : PlayerController, IPlayerControllerState, IPlayerControllerStateValidation,
            IEntityReplacementReferenceOwner
        {
            private readonly ReplacementControllerDef _definition;
            private EntityId _target, _original;
            private PlayerId _company = PlayerId.None;
            private int _pending, _commits;
            private bool _ordered;
            internal Strategy(ReplacementControllerDef definition) { _definition = definition; }
            public int StateSchemaVersion => 1;
            public override void Decide(PlayerDecisionContext context)
            {
                if (_pending > 0)
                {
                    if (!context.TryGetAnswer(_pending, out var answer)) return;
                    if (!answer.Accepted || answer.Player != _company ||
                        answer.Command != "sim.set-worker-held" && answer.Command != "sim.post-order")
                        throw new InvalidOperationException("controller lost its ordinary command answer");
                    _ordered = answer.Command == "sim.post-order"; _pending = 0;
                }
                if (_ordered) return;
                if (!_target.IsValid)
                {
                    var entities = new List<EntityView>(); context.View.CopyEntities(context.Id, entities);
                    EntityId worker = EntityId.None;
                    foreach (var entity in entities)
                    {
                        if (entity.Def == _definition._targetDef.Id) _target = entity.Id;
                        if (entity.Def == _definition._workerDef.Id) worker = entity.Id;
                    }
                    if (!_target.IsValid || !worker.IsValid) throw new InvalidOperationException("controller needs its actual owned target and worker");
                    _original = _target; _company = context.Id;
                    var hold = new SetWorkerHeldCommand { Worker = worker, Held = true };
                    Code refusal = context.Preview(hold);
                    if (!refusal.IsNone) throw new InvalidOperationException("ordinary controller hold refused: " + refusal);
                    _pending = context.Submit(hold); return;
                }
                var command = new PostOrderCommand { Target = _target, Plan = _definition._activity };
                Code blocker = context.Preview(command);
                if (!blocker.IsNone) { context.ReportBlocker(blocker); return; }
                _pending = context.Submit(command);
            }
            public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
            {
                if (_definition._mode == "foreign-trap") throw new InvalidOperationException("another company received a private replacement");
                if (_target != source.Id) return Code.None;
                if (_company != source.Owner) return Codes.NotOwned;
                if (destination.Id != _definition._replacementDef.Id) return Codes.UnknownDef;
                if (_definition._mode == "deny") return new Code("proof.controller-replacement-denied");
                if (_definition._mode == "mutate-check") _commits++;
                return Code.None;
            }
            public void ReplaceEntityReferences(World world, Entity source, Entity destination)
            {
                if (_definition._mode == "foreign-trap") throw new InvalidOperationException("another company received a private replacement");
                if (_target != source.Id) return;
                if (_company != destination.Owner || !ReferenceEquals(world.Entities.Get(destination.Id), destination))
                    throw new InvalidOperationException("controller replacement lost actual owner or participant");
                _target = destination.Id; _commits++;
                if (_definition._mode == "throw-commit") throw new InvalidOperationException("controller commit failed after rebind");
            }
            public void ValidateState(PlayerView view, IReadOnlyList<int> awaiting, IReadOnlyList<CommandResult> answers)
            {
                if (_commits < 0 || _commits > 1 || _pending < 0 ||
                    _target.IsValid != _original.IsValid || _target.IsValid != _company.IsValid ||
                    !_target.IsValid && (_pending != 0 || _commits != 0 || _ordered) ||
                    _target.IsValid && (_company != view.Viewer || !view.TryGetEntity(_target, out var target) || target.Owner != _company ||
                        target.Def != (_commits == 0 ? _definition._targetDef.Id : _definition._replacementDef.Id)))
                    throw new CorruptStateException("invalid controller live reference or retained origin");
                if (_pending == 0) return;
                foreach (int ticket in awaiting) if (ticket == _pending) return;
                foreach (var answer in answers) if (answer.Sequence == _pending && answer.Player == _company) return;
                throw new CorruptStateException("controller reference orphaned its ordinary reply");
            }
            public void Sync(IStateSync sync)
            {
                sync.Entity(ref _target); sync.Entity(ref _original); sync.Player(ref _company);
                sync.Int(ref _pending); sync.Int(ref _commits); sync.Bool(ref _ordered);
            }
        }
    }
}
