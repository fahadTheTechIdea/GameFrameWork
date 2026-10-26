using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored completion binding, not an incident state or completion authority.</summary>
    public interface IIncidentCompletionEffect : IWorkEffect
    {
        Code Source { get; }
        bool BoundResponse { get; }
    }

    /// <summary>Sole JSON completion metadata parser for native and independent effects.</summary>
    public sealed class IncidentCompletionDeclaration
    {
        public Code Source { get; }
        public bool BoundResponse { get; }
        private IncidentCompletionDeclaration(JsonReader reader)
        {
            string source = reader.String("source", null);
            BoundResponse = reader.Bool("bound_response", false);
            if (BoundResponse == !string.IsNullOrWhiteSpace(source))
                throw reader.Error("response effect needs exactly one explicit source or bound_response");
            Source = string.IsNullOrWhiteSpace(source) ? Code.None : new Code(source);
            if (!Source.IsNone)
                try { ContractIdentity.ValidateId(Source.Id, "incident completion source"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static IncidentCompletionDeclaration Read(JsonReader reader)
            => new IncidentCompletionDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        internal static void Validate(IIncidentCompletionEffect effect)
        {
            if (effect.BoundResponse == !effect.Source.IsNone)
                throw new ContentException(effect.Id + ": exactly one explicit source or bound_response required");
            if (!effect.Source.IsNone)
                try { ContractIdentity.ValidateId(effect.Source.Id, "incident completion source"); }
                catch (ArgumentException error) { throw new ContentException(effect.Id + ": " + error.Message); }
        }
    }

    /// <summary>Sole workflow JSON parser/resolver. Phases label existing DAG nodes, never copy progress.</summary>
    public sealed class IncidentWorkflowDeclaration
    {
        private readonly string _response;
        private IAuthoredActivityDefinition _activity;
        public Code Kind { get; }
        public IActivityDefinition Response => _activity;
        public int Priority => _activity.Priority;
        public IReadOnlyList<IIncidentPhaseDefinition> Phases { get; }
        private IncidentWorkflowDeclaration(JsonReader reader)
        {
            string kind = reader.String("kind");
            try { ContractIdentity.ValidateId(kind, "incident kind"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Kind = new Code(kind); _response = reader.String("response");
            JsonValue phases = reader.Array("phases") ?? throw reader.Error("incident workflow requires phases");
            var captured = new List<IIncidentPhaseDefinition>();
            for (int i = 0; i < phases.Count; i++)
            {
                var phase = new JsonReader(phases[i], reader.Where + ".phases[" + i + "]");
                string id = phase.String("id"); string[] nodes = phase.Strings("nodes"); phase.End();
                try { captured.Add(new IncidentPhaseDefinition(id, nodes)); }
                catch (ArgumentException error) { throw phase.Error(error.Message); }
            }
            Phases = captured.AsReadOnly();
        }
        public static IncidentWorkflowDeclaration Read(JsonReader reader)
            => new IncidentWorkflowDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content, IIncidentWorkflowDefinition workflow)
        {
            if (_activity != null) throw new InvalidOperationException("incident workflow resolves once");
            _activity = content.Def<IAuthoredActivityDefinition>(_response);
            if (workflow.Kind != Kind || !ReferenceEquals(workflow.Response, _activity) ||
                !ReferenceEquals(workflow.Phases, Phases) || workflow.Priority != Priority)
                throw content.Fail("workflow metadata does not belong to its declaration");
            // Graph/effect admission belongs to composition after all file sections resolve.
        }
    }
}
