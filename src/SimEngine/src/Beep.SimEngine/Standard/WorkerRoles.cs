using System;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    /// <summary>Section worker_roles declares the roles that workers and work requirements may reference.</summary>
    public interface IWorkerRoleDefinition : IContentDefinition
    {
        WorkerRoleId Role { get; }
    }
    public static class WorkerRoleContracts
    {
        public static WorkerRoleId Read(string id, JsonReader reader)
        {
            try { return new WorkerRoleId(id); }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
        }
        internal static void ValidateComposition(Kernel.World world)
        {
            foreach (var definition in world.Content.All<IWorkerRoleDefinition>())
                Kernel.EntityObservation.ReadProvider(world, definition, null, () => {
                    if (!definition.Role.IsValid || definition.Role.Value != definition.Id ||
                        !world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<IWorkerRoleDefinition>(definition.Id), definition))
                        throw new ContentException("worker role must alias actual owned content with the same canonical typed identity");
                    return true;
                });
        }
    }
    public sealed class WorkerRoleDef : Def, IWorkerRoleDefinition
    {
        public WorkerRoleId Role { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Role = WorkerRoleContracts.Read(Id, reader);
        }
    }
}
