using System;

namespace Beep.SimEngine.Content
{
    /// <summary>One permission owner for definition authoring; never session state or a content registry.</summary>
    internal sealed class ContentAuthoring
    {
        private enum Phase : byte { Created, Reading, Read, Resolving, Resolved, Validating, Validated, Frozen, Failed }
        private Phase _phase;
        internal bool IsFrozen => _phase == Phase.Frozen;
        internal void RequireMutable()
        {
            if (_phase == Phase.Frozen || _phase == Phase.Failed)
                throw new ContentException("definition metadata is no longer mutable");
        }
        internal void Read(Action read)
        {
            if (_phase != Phase.Created) Reject("definition authoring may run only once, before resolution");
            Run(Phase.Reading, Phase.Read, read);
        }
        internal void Resolve(Action resolve)
        {
            if (_phase != Phase.Created && _phase != Phase.Read)
                Reject("definition resolution may run only once");
            Run(Phase.Resolving, Phase.Resolved, resolve);
        }
        internal void Freeze()
        {
            if (_phase != Phase.Validated) Reject("only successfully resolved and validated metadata may be published");
            _phase = Phase.Frozen;
        }
        internal void Validate(Action validate)
        {
            if (_phase != Phase.Resolved) Reject("linked metadata validation may run only once after resolution");
            Run(Phase.Validating, Phase.Validated, validate);
        }
        private void Run(Phase active, Phase completed, Action action)
        {
            _phase = active;
            try
            {
                action();
                // A provider cannot swallow a rejected recursive call and publish partial metadata.
                if (_phase != active) throw new ContentException("definition authoring was interrupted by an invalid lifecycle call");
                _phase = completed;
            }
            catch { _phase = Phase.Failed; throw; }
        }
        private void Reject(string message)
        {
            if (_phase == Phase.Reading || _phase == Phase.Resolving || _phase == Phase.Validating)
                _phase = Phase.Failed;
            throw new ContentException(message);
        }
    }
}
